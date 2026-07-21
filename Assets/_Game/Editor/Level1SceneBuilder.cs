using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Builds Level 1 — "Rusty's Roadside Garage" — as a compact, mobile-framed
    /// SERVICE SPINE using the authored 1k "real" building &amp; character GLBs
    /// (Assets/_Game/Art/Models/Buildings + /Characters). Contrast
    /// <see cref="CityGarageSceneBuilder"/>, which places all 21 graybox stations
    /// at once on a grid.
    ///
    /// Layout follows Doc 02 §5.2:
    ///   • Cars flow one direction (west→east) along the near customer lane (z=0):
    ///       ENTRANCE → Reception → Queue → Bay 1 → Bay 2 → Wash → Pay → EXIT.
    ///   • Parts logistics sit on the FAR lane (z=+9.5), directly behind the
    ///     consumers, so the player's carry trips are short north↔south hops.
    ///   • Humans on the near side, parts on the far side.
    ///
    /// Each station gets its authored building plus its matching staff character
    /// standing in front of it (e.g. wheel_tire_station_1k_real + wheel_tire_
    /// character_1k_real). Progressive reveal (Doc 03 §1.3): only the entrance,
    /// Reception, Queue, Bay 1, Parts Delivery and pay/exit ship built; the rest
    /// appear as fundable construction zones. Every remaining authored building is
    /// showcased on the "future locations" preview row beyond the back fence, so
    /// the world itself is the roadmap (Pillar 3/4).
    /// </summary>
    public static class Level1SceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Level1.unity";
        private const string K = "Assets/_Game/Art/Models/Kenney";
        private const string BuildDir = "Assets/_Game/Art/Models/Buildings";
        private const string CharDir = "Assets/_Game/Art/Models/Characters";
        private const string MatDir = "Assets/_Game/Art/Materials/Kenney";
        private const string GenMatDir = "Assets/_Game/Art/GrayboxMats";

        // ---- spine metrics (compact: active area ~40m wide, fits one diorama screen) ----
        private const float PadTop = 0.12f;
        private const float CarLaneZ = 0f;     // customer vehicles flow here, west→east
        private const float BayZ = 5.0f;       // service buildings, fronts face -Z (camera)
        private const float LogiZ = 9.5f;      // parts logistics lane (player/staff only)
        private const float PayZ = -5.5f;      // pay stall + parking apron
        private const float PadSize = 6.6f;
        private const float BuildingWidth = 6.6f;
        private const float CharHeight = 1.55f;

        // sign palette
        private static readonly Color Navy = new Color(0.13f, 0.16f, 0.27f);
        private static readonly Color Green = new Color(0.15f, 0.40f, 0.22f);
        private static readonly Color Purple = new Color(0.34f, 0.18f, 0.48f);
        private static readonly Color Blue = new Color(0.12f, 0.25f, 0.45f);
        private static readonly Color Amber = new Color(0.55f, 0.36f, 0.10f);

        private static Material _roadsMat, _suburbanMat, _carsMat, _platformerMat;
        private static System.Random _rng;

        [MenuItem("Overhaul/Build Level 1 (Service Spine)")]
        public static void Build()
        {
            _rng = new System.Random(7);
            LoadMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            var cam = BuildCamera();
            BuildGround();
            BuildSpineRoads();
            BuildPerimeter();
            BuildPreviewRow();

            // ---------------- the built starting stations (authored GLBs) ----------------
            // Bay 1 is the profit engine: dead centre of screen and thumb.
            var bay1Center = new Vector3(-2f, 0f, BayZ);
            BuildRealStation("BASIC CHANGE BAY", "BasicChangeBay_1k_Real.glb", "change_bay_character_1k_real.glb", bay1Center, Navy, charFront: true);

            // Reception + Queue at the west (left) end where cars enter.
            BuildRealStation("RECEPTION", "reception_1k_real.glb", null, new Vector3(-15f, 0f, BayZ), Navy, charFront: true);
            BuildRealStation("CUSTOMER QUEUE", "customer_queue_1k_real.glb", "customer_queue_character_1k_real.glb", new Vector3(-9f, 0f, BayZ), Navy, charFront: true);

            // Parts Delivery on the far logistics lane, behind/left of Bay 1.
            var partsCenter = new Vector3(-9f, 0f, LogiZ);
            BuildRealStation("PARTS DELIVERY", "PartsDelivery_1k_Real.glb", "delivery_part_character_1k_real.glb", partsCenter, Amber, charFront: true);

            // Pay stall / completed-car delivery at the east (right) end.
            var payCenter = new Vector3(16f, 0f, CarLaneZ + 0.5f);
            BuildLotPad("DELIVERY & PAY", payCenter, Green);

            // ---------------- gameplay wiring (same tested loop) ----------------
            var itemTemplate = BuildCarriedItemTemplate();
            BuildPlayer(bay1Center + new Vector3(3.5f, 0f, -3.5f));
            new GameObject("ResourceCatalog").AddComponent<ResourceCatalog>().AddEntry("tire", 1);

            // Parts pallet = source; collect zone on its front apron.
            var palletPos = partsCenter + new Vector3(2.6f, PadTop, -3.4f);
            var palletGo = PlaceModel($"{K}/Cars/box.fbx", _carsMat, palletPos, 0f, "PartsPallet");
            PlaceModel($"{K}/Platformer/crate.fbx", _platformerMat, palletPos + new Vector3(0.9f, 0f, 0.3f), 15f, "PartsPallet_Crate");
            var source = palletGo.AddComponent<PartsSource>();
            source.Configure("tire", 12, 2f);
            var collectZone = MakeZone("CollectZone", palletPos, 2.0f);
            collectZone.Configure(InteractionKind.Collect, "tire", source, null, itemTemplate);

            // Bay 1: ServiceBay + deposit rack on its FAR (logistics) side so the
            // hauler feeds it from behind and customers approach from the front.
            var bayGo = new GameObject("RepairBay");
            bayGo.transform.position = bay1Center;
            var rack = bayGo.AddComponent<ResourceRack>();
            var eco = bayGo.AddComponent<EconomyManager>();
            var bay = bayGo.AddComponent<ServiceBay>();
            var village = bayGo.AddComponent<VillageController>();

            var depositZone = MakeZone("DepositZone", bay1Center + new Vector3(0f, PadTop, 2.4f), 1.8f);
            depositZone.transform.SetParent(bayGo.transform, true);
            depositZone.Configure(InteractionKind.Deposit, "tire", null, rack, null);

            // Customer route: entrance (west) → queue slots → bay slot → exit (east),
            // all on the near car lane, one-way, collision-free by construction.
            var entrance = MakeMarker("Entrance", new Vector3(-26f, 0.06f, CarLaneZ), 90f);
            var baySlot = MakeMarker("BaySlot", bay1Center + new Vector3(0f, PadTop, -3.4f), 0f);
            var exit = MakeMarker("Exit", new Vector3(26f, 0.06f, CarLaneZ), 90f);

            var queueRoot = new GameObject("QueueSlots");
            var queueSlots = new Transform[4];
            for (int i = 0; i < queueSlots.Length; i++)
            {
                var m = MakeMarker($"QueueSlot_{i}", new Vector3(bay1Center.x - 5f - i * 4.2f, 0.06f, CarLaneZ), 90f);
                m.SetParent(queueRoot.transform, true);
                queueSlots[i] = m;
            }

            var carPrefabs = LoadCars("sedan", "suv", "hatchback-sports", "sedan-sports", "taxi");
            village.Configure(bay, rack, eco, entrance, queueSlots, baySlot, exit, carPrefabs, _carsMat);

            // ---------------- progressive construction zones (Doc 03 §1.3 order) ----------------
            // Each appears next to the bottleneck it fixes; partial funding persists.
            var zoneQueue = BuildConstructionZone("zone_queue_slot_4", 80, eco,
                new Vector3(bay1Center.x - 5f - 3 * 4.2f, 0.06f, CarLaneZ), null);

            var zoneTire = BuildConstructionZone("zone_tire_storage", 30, eco,
                bay1Center + new Vector3(0f, PadTop, 4.4f),
                builtVisual: BuildTirePallet(bay1Center + new Vector3(0f, PadTop, 4.4f), itemTemplate));

            // Second service line: the authored Wheel & Tire Station + its technician.
            var zoneWheelTire = BuildConstructionZone("zone_wheel_tire_bay", 120, eco,
                new Vector3(5f, PadTop, BayZ),
                builtVisual: BuildRealBuilt("WHEEL & TIRE STATION", "wheel_tire_station_1k_real.glb", "wheel_tire_character_1k_real.glb", new Vector3(5f, 0f, BayZ), Navy));

            var zoneTransporter = BuildConstructionZone("zone_transporter_pad", 250, eco,
                new Vector3(-3f, PadTop, LogiZ), builtVisual: BuildHirePad(new Vector3(-3f, 0f, LogiZ)));

            var zoneEmployeeRoom = BuildConstructionZone("zone_employee_room", 150, eco,
                new Vector3(-16f, PadTop, LogiZ),
                builtVisual: BuildRealBuilt("EMPLOYEE ROOM", "employee_room_1k_real.glb", "employee_room_character_1k_real.glb", new Vector3(-16f, 0f, LogiZ), Amber));

            var zoneWash = BuildConstructionZone("zone_car_wash", 600, eco,
                new Vector3(11f, PadTop, BayZ),
                builtVisual: BuildRealBuilt("CAR WASH", "car_wash_1k_real.glb", "car_wash_character_1k_real.glb", new Vector3(11f, 0f, BayZ), Blue));

            // The level's finish line is itself a construction zone (Doc 03 §1.4).
            var zoneContract = BuildConstructionZone("zone_city_contract", 2500, eco,
                new Vector3(22f, 0.06f, CarLaneZ),
                builtVisual: BuildContractGate(new Vector3(22f, 0f, CarLaneZ)));

            var unlocks = bayGo.AddComponent<VillageUnlocks>();
            unlocks.Configure(village, zoneQueue, new[]
            {
                zoneQueue, zoneTire, zoneWheelTire, zoneTransporter,
                zoneEmployeeRoom, zoneWash, zoneContract
            });

            // Save + capped offline earnings.
            var saveGo = new GameObject("SaveManager");
            var save = saveGo.AddComponent<SaveManager>();
            SetPrivate(save, "economy", eco);

            // A little comedy: a couple of impatient customers milling by reception & pay.
            ScatterPeople();
            FrameCamera(cam);

            AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Overhaul] Level 1 (Service Spine) built at {ScenePath}");
        }

        // ---------------------------------------------------------------- environment

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.64f);
        }

        private static Camera BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.backgroundColor = new Color(0.45f, 0.63f, 0.78f);
            return cam;
        }

        private static void FrameCamera(Camera cam)
        {
            // Steep diorama angle framing the compact spine; centred on Bay 1.
            var lookTarget = new Vector3(0f, 0f, 3.5f);
            var rot = Quaternion.Euler(56f, 0f, 0f);
            cam.transform.SetPositionAndRotation(lookTarget - (rot * Vector3.forward) * 46f, rot);
            cam.fieldOfView = 37f;
        }

        private static void BuildGround()
        {
            var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "Grass";
            grass.transform.localScale = new Vector3(18f, 1f, 18f);
            grass.transform.position = new Vector3(0f, 0f, 8f);
            Paint(grass, new Color(0.35f, 0.46f, 0.28f));

            // A single dirt-lot apron under the whole active spine.
            var lot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lot.name = "GarageLot";
            lot.transform.position = new Vector3(0f, 0.02f, 3f);
            lot.transform.localScale = new Vector3(58f, 0.06f, 24f);
            Paint(lot, new Color(0.30f, 0.27f, 0.23f)); // packed dirt
        }

        private static void BuildSpineRoads()
        {
            // One straight customer street the full width, on the near lane.
            var street = GameObject.CreatePrimitive(PrimitiveType.Cube);
            street.name = "CustomerStreet";
            street.transform.position = new Vector3(0f, 0.05f, CarLaneZ);
            street.transform.localScale = new Vector3(58f, 0.08f, 4.2f);
            Paint(street, new Color(0.17f, 0.18f, 0.19f));

            // Dashed centre line so the one-way flow reads at a glance.
            for (float x = -26f; x <= 26f; x += 3f)
            {
                var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "LaneDash";
                dash.transform.position = new Vector3(x, 0.10f, CarLaneZ);
                dash.transform.localScale = new Vector3(1.2f, 0.02f, 0.16f);
                Paint(dash, new Color(0.85f, 0.82f, 0.55f), "LaneDash");
            }

            // Parking apron in front of the pay stall (near side).
            for (int i = 0; i < 6; i++)
            {
                float x = 9f + i * 2.6f;
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "ParkLine";
                line.transform.position = new Vector3(x - 1.3f, 0.09f, PayZ);
                line.transform.localScale = new Vector3(0.14f, 0.02f, 4.4f);
                Paint(line, new Color(0.85f, 0.85f, 0.85f), "ParkLine");
            }
        }

        private static void BuildPerimeter()
        {
            // Back fence separates the active garage from the future-location plots.
            for (float x = -28f; x <= 28f; x += 2.5f)
                FencePiece(new Vector3(x, 0.05f, LogiZ + 3.6f), 0f);
            for (float z = -8f; z <= LogiZ + 3.6f; z += 2.5f)
            {
                if (Mathf.Abs(z - CarLaneZ) < 3f) continue; // gaps where the street exits
                FencePiece(new Vector3(-29f, 0.05f, z), 90f);
                FencePiece(new Vector3(29f, 0.05f, z), 90f);
            }

            // A few trees to the far sides, clear of the preview row.
            string[] trees = { "Suburban/tree-large", "Suburban/tree-small", "Nature/tree_default" };
            float[] xs = { -34f, -30f, 30f, 34f };
            for (int i = 0; i < xs.Length; i++)
            {
                var pos = new Vector3(xs[i], 0f, LogiZ + 8f);
                var tree = PlaceModel($"{K}/{trees[i % trees.Length]}.fbx", TreeMat(trees[i % trees.Length]), pos, Jitter(180f), $"Tree_{i}");
                if (tree != null) ScaleInPlace(tree, 3.0f);
            }
        }

        /// <summary>
        /// Locations 2–10 previewed beyond the back fence, using the remaining
        /// authored buildings + their staff so every asset is showcased.
        /// </summary>
        private static void BuildPreviewRow()
        {
            // (displayName, buildingFile, characterFile, x, z)
            var previews = new (string name, string b, string c, float x, float z)[]
            {
                ("OFFICE & FINANCE",     "office_finance_1k_real.glb",       "office_finance_character_1k_real.glb",     -20f, 19f),
                ("ENGINE WORKSHOP",      "engine_workshop_1k_real.glb",      "engine_workshop_character_1k_real.glb",    -10f, 19f),
                ("BODY REPAIR",          "body_repair_1k_real.glb",          "body_repair_character_1k_real.glb",          0f, 19f),
                ("USED CAR SHOWROOM",    "used_car_showroom_1k_real.glb",    "used_car_showroom_character_1k_real.glb",   10f, 19f),
                ("PREMIUM SHOWROOM",     "premium_car_showroom_1k_real.glb", "premium_car_showroom_character_1k_real.glb", 20f, 19f),

                ("TUNING STATION",       "tuning_station_1k_real.glb",       "tuning_station_character_1k_real.glb",     -15f, 27f),
                ("DETAILING STATION",    "detailing_station_1k.glb",         "detailing_station_character_1k_real.glb",   -5f, 27f),
                ("DIAGNOSTIC STATION",   "diagnostic_station_1k_real.glb",   "diagnostic_station_character_1k_real.glb",   5f, 27f),
                ("VEHICLE INSPECTION",   "vehicle_inspection_1k_real.glb",   "vehicle_inspection_character_1k_real.glb",  15f, 27f),
            };

            foreach (var p in previews)
            {
                var center = new Vector3(p.x, 0f, p.z);
                BuildRealStation(p.name, p.b, p.c, center, new Color(0.20f, 0.22f, 0.26f), charFront: true, previewPad: true);
            }
        }

        private static Material TreeMat(string path) => path.StartsWith("Suburban") ? _suburbanMat : null;

        private static void FencePiece(Vector3 pos, float rotY)
        {
            var f = PlaceModel($"{K}/Suburban/fence.fbx", _suburbanMat, pos, rotY, "Fence");
            if (f != null) ScaleInPlace(f, 2.5f);
        }

        // ---------------------------------------------------------------- authored stations

        /// <summary>
        /// Places an authored building GLB on a tinted pad, its matching staff
        /// character in front (facing the camera), and a readable banner sign.
        /// </summary>
        private static GameObject BuildRealStation(string name, string buildingFile, string characterFile,
            Vector3 center, Color sign, bool charFront, bool previewPad = false)
        {
            var root = new GameObject($"Station_{name.Replace(' ', '_').Replace('&', 'n')}");
            root.transform.position = center;

            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad";
            pad.transform.SetParent(root.transform);
            pad.transform.position = new Vector3(center.x, 0.08f, center.z);
            pad.transform.localScale = new Vector3(PadSize, 0.08f, PadSize);
            Paint(pad, previewPad ? new Color(0.22f, 0.24f, 0.27f) : new Color(0.26f, 0.27f, 0.29f),
                previewPad ? "PreviewPad" : "StationPad");

            PlaceRealBuilding(buildingFile, center, BuildingWidth, root.transform);

            if (!string.IsNullOrEmpty(characterFile))
            {
                var charPos = center + new Vector3(charFront ? 2.4f : -2.4f, PadTop, -PadSize / 2f - 0.2f);
                PlaceRealCharacter(characterFile, charPos, 180f, root.transform);
            }

            MakeSign(name, sign, new Vector3(center.x, 0f, center.z - PadSize / 2f + 0.4f), root.transform);
            return root;
        }

        private static void BuildLotPad(string name, Vector3 center, Color sign)
        {
            var root = new GameObject($"Station_{name.Replace(' ', '_').Replace('&', 'n')}");
            root.transform.position = center;
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad";
            pad.transform.SetParent(root.transform);
            pad.transform.position = new Vector3(center.x, 0.08f, center.z);
            pad.transform.localScale = new Vector3(PadSize, 0.08f, PadSize);
            Paint(pad, new Color(0.24f, 0.26f, 0.28f), "LotPad");
            MakeSign(name, sign, new Vector3(center.x, 0f, center.z - PadSize / 2f + 0.4f), root.transform);
        }

        /// <summary>Built-visual for a construction zone: an authored building + staff that pops in on completion.</summary>
        private static GameObject BuildRealBuilt(string name, string buildingFile, string characterFile, Vector3 center, Color sign)
        {
            return BuildRealStation(name, buildingFile, characterFile, center, sign, charFront: true);
        }

        private static GameObject BuildHirePad(Vector3 center)
        {
            var root = new GameObject("Built_HirePad");
            root.transform.position = center;
            var pad = PlaceModel($"{K}/Platformer/button-round.fbx", _platformerMat, center + new Vector3(0f, PadTop, 0f), 0f, "HirePad");
            if (pad != null) pad.transform.SetParent(root.transform, true);
            PlaceRealCharacter("delivery_part_character_1k_real.glb", center + new Vector3(0f, PadTop, 0.6f), 180f, root.transform);
            MakeSign("TRANSPORTER", Purple, new Vector3(center.x, 0f, center.z - PadSize / 2f + 0.4f), root.transform);
            return root;
        }

        private static GameObject BuildContractGate(Vector3 center)
        {
            var root = new GameObject("Built_CityContract");
            root.transform.position = center;
            var flag = PlaceModel($"{K}/Racing/flagGreen.fbx", null, center + new Vector3(0f, PadTop, 1.2f), 0f, "ContractFlag");
            if (flag != null) { ScaleInPlace(flag, 2.5f); flag.transform.SetParent(root.transform, true); }
            MakeSign("CITY CONTRACT", Green, new Vector3(center.x, 0f, center.z - 1.6f), root.transform, 8f);
            return root;
        }

        private static void ScatterPeople()
        {
            // Generic customers (kenney blocky) waiting in the world — the staff are the authored GLBs.
            string[] chars = { "character-c", "character-d", "character-m" };
            var spots = new Vector3[]
            {
                new Vector3(-15f, PadTop, BayZ - 4.4f), // outside reception (the impatient one)
                new Vector3(15f, 0.06f, PayZ + 1.5f),   // near the pay apron
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var person = InstantiateNormalized($"{K}/Characters/{chars[i % chars.Length]}.fbx", null, 1.6f, $"Customer_{i}");
                if (person == null) continue;
                person.transform.rotation = Quaternion.Euler(0f, Jitter(180f), 0f);
                GroundAt(person, spots[i]);
            }
        }

        // ---------------------------------------------------------------- authored-asset loaders

        private static GameObject PlaceRealBuilding(string fileName, Vector3 center, float targetWidth, Transform parent)
        {
            var path = $"{BuildDir}/{fileName}";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning($"[Overhaul] building missing: {path}"); return null; }
            var go = (GameObject)Object.Instantiate(asset); // keep the GLB's own materials
            go.name = "Building";
            go.transform.rotation = Quaternion.identity; // authored front faces -Z (camera)
            go.transform.localScale = Vector3.one;
            ScaleToWidth(go, targetWidth, new Vector3(center.x, PadTop, center.z));
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        private static GameObject PlaceRealCharacter(string fileName, Vector3 groundPos, float yaw, Transform parent)
        {
            var path = $"{CharDir}/{fileName}";
            var go = InstantiateNormalized(path, null, CharHeight, "Staff"); // null mat = keep own materials
            if (go == null) return null;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            GroundAt(go, groundPos);
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            if (parent != null) go.transform.SetParent(parent, true);
            return go;
        }

        // ---------------------------------------------------------------- player & template

        private static GameObject BuildCarriedItemTemplate()
        {
            var itemTemplate = new GameObject("CarriedItemTemplate");
            var wheelAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{K}/Cars/wheel-default.fbx");
            if (wheelAsset != null)
            {
                var wheel = (GameObject)Object.Instantiate(wheelAsset, itemTemplate.transform);
                wheel.name = "RealTireVisual";
                wheel.transform.localPosition = Vector3.zero;
                wheel.transform.localRotation = Quaternion.identity;
                foreach (var renderer in wheel.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = _carsMat;
            }
            itemTemplate.transform.position = new Vector3(0f, -5f, 0f);
            itemTemplate.SetActive(false);
            return itemTemplate;
        }

        private static GameObject BuildPlayer(Vector3 spawn)
        {
            var root = new GameObject("Player");
            root.transform.position = spawn;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.7f; cc.center = new Vector3(0f, 0.85f, 0f); cc.radius = 0.35f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            root.AddComponent<PlayerController>();
            root.AddComponent<KeyboardDriver>();
            root.AddComponent<PlayerViewController>();
            root.AddComponent<PlayerSprintHud>();
            var carrier = root.AddComponent<CarrierView>();

            var anchor = new GameObject("StackAnchor").transform;
            anchor.SetParent(root.transform);
            anchor.localPosition = new Vector3(0f, 1.5f, 0f);
            SetPrivate(carrier, "stackAnchor", anchor);

            var visual = InstantiateNormalized($"{K}/Characters/character-a.fbx", null, 1.7f, "Visual");
            if (visual != null)
            {
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            return root;
        }

        // ---------------------------------------------------------------- signs

        private static void MakeSign(string text, Color bannerColor, Vector3 frontCenter, Transform parent, float width = 6.6f)
        {
            var root = new GameObject($"Sign_{text.Replace(' ', '_')}");
            if (parent != null) root.transform.SetParent(parent);
            root.transform.position = frontCenter;

            for (int side = -1; side <= 1; side += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Post";
                post.transform.SetParent(root.transform);
                post.transform.position = frontCenter + new Vector3(side * (width / 2f - 0.3f), 1.55f, 0f);
                post.transform.localScale = new Vector3(0.16f, 3.1f, 0.16f);
                Paint(post, new Color(0.35f, 0.35f, 0.37f), "SignPost");
            }

            var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Banner";
            banner.transform.SetParent(root.transform);
            banner.transform.position = frontCenter + new Vector3(0f, 3.35f, 0f);
            banner.transform.localScale = new Vector3(width, 1.05f, 0.22f);
            Paint(banner, bannerColor, $"SignBanner_{ColorUtility.ToHtmlStringRGB(bannerColor)}");

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(root.transform);
            textGo.transform.position = frontCenter + new Vector3(0f, 3.35f, -0.14f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = 0.075f;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Color.white;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                var mat = CityGarageFixups.EnsureSignMaterial(font);
                textGo.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : font.material;
            }
        }

        // ---------------------------------------------------------------- construction

        private static InteractionZone MakeZone(string name, Vector3 pos, float radius)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            return go.AddComponent<InteractionZone>();
        }

        private static ConstructionZoneView BuildConstructionZone(
            string id, int cost, EconomyManager eco, Vector3 pos, GameObject builtVisual)
        {
            var root = new GameObject($"Construction_{id}");
            root.transform.position = pos;

            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.4f;

            var blueprint = new GameObject("Blueprint");
            blueprint.transform.SetParent(root.transform, false);
            var b1 = PlaceModel($"{K}/Roads/construction-barrier.fbx", _roadsMat, pos + new Vector3(-1.6f, 0f, 0f), 0f, "Barrier_L");
            var b2 = PlaceModel($"{K}/Roads/construction-barrier.fbx", _roadsMat, pos + new Vector3(1.6f, 0f, 0f), 0f, "Barrier_R");
            var lite = PlaceModel($"{K}/Roads/construction-light.fbx", _roadsMat, pos + new Vector3(0f, 0f, 1.4f), 0f, "Light");
            var pad = PlaceModel($"{K}/Platformer/button-round.fbx", _platformerMat, pos, 0f, "FundPad");
            foreach (var g in new[] { b1, b2, lite, pad })
                if (g != null) g.transform.SetParent(blueprint.transform, true);

            if (builtVisual != null)
            {
                builtVisual.transform.SetParent(root.transform, true);
                builtVisual.SetActive(false);
            }

            var view = root.AddComponent<ConstructionZoneView>();
            view.Configure(id, cost, eco);
            SetPrivate(view, "blueprintVisual", blueprint);
            if (builtVisual != null) SetPrivate(view, "builtVisual", builtVisual);
            return view;
        }

        private static GameObject BuildTirePallet(Vector3 pos, GameObject itemTemplate)
        {
            var root = new GameObject("TirePallet");
            root.transform.position = pos;

            var box = PlaceModel($"{K}/Cars/box.fbx", _carsMat, pos, 0f, "TirePallet_Box");
            if (box != null) box.transform.SetParent(root.transform, true);
            for (int i = 0; i < 3; i++)
            {
                var w = PlaceModel($"{K}/Cars/wheel-default.fbx", _carsMat,
                    pos + new Vector3(1.1f, 0.26f * i, 0.2f), 0f, $"TireStack_{i}");
                if (w != null) w.transform.SetParent(root.transform, true);
            }

            var src = root.AddComponent<PartsSource>();
            src.Configure("tire", 12, 2f);

            var zone = MakeZone("TirePallet_CollectZone", pos, 2.0f);
            zone.transform.SetParent(root.transform, true);
            zone.Configure(InteractionKind.Collect, "tire", src, null, itemTemplate);
            return root;
        }

        private static Transform MakeMarker(string name, Vector3 pos, float rotY)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            return go.transform;
        }

        // ---------------------------------------------------------------- generic helpers

        private static float Jitter(float range) => (float)(_rng.NextDouble() * 2.0 - 1.0) * range;

        private static GameObject[] LoadCars(params string[] names)
        {
            var list = new List<GameObject>();
            foreach (var n in names)
            {
                var a = AssetDatabase.LoadAssetAtPath<GameObject>($"{K}/Cars/{n}.fbx");
                if (a != null) list.Add(a);
            }
            return list.ToArray();
        }

        private static GameObject PlaceModel(string path, Material mat, Vector3 targetPos, float rotY, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning($"[Overhaul] model missing: {path}"); return null; }
            var go = (GameObject)Object.Instantiate(asset);
            go.name = name;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, rotY, 0f));
            if (mat != null) ApplyMat(go, mat);
            GroundAt(go, targetPos);
            return go;
        }

        private static void GroundAt(GameObject go, Vector3 targetPos)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { go.transform.position = targetPos; return; }
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            go.transform.position += new Vector3(targetPos.x - b.center.x, targetPos.y - b.min.y, targetPos.z - b.center.z);
        }

        private static void ScaleToWidth(GameObject go, float targetWidth, Vector3 groundPos)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.x < 0.001f) return;
            go.transform.localScale *= targetWidth / b.size.x;
            GroundAt(go, groundPos);
        }

        private static void ScaleInPlace(GameObject go, float scale)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { go.transform.localScale *= scale; return; }
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            Vector3 baseCenter = new Vector3(b.center.x, b.min.y, b.center.z);
            go.transform.localScale *= scale;
            GroundAt(go, baseCenter);
        }

        private static GameObject InstantiateNormalized(string path, Material mat, float targetHeight, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning($"[Overhaul] model missing: {path}"); return null; }
            var go = (GameObject)Object.Instantiate(asset);
            go.name = name;
            if (mat != null) ApplyMat(go, mat);
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return go;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y > 0.001f) go.transform.localScale *= targetHeight / b.size.y;
            return go;
        }

        private static void ApplyMat(GameObject go, Material mat)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                int n = Mathf.Max(1, r.sharedMaterials.Length);
                var arr = new Material[n];
                for (int i = 0; i < n; i++) arr[i] = mat;
                r.sharedMaterials = arr;
            }
        }

        private static void Paint(GameObject go, Color c, string matName = null)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null || r.sharedMaterial == null) return;
            matName ??= go.name;
            System.IO.Directory.CreateDirectory(GenMatDir);
            var path = $"{GenMatDir}/{matName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null && existing.color == c) { r.sharedMaterial = existing; return; }
            var mat = new Material(Shader.Find("Standard"));
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            mat.color = c;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);
            r.sharedMaterial = mat;
        }

        private static void SetPrivate(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void LoadMaterials()
        {
            _roadsMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Roads.mat");
            _suburbanMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Suburban.mat");
            _carsMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Cars.mat");
            _platformerMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Platformer.mat");
        }
    }
}

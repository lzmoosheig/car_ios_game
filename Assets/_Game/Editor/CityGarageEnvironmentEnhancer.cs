using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    public static class CityGarageEnvironmentEnhancer
    {
        private const string ScenePath = "Assets/_Game/Scenes/CityGarage.unity";
        private const string K = "Assets/_Game/Art/Models/Kenney";
        private const string MatDir = "Assets/_Game/Art/Materials/Kenney";
        private const string GenMatDir = "Assets/_Game/Art/GrayboxMats";
        private const string LayoutMarker = "__OrganicLayoutApplied";

        private static Material _carsMat;
        private static Material _roadsMat;
        private static Material _platformerMat;
        private static Material _suburbanMat;
        private static Material _charactersMat;
        private static readonly System.Random Rng = new System.Random(731);

        [MenuItem("Overhaul/Apply City Environment Plan")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
                scene = EditorSceneManager.GetActiveScene();
            }

            ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Overhaul] Applied the organized automotive-district environment pass.");
        }

        public static void ApplyToOpenScene()
        {
            LoadMaterials();
            ApplyOrganicLayoutOnce();
            ExpandGroundCoverage();

            var buildings = GetOrCreateRoot("City_Buildings");
            var roads = GetOrCreateRoot("City_Roads");
            var props = GetOrCreateRoot("City_Props");
            var vehicles = GetOrCreateRoot("City_Vehicles");
            var npcs = GetOrCreateRoot("City_NPCs");
            var vegetation = GetOrCreateRoot("City_Vegetation");
            var decoration = GetOrCreateRoot("City_Decoration");

            var generatedProps = ResetGeneratedRoot(props, "Generated_ContextProps");
            var generatedVehicles = ResetGeneratedRoot(vehicles, "Generated_Vehicles");
            var generatedNpcs = ResetGeneratedRoot(npcs, "Generated_NPCs");
            var generatedVegetation = ResetGeneratedRoot(vegetation, "Generated_Vegetation");
            var generatedDecoration = ResetGeneratedRoot(decoration, "Generated_Decoration");

            RepairRoadMarkings();
            BuildContextProps(generatedProps);
            BuildFrontParking(generatedVehicles, generatedDecoration);
            BuildPeople(generatedNpcs);
            BuildVegetation(generatedVegetation);
            BuildStreetFurniture(generatedDecoration);
            WireVehicleGameplay();
            OrganizeExistingRoots(buildings, roads, props, vehicles, npcs, vegetation, decoration);
        }

        private static void ApplyOrganicLayoutOnce()
        {
            if (GameObject.Find(LayoutMarker) != null) return;

            var marker = new GameObject(LayoutMarker);
            marker.hideFlags = HideFlags.HideInHierarchy;

            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!root.name.StartsWith("Station_")) continue;

                var delta = LayoutDelta(root.transform.position);
                root.transform.position += delta;

                if (root.name == "Station_PARTS_DELIVERY")
                {
                    MoveRoot("PartsPallet", delta);
                    MoveRoot("PartsPallet_Crate", delta);
                    MoveRoot("CollectZone", delta);
                }
                else if (root.name == "Station_BASIC_CHANGE_BAY")
                {
                    MoveRoot("RepairBay", delta);
                    MoveRoot("BaySlot", delta);
                }
            }
        }

        private static Vector3 LayoutDelta(Vector3 position)
        {
            float z = position.z > 20f ? 1.5f : position.z < 5f ? -1.5f : 0f;
            int column = Mathf.RoundToInt((position.x + 31.5f) / 9f);
            float[] offsets = { -0.35f, 0.25f, -0.2f, 0.3f, -0.25f, 0.2f, -0.3f, 0.35f };
            float x = offsets[Mathf.Clamp(column, 0, offsets.Length - 1)];
            return new Vector3(x, 0f, z);
        }

        private static void MoveRoot(string name, Vector3 delta)
        {
            var go = GameObject.Find(name);
            if (go != null) go.transform.position += delta;
        }

        private static void BuildContextProps(Transform parent)
        {
            AddStationProp("Station_PARTS_DELIVERY", "Platformer/crate-item.fbx", new Vector3(2.8f, 0.12f, -1.1f), 18f, parent, _platformerMat);
            AddStationProp("Station_PARTS_WAREHOUSE", "Platformer/crate.fbx", new Vector3(2.4f, 0.12f, -2.5f), -12f, parent, _platformerMat);
            AddStationProp("Station_PARTS_WAREHOUSE", "Cars/box.fbx", new Vector3(2.9f, 0.12f, -1.5f), 24f, parent, _carsMat);
            AddStationProp("Station_TIRE_STORAGE", "Cars/wheel-dark.fbx", new Vector3(2.5f, 0.28f, -2.2f), 90f, parent, _carsMat);
            AddStationProp("Station_ENGINE_WORKSHOP", "Cars/debris-drivetrain.fbx", new Vector3(2.4f, 0.12f, -2.5f), 15f, parent, _carsMat);
            AddStationProp("Station_ENGINE_WORKSHOP", "Cars/debris-bolt.fbx", new Vector3(2.8f, 0.12f, -1.8f), 0f, parent, _carsMat, 1.4f);
            AddStationProp("Station_BODY_REPAIR", "Cars/debris-spoiler-a.fbx", new Vector3(2.5f, 0.12f, -2.3f), 25f, parent, _carsMat);
            AddStationProp("Station_PAINT_MIXING", "Platformer/barrel.fbx", new Vector3(2.7f, 0.12f, -1.2f), 8f, parent, _platformerMat);
            AddStationProp("Station_CAR_WASH", "Roads/construction-light.fbx", new Vector3(2.7f, 0.12f, -2.6f), 0f, parent, _roadsMat, 1.35f);
            AddStationProp("Station_CAR_WASH", "Roads/construction-cone.fbx", new Vector3(2.7f, 0.12f, -1.6f), 0f, parent, _roadsMat);
            AddStationProp("Station_TUNING_STATION", "Cars/wheel-racing.fbx", new Vector3(2.6f, 0.3f, -2.4f), 90f, parent, _carsMat);
            AddStationProp("Station_TUNING_STATION", "Cars/debris-spoiler-a.fbx", new Vector3(2.8f, 0.12f, -1.5f), -15f, parent, _carsMat);
            AddStationProp("Station_VEHICLE_INSPECTION", "Racing/barrierWhite.fbx", new Vector3(2.3f, 0.12f, -2.6f), 90f, parent, null, 0.75f);
            AddStationProp("Station_COMPLETED_CAR_DELIVERY", "Racing/flagGreen.fbx", new Vector3(-2.7f, 0.12f, -2.3f), 0f, parent, null);
            AddStationProp("Station_USED_CAR_SHOWROOM", "Racing/barrierWhite.fbx", new Vector3(2.7f, 0.12f, -2.7f), 0f, parent, null, 0.7f);
            AddStationProp("Station_PREMIUM_CAR_SHOWROOM", "Racing/flagGreen.fbx", new Vector3(2.7f, 0.12f, -2.4f), 0f, parent, null);

            MakeBench(new Vector3(-4.2f, 0.12f, -2.3f), 0f, parent);
            MakeBench(new Vector3(13.1f, 0.12f, -2.5f), 0f, parent);
            MakeBench(new Vector3(25.4f, 0.12f, -4.9f), 0f, parent);
            MakeBin(new Vector3(-2.2f, 0.12f, -2.3f), parent);
            MakeBin(new Vector3(27.2f, 0.12f, -4.9f), parent);
        }

        private static void BuildFrontParking(Transform vehicles, Transform decoration)
        {
            float[] xs = { 11f, 15f, 20f, 25f, 30f };
            string[] cars = { "sedan", "suv", "hatchback-sports", "van", "sedan-sports" };
            float[] zOffsets = { 0.15f, -0.18f, 0f, 0.22f, -0.1f };
            float[] yaws = { 176f, 4f, 180f, -3f, 184f };
            for (int i = 0; i < xs.Length; i++)
            {
                if (i != 2)
                {
                    var visitorCar = AddModel($"{K}/Cars/{cars[i]}.fbx", _carsMat, new Vector3(xs[i], 0.1f, -13f + zOffsets[i]), yaws[i], $"VisitorCar_{i}", vehicles);
                    if (visitorCar != null) visitorCar.AddComponent<ArcadeVehicleController>();
                }
                MakeParkingLine(new Vector3(xs[i] - 2.25f, 0.125f, -13f), decoration);
            }
            MakeParkingLine(new Vector3(32.25f, 0.125f, -13f), decoration);

            var delivery = Station("Station_PARTS_DELIVERY");
            if (delivery != null)
                AddModel($"{K}/Cars/van.fbx", _carsMat, delivery.position + new Vector3(4.1f, 0.12f, -0.2f), 90f, "ServiceVan", vehicles);
        }

        private static void BuildPeople(Transform parent)
        {
            AddPerson("character-k", new Vector3(3.0f, 0.12f, 22.0f), 145f, parent, "Mechanic_Engine");
            AddPerson("character-m", new Vector3(-28.5f, 0.12f, 22.3f), 235f, parent, "Worker_Logistics");
            AddPerson("character-c", new Vector3(26.0f, 0.12f, -4.8f), 20f, parent, "Customer_Office");
            AddPerson("character-e", new Vector3(11.0f, 0.12f, -4.6f), 330f, parent, "Customer_Showroom");
        }

        private static void BuildVegetation(Transform parent)
        {
            var planters = new[]
            {
                new Vector3(-34.8f, 0.12f, -4.8f), new Vector3(-5.0f, 0.12f, 22.8f),
                new Vector3(14.0f, 0.12f, -4.8f), new Vector3(24.0f, 0.12f, -4.8f),
                new Vector3(33.8f, 0.12f, -4.8f)
            };
            for (int i = 0; i < planters.Length; i++)
            {
                AddModel($"{K}/Suburban/planter.fbx", _suburbanMat, planters[i], i * 17f, $"Planter_{i}", parent, 1.35f);
                AddModel($"{K}/Nature/plant_bushSmall.fbx", null, planters[i] + Vector3.up * 0.25f, i * 31f, $"PlanterBush_{i}", parent, 1.25f);
            }

            AddModel($"{K}/Suburban/tree-small.fbx", _suburbanMat, new Vector3(-9f, 0.12f, -4.4f), 25f, "PlazaTree_Left", parent, 2.2f);
            AddModel($"{K}/Suburban/tree-small.fbx", _suburbanMat, new Vector3(17.5f, 0.12f, -4.4f), 210f, "PlazaTree_Right", parent, 2.2f);
        }

        private static void BuildStreetFurniture(Transform parent)
        {
            var lampPositions = new[]
            {
                new Vector3(-40f, 0.12f, -10f), new Vector3(40f, 0.12f, -10f),
                new Vector3(-40f, 0.12f, 30f), new Vector3(40f, 0.12f, 30f)
            };
            for (int i = 0; i < lampPositions.Length; i++)
                AddModel($"{K}/Roads/light-square.fbx", _roadsMat, lampPositions[i], i % 2 == 0 ? 0f : 180f, $"PerimeterLight_{i}", parent, 0.6f);

            BuildCrosswalk(new Vector3(-37.3f, 0.125f, 6.5f), parent);
            BuildCrosswalk(new Vector3(37.5f, 0.125f, 6.5f), parent);
        }

        private static void WireVehicleGameplay()
        {
            var player = GameObject.Find("Player");
            if (player != null)
            {
                if (player.GetComponent<VehicleInteractor>() == null) player.AddComponent<VehicleInteractor>();
                if (player.GetComponent<VehicleMobileHud>() == null) player.AddComponent<VehicleMobileHud>();
            }

            if (Camera.main != null && Camera.main.GetComponent<ThirdPersonDriveCamera>() == null)
                Camera.main.gameObject.AddComponent<ThirdPersonDriveCamera>();
        }

        private static void BuildCrosswalk(Vector3 center, Transform parent)
        {
            var white = GetOrCreateMaterial("RoadPaintWhite", new Color(0.88f, 0.9f, 0.86f));
            for (int i = -2; i <= 2; i++)
            {
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "CrosswalkStripe";
                stripe.transform.SetParent(parent);
                stripe.transform.position = center + new Vector3(0f, 0f, i * 0.75f);
                stripe.transform.localScale = new Vector3(3.2f, 0.025f, 0.34f);
                stripe.GetComponent<Renderer>().sharedMaterial = white;
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
            }
        }

        private static void RepairRoadMarkings()
        {
            var white = GetOrCreateMaterial("RoadPaintWhite", new Color(0.88f, 0.9f, 0.86f));
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (renderer.name.StartsWith("RoadMark_")) renderer.sharedMaterial = white;
            }
        }

        private static void ExpandGroundCoverage()
        {
            var grass = GameObject.Find("Grass");
            if (grass != null) grass.transform.localScale = new Vector3(40f, 1f, 40f);
        }

        private static void MakeParkingLine(Vector3 position, Transform parent)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "VisitorParkingLine";
            line.transform.SetParent(parent);
            line.transform.position = position;
            line.transform.localScale = new Vector3(0.13f, 0.025f, 5f);
            line.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("RoadPaintWhite", new Color(0.88f, 0.9f, 0.86f));
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }

        private static void MakeBench(Vector3 position, float yaw, Transform parent)
        {
            var root = new GameObject("WaitingBench");
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var wood = GetOrCreateMaterial("FurnitureWood", new Color(0.32f, 0.18f, 0.08f));
            var metal = GetOrCreateMaterial("FurnitureMetal", new Color(0.16f, 0.18f, 0.2f));
            MakeCube("Seat", root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 0.16f, 0.65f), wood);
            MakeCube("Back", root.transform, new Vector3(0f, 1.05f, 0.28f), new Vector3(2.4f, 0.75f, 0.14f), wood);
            MakeCube("Leg", root.transform, new Vector3(-0.8f, 0.27f, 0f), new Vector3(0.14f, 0.55f, 0.55f), metal);
            MakeCube("Leg", root.transform, new Vector3(0.8f, 0.27f, 0f), new Vector3(0.14f, 0.55f, 0.55f), metal);
        }

        private static void MakeBin(Vector3 position, Transform parent)
        {
            var bin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bin.name = "TrashBin";
            bin.transform.SetParent(parent);
            bin.transform.position = position + Vector3.up * 0.55f;
            bin.transform.localScale = new Vector3(0.65f, 1.1f, 0.65f);
            bin.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("FurnitureGreen", new Color(0.12f, 0.28f, 0.2f));
            Object.DestroyImmediate(bin.GetComponent<Collider>());
        }

        private static void MakeCube(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        private static void AddStationProp(string stationName, string relativePath, Vector3 offset, float yaw, Transform parent, Material material, float scale = 1f)
        {
            var station = Station(stationName);
            if (station == null) return;
            AddModel($"{K}/{relativePath}", material, station.position + offset, yaw, System.IO.Path.GetFileNameWithoutExtension(relativePath), parent, scale);
        }

        private static Transform Station(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.transform : null;
        }

        private static void AddPerson(string assetName, Vector3 position, float yaw, Transform parent, string name)
        {
            var go = AddModel($"{K}/Characters/{assetName}.fbx", _charactersMat, position, yaw, name, parent);
            if (go == null) return;
            var bounds = CombinedBounds(go);
            if (bounds.size.y > 0.001f)
            {
                go.transform.localScale *= 1.65f / bounds.size.y;
                GroundAt(go, position);
            }
        }

        private static GameObject AddModel(string path, Material material, Vector3 position, float yaw, string name, Transform parent, float scale = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[Overhaul] enhancement model missing: {path}");
                return null;
            }

            var go = (GameObject)Object.Instantiate(asset);
            go.name = name;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));
            if (material != null) ApplyMaterial(go, material);
            GroundAt(go, position);
            if (!Mathf.Approximately(scale, 1f))
            {
                var baseCenter = new Vector3(CombinedBounds(go).center.x, CombinedBounds(go).min.y, CombinedBounds(go).center.z);
                go.transform.localScale *= scale;
                GroundAt(go, baseCenter);
            }
            go.transform.SetParent(parent, true);
            return go;
        }

        private static void GroundAt(GameObject go, Vector3 target)
        {
            var bounds = CombinedBounds(go);
            if (bounds.size == Vector3.zero)
            {
                go.transform.position = target;
                return;
            }
            go.transform.position += new Vector3(target.x - bounds.center.x, target.y - bounds.min.y, target.z - bounds.center.z);
        }

        private static Bounds CombinedBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void ApplyMaterial(GameObject go, Material material)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                int count = Mathf.Max(1, renderer.sharedMaterials.Length);
                var materials = new Material[count];
                for (int i = 0; i < count; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = $"{GenMatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Standard");
            if (existing != null)
            {
                if (existing.shader != shader) existing.shader = shader;
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            System.IO.Directory.CreateDirectory(GenMatDir);
            var material = new Material(shader) { color = color, name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Transform GetOrCreateRoot(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go.transform;
        }

        private static Transform ResetGeneratedRoot(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void OrganizeExistingRoots(Transform buildings, Transform roads, Transform props, Transform vehicles, Transform npcs, Transform vegetation, Transform decoration)
        {
            var categories = new HashSet<Transform> { buildings, roads, props, vehicles, npcs, vegetation, decoration };
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (categories.Contains(root.transform) || root.name == LayoutMarker) continue;
                Transform target = null;
                if (root.name.StartsWith("Station_")) target = buildings;
                else if (root.name.StartsWith("Road") || root.name.StartsWith("ParkLine") || root.name == "CampusAsphalt") target = roads;
                else if (root.name == "ParkedCar") target = vehicles;
                else if (root.name.StartsWith("Person_")) target = npcs;
                else if (root.name.StartsWith("RingTree") || root.name.StartsWith("Green") || root.name.StartsWith("Rock_") || root.name == "Grass") target = vegetation;
                else if (root.name == "Fence" || root.name.StartsWith("Sign_") || root.name.StartsWith("construction-")) target = decoration;
                if (target != null) root.transform.SetParent(target, true);
            }
        }

        private static void LoadMaterials()
        {
            _carsMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Cars.mat");
            _roadsMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Roads.mat");
            _platformerMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Platformer.mat");
            _suburbanMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Suburban.mat");
            _charactersMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Characters.mat");
        }
    }
}

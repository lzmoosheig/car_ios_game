using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Builds the base scene as a full automotive service CAMPUS, modeled on the
    /// reference layout: a grid of labeled station lots (Parts Delivery, Warehouse,
    /// Tire Storage, Engine Workshop, Body Repair, Paint Mixing/Booth, Reception,
    /// Customer Queue, Basic Change Bay, Wheel &amp; Tire, Car Wash, Detailing,
    /// Diagnostic, Tuning, Inspection, Completed Delivery, Showrooms, Employee Room,
    /// Office) separated by marked streets, with a front parking apron, entrance/exit
    /// gates, perimeter fence and trees. Every lot = tinted floor pad + building shell
    /// + colored signboard with 3D text + themed props. The tested gameplay loop
    /// (PartsSource -> Collect/Deposit zones -> ServiceBay -> GarageController) is
    /// wired to the Parts Delivery and Basic Change Bay lots. Doc 02 §5 station
    /// catalog / Doc 07 Phase 0.
    /// </summary>
    public static class CityGarageSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/CityGarage.unity";
        private const string K = "Assets/_Game/Art/Models/Kenney";
        private const string MatDir = "Assets/_Game/Art/Materials/Kenney";
        private const string GenMatDir = "Assets/_Game/Art/GrayboxMats";

        // ---- campus metrics ----
        private const float Cell = 9f;      // grid column spacing
        private const float PadSize = 7.4f; // station lot pad
        private const float PadTop = 0.12f; // y of pad surface
        private const float RoadY = 0.06f;  // y for road tiles (on the asphalt)
        private const float RoadScale = 3f; // 1m Kenney road tiles -> 3m
        private static readonly Vector3 LongRoadScale = new Vector3(70f, 1f, 5f);
        private static readonly Vector3 SideRoadScale = new Vector3(46.5f, 1f, 5f);
        private static readonly Vector3 JunctionRoadScale = new Vector3(5f, 1f, 5f);

        // rows (z centers) and streets between them
        private static readonly float[] RowZ = { 26f, 13f, 0f, -13f };
        private static readonly float[] StreetZ = { 19.5f, 6.5f, -6.5f };

        private static Material _roadsMat, _commercialMat, _suburbanMat, _industrialMat,
            _modularMat, _carsMat, _charactersMat, _platformerMat;

        private static System.Random _rng;

        // ---- sign palette (approximates the reference banners) ----
        private static readonly Color Navy = new Color(0.13f, 0.16f, 0.27f);
        private static readonly Color Green = new Color(0.15f, 0.40f, 0.22f);
        private static readonly Color Purple = new Color(0.34f, 0.18f, 0.48f);
        private static readonly Color Red = new Color(0.62f, 0.14f, 0.14f);
        private static readonly Color Brown = new Color(0.44f, 0.29f, 0.14f);

        private enum Shell { Garage, Office, Industrial, Open }
        private enum Props { None, Crates, Wheels, Barrels, CarInside, Queue, ShowroomCars, DeliveryPad }

        private struct StationDef
        {
            public string Name; public int Col; public int Row;
            public Shell Shell; public Color Sign; public Props Props;
            public StationDef(string name, int col, int row, Shell shell, Color sign, Props props)
            { Name = name; Col = col; Row = row; Shell = shell; Sign = sign; Props = props; }
        }

        // Column x = (col - 3.5) * Cell  ->  8 columns from -31.5 to 31.5.
        private static readonly StationDef[] Stations =
        {
            // Row 0 (back): logistics + heavy production (col 3 left green)
            new StationDef("PARTS DELIVERY", 0, 0, Shell.Industrial, Navy, Props.Crates),
            new StationDef("PARTS WAREHOUSE", 1, 0, Shell.Industrial, Navy, Props.Crates),
            new StationDef("TIRE STORAGE", 2, 0, Shell.Industrial, Navy, Props.Wheels),
            new StationDef("ENGINE WORKSHOP", 4, 0, Shell.Garage, Green, Props.CarInside),
            new StationDef("BODY REPAIR", 5, 0, Shell.Garage, Purple, Props.CarInside),
            new StationDef("PAINT MIXING", 6, 0, Shell.Industrial, Red, Props.Barrels),
            new StationDef("PAINT BOOTH", 7, 0, Shell.Garage, Red, Props.CarInside),

            // Row 1 (middle): the customer service line
            new StationDef("RECEPTION", 0, 1, Shell.Office, Navy, Props.None),
            new StationDef("CUSTOMER QUEUE", 1, 1, Shell.Open, Navy, Props.Queue),
            new StationDef("BASIC CHANGE BAY", 2, 1, Shell.Garage, Navy, Props.None), // gameplay bay
            new StationDef("WHEEL & TIRE STATION", 3, 1, Shell.Garage, Navy, Props.CarInside),
            new StationDef("CAR WASH", 4, 1, Shell.Garage, new Color(0.12f, 0.25f, 0.45f), Props.CarInside),
            new StationDef("DETAILING STATION", 5, 1, Shell.Garage, Purple, Props.CarInside),
            new StationDef("DIAGNOSTIC STATION", 6, 1, Shell.Garage, Navy, Props.CarInside),
            new StationDef("TUNING STATION", 7, 1, Shell.Garage, Navy, Props.CarInside),

            // Row 2 (front): inspection, delivery, sales, admin (cols 6-7 hold Employee/Office)
            new StationDef("VEHICLE INSPECTION", 0, 2, Shell.Garage, Navy, Props.CarInside),
            new StationDef("COMPLETED CAR DELIVERY", 1, 2, Shell.Open, Green, Props.DeliveryPad),
            new StationDef("USED CAR SHOWROOM", 2, 2, Shell.Office, Brown, Props.ShowroomCars),
            new StationDef("PREMIUM CAR SHOWROOM", 4, 2, Shell.Office, Purple, Props.ShowroomCars),
            new StationDef("EMPLOYEE ROOM", 6, 2, Shell.Office, Brown, Props.None),
            new StationDef("OFFICE & FINANCE", 7, 2, Shell.Office, Green, Props.None),
        };

        private static float ColX(int col) => (col - 3.5f) * Cell;

        [MenuItem("Overhaul/Build City Garage Scene")]
        public static void Build()
        {
            _rng = new System.Random(42);
            LoadMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            var cam = BuildCamera();
            BuildGround();
            BuildStreets();
            BuildPerimeter();

            Vector3 bayFront = Vector3.zero, partsPos = Vector3.zero;
            foreach (var def in Stations)
            {
                var padCenter = new Vector3(ColX(def.Col), 0f, RowZ[def.Row]);
                BuildStation(def, padCenter);
                if (def.Name == "BASIC CHANGE BAY") bayFront = padCenter;
                if (def.Name == "PARTS DELIVERY") partsPos = padCenter;
            }

            BuildGreenCells();
            BuildParkingApron();
            BuildGates();
            ScatterPeople();

            // ---------------- gameplay wiring (same tested loop as before) ----------------
            var itemTemplate = BuildCarriedItemTemplate();
            BuildPlayer(bayFront + new Vector3(3f, 0f, -4f));
            new GameObject("ResourceCatalog").AddComponent<ResourceCatalog>().AddEntry("tire", 1);

            // Parts Delivery lot: crate pallet is the source; collect zone on its front apron.
            var palletPos = partsPos + new Vector3(0f, PadTop, -4.6f);
            var palletGo = PlaceModel($"{K}/Cars/box.fbx", _carsMat, palletPos, 0f, "PartsPallet");
            PlaceModel($"{K}/Platformer/crate.fbx", _platformerMat, palletPos + new Vector3(0.9f, 0f, 0.3f), 15f, "PartsPallet_Crate");
            var source = palletGo.AddComponent<PartsSource>();
            source.Configure("tire", 12, 2f);
            var collectZone = MakeZone("CollectZone", palletPos, 2.0f);
            collectZone.Configure(InteractionKind.Collect, "tire", source, null, itemTemplate);

            // Basic Change Bay: ServiceBay + deposit zone at the lot's street edge.
            var bayGo = new GameObject("RepairBay");
            bayGo.transform.position = bayFront;
            var rack = bayGo.AddComponent<ResourceRack>();
            var eco = bayGo.AddComponent<EconomyManager>();
            var bay = bayGo.AddComponent<ServiceBay>();
            var controller = bayGo.AddComponent<GarageController>();

            var depositZone = MakeZone("DepositZone", bayFront + new Vector3(2.8f, PadTop, -3.2f), 1.8f);
            depositZone.transform.SetParent(bayGo.transform, true);
            depositZone.Configure(InteractionKind.Deposit, "tire", null, rack, null);

            // Customer route runs along the middle street (z = 6.5), straight past the bay.
            var entrance = MakeMarker("Entrance", new Vector3(-40f, RoadY, StreetZ[1]), 90f);
            var baySlot = MakeMarker("BaySlot", bayFront + new Vector3(0f, PadTop, -1.2f), 0f);
            var exit = MakeMarker("Exit", new Vector3(40f, RoadY, StreetZ[1]), 90f);

            var carPrefabs = LoadCars("sedan", "suv", "hatchback-sports", "sedan-sports", "taxi");
            controller.Configure(bay, rack, eco, entrance, baySlot, exit, carPrefabs, _carsMat);

            FrameCamera(cam);

            AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Overhaul] CityGarage scene built at {ScenePath} (stations: {Stations.Length}, cars: {carPrefabs.Length})");
        }

        // ---------------------------------------------------------------- environment

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.56f, 0.58f, 0.62f);
        }

        private static Camera BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.backgroundColor = new Color(0.42f, 0.60f, 0.75f);
            return cam;
        }

        private static void FrameCamera(Camera cam)
        {
            // High elevated view like the reference: nearly axis-aligned, steep pitch,
            // whole campus in frame, station fronts (south faces) toward the camera.
            var lookTarget = new Vector3(0f, 0f, 8f);
            var rot = Quaternion.Euler(56f, 0f, 0f);
            cam.transform.SetPositionAndRotation(lookTarget - (rot * Vector3.forward) * 62f, rot);
            cam.fieldOfView = 46f;
        }

        private static void BuildGround()
        {
            var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "Grass";
            grass.transform.localScale = new Vector3(24f, 1f, 24f); // 240x240
            Paint(grass, new Color(0.35f, 0.46f, 0.28f));

            var asphalt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            asphalt.name = "CampusAsphalt";
            asphalt.transform.position = new Vector3(0f, 0.0f, 7f);
            asphalt.transform.localScale = new Vector3(82f, 0.1f, 52f); // top at y=0.05
            Paint(asphalt, new Color(0.17f, 0.18f, 0.19f));
        }

        private static void BuildStreets()
        {
            // Kenney city-kit-roads layout. Use the same scale as the hand-placed
            // reference pieces: long straights are stretched to 70x5, junctions are
            // 5x5. This avoids the repeated zebra-tile look while still using the
            // road pack for the actual road surface.
            foreach (float z in StreetZ)
            {
                PlaceRoadModel($"{K}/Roads/road-straight.fbx", _roadsMat, new Vector3(0.2f, 0.1f, z), 0f, LongRoadScale, $"RoadLong_{z}");
            }

            foreach (float x in new[] { -37.5f, 37.5f })
            {
                PlaceRoadModel($"{K}/Roads/road-straight.fbx", _roadsMat, new Vector3(x, 0.1f, 7.5f), 90f, SideRoadScale, $"RoadSide_{x}");
            }

            PlaceRoadModel($"{K}/Roads/road-straight.fbx", _roadsMat, new Vector3(0.2f, 0.1f, -16.5f), 0f, LongRoadScale, "RoadLong_Front");

            foreach (float x in new[] { -37.5f, 37.5f })
            {
                foreach (float z in StreetZ)
                {
                    float yaw = x < 0f ? 90f : 270f;
                    PlaceRoadModel($"{K}/Roads/road-intersection.fbx", _roadsMat, new Vector3(x, 0.1f, z), yaw, JunctionRoadScale, $"RoadJoin_{x}_{z}");
                }
            }

            PlaceRoadModel($"{K}/Roads/road-bend.fbx", _roadsMat, new Vector3(-37.5f, 0.1f, -16.5f), 180f, JunctionRoadScale, "RoadBend_LeftFront");
            PlaceRoadModel($"{K}/Roads/road-bend.fbx", _roadsMat, new Vector3(37.5f, 0.1f, -16.5f), 90f, JunctionRoadScale, "RoadBend_RightFront");
            PlaceRoadModel($"{K}/Roads/road-end.fbx", _roadsMat, new Vector3(-37.5f, 0.1f, 30f), 0f, JunctionRoadScale, "RoadEnd_LeftNorth");
            PlaceRoadModel($"{K}/Roads/road-end.fbx", _roadsMat, new Vector3(37.5f, 0.1f, 30f), 0f, JunctionRoadScale, "RoadEnd_RightNorth");
            PlaceRoadModel($"{K}/Roads/road-crossing.fbx", _roadsMat, new Vector3(-37.5f, 0.105f, StreetZ[1]), 90f, JunctionRoadScale, "RoadCrossing_Entrance");
            PlaceRoadModel($"{K}/Roads/road-crossing.fbx", _roadsMat, new Vector3(37.5f, 0.105f, StreetZ[1]), 90f, JunctionRoadScale, "RoadCrossing_Exit");
        }

        private static void BuildPerimeter()
        {
            // Fence along north and both sides (south stays open for the front street).
            for (float x = -40f; x <= 40f; x += 2.5f)
                FencePiece(new Vector3(x, 0.05f, 33.5f), 0f);
            for (float z = -14f; z <= 32f; z += 2.5f)
            {
                if (Mathf.Abs(z - StreetZ[1]) < 2.6f) continue; // gaps where the customer street exits
                FencePiece(new Vector3(-41.5f, 0.05f, z), 90f);
                FencePiece(new Vector3(41.5f, 0.05f, z), 90f);
            }

            // Tree ring outside the fence + a couple of rocks.
            string[] trees = { "Suburban/tree-large", "Suburban/tree-small", "Nature/tree_default", "Nature/tree_small" };
            for (int i = 0; i < 26; i++)
            {
                float t = i / 26f * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(t) * 50f, 0f, Mathf.Sin(t) * 42f + 8f);
                if (pos.z < -14f) continue; // keep the front open
                var tree = PlaceModel($"{K}/{trees[i % trees.Length]}.fbx", TreeMat(trees[i % trees.Length]), pos, (float)(_rng.NextDouble() * 360.0), $"RingTree_{i}");
                if (tree != null) ScaleInPlace(tree, 3.2f);
            }
            var r1 = PlaceModel($"{K}/Nature/rock_smallA.fbx", null, new Vector3(-47f, 0f, -8f), 20f, "Rock_1");
            if (r1 != null) ScaleInPlace(r1, 3f);
            var r2 = PlaceModel($"{K}/Nature/rock_smallC.fbx", null, new Vector3(47f, 0f, 30f), 150f, "Rock_2");
            if (r2 != null) ScaleInPlace(r2, 3f);
        }

        private static Material TreeMat(string path) => path.StartsWith("Suburban") ? _suburbanMat : null;

        private static void FencePiece(Vector3 pos, float rotY)
        {
            var f = PlaceModel($"{K}/Suburban/fence.fbx", _suburbanMat, pos, rotY, "Fence");
            if (f != null) ScaleInPlace(f, 2.5f);
        }

        private static void BuildGreenCells()
        {
            // Unused grid cells become little green squares with trees (like the
            // hedge/planting strips in the reference).
            var greenCells = new (int col, int row)[] { (3, 0), (3, 2), (5, 2) };
            foreach (var (col, row) in greenCells)
            {
                var c = new Vector3(ColX(col), 0f, RowZ[row]);
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pad.name = $"Green_{col}_{row}";
                pad.transform.position = new Vector3(c.x, 0.08f, c.z);
                pad.transform.localScale = new Vector3(PadSize, 0.08f, PadSize);
                Paint(pad, new Color(0.30f, 0.42f, 0.24f));
                var t = PlaceModel($"{K}/Suburban/tree-large.fbx", _suburbanMat, c + new Vector3(-1.5f, PadTop, 1f), 30f, $"GreenTree_{col}_{row}");
                if (t != null) ScaleInPlace(t, 3f);
                var b = PlaceModel($"{K}/Nature/plant_bush.fbx", null, c + new Vector3(1.8f, PadTop, -1.5f), 0f, $"GreenBush_{col}_{row}");
                if (b != null) ScaleInPlace(b, 2.5f);
            }
        }

        // ---------------------------------------------------------------- stations

        private static void BuildStation(StationDef def, Vector3 center)
        {
            var root = new GameObject($"Station_{def.Name.Replace(' ', '_')}");
            root.transform.position = center;

            // Tinted floor pad.
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad";
            pad.transform.SetParent(root.transform);
            pad.transform.position = new Vector3(center.x, 0.08f, center.z);
            pad.transform.localScale = new Vector3(PadSize, 0.08f, PadSize);
            Paint(pad, PadColor(def), $"Pad_{def.Name.Replace(' ', '_').Replace('&', 'n')}");

            // Building shell, front (detailed face) toward -Z / the camera.
            GameObject shell = null;
            switch (def.Shell)
            {
                // Isolated orbit-capture proof: pitsGarage/pitsOffice open pillared
                // fronts face -Z (south, toward the camera) at rotY=0. Note their
                // interiors have a raised platform, so cars must sit on the pad apron
                // at the bay mouth, not at the shell center (they sink otherwise).
                case Shell.Garage:
                    shell = PlaceModel($"{K}/Racing/pitsGarage.fbx", null, center + new Vector3(0f, PadTop, 0.6f), 0f, "Shell");
                    if (shell != null) ScaleToWidth(shell, 6.6f, center + new Vector3(0f, PadTop, 0.6f));
                    break;
                case Shell.Office:
                    shell = PlaceModel($"{K}/Racing/pitsOffice.fbx", null, center + new Vector3(0f, PadTop, 0.6f), 0f, "Shell");
                    if (shell != null) ScaleToWidth(shell, 6.6f, center + new Vector3(0f, PadTop, 0.6f));
                    break;
                case Shell.Industrial:
                    string[] inds = { "building-a", "building-d", "building-h" };
                    shell = PlaceModel($"{K}/Industrial/{inds[_rng.Next(inds.Length)]}.fbx", _industrialMat, center + new Vector3(0f, PadTop, 1.2f), 180f, "Shell");
                    if (shell != null) ScaleToWidth(shell, 6.2f, center + new Vector3(0f, PadTop, 1.2f));
                    break;
                case Shell.Open:
                    break;
            }
            if (shell != null) shell.transform.SetParent(root.transform, true);

            // Signboard across the lot front.
            MakeSign(def.Name, def.Sign, new Vector3(center.x, 0f, center.z - PadSize / 2f + 0.4f), root.transform);

            BuildStationProps(def, center, root.transform);
        }

        private static Color PadColor(StationDef def)
        {
            if (def.Name == "CAR WASH") return new Color(0.22f, 0.34f, 0.48f);
            if (def.Name == "COMPLETED CAR DELIVERY") return new Color(0.22f, 0.38f, 0.26f);
            if (def.Name == "PREMIUM CAR SHOWROOM") return new Color(0.34f, 0.28f, 0.42f);
            if (def.Name == "DETAILING STATION") return new Color(0.30f, 0.26f, 0.38f);
            return new Color(0.26f, 0.27f, 0.29f); // light asphalt
        }

        private static void BuildStationProps(StationDef def, Vector3 c, Transform parent)
        {
            switch (def.Props)
            {
                case Props.Crates:
                    Prop($"{K}/Platformer/crate.fbx", _platformerMat, c + new Vector3(-2.2f, PadTop, -2.2f), 10f, parent);
                    Prop($"{K}/Cars/box.fbx", _carsMat, c + new Vector3(-1.2f, PadTop, -2.5f), -15f, parent);
                    Prop($"{K}/Platformer/crate-item.fbx", _platformerMat, c + new Vector3(-2.0f, PadTop, -1.2f), 40f, parent);
                    if (def.Name == "PARTS DELIVERY")
                        Prop($"{K}/Cars/delivery-flat.fbx", _carsMat, c + new Vector3(2.2f, PadTop, -2.0f), 100f, parent);
                    break;

                case Props.Wheels:
                    for (int i = 0; i < 4; i++)
                        Prop($"{K}/Cars/wheel-default.fbx", _carsMat, c + new Vector3(-2.4f + i * 1.0f, PadTop, -2.4f), 0f, parent);
                    Prop($"{K}/Cars/wheel-racing.fbx", _carsMat, c + new Vector3(1.9f, PadTop, -2.4f), 0f, parent);
                    break;

                case Props.Barrels:
                    Prop($"{K}/Platformer/barrel.fbx", _platformerMat, c + new Vector3(-2.2f, PadTop, -2.3f), 0f, parent);
                    Prop($"{K}/Platformer/barrel.fbx", _platformerMat, c + new Vector3(-1.3f, PadTop, -2.5f), 30f, parent);
                    Prop($"{K}/Industrial/detail-tank.fbx", _industrialMat, c + new Vector3(2.2f, PadTop, -2.2f), 0f, parent);
                    break;

                case Props.CarInside:
                    // At the bay mouth on the apron — visible in front of the opening
                    // (the shell interior has a raised platform that swallows cars).
                    ParkedCar(c + new Vector3(0f, PadTop, -2.2f), 180f + Jitter(6f), parent);
                    Prop($"{K}/Cars/cone.fbx", _carsMat, c + new Vector3(-2.6f, PadTop, -2.8f), 0f, parent);
                    break;

                case Props.Queue:
                    for (int i = 0; i < 3; i++)
                        ParkedCar(c + new Vector3(-2f + i * 2.6f, PadTop, 0.3f), 90f + Jitter(4f), parent);
                    Prop($"{K}/Roads/construction-cone.fbx", _roadsMat, c + new Vector3(-3f, PadTop, -2.6f), 0f, parent);
                    break;

                case Props.ShowroomCars:
                    ParkedCar(c + new Vector3(-1.6f, PadTop, -2.4f), 160f, parent);
                    ParkedCar(c + new Vector3(1.6f, PadTop, -2.4f), 200f, parent);
                    break;

                case Props.DeliveryPad:
                    ParkedCar(c + new Vector3(0f, PadTop, 0f), 180f, parent);
                    Prop($"{K}/Racing/flagGreen.fbx", null, c + new Vector3(2.6f, PadTop, -2.4f), 0f, parent);
                    break;
            }
        }

        // ---------------------------------------------------------------- apron, gates, people

        private static void BuildParkingApron()
        {
            // Front row (z = -13): customer parking with painted lines and parked cars.
            float z = RowZ[3];
            for (int i = 0; i < 8; i++)
            {
                float x = -15f + i * 3.0f;
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = $"ParkLine_{i}";
                line.transform.position = new Vector3(x - 1.5f, 0.09f, z);
                line.transform.localScale = new Vector3(0.15f, 0.02f, 5f);
                Paint(line, new Color(0.85f, 0.85f, 0.85f), "ParkLine");
                if (i < 7 && _rng.NextDouble() < 0.7)
                    ParkedCar(new Vector3(x, 0.06f, z), (_rng.NextDouble() < 0.5 ? 0f : 180f) + Jitter(5f), null);
            }
        }

        private static void BuildGates()
        {
            // Entrance (west) and exit (east) gates across the customer street.
            MakeSign("ENTRANCE", new Color(0.12f, 0.12f, 0.13f), new Vector3(-36f, 0f, StreetZ[1] - 2.2f), null, 7f);
            MakeSign("EXIT / DELIVERY ZONE", Green, new Vector3(36f, 0f, StreetZ[1] - 2.2f), null, 7f);
            Prop($"{K}/Roads/construction-barrier.fbx", _roadsMat, new Vector3(-36f, 0.06f, StreetZ[1] + 2.4f), 90f, null, 2f);
            Prop($"{K}/Roads/construction-light.fbx", _roadsMat, new Vector3(36f, 0.06f, StreetZ[1] + 2.4f), 0f, null, 2f);
        }

        private static void ScatterPeople()
        {
            string[] chars = { "character-c", "character-d", "character-e", "character-k", "character-m" };
            var spots = new Vector3[]
            {
                new Vector3(ColX(0), PadTop, RowZ[1] - 4.4f), // outside reception
                new Vector3(ColX(1) + 1.5f, PadTop, RowZ[1] - 4.2f), // by the queue
                new Vector3(ColX(2) - 2.5f, PadTop, RowZ[1] - 4.6f), // near the bay
                new Vector3(ColX(1) - 2f, PadTop, RowZ[0] - 4.4f),   // warehouse front
                new Vector3(ColX(2) + 1f, PadTop, RowZ[2] - 4.3f),   // showroom front
                new Vector3(ColX(6) - 1f, PadTop, RowZ[2] - 4.5f),   // employee room
                new Vector3(-6f, 0.06f, -14.5f),                     // parking apron
                new Vector3(ColX(5) + 2f, PadTop, RowZ[0] - 4.5f),   // paint row
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var person = InstantiateNormalized($"{K}/Characters/{chars[i % chars.Length]}.fbx", _charactersMat, 1.6f, $"Person_{i}");
                if (person == null) continue;
                person.transform.rotation = Quaternion.Euler(0f, (float)(_rng.NextDouble() * 360.0), 0f);
                GroundAt(person, spots[i]);
            }
        }

        // ---------------------------------------------------------------- player & item template

        private static GameObject BuildCarriedItemTemplate()
        {
            var itemTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemTemplate.name = "CarriedItemTemplate";
            Paint(itemTemplate, new Color(0.20f, 0.75f, 0.72f));
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
            var carrier = root.AddComponent<CarrierView>();

            var anchor = new GameObject("StackAnchor").transform;
            anchor.SetParent(root.transform);
            anchor.localPosition = new Vector3(0f, 1.5f, 0f);
            SetPrivate(carrier, "stackAnchor", anchor);

            var visual = InstantiateNormalized($"{K}/Characters/character-a.fbx", _charactersMat, 1.7f, "Visual");
            if (visual != null)
            {
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            return root;
        }

        // ---------------------------------------------------------------- signs

        /// <summary>
        /// A signboard like the reference: two posts, a colored banner, and bold white
        /// 3D text (legacy TextMesh — zero package dependencies), facing -Z (camera).
        /// </summary>
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
            // Identity rotation: TextMesh glyphs read correctly from the -Z side, which
            // is where the main camera sits (verified via close-up capture — Y=180 was
            // mirrored from the camera side).
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
                textGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }

        // ---------------------------------------------------------------- generic helpers

        private static float Jitter(float range) => (float)(_rng.NextDouble() * 2.0 - 1.0) * range;

        private static void ParkedCar(Vector3 pos, float rotY, Transform parent)
        {
            string[] cars = { "sedan", "suv", "hatchback-sports", "sedan-sports", "taxi", "van", "delivery-flat" };
            var go = PlaceModel($"{K}/Cars/{cars[_rng.Next(cars.Length)]}.fbx", _carsMat, pos, rotY, "ParkedCar");
            if (go != null && parent != null) go.transform.SetParent(parent, true);
        }

        private static void Prop(string path, Material mat, Vector3 pos, float rotY, Transform parent, float scale = 1f)
        {
            var go = PlaceModel(path, mat, pos, rotY, System.IO.Path.GetFileNameWithoutExtension(path));
            if (go == null) return;
            if (scale != 1f) ScaleInPlace(go, scale);
            if (parent != null) go.transform.SetParent(parent, true);
        }

        private static GameObject[] LoadCars(params string[] names)
        {
            var list = new List<GameObject>();
            foreach (var n in names)
            {
                var a = AssetDatabase.LoadAssetAtPath<GameObject>($"{K}/Cars/{n}.fbx");
                if (a != null) list.Add(a);
                else Debug.LogWarning($"[Overhaul] car model missing: {n}");
            }
            return list.ToArray();
        }

        private static InteractionZone MakeZone(string name, Vector3 pos, float radius)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            return go.AddComponent<InteractionZone>();
        }

        private static Transform MakeMarker(string name, Vector3 pos, float rotY)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            return go.transform;
        }

        private static GameObject PlaceRoadModel(string path, Material mat, Vector3 targetPos, float rotY, Vector3 scale, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning($"[Overhaul] road model missing: {path}"); return null; }

            var go = (GameObject)Object.Instantiate(asset);
            go.name = name;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, rotY, 0f));
            go.transform.localScale = scale;
            if (mat != null) ApplyMat(go, mat);
            GroundAt(go, targetPos);
            return go;
        }

        /// <summary>
        /// Places a model so its actual world-space bounds land centered (X/Z) and
        /// grounded (Y) at <paramref name="targetPos"/> — robust to models whose FBX
        /// pivot is not at their local origin (the racing-kit pit pieces bake in large
        /// offsets from their source layout).
        /// </summary>
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

        /// <summary>Uniformly scales an already-placed model so its bounds width (X) hits the target, re-grounding it after.</summary>
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

        /// <summary>Uniformly rescales an already ground-placed object about its own base center.</summary>
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

        /// <summary>Instantiates a model uniformly rescaled to a target world height (characters).</summary>
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

        /// <summary>Creates (or reuses) a flat-tinted material asset and assigns it. Shared by name so 30 pads don't make 30 materials.</summary>
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
            _commercialMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Commercial.mat");
            _suburbanMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Suburban.mat");
            _industrialMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Industrial.mat");
            _modularMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Modular.mat");
            _carsMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Cars.mat");
            _charactersMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Characters.mat");
            _platformerMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Platformer.mat");
            // Racing and Nature packs keep their own embedded per-slot materials
            // (see KenneyModelSetup notes) — no override materials loaded for them.
        }
    }
}

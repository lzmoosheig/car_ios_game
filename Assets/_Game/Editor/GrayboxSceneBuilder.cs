using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Builds the graybox scene (Assets/_Game/Scenes/Graybox.unity) from the imported
    /// KayKit city / forest packs and the low-poly car pack, wiring the functional loop:
    /// a Parts pallet (Collect zone) -> the player carrier -> the bay's rack (Deposit zone)
    /// -> the tested ServiceBay, with GarageController driving real cars in and out.
    /// Regenerate via the menu or headless -executeMethod. Doc 07 Phase 0.
    /// </summary>
    public static class GrayboxSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Graybox.unity";
        private const string MatDir = "Assets/_Game/Art/GrayboxMats";
        private const string Models = "Assets/_Game/Art/Models";

        [MenuItem("Overhaul/Build Graybox Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var carsMat = AssetDatabase.LoadAssetAtPath<Material>(ModelSetup.CarsMat);
            var cityMat = AssetDatabase.LoadAssetAtPath<Material>(ModelSetup.CityMat);
            var natureMat = AssetDatabase.LoadAssetAtPath<Material>(ModelSetup.NatureMat);

            // --- Lighting & ambient ---
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);

            // --- Isometric camera (perspective FOV 35, pitch 52 / yaw 45; Doc 05 §2) ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 35f;
            cam.backgroundColor = new Color(0.42f, 0.62f, 0.78f);
            var camRot = Quaternion.Euler(52f, 45f, 0f);
            var lookTarget = new Vector3(0f, 0f, -1f);
            camGo.transform.SetPositionAndRotation(lookTarget - (camRot * Vector3.forward) * 24f, camRot);

            // --- Ground ---
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            Paint(ground, new Color(0.34f, 0.42f, 0.28f)); // grass-ish

            // --- Street (city road tiles, 2m grid so they connect) along the near edge ---
            for (int i = -3; i <= 3; i++)
                PlaceModel($"{Models}/City/road_straight.fbx", cityMat,
                    new Vector3(i * 2f, 0.01f, -8f), 0f, 1f, $"Road_{i}");

            // --- Backdrop buildings ---
            string[] blds = { "building_A", "building_C", "building_E", "building_G", "building_B" };
            for (int i = 0; i < blds.Length; i++)
                PlaceModel($"{Models}/City/{blds[i]}.fbx", cityMat,
                    new Vector3(-8f + i * 4f, 0f, 8f), 180f, 1f, $"Bld_{blds[i]}");

            // --- Decor: streetlights, trees, rocks ---
            PlaceModel($"{Models}/City/streetlight.fbx", cityMat, new Vector3(-6f, 0f, -6f), 0f, 1f, "Light_L");
            PlaceModel($"{Models}/City/streetlight.fbx", cityMat, new Vector3(6f, 0f, -6f), 0f, 1f, "Light_R");
            PlaceModel($"{Models}/Nature/Tree_1_A_Color1.fbx", natureMat, new Vector3(-9f, 0f, 3f), 20f, 1f, "Tree_1");
            PlaceModel($"{Models}/Nature/Tree_3_A_Color1.fbx", natureMat, new Vector3(9f, 0f, 4f), -40f, 1f, "Tree_2");
            PlaceModel($"{Models}/Nature/Tree_2_A_Color1.fbx", natureMat, new Vector3(-10f, 0f, 6f), 90f, 1f, "Tree_3");
            PlaceModel($"{Models}/Nature/Rock_1_A_Color1.fbx", natureMat, new Vector3(8f, 0f, -1f), 0f, 1f, "Rock_1");
            PlaceModel($"{Models}/Nature/Rock_2_C_Color1.fbx", natureMat, new Vector3(-9f, 0f, -1f), 30f, 1f, "Rock_2");

            // --- The garage building (visual) ---
            PlaceModel($"{Models}/City/base.fbx", cityMat, new Vector3(3f, 0f, 3f), 0f, 1f, "GarageBase");
            PlaceModel($"{Models}/City/building_D.fbx", cityMat, new Vector3(3f, 0f, 4.5f), 180f, 1f, "GarageBuilding");

            // --- Carried-item template (small crate cube), inactive ---
            var itemTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemTemplate.name = "CarriedItemTemplate";
            Paint(itemTemplate, new Color(0.20f, 0.75f, 0.72f)); // teal = tire/part
            itemTemplate.transform.position = new Vector3(0f, -5f, 0f);
            itemTemplate.SetActive(false);

            // --- Player (capsule avatar; no character model in packs) ---
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.7f, -4f);
            player.transform.localScale = Vector3.one * 0.7f; // ~1.4m worker, fits the diorama
            Object.DestroyImmediate(player.GetComponent<Collider>());
            Paint(player, new Color(0.95f, 0.85f, 0.30f));
            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.5f;
            var rb = player.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false; // reliable trigger events with the zones
            player.AddComponent<PlayerController>();
            player.AddComponent<KeyboardDriver>();
            var carrier = player.AddComponent<CarrierView>();
            var anchor = new GameObject("StackAnchor").transform;
            anchor.SetParent(player.transform);
            anchor.localPosition = new Vector3(0f, 1.2f, 0f);
            SetPrivate(carrier, "stackAnchor", anchor);

            // --- Resource catalog ---
            new GameObject("ResourceCatalog").AddComponent<ResourceCatalog>().AddEntry("tire", 1);

            // --- Parts pallet + Collect zone (KayKit dumpster as the pallet) ---
            var palletVisual = PlaceModel($"{Models}/City/dumpster.fbx", cityMat, new Vector3(-4f, 0f, -2f), 0f, 1f, "PartsPallet");
            var palletGo = palletVisual != null ? palletVisual : new GameObject("PartsPallet");
            if (palletVisual == null) palletGo.transform.position = new Vector3(-4f, 0f, -2f);
            var source = palletGo.AddComponent<PartsSource>();
            source.Configure("tire", 12, 2f);
            var collectZone = MakeZone("CollectZone", palletGo.transform.position, 1.8f);
            collectZone.Configure(InteractionKind.Collect, "tire", source, null, itemTemplate);

            // --- Repair bay logic + Deposit zone + route markers ---
            var bayGo = new GameObject("RepairBay");
            bayGo.transform.position = new Vector3(3f, 0f, 2f);
            var rack = bayGo.AddComponent<ResourceRack>();
            var eco = bayGo.AddComponent<EconomyManager>();
            var bay = bayGo.AddComponent<ServiceBay>();
            var controller = bayGo.AddComponent<GarageController>();

            var depositZone = MakeZone("DepositZone", new Vector3(3f, 0f, 0.2f), 1.8f);
            depositZone.transform.SetParent(bayGo.transform, true);
            depositZone.Configure(InteractionKind.Deposit, "tire", null, rack, null);

            var entrance = MakeMarker("Entrance", new Vector3(-11f, 0.2f, -8f), 90f);
            var baySlot = MakeMarker("BaySlot", new Vector3(3f, 0.2f, -1.8f), 0f);
            var exit = MakeMarker("Exit", new Vector3(11f, 0.2f, -8f), 90f);

            var carPrefabs = LoadCars("coupe", "italia", "jeep", "van", "police");
            controller.Configure(bay, rack, eco, entrance, baySlot, exit, carPrefabs, carsMat);

            AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Overhaul] Graybox scene built at {ScenePath} (cars: {carPrefabs.Length})");
        }

        // ---- helpers ----

        private static GameObject[] LoadCars(params string[] names)
        {
            var list = new List<GameObject>();
            foreach (var n in names)
            {
                var a = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/Cars/{n}.fbx");
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

        private static GameObject PlaceModel(string path, Material mat, Vector3 pos, float rotY, float scale, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning($"[Overhaul] model missing: {path}"); return null; }

            var go = (GameObject)Object.Instantiate(asset);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            if (scale > 0f) go.transform.localScale = Vector3.one * scale;
            if (mat != null) ApplyMat(go, mat);
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

        private static void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null || r.sharedMaterial == null) return;
            var mat = new Material(r.sharedMaterial);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            System.IO.Directory.CreateDirectory(MatDir);
            var path = $"{MatDir}/{go.name}.mat";
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
    }
}

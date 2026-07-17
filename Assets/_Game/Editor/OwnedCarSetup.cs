using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Builds the Phase B personal-car slice into the OPEN scene (Doc 09 §7, §14):
    /// the claimable starter car in the free front-parking slot, the owned-car system,
    /// and one interaction pad per station role (Basic Bay service, Diagnostic scan,
    /// Tuning setup, Inspection certification). Same idempotent build-into-live-scene
    /// approach as HudSetup / CarDeliverySetup.
    /// </summary>
    public static class OwnedCarSetup
    {
        private const string K = "Assets/_Game/Art/Models/Kenney";
        private const string CarsMatPath = "Assets/_Game/Art/Materials/Kenney/Cars.mat";

        // The free slot CityGarageEnvironmentEnhancer.BuildFrontParking deliberately
        // leaves empty (index 2, x=20) - and hatchback-sports is the one Kenney car
        // no visitor prop uses, so the starter car reads as distinct.
        private static readonly Vector3 StarterCarPosition = new(20f, 0.1f, -13f);
        private const float StarterCarYaw = 180f;

        private struct PadDef
        {
            public string StationName;
            public string FallbackStationName;
            public OwnedCarStationRole Role;
            public string Label;
            public Color Color;
        }

        private static readonly PadDef[] Pads =
        {
            new() { StationName = "Station_BASIC_CHANGE_BAY", FallbackStationName = "Basic Change Bay",
                    Role = OwnedCarStationRole.Maintain, Label = "SERVICE", Color = new Color(0.20f, 0.62f, 0.45f) },
            new() { StationName = "Station_DIAGNOSTIC_STATION", FallbackStationName = "Diagnostic Station",
                    Role = OwnedCarStationRole.Diagnose, Label = "SCAN", Color = new Color(0.25f, 0.50f, 0.80f) },
            new() { StationName = "Station_TUNING_STATION", FallbackStationName = "Tuning Station",
                    Role = OwnedCarStationRole.Tune, Label = "TUNE", Color = new Color(0.80f, 0.45f, 0.15f) },
            new() { StationName = "Station_VEHICLE_INSPECTION", FallbackStationName = "Vehicle Inspection",
                    Role = OwnedCarStationRole.Inspect, Label = "CERTIFY", Color = new Color(0.60f, 0.30f, 0.70f) },
        };

        [MenuItem("Overhaul/Build Owned Car")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();

            DestroyAllNamed("OwnedCarSystem", "StarterCar", "WorkshopTestDriveLoop");
            DestroyAllByPrefix("OwnedCarPad_", "OwnedCarSign_");

            var economy = Object.FindAnyObjectByType<EconomyManager>();
            var carDelivery = Object.FindAnyObjectByType<CarDeliverySystem>();

            var systemGo = new GameObject("OwnedCarSystem");
            var system = systemGo.AddComponent<OwnedCarSystem>();
            system.Configure(economy, carDelivery);

            BuildStarterCar(system);
            BuildStationPads(system, carDelivery);
            BuildTestDriveLoop(system);

            var saveManager = Object.FindAnyObjectByType<SaveManager>();
            saveManager?.ConfigureOwnedCar(system);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Overhaul] Owned car built: starter car, system, 4 station pads, and workshop test loop.");
        }

        // ------------------------------------------------------------------ starter car

        private static void BuildStarterCar(OwnedCarSystem system)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{K}/Cars/hatchback-sports.fbx");
            if (asset == null)
            {
                Debug.LogError("[Overhaul] hatchback-sports.fbx missing; starter car not built.");
                return;
            }

            var car = (GameObject)Object.Instantiate(asset);
            car.name = "StarterCar";
            car.transform.SetPositionAndRotation(StarterCarPosition, Quaternion.Euler(0f, StarterCarYaw, 0f));

            var carsMat = AssetDatabase.LoadAssetAtPath<Material>(CarsMatPath);
            if (carsMat != null)
                foreach (var renderer in car.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = carsMat;

            // Tap-select needs a collider before the claim; ArcadeVehicleController's
            // EnsurePhysics reuses this same box once the car becomes drivable.
            var renderers = car.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                var box = car.AddComponent<BoxCollider>();
                box.center = car.transform.InverseTransformPoint(bounds.center);
                box.size = new Vector3(bounds.size.x * 0.9f, Mathf.Max(0.45f, bounds.size.y * 0.95f), bounds.size.z * 0.9f);
            }

            var view = car.AddComponent<OwnedCarView>();
            view.Configure(system);
        }

        // ------------------------------------------------------------------ station pads

        private static void BuildStationPads(OwnedCarSystem system, CarDeliverySystem carDelivery)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(HudSetup.RoundedFontPath)
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                       ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            foreach (var def in Pads)
            {
                var station = GameObject.Find(def.StationName) ?? GameObject.Find(def.FallbackStationName);
                if (station == null)
                {
                    Debug.LogWarning($"[Overhaul] station not found for pad: {def.StationName}");
                    continue;
                }

                Vector3 c = station.transform.position;
                // Offset to the side of the station mouth so the pad never blocks the
                // service lane a customer car drives into (roads/bays stay clear).
                Vector3 padPos = c + new Vector3(3.2f, 0.05f, -3.2f);

                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = $"OwnedCarPad_{def.Role}";
                pad.transform.SetParent(station.transform, true);
                pad.transform.position = padPos;
                pad.transform.localScale = new Vector3(1.2f, 0.03f, 1.2f);
                Paint(pad, def.Color, $"OwnedCar_Pad_{def.Role}");

                var component = pad.AddComponent<OwnedCarStationPad>();
                component.Configure(def.Role, system, carDelivery);

                BuildSign(def, padPos, station.transform, font);
            }
        }

        private static void BuildSign(PadDef def, Vector3 padPos, Transform parent, Font font)
        {
            var signGo = new GameObject($"OwnedCarSign_{def.Role}", typeof(Canvas), typeof(CanvasScaler));
            signGo.transform.SetParent(parent, true);
            signGo.transform.position = padPos + new Vector3(0f, 1.6f, 0f);
            signGo.transform.rotation = Quaternion.identity;
            signGo.transform.localScale = Vector3.one * 0.01f;
            var signRect = (RectTransform)signGo.transform;
            signRect.sizeDelta = new Vector2(200f, 54f);
            signGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(signGo.transform, false);
            HudSetup.Stretch(bg.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            bg.GetComponent<Image>().color = new Color(def.Color.r, def.Color.g, def.Color.b, 0.95f);

            var label = HudSetup.MakeLabel(signGo.transform, font, "Label", def.Label, 28, FontStyle.Bold, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)label.transform, Vector2.zero, Vector2.zero);
        }

        // ------------------------------------------------------------------ test-drive loop

        private static void BuildTestDriveLoop(OwnedCarSystem system)
        {
            var root = new GameObject("WorkshopTestDriveLoop");
            var loop = root.AddComponent<WorkshopTestDriveLoop>();
            loop.Configure(system, 3);

            // Ordered around the existing city road rectangle. The thin dimension is
            // the travel axis; the wide dimension spans the lane. All visuals have
            // their colliders removed, leaving only the trigger volume non-blocking.
            BuildLapGate(root.transform, loop, 0, "START", new Vector3(20f, 0.1f, -6.5f), new Vector3(1.5f, 2.5f, 8f), new Color(0.20f, 0.90f, 0.65f));
            BuildLapGate(root.transform, loop, 1, "CP 1", new Vector3(37.5f, 0.1f, 6.5f), new Vector3(8f, 2.5f, 1.5f), new Color(0.25f, 0.65f, 1f));
            BuildLapGate(root.transform, loop, 2, "CP 2", new Vector3(20f, 0.1f, 19.5f), new Vector3(1.5f, 2.5f, 8f), new Color(0.25f, 0.65f, 1f));
            BuildLapGate(root.transform, loop, 3, "CP 3", new Vector3(-37.5f, 0.1f, 6.5f), new Vector3(8f, 2.5f, 1.5f), new Color(0.25f, 0.65f, 1f));
        }

        private static void BuildLapGate(Transform parent, WorkshopTestDriveLoop loop, int index, string label,
            Vector3 position, Vector3 triggerSize, Color color)
        {
            var gate = new GameObject(index == 0 ? "TestLoop_StartFinish" : $"TestLoop_Checkpoint{index}",
                typeof(BoxCollider), typeof(WorkshopLapGate));
            gate.transform.SetParent(parent, false);
            gate.transform.position = position;
            var box = gate.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = triggerSize;
            box.center = Vector3.up * (triggerSize.y * 0.5f);
            gate.GetComponent<WorkshopLapGate>().Configure(loop, index);

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "RoadMarking";
            stripe.transform.SetParent(gate.transform, false);
            stripe.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            stripe.transform.localScale = new Vector3(triggerSize.x, 0.04f, triggerSize.z);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
            Paint(stripe, color, $"TestLoop_{label}");

            var font = AssetDatabase.LoadAssetAtPath<Font>(HudSetup.RoundedFontPath)
                       ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var sign = new GameObject("GateLabel", typeof(Canvas));
            sign.transform.SetParent(gate.transform, false);
            sign.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            sign.transform.localScale = Vector3.one * 0.01f;
            var rect = (RectTransform)sign.transform;
            rect.sizeDelta = new Vector2(180f, 44f);
            sign.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var text = HudSetup.MakeLabel(sign.transform, font, "Text", label, 25, FontStyle.Bold, Color.white);
            text.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)text.transform, Vector2.zero, Vector2.zero);
        }

        // ------------------------------------------------------------------ helpers

        private static void Paint(GameObject go, Color color, string name)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var mat = new Material(Shader.Find("Standard")) { name = name };
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        private static void DestroyAllNamed(params string[] names)
        {
            var nameSet = new HashSet<string>(names);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null) continue;
                if (go.scene.IsValid() && nameSet.Contains(go.name))
                    Object.DestroyImmediate(go);
            }
        }

        private static void DestroyAllByPrefix(params string[] prefixes)
        {
            var doomed = new List<GameObject>();
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || !go.scene.IsValid()) continue;
                foreach (var prefix in prefixes)
                    if (go.name.StartsWith(prefix)) { doomed.Add(go); break; }
            }
            foreach (var go in doomed)
                if (go != null) Object.DestroyImmediate(go);
        }
    }
}

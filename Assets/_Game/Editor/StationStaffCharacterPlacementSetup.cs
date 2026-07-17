using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overhaul.EditorTools
{
    /// <summary>Places authored staff character models near their matching station buildings.</summary>
    public static class StationStaffCharacterPlacementSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/CityGarage.unity";
        private const string CharacterDir = "Assets/_Game/Art/Models/Characters";
        private const string StaffRootName = "StaffCharacter";
        private const float TargetHeight = 1.55f;
        private const float GroundOffset = 0.12f;

        private readonly struct StaffDef
        {
            public readonly string StationName;
            public readonly string ModelFile;
            public readonly Vector3 LocalPosition;
            public readonly float Yaw;

            public StaffDef(string stationName, string modelFile, Vector3 localPosition, float yaw)
            {
                StationName = stationName;
                ModelFile = modelFile;
                LocalPosition = localPosition;
                Yaw = yaw;
            }
        }

        private static readonly StaffDef[] Staff =
        {
            new("Station_PARTS_DELIVERY", "delivery_part_character_1k_real.glb", new Vector3(-2.8f, GroundOffset, -3.95f), 180f),
            new("Station_CUSTOMER_QUEUE", "customer_queue_character_1k_real.glb", new Vector3(-3.0f, GroundOffset, -3.95f), 180f),
            new("Station_BASIC_CHANGE_BAY", "change_bay_character_1k_real.glb", new Vector3(2.85f, GroundOffset, -3.95f), 180f),
            new("Station_WHEEL_&_TIRE_STATION", "wheel_tire_character_1k_real.glb", new Vector3(2.85f, GroundOffset, -3.95f), 180f),
            new("Station_CAR_WASH", "car_wash_character_1k_real.glb", new Vector3(-2.85f, GroundOffset, -3.95f), 180f),
            new("Station_DETAILING_STATION", "detailing_station_character_1k_real.glb", new Vector3(2.85f, GroundOffset, -3.95f), 180f),
            new("Station_DIAGNOSTIC_STATION", "diagnostic_station_character_1k_real.glb", new Vector3(-2.85f, GroundOffset, -3.95f), 180f),
            new("Station_TUNING_STATION", "tuning_station_character_1k_real.glb", new Vector3(2.85f, GroundOffset, -3.95f), 180f),
            new("Station_ENGINE_WORKSHOP", "engine_workshop_character_1k_real.glb", new Vector3(-2.85f, GroundOffset, -3.95f), 180f),
            new("Station_BODY_REPAIR", "body_repair_character_1k_real.glb", new Vector3(2.85f, GroundOffset, -3.95f), 180f),
            new("Station_VEHICLE_INSPECTION", "vehicle_inspection_character_1k_real.glb", new Vector3(-2.85f, GroundOffset, -3.95f), 180f),
            new("Station_USED_CAR_SHOWROOM", "used_car_showroom_character_1k_real.glb", new Vector3(-2.85f, GroundOffset, -3.95f), 180f),
            new("Station_PREMIUM_CAR_SHOWROOM", "premium_car_showroom_character_1k_real.glb", new Vector3(2.85f, GroundOffset, -3.95f), 180f),
            new("Station_EMPLOYEE_ROOM", "employee_room_character_1k_real.glb", new Vector3(2.85f, GroundOffset, -3.95f), 180f),
            new("Station_OFFICE_&_FINANCE", "office_finance_character_1k_real.glb", new Vector3(-2.85f, GroundOffset, -3.95f), 180f),
        };

        [MenuItem("Overhaul/Place Station Staff Characters")]
        public static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            var placed = ApplyToOpenScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Overhaul] Placed {placed} station staff characters.");
        }

        public static int ApplyToOpenScene()
        {
            var placed = 0;
            foreach (var staff in Staff)
            {
                var station = GameObject.Find(staff.StationName);
                if (station == null)
                {
                    Debug.LogWarning($"[Overhaul] {staff.StationName} was not found; skipping staff character.");
                    continue;
                }

                RemoveExistingStaff(station.transform);

                var modelPath = $"{CharacterDir}/{staff.ModelFile}";
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelAsset == null)
                {
                    Debug.LogWarning($"[Overhaul] Staff character model is missing: {modelPath}");
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(modelAsset, station.transform) as GameObject;
                if (instance == null)
                {
                    Debug.LogWarning($"[Overhaul] Could not instantiate staff character for {staff.StationName}.");
                    continue;
                }

                instance.name = $"{StaffRootName}_{staff.StationName.Substring("Station_".Length)}";
                instance.transform.localPosition = staff.LocalPosition;
                instance.transform.localRotation = Quaternion.Euler(0f, staff.Yaw, 0f);
                instance.transform.localScale = Vector3.one;
                FitToHeight(instance, TargetHeight, GroundOffset);

                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(collider);

                EditorUtility.SetDirty(station);
                placed++;
            }

            return placed;
        }

        private static void RemoveExistingStaff(Transform station)
        {
            for (var i = station.childCount - 1; i >= 0; i--)
            {
                var child = station.GetChild(i);
                if (child.name == StaffRootName || child.name.StartsWith("StaffCharacter_", System.StringComparison.Ordinal))
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void FitToHeight(GameObject instance, float targetHeight, float groundOffset)
        {
            if (!TryGetRendererBounds(instance, out var bounds)) return;

            var scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
            instance.transform.localScale = Vector3.one * scale;

            if (!TryGetRendererBounds(instance, out bounds)) return;
            var stationY = instance.transform.parent.position.y;
            instance.transform.position += Vector3.up * (stationY + groundOffset - bounds.min.y);
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overhaul.EditorTools
{
    /// <summary>Installs the authored Employee Room model without replacing station gameplay.</summary>
    public static class EmployeeRoomModelSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/CityGarage.unity";
        private const string ModelPath = "Assets/_Game/Art/Models/Buildings/employee_room_1k_real.glb";
        private const string ModelRootName = "EmployeeRoomModel";
        private const float TargetWidth = 7.4f;
        private const float GroundOffset = 0.1f;

        [MenuItem("Overhaul/Apply Employee Room Model")]
        public static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            if (!ApplyToOpenScene()) return;

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Overhaul] Employee Room GLB model applied.");
        }

        public static bool ApplyToOpenScene()
        {
            var station = GameObject.Find("Station_EMPLOYEE_ROOM");
            if (station == null)
            {
                Debug.LogWarning("[Overhaul] Station_EMPLOYEE_ROOM was not found.");
                return false;
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"[Overhaul] Employee Room model is missing: {ModelPath}");
                return false;
            }

            RemoveGeneratedVisuals(station.transform);

            var instance = PrefabUtility.InstantiatePrefab(modelAsset, station.transform) as GameObject;
            if (instance == null)
            {
                Debug.LogWarning("[Overhaul] Could not instantiate the Employee Room model.");
                return false;
            }

            instance.name = ModelRootName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            FitToStation(instance, TargetWidth, GroundOffset);

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            EditorUtility.SetDirty(station);
            return true;
        }

        private static void RemoveGeneratedVisuals(Transform station)
        {
            for (var i = station.childCount - 1; i >= 0; i--)
            {
                var child = station.GetChild(i);
                var name = child.name;
                if (name == ModelRootName || name == "employee_room_1k_real" ||
                    name == "Shell" || name == "Sign_EMPLOYEE_ROOM" ||
                    name == "ParkedCar" || name == "cone" || name == "SliceDressing")
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void FitToStation(GameObject instance, float targetWidth, float groundOffset)
        {
            if (!TryGetRendererBounds(instance, out var bounds)) return;

            var scale = targetWidth / Mathf.Max(0.001f, bounds.size.x);
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

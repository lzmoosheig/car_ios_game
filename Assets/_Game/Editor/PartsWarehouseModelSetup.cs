using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overhaul.EditorTools
{
    /// <summary>Installs the authored Parts Warehouse model without replacing station gameplay.</summary>
    [InitializeOnLoad]
    public static class PartsWarehouseModelSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/CityGarage.unity";
        private const string ModelPath = "Assets/_Game/Art/Models/Buildings/parts_warehouse_1k_real.glb";
        private const string ModelRootName = "PartsWarehouseModel";
        private const string SessionAppliedKey = "Overhaul.PartsWarehouseModelSetup.Applied";
        private const float TargetWidth = 7.4f;
        private const float GroundOffset = 0.1f;

        static PartsWarehouseModelSetup()
        {
            EditorApplication.delayCall += ApplyToCurrentSceneOnce;
            EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += ApplyToCurrentSceneOnce;
        }

        [MenuItem("Overhaul/Apply Parts Warehouse Model")]
        public static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            if (!ApplyToOpenScene()) return;

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Overhaul] Parts Warehouse GLB model applied.");
        }

        private static void ApplyToCurrentSceneOnce()
        {
            if (SessionState.GetBool(SessionAppliedKey, false) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;

            if (!ApplyToOpenScene())
                return;

            SessionState.SetBool(SessionAppliedKey, true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Overhaul] Parts Warehouse GLB model auto-applied.");
        }

        public static bool ApplyToOpenScene()
        {
            var station = GameObject.Find("Station_PARTS_WAREHOUSE");
            if (station == null)
            {
                Debug.LogWarning("[Overhaul] Station_PARTS_WAREHOUSE was not found.");
                return false;
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"[Overhaul] Parts Warehouse model is missing: {ModelPath}");
                return false;
            }

            RemoveGeneratedVisuals(station.transform);

            var instance = PrefabUtility.InstantiatePrefab(modelAsset, station.transform) as GameObject;
            if (instance == null)
            {
                Debug.LogWarning("[Overhaul] Could not instantiate the Parts Warehouse model.");
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
                if (name == ModelRootName || name == "parts_warehouse_1k_real" ||
                    name == "Shell" || name == "Sign_PARTS_WAREHOUSE" ||
                    name == "crate" || name == "box" || name == "crate-item" ||
                    name.StartsWith("DisplayAwning") || name.StartsWith("DisplayShelf") ||
                    name.StartsWith("DisplayProp"))
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

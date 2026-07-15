using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overhaul.EditorTools
{
    public static class FixRoadTiles
    {
        private const string ScenePath = "Assets/_Game/Scenes/CityGarage.unity";
        private const string K = "Assets/_Game/Art/Models/Kenney";
        private const string MatDir = "Assets/_Game/Art/Materials/Kenney";
        private static readonly Vector3 LongRoadScale = new Vector3(70f, 1f, 5f);
        private static readonly Vector3 SideRoadScale = new Vector3(46.5f, 1f, 5f);
        private static readonly Vector3 JunctionScale = new Vector3(5f, 1f, 5f);
        private static readonly float[] StreetZ = { 19.5f, 6.5f, -6.5f };

        [MenuItem("Overhaul/Fix Road Tiles")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
                scene = EditorSceneManager.GetActiveScene();
            }

            RemoveBadRoadLayer(scene);
            BuildRoadLayer();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Overhaul] Rebuilt CityGarage road layer with clean asphalt lanes and sparse Kenney connectors.");
        }

        private static void RemoveBadRoadLayer(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (IsRoadLayerObject(root.name))
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static bool IsRoadLayerObject(string name)
        {
            return name.StartsWith("St19.5_")
                || name.StartsWith("St6.5_")
                || name.StartsWith("St-6.5_")
                || name.StartsWith("Ave")
                || name.StartsWith("Front_")
                || name == "road-straight"
                || name == "road-intersection"
                || name.StartsWith("DBG_road")
                || name.StartsWith("Road_")
                || name.StartsWith("RoadLong_")
                || name.StartsWith("RoadSide_")
                || name.StartsWith("RoadJoin_")
                || name.StartsWith("RoadBend_")
                || name.StartsWith("RoadEnd_")
                || name.StartsWith("RoadCrossing_");
        }

        private static void BuildRoadLayer()
        {
            var roadsMat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Roads.mat");

            foreach (float z in StreetZ)
            {
                PlaceRoadModel($"{K}/Roads/road-straight.fbx", roadsMat, new Vector3(0.2f, 0.1f, z), 0f, LongRoadScale, $"RoadLong_{z}");
            }

            foreach (float x in new[] { -37.5f, 37.5f })
            {
                PlaceRoadModel($"{K}/Roads/road-straight.fbx", roadsMat, new Vector3(x, 0.1f, 7.5f), 90f, SideRoadScale, $"RoadSide_{x}");
            }

            PlaceRoadModel($"{K}/Roads/road-straight.fbx", roadsMat, new Vector3(0.2f, 0.1f, -16.5f), 0f, LongRoadScale, "RoadLong_Front");

            foreach (float x in new[] { -37.5f, 37.5f })
            {
                foreach (float z in StreetZ)
                {
                    float yaw = x < 0f ? 90f : 270f;
                    PlaceRoadModel($"{K}/Roads/road-intersection.fbx", roadsMat, new Vector3(x, 0.1f, z), yaw, JunctionScale, $"RoadJoin_{x}_{z}");
                }
            }

            PlaceRoadModel($"{K}/Roads/road-bend.fbx", roadsMat, new Vector3(-37.5f, 0.1f, -16.5f), 180f, JunctionScale, "RoadBend_LeftFront");
            PlaceRoadModel($"{K}/Roads/road-bend.fbx", roadsMat, new Vector3(37.5f, 0.1f, -16.5f), 90f, JunctionScale, "RoadBend_RightFront");
            PlaceRoadModel($"{K}/Roads/road-end.fbx", roadsMat, new Vector3(-37.5f, 0.1f, 30f), 0f, JunctionScale, "RoadEnd_LeftNorth");
            PlaceRoadModel($"{K}/Roads/road-end.fbx", roadsMat, new Vector3(37.5f, 0.1f, 30f), 0f, JunctionScale, "RoadEnd_RightNorth");
            PlaceRoadModel($"{K}/Roads/road-crossing.fbx", roadsMat, new Vector3(-37.5f, 0.105f, 6.5f), 90f, JunctionScale, "RoadCrossing_Entrance");
            PlaceRoadModel($"{K}/Roads/road-crossing.fbx", roadsMat, new Vector3(37.5f, 0.105f, 6.5f), 90f, JunctionScale, "RoadCrossing_Exit");
        }

        private static void PlaceRoadModel(string path, Material mat, Vector3 targetPos, float rotY, Vector3 scale, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[Overhaul] road model missing: {path}");
                return;
            }

            var go = (GameObject)Object.Instantiate(asset);
            go.name = name;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, rotY, 0f));
            go.transform.localScale = scale;
            if (mat != null) ApplyMat(go, mat);
            GroundAt(go, targetPos);
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

        private static void GroundAt(GameObject go, Vector3 targetPos)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                go.transform.position = targetPos;
                return;
            }

            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            go.transform.position += new Vector3(targetPos.x - b.center.x, targetPos.y - b.min.y, targetPos.z - b.center.z);
        }

    }
}

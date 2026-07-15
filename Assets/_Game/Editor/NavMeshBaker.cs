using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;      // NavMeshData, NavMeshCollectGeometry
using Unity.AI.Navigation; // NavMeshSurface, CollectObjects

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Bakes the walkable surface for employees (Doc 02 §4.5: workers depend on a NavMesh).
    /// Built from physics colliders, so it carves around the building mesh colliders added
    /// by <see cref="CityGarageFixups"/> - workers path around the shells instead of
    /// through them.
    ///
    /// Separate from the scene builder on purpose: baking must not require a full rebuild,
    /// which would regenerate the road layout.
    /// </summary>
    public static class NavMeshBaker
    {
        private const string NavMeshAssetPath = "Assets/_Game/Scenes/CityGarage_NavMesh.asset";

        [MenuItem("Overhaul/Bake Employee NavMesh")]
        public static void Bake()
        {
            var scene = EditorSceneManager.GetActiveScene();

            var surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                var go = new GameObject("NavMesh");
                surface = go.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            surface.BuildNavMesh();

            if (surface.navMeshData == null)
            {
                Debug.LogError("[Overhaul] NavMesh bake produced no data.");
                return;
            }

            // Persist the baked data so it survives play/reload without re-baking.
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath);
            if (existing == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(NavMeshAssetPath));
                AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);
            }
            else if (existing != surface.navMeshData)
            {
                EditorUtility.CopySerialized(surface.navMeshData, existing);
                surface.navMeshData = existing;
            }

            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            var tri = NavMesh.CalculateTriangulation();
            Debug.Log($"[Overhaul] NavMesh baked: {tri.vertices.Length} verts, {tri.indices.Length / 3} tris -> {NavMeshAssetPath}");
        }
    }
}

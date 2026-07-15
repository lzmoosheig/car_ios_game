using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Repairs the OPEN CityGarage scene in place. Deliberately not part of
    /// CityGarageSceneBuilder.Build(), which does a full rebuild and would regenerate the
    /// road layout; these fixes must be applicable without touching the roads.
    ///
    /// 1. Station signs: swap the built-in GUI/Text Shader material (ZTest Always, so the
    ///    labels drew through buildings and mirrored from behind) for a depth-tested one.
    /// 2. Buildings: give the shells mesh colliders so the player can no longer walk
    ///    through them. Mesh (not box) colliders, because the garage shells have open
    ///    fronts a box would seal.
    /// </summary>
    public static class CityGarageFixups
    {
        public const string SignMatPath = "Assets/_Game/Art/Materials/SignText.mat";
        private const string ShaderName = "Overhaul/DepthTestedText";

        [MenuItem("Overhaul/Fix Signs and Building Colliders")]
        public static void Apply()
        {
            int signs = FixSigns();
            int colliders = AddBuildingColliders();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Overhaul] Fixups applied: {signs} sign labels depth-tested, " +
                      $"{colliders} building colliders added. Roads untouched.");
        }

        /// <summary>Creates (or refreshes) the shared depth-tested sign material.</summary>
        public static Material EnsureSignMaterial(Font font)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SignMatPath);
            if (mat == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    Debug.LogError($"[Overhaul] shader '{ShaderName}' not found; is the .shader imported?");
                    return null;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(SignMatPath));
                mat = new Material(shader) { name = "SignText" };
                AssetDatabase.CreateAsset(mat, SignMatPath);
            }

            if (font != null && font.material != null) mat.mainTexture = font.material.mainTexture;
            mat.color = Color.white;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static int FixSigns()
        {
            var labels = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include);
            if (labels.Length == 0) return 0;

            var font = labels[0].font;
            var mat = EnsureSignMaterial(font);
            if (mat == null) return 0;

            int n = 0;
            foreach (var tm in labels)
            {
                var r = tm.GetComponent<MeshRenderer>();
                if (r == null) continue;
                r.sharedMaterial = mat;
                EditorUtility.SetDirty(r);
                n++;
            }

            // One syncer keeps the shared material pointed at the live font atlas.
            var syncGo = Object.FindFirstObjectByType<SignFontSync>();
            if (syncGo == null)
            {
                var go = new GameObject("SignFontSync");
                syncGo = go.AddComponent<SignFontSync>();
            }
            syncGo.Configure(mat, font);
            EditorUtility.SetDirty(syncGo);
            return n;
        }

        private static int AddBuildingColliders()
        {
            int added = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                bool isShell = t.name == "Shell";
                bool isCityBuilding = t.parent != null && t.parent.name == "City_Buildings";
                if (!isShell && !isCityBuilding) continue;
                added += AddMeshColliders(t.gameObject);
            }
            return added;
        }

        private static int AddMeshColliders(GameObject root)
        {
            int n = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;

                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;   // static geometry: concave is fine
                EditorUtility.SetDirty(mf.gameObject);
                n++;
            }
            return n;
        }
    }
}

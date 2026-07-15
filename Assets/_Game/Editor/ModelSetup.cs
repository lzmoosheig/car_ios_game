using System.IO;
using UnityEditor;
using UnityEngine;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// One-shot setup for the imported KayKit / low-poly car packs. Each pack is a single
    /// texture atlas with palette-UV meshes, so one material per pack colours everything.
    /// Materials use the Standard shader (Built-in pipeline is active in this prototype);
    /// the art pass will convert to URP (Doc 07 P1-20). Also probes model bounds so the
    /// scene builder can normalise car scale.
    /// </summary>
    public static class ModelSetup
    {
        public const string MatDir = "Assets/_Game/Art/Materials";
        public const string CarsMat = MatDir + "/Cars.mat";
        public const string CityMat = MatDir + "/City.mat";
        public const string NatureMat = MatDir + "/Nature.mat";

        [MenuItem("Overhaul/Setup Model Materials")]
        public static void Run()
        {
            Directory.CreateDirectory(MatDir);
            MakeAtlas("Cars", "Assets/_Game/Art/Models/Cars/texture-palette.png", CarsMat);
            MakeAtlas("City", "Assets/_Game/Art/Models/City/citybits_texture.png", CityMat);
            MakeAtlas("Nature", "Assets/_Game/Art/Models/Nature/forest_texture.png", NatureMat);
            AssetDatabase.SaveAssets();

            Probe("Assets/_Game/Art/Models/Cars/ghini.fbx");
            Probe("Assets/_Game/Art/Models/Cars/jeep.fbx");
            Probe("Assets/_Game/Art/Models/City/building_A.fbx");
            Probe("Assets/_Game/Art/Models/City/road_straight.fbx");
            Probe("Assets/_Game/Art/Models/Nature/Tree_1_A_Color1.fbx");
            Debug.Log("[Overhaul] ModelSetup complete.");
        }

        private static void MakeAtlas(string name, string atlasPath, string matPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            if (tex == null) { Debug.LogWarning($"[Overhaul] atlas missing: {atlasPath}"); }

            var mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log($"[Overhaul] material {name} -> atlas {(tex != null ? tex.name : "NULL")}");
        }

        private static void Probe(string fbxPath)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (go == null) { Debug.Log($"[Probe] MISSING {fbxPath}"); return; }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
            var rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                Debug.Log($"[Probe] {Path.GetFileName(fbxPath)}: NO renderers");
            }
            else
            {
                var b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                Debug.Log($"[Probe] {Path.GetFileName(fbxPath)}: size={b.size} submeshes~{rs.Length}");
            }
            Object.DestroyImmediate(inst);
        }
    }
}

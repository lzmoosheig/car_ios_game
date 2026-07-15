using System.IO;
using UnityEditor;
using UnityEngine;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// One-shot material setup for the curated Kenney packs under
    /// Assets/_Game/Art/Models/Kenney/&lt;Pack&gt;/. Most packs are a single palette
    /// atlas (colormap.png) -> one Standard material per pack. Two packs
    /// (Racing, Nature) ship baked vertex colors instead -> Overhaul/VertexColorLit.
    /// Built-in render pipeline is active in this project (Doc 07 P1-20 covers the
    /// eventual URP conversion).
    /// </summary>
    public static class KenneyModelSetup
    {
        private const string Root = "Assets/_Game/Art/Models/Kenney";
        private const string MatDir = "Assets/_Game/Art/Materials/Kenney";

        // pack folder -> material name
        private static readonly (string pack, bool vertexColor)[] Packs =
        {
            ("Roads", false), ("Commercial", false), ("Suburban", false),
            ("Industrial", false), ("Modular", false), ("Cars", false), ("Platformer", false),
            ("Racing", true), ("Nature", true), ("Characters", false), // characters handled separately (per-letter textures)
        };

        [MenuItem("Overhaul/Setup Kenney Materials")]
        public static void Run()
        {
            Directory.CreateDirectory(MatDir);

            foreach (var (pack, vertexColor) in Packs)
            {
                if (pack == "Characters") { MakeCharacterMaterial(); continue; }
                MakeAtlasOrVertexMaterial(pack, vertexColor);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Overhaul] Kenney material setup complete.");
        }

        private static void MakeAtlasOrVertexMaterial(string pack, bool vertexColor)
        {
            string matPath = $"{MatDir}/{pack}.mat";
            Material mat;

            if (vertexColor)
            {
                var shader = Shader.Find("Overhaul/VertexColorLit");
                mat = new Material(shader);
            }
            else
            {
                string atlasPath = $"{Root}/{pack}/colormap.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                if (tex == null) Debug.LogWarning($"[Overhaul] atlas missing: {atlasPath}");
                mat = new Material(Shader.Find("Standard"));
                mat.mainTexture = tex;
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            }

            AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log($"[Overhaul] material {pack} -> {(vertexColor ? "vertex color" : "atlas")}");
        }

        [MenuItem("Overhaul/Probe Kenney Bounds")]
        public static void ProbeBounds()
        {
            Probe($"{Root}/Roads/road-straight.fbx");
            Probe($"{Root}/Roads/road-intersection.fbx");
            Probe($"{Root}/Roads/road-side.fbx");
            Probe($"{Root}/Commercial/low-detail-building-a.fbx");
            Probe($"{Root}/Commercial/building-skyscraper-a.fbx");
            Probe($"{Root}/Suburban/building-type-a.fbx");
            Probe($"{Root}/Suburban/tree-small.fbx");
            Probe($"{Root}/Industrial/building-a.fbx");
            Probe($"{Root}/Modular/building-sample-tower-a.fbx");
            Probe($"{Root}/Modular/building-sample-house-a.fbx");
            Probe($"{Root}/Racing/pitsGarage.fbx");
            Probe($"{Root}/Racing/pitsOffice.fbx");
            Probe($"{Root}/Racing/roadPitGarage.fbx");
            Probe($"{Root}/Racing/barrierWhite.fbx");
            Probe($"{Root}/Cars/sedan.fbx");
            Probe($"{Root}/Cars/wheel-default.fbx");
            Probe($"{Root}/Cars/box.fbx");
            Probe($"{Root}/Characters/character-a.fbx");
            Probe($"{Root}/Platformer/crate.fbx");
            Probe($"{Root}/Nature/tree_default.fbx");
            Debug.Log("[Overhaul] Kenney bounds probe complete.");
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
                Debug.Log($"[Probe] {Path.GetFileName(fbxPath)}: size={b.size} center={b.center}");
            }
            Object.DestroyImmediate(inst);
        }

        private static void MakeCharacterMaterial()
        {
            // Characters use per-letter textures (texture-a..texture-r); default the
            // shared material to texture-a (player) and let per-instance overrides
            // swap textures for customer/employee variants.
            string texPath = $"{Root}/Characters/texture-a.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            string matPath = $"{MatDir}/Characters.mat";
            AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log("[Overhaul] material Characters -> texture-a (default)");
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>Targeted, idempotent pass for carried-part visuals, sprint controls,
    /// and per-building third-person need bubbles. It does not rebuild roads or stations.</summary>
    public static class GameplayClaritySetup
    {
        private const string WheelPath = "Assets/_Game/Art/Models/Kenney/Cars/wheel-default.fbx";
        private const string CarsMaterialPath = "Assets/_Game/Art/Materials/Kenney/Cars.mat";

        [MenuItem("Overhaul/Apply Character and Building Clarity Polish")]
        public static void Apply()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[Overhaul] PlayerController was not found in the open scene.");
                return;
            }

            player.SetMoveSpeed(4.8f);
            if (player.GetComponent<PlayerSprintHud>() == null) player.gameObject.AddComponent<PlayerSprintHud>();

            var carrier = player.GetComponent<CarrierView>();
            if (carrier != null)
            {
                carrier.ConfigureVisualLayout(0.24f, 0.72f, new Vector3(0f, 0f, 90f));
                var anchor = player.transform.Find("StackAnchor");
                if (anchor != null) anchor.localPosition = new Vector3(0.48f, 0.78f, -0.38f);
                EditorUtility.SetDirty(carrier);
            }

            bool tireReady = UpgradeCarriedItemTemplate();
            bool bayPolished = BasicChangeBayPolishSetup.ApplyToOpenScene();
            bool deliveryPolished = PartsDeliveryModelSetup.ApplyToOpenScene();
            int bubbles = 0;
            foreach (var building in Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Include))
            {
                if (building.GetComponent<BuildingNeedBubble>() == null)
                    building.gameObject.AddComponent<BuildingNeedBubble>();
                EditorUtility.SetDirty(building.gameObject);
                bubbles++;
            }

            EditorUtility.SetDirty(player.gameObject);
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Overhaul] Clarity polish applied: sprint toggle, {bubbles} building bubbles, " +
                      $"carried tire model={(tireReady ? "ready" : "missing")}, " +
                      $"basic bay detail={(bayPolished ? "ready" : "missing")}, " +
                      $"parts delivery detail={(deliveryPolished ? "ready" : "missing")}.");
        }

        private static bool UpgradeCarriedItemTemplate()
        {
            GameObject template = null;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                if (candidate != null && candidate.scene.IsValid() && candidate.name == "CarriedItemTemplate")
                {
                    template = candidate;
                    break;
                }
            if (template == null) return false;

            for (int i = template.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(template.transform.GetChild(i).gameObject);
            var oldCollider = template.GetComponent<Collider>();
            var oldRenderer = template.GetComponent<MeshRenderer>();
            var oldFilter = template.GetComponent<MeshFilter>();
            if (oldCollider != null) Object.DestroyImmediate(oldCollider);
            if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
            if (oldFilter != null) Object.DestroyImmediate(oldFilter);

            var wheelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WheelPath);
            if (wheelAsset == null) return false;
            var visual = (GameObject)Object.Instantiate(wheelAsset, template.transform);
            visual.name = "RealTireVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            var material = AssetDatabase.LoadAssetAtPath<Material>(CarsMaterialPath);
            if (material != null)
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = material;

            template.SetActive(false);
            EditorUtility.SetDirty(template);
            return true;
        }
    }
}

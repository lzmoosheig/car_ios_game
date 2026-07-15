using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>Adds the remaining Phase A Office upgrade and world bottleneck cues.</summary>
    public static class PhaseACompletionSetup
    {
        private const string K = "Assets/_Game/Art/Models/Kenney";

        [MenuItem("Overhaul/Complete Phase A Office and Cues")]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var bay = Object.FindAnyObjectByType<ServiceBay>();
            var village = Object.FindAnyObjectByType<VillageController>();
            var economy = Object.FindAnyObjectByType<EconomyManager>();
            if (bay == null || village == null || economy == null)
            {
                Debug.LogError("[Overhaul] Phase A gameplay objects were not found in the open scene.");
                return;
            }

            RemoveExisting("OfficeUpgradePad");
            RemoveExisting("WorldBottleneckCues");

            var manager = Object.FindAnyObjectByType<VillageUpgradeManager>();
            if (manager == null) manager = bay.gameObject.AddComponent<VillageUpgradeManager>();
            manager.Configure(economy, bay);

            BuildOfficePad(manager);
            BuildBottleneckCues(bay, village);

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Overhaul] Phase A Office pricing pad and world bottleneck cues are ready.");
        }

        private static void BuildOfficePad(VillageUpgradeManager manager)
        {
            var office = GameObject.Find("Station_OFFICE_&_FINANCE");
            Vector3 pos = office != null
                ? office.transform.position + new Vector3(0f, 0.12f, -4.5f)
                : new Vector3(27f, 0.12f, -4.5f);

            var root = new GameObject("OfficeUpgradePad");
            root.transform.position = pos;
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.15f;

            Renderer padRenderer = null;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{K}/Platformer/button-square.fbx");
            if (asset != null)
            {
                var pad = (GameObject)Object.Instantiate(asset);
                pad.name = "PricingUpgradePad";
                pad.transform.SetParent(root.transform, false);
                padRenderer = pad.GetComponentInChildren<Renderer>();
            }

            var label = CreateText("Status", root.transform, new Vector3(0f, 1.15f, 0f),
                                   Color.white, 0.042f, "SERVICE PRICING  LV 1  $150");
            var purchase = root.AddComponent<UpgradePurchasePad>();
            purchase.Configure(manager, VillageUpgradeManager.OfficePricingId,
                               "Service pricing", label, padRenderer);
        }

        private static void BuildBottleneckCues(ServiceBay bay, VillageController village)
        {
            var root = new GameObject("WorldBottleneckCues");
            var bayStation = GameObject.Find("Station_BASIC_CHANGE_BAY");
            var queueStation = GameObject.Find("Station_CUSTOMER_QUEUE");

            Vector3 bayPos = bayStation != null ? bayStation.transform.position : bay.transform.position;
            Vector3 queuePos = queueStation != null ? queueStation.transform.position : village.transform.position;
            var starved = CreateCue("BayStarvedCue", root.transform, bayPos + Vector3.up * 5.2f,
                                    new Color(1f, 0.35f, 0.2f), "!  NEED TIRES");
            var full = CreateCue("QueueFullCue", root.transform, queuePos + Vector3.up * 5.2f,
                                 new Color(1f, 0.72f, 0.15f), "!  QUEUE FULL");

            root.AddComponent<WorldBottleneckCues>().Configure(bay, village, starved, full);
        }

        private static GameObject CreateCue(string name, Transform parent, Vector3 pos,
                                            Color color, string message)
        {
            var cue = new GameObject(name);
            cue.transform.SetParent(parent, true);
            cue.transform.position = pos;
            CreateText("Label", cue.transform, Vector3.zero, color, 0.14f, message);
            cue.SetActive(false);
            return cue;
        }

        private static TextMesh CreateText(string name, Transform parent, Vector3 localPos,
                                           Color color, float characterSize, string value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var text = go.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = characterSize;
            text.color = color;
            text.fontStyle = FontStyle.Bold;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font != null) go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return text;
        }

        private static void RemoveExisting(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }
    }
}

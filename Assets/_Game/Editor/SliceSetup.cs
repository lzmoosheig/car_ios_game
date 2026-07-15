using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Wires the first playable management slice into the OPEN CityGarage scene:
    /// tap-to-select buildings and NPCs, the shared info panel and toast, the customer/job
    /// templates, the reward pickup, and per-station visual dressing so lots read as what
    /// they are instead of white boxes.
    ///
    /// Targeted edits only - never rebuilds the scene, never touches the road layout.
    /// </summary>
    public static class SliceSetup
    {
        private const string K = "Assets/_Game/Art/Models/Kenney";
        private const string KMat = "Assets/_Game/Art/Materials/Kenney";

        [MenuItem("Overhaul/Setup Management Slice")]
        public static void Apply()
        {
            var village = Object.FindFirstObjectByType<VillageController>();
            var bay = Object.FindFirstObjectByType<ServiceBay>();
            var rack = Object.FindFirstObjectByType<ResourceRack>();
            if (village == null || bay == null)
            {
                Debug.LogError("[Overhaul] SliceSetup needs VillageController + ServiceBay in the scene.");
                return;
            }

            SeedCatalog();
            var panel = BuildUi(out _);
            BuildSelector(panel);
            var npcTemplate = BuildNpcTemplate();
            var rewardTemplate = BuildRewardTemplate();
            WireVillage(village, npcTemplate, rewardTemplate);
            int buildings = AddBuildingViews(bay, rack);
            int dressed = DressStations();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Overhaul] Slice setup: {buildings} clickable buildings, {dressed} stations dressed, " +
                      "NPC + reward templates, panel/toast/selector wired. Roads untouched.");
        }

        private static void SeedCatalog()
        {
            var catalog = Object.FindFirstObjectByType<ResourceCatalog>();
            if (catalog == null) catalog = new GameObject("ResourceCatalog").AddComponent<ResourceCatalog>();
            catalog.SeedDefaults();
            EditorUtility.SetDirty(catalog);
        }

        // ------------------------------------------------------------------ UI

        private static InfoPanelView BuildUi(out ScreenToast toast)
        {
            var hudRoot = Object.FindFirstObjectByType<HUDRoot>();
            var parent = hudRoot != null && hudRoot.SafeAreaRoot != null
                ? hudRoot.SafeAreaRoot : Object.FindFirstObjectByType<Canvas>()?.transform as RectTransform;
            if (parent == null)
            {
                Debug.LogError("[Overhaul] no HUD canvas found; run Overhaul/Build HUD first.");
                toast = null;
                return null;
            }

            var panel = parent.GetComponentInChildren<InfoPanelView>(true);
            if (panel == null)
            {
                var go = new GameObject("InfoPanel", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(18f, 18f);
                rect.sizeDelta = new Vector2(340f, 10f);
                panel = go.AddComponent<InfoPanelView>();
            }

            toast = parent.GetComponentInChildren<ScreenToast>(true);
            if (toast == null)
            {
                var go = new GameObject("Toast", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 140f);
                rect.sizeDelta = new Vector2(460f, 52f);
                toast = go.AddComponent<ScreenToast>();
            }
            return panel;
        }

        private static void BuildSelector(InfoPanelView panel)
        {
            var selector = Object.FindFirstObjectByType<InteractionSelector>();
            if (selector == null)
                selector = new GameObject("InteractionSelector").AddComponent<InteractionSelector>();
            selector.Configure(Camera.main, panel);
            EditorUtility.SetDirty(selector);
        }

        // ------------------------------------------------------------------ templates

        private static GameObject BuildNpcTemplate()
        {
            var existing = GameObject.Find("CustomerNpcTemplate");
            if (existing != null) return existing;

            var root = new GameObject("CustomerNpcTemplate");
            var visual = InstantiateModel($"{K}/Characters/character-c.fbx", $"{KMat}/Characters.mat", 1.7f);
            if (visual != null) visual.transform.SetParent(root.transform, false);

            // Generous capsule so a fingertip can hit a 1.7m person from the elevated view.
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.9f; col.radius = 0.55f; col.center = new Vector3(0f, 0.95f, 0f);

            root.AddComponent<CustomerNpc>();
            root.SetActive(false);
            return root;
        }

        private static GameObject BuildRewardTemplate()
        {
            var existing = GameObject.Find("RewardPickupTemplate");
            if (existing != null) return existing;

            var root = new GameObject("RewardPickupTemplate");
            var visual = InstantiateModel($"{K}/Platformer/coin-gold.fbx", $"{KMat}/Platformer.mat", 1.1f);
            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.transform.localScale = Vector3.one * 0.8f;
            }
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            root.AddComponent<RewardPickup>();
            root.SetActive(false);
            return root;
        }

        private static void WireVillage(VillageController village, GameObject npcTemplate, GameObject rewardTemplate)
        {
            // Reception point: beside the reception station's pad, facing the street.
            var receptionStation = GameObject.Find("Station_RECEPTION");
            var receptionPoint = GetOrMakeMarker("ReceptionPoint",
                receptionStation != null
                    ? receptionStation.transform.position + new Vector3(-2.2f, 0.12f, -4.6f)
                    : new Vector3(-31.5f, 0.12f, 8.4f),
                180f);

            // Pay marker: on the apron between bay and street — far enough from the player
            // spawn that collecting is a deliberate walk, not an accidental overlap (the
            // first placement sat 0.6m from spawn and pickups vanished instantly).
            var bay = Object.FindFirstObjectByType<ServiceBay>();
            var rewardPos = bay != null
                ? bay.transform.position + new Vector3(6.8f, 0.12f, -5.2f)
                : new Vector3(-7f, 0.12f, 6.8f);
            var rewardPoint = GetOrMakeMarker("RewardPoint", rewardPos, 0f);
            rewardPoint.position = rewardPos; // reposition if the marker already existed

            village.ConfigureJobSlice(npcTemplate, rewardTemplate, receptionPoint, rewardPoint);
            EditorUtility.SetDirty(village);
        }

        private static Transform GetOrMakeMarker(string name, Vector3 pos, float rotY)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing.transform;
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            return go.transform;
        }

        // ------------------------------------------------------------------ buildings

        /// <summary>Station name -> (input item, count per job) for panel "Needs" lines.</summary>
        private static readonly Dictionary<string, (string item, int count)> StationInputs = new()
        {
            { "BASIC CHANGE BAY", ("tire", 4) },
            { "WHEEL & TIRE STATION", ("tire", 4) },
            { "TIRE STORAGE", ("tire", 0) },
            { "PARTS DELIVERY", ("tire", 0) },
            { "CAR WASH", ("cleaning", 1) },
            { "DETAILING STATION", ("cleaning", 2) },
            { "PAINT MIXING", ("paint", 2) },
            { "PAINT BOOTH", ("paint", 2) },
            { "ENGINE WORKSHOP", ("engine", 2) },
            { "BODY REPAIR", ("panels", 2) },
        };

        private static int AddBuildingViews(ServiceBay bay, ResourceRack rack)
        {
            var zones = Object.FindObjectsByType<ConstructionZoneView>(FindObjectsInactive.Include);
            var sources = Object.FindObjectsByType<PartsSource>(FindObjectsInactive.Include);

            int count = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!t.name.StartsWith("Station_")) continue;

                string pretty = t.name.Substring("Station_".Length).Replace('_', ' ');
                var view = t.GetComponent<BuildingView>();
                if (view == null) view = t.gameObject.AddComponent<BuildingView>();

                bool isBay = pretty == "BASIC CHANGE BAY";
                StationInputs.TryGetValue(pretty, out var input);

                view.Configure(
                    ToTitleCase(pretty),
                    isBay ? bay : null,
                    isBay ? rack : null,
                    NearestTo(sources, t.position, 6f),
                    NearestZoneTo(zones, t.position, 6f),
                    input.item, input.count);

                // The pad cube already has a collider; make sure something tappable exists.
                if (t.GetComponentInChildren<Collider>() == null)
                {
                    var box = t.gameObject.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 1.5f, 0f);
                    box.size = new Vector3(7.4f, 3f, 7.4f);
                }

                EditorUtility.SetDirty(t.gameObject);
                count++;
            }
            return count;
        }

        private static PartsSource NearestTo(PartsSource[] all, Vector3 pos, float maxDist)
        {
            PartsSource best = null;
            float bestD = maxDist;
            foreach (var s in all)
            {
                if (s == null) continue;
                float d = Vector3.Distance(s.transform.position, pos);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        private static ConstructionZoneView NearestZoneTo(ConstructionZoneView[] all, Vector3 pos, float maxDist)
        {
            ConstructionZoneView best = null;
            float bestD = maxDist;
            foreach (var z in all)
            {
                if (z == null) continue;
                float d = Vector3.Distance(z.transform.position, pos);
                if (d < bestD) { bestD = d; best = z; }
            }
            return best;
        }

        private static string ToTitleCase(string upper)
        {
            var words = upper.ToLowerInvariant().Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0 && char.IsLetter(words[i][0]))
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words);
        }

        // ------------------------------------------------------------------ visual dressing

        /// <summary>
        /// Distinct props per station so lots read at a glance (user priority 9). Existing
        /// props from the original build stay; this adds only what's missing, keyed by a
        /// "SliceDressing" child so re-running never duplicates.
        /// </summary>
        private static int DressStations()
        {
            int dressed = 0;
            dressed += Dress("Station_TIRE_STORAGE", root =>
            {
                // Tire towers: black stacks along the back edge.
                for (int s = 0; s < 3; s++)
                    for (int h = 0; h < 3 - s % 2; h++)
                        Prop(root, $"{K}/Cars/wheel-default.fbx", $"{KMat}/Cars.mat",
                            new Vector3(-2.2f + s * 2.1f, 0.12f + h * 0.42f, 2.4f),
                            new Vector3(90f, s * 25f, 0f), 1.6f);
            });
            dressed += Dress("Station_PARTS_DELIVERY", root =>
            {
                Prop(root, $"{K}/Cars/box.fbx", $"{KMat}/Cars.mat", new Vector3(2.0f, 0.12f, 2.2f), new Vector3(0f, 18f, 0f), 1.4f);
                Prop(root, $"{K}/Platformer/crate.fbx", $"{KMat}/Platformer.mat", new Vector3(2.9f, 0.12f, 1.4f), new Vector3(0f, -12f, 0f), 1.2f);
                Prop(root, $"{K}/Platformer/crate.fbx", $"{KMat}/Platformer.mat", new Vector3(2.45f, 1.0f, 1.8f), new Vector3(0f, 31f, 0f), 1.0f);
            });
            dressed += Dress("Station_BASIC_CHANGE_BAY", root =>
            {
                // Reads as a working bay: cones at the mouth, scattered fasteners, a wheel
                // leaning on the wall.
                Prop(root, $"{K}/Cars/cone.fbx", $"{KMat}/Cars.mat", new Vector3(-3.0f, 0.12f, -3.2f), Vector3.zero, 1.2f);
                Prop(root, $"{K}/Cars/cone.fbx", $"{KMat}/Cars.mat", new Vector3(3.0f, 0.12f, -3.2f), Vector3.zero, 1.2f);
                Prop(root, $"{K}/Cars/debris-bolt.fbx", $"{KMat}/Cars.mat", new Vector3(-1.6f, 0.12f, 1.9f), new Vector3(0f, 40f, 0f), 1.6f);
                Prop(root, $"{K}/Cars/debris-nut.fbx", $"{KMat}/Cars.mat", new Vector3(1.4f, 0.12f, 2.2f), new Vector3(0f, -25f, 0f), 1.6f);
                Prop(root, $"{K}/Cars/wheel-default.fbx", $"{KMat}/Cars.mat", new Vector3(2.6f, 0.35f, 2.6f), new Vector3(0f, 0f, 90f), 1.5f);
            });
            dressed += Dress("Station_CAR_WASH", root =>
            {
                // Blue identity: barrels recolored + overhead accent.
                var blue = new Color(0.25f, 0.55f, 0.95f);
                TintedProp(root, $"{K}/Platformer/barrel.fbx", new Vector3(-2.4f, 0.12f, 2.3f), 1.2f, blue);
                TintedProp(root, $"{K}/Platformer/barrel.fbx", new Vector3(-1.5f, 0.12f, 2.5f), 1.1f, blue);
                AccentStrip(root, blue);
            });
            dressed += Dress("Station_PAINT_MIXING", root =>
            {
                TintedProp(root, $"{K}/Platformer/barrel.fbx", new Vector3(-2.2f, 0.12f, 2.2f), 1.1f, new Color(0.85f, 0.2f, 0.2f));
                TintedProp(root, $"{K}/Platformer/barrel.fbx", new Vector3(-1.2f, 0.12f, 2.45f), 1.1f, new Color(0.2f, 0.4f, 0.9f));
                TintedProp(root, $"{K}/Platformer/barrel.fbx", new Vector3(-1.7f, 1.0f, 2.3f), 0.95f, new Color(0.95f, 0.8f, 0.2f));
            });
            dressed += Dress("Station_PAINT_BOOTH", root =>
            {
                AccentStrip(root, new Color(0.85f, 0.25f, 0.25f));
                TintedProp(root, $"{K}/Platformer/barrel.fbx", new Vector3(2.4f, 0.12f, 2.3f), 1.0f, new Color(0.2f, 0.7f, 0.35f));
            });
            dressed += Dress("Station_ENGINE_WORKSHOP", root =>
            {
                Prop(root, $"{K}/Cars/debris-drivetrain.fbx", $"{KMat}/Cars.mat", new Vector3(-2.2f, 0.2f, 2.2f), new Vector3(0f, 15f, 0f), 1.6f);
                Prop(root, $"{K}/Platformer/crate.fbx", $"{KMat}/Platformer.mat", new Vector3(2.5f, 0.12f, 2.4f), new Vector3(0f, -20f, 0f), 1.1f);
                AccentStrip(root, new Color(0.30f, 0.32f, 0.36f));
            });
            return dressed;
        }

        private static int Dress(string stationName, System.Action<Transform> build)
        {
            var station = GameObject.Find(stationName);
            if (station == null) return 0;
            if (station.transform.Find("SliceDressing") != null) return 0; // already dressed

            var root = new GameObject("SliceDressing").transform;
            root.SetParent(station.transform, false);
            build(root);
            EditorUtility.SetDirty(station);
            return 1;
        }

        private static void Prop(Transform parent, string fbxPath, string matPath, Vector3 localPos, Vector3 euler, float scale)
        {
            var go = InstantiateModel(fbxPath, matPath, 0f);
            if (go == null) return;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = Vector3.one * scale;
        }

        private static void TintedProp(Transform parent, string fbxPath, Vector3 localPos, float scale, Color tint)
        {
            var go = InstantiateModel(fbxPath, null, 0f);
            if (go == null) return;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * scale;

            var mat = new Material(Shader.Find("Standard")) { color = tint };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.1f);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
        }

        /// <summary>A thin colored strip across the lot front - cheap identity color.</summary>
        private static void AccentStrip(Transform parent, Color color)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(strip.GetComponent<Collider>());
            strip.name = "AccentStrip";
            strip.transform.SetParent(parent, false);
            strip.transform.localPosition = new Vector3(0f, 0.14f, -3.55f);
            strip.transform.localScale = new Vector3(7.2f, 0.05f, 0.35f);
            var mat = new Material(Shader.Find("Standard")) { color = color };
            strip.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GameObject InstantiateModel(string path, string matPath, float normalizeHeight)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning($"[Overhaul] model missing: {path}"); return null; }

            var go = (GameObject)Object.Instantiate(asset);
            go.name = System.IO.Path.GetFileNameWithoutExtension(path);

            if (matPath != null)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat != null)
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    {
                        var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                        for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                        r.sharedMaterials = arr;
                    }
            }

            if (normalizeHeight > 0f)
            {
                var rs = go.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    if (b.size.y > 0.001f) go.transform.localScale *= normalizeHeight / b.size.y;
                }
            }
            return go;
        }
    }
}

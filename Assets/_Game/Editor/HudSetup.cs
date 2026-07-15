using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Builds the management HUD into the OPEN scene (no rebuild, so the roads are safe).
    /// Kept to Doc 09 §12.1: cash, gold, current objective — plus an F3 debug panel that is
    /// off by default. Legacy uGUI Text rather than TextMeshPro because TMP's essential
    /// resources are not imported in this project and that is an interactive step.
    /// </summary>
    public static class HudSetup
    {
        private const string UiDir = "Assets/_Game/Art/UI";

        [MenuItem("Overhaul/Build HUD")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            ImportIcon($"{UiDir}/cash.png");
            ImportIcon($"{UiDir}/gold.png");

            var old = GameObject.Find("HUD");
            if (old != null) Object.DestroyImmediate(old);

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // sane on both phone orientations

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // UI skin sprites live in builtin_extra, not builtin_resources, so
            // Resources.GetBuiltinResource can't see them - this is the editor-side lookup.
            var panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (panelSprite == null)
                Debug.LogWarning("[Overhaul] rounded UI sprite unavailable; pills fall back to plain rects.");

            // Currency pills, top-left, stacked.
            var cashText = MakePill(canvasGo.transform, panelSprite, font, $"{UiDir}/cash.png",
                "CashPill", new Vector2(28f, -28f), new Color(0.10f, 0.13f, 0.18f, 0.82f));
            var goldText = MakePill(canvasGo.transform, panelSprite, font, $"{UiDir}/gold.png",
                "GoldPill", new Vector2(28f, -112f), new Color(0.20f, 0.15f, 0.04f, 0.86f));

            // Objective line under the pills.
            var objective = MakeText(canvasGo.transform, font, "ObjectiveText", new Vector2(30f, -196f),
                new Vector2(860f, 38f), 28, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.95f));
            objective.text = "Serve customers";
            AddShadow(objective.gameObject);

            // Debug panel (F3), hidden by default.
            var debugRoot = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image));
            debugRoot.transform.SetParent(canvasGo.transform, false);
            var dbgRect = debugRoot.GetComponent<RectTransform>();
            Anchor(dbgRect, new Vector2(0f, 1f), new Vector2(28f, -244f), new Vector2(860f, 76f));
            var dbgBg = debugRoot.GetComponent<Image>();
            dbgBg.sprite = panelSprite;
            dbgBg.type = Image.Type.Sliced;
            dbgBg.color = new Color(0f, 0f, 0f, 0.55f);

            var debugText = MakeText(debugRoot.transform, font, "DebugText", new Vector2(12f, -8f),
                new Vector2(736f, 54f), 18, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f, 0.95f));
            debugRoot.SetActive(false);

            var eco = Object.FindFirstObjectByType<EconomyManager>();
            var village = Object.FindFirstObjectByType<VillageController>();
            var bay = Object.FindFirstObjectByType<ServiceBay>();
            var rack = Object.FindFirstObjectByType<ResourceRack>();

            var hud = canvasGo.AddComponent<HudView>();
            hud.Configure(eco, village, bay, rack, cashText, goldText, objective, debugText, debugRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Overhaul] HUD built (cash + gold pills, objective, F3 debug). Roads untouched.");
        }

        /// <summary>UI textures: sprite, no mips, modest size — Doc 09 §13.4 mobile budgets.</summary>
        private static void ImportIcon(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"[Overhaul] icon missing: {path}"); return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 256; // displayed ~56px; 1024+ would be pure waste
            importer.SaveAndReimport();
        }

        private static Text MakePill(Transform parent, Sprite panelSprite, Font font, string iconPath,
                                     string name, Vector2 pos, Color bg)
        {
            var pill = new GameObject(name, typeof(RectTransform), typeof(Image));
            pill.transform.SetParent(parent, false);
            Anchor(pill.GetComponent<RectTransform>(), new Vector2(0f, 1f), pos, new Vector2(260f, 72f));
            var img = pill.GetComponent<Image>();
            img.sprite = panelSprite;
            img.type = Image.Type.Sliced;
            img.color = bg;

            // Icon and value are centred on the pill's vertical midline, so they need a
            // left-middle pivot - not the top-left pivot the generic Anchor helper applies.
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(pill.transform, false);
            CenterLeft(icon.GetComponent<RectTransform>(), new Vector2(14f, 0f), new Vector2(52f, 52f));
            var iconImg = icon.GetComponent<Image>();
            iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var textGo = new GameObject("Value", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(pill.transform, false);
            CenterLeft(textGo.GetComponent<RectTransform>(), new Vector2(78f, 0f), new Vector2(170f, 48f));
            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "0";
            AddShadow(textGo);
            return text;
        }

        /// <summary>Anchored to the parent's left edge, vertically centred.</summary>
        private static void CenterLeft(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Text MakeText(Transform parent, Font font, string name, Vector2 pos, Vector2 size,
                                     int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Anchor(go.GetComponent<RectTransform>(), new Vector2(0f, 1f), pos, size);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void AddShadow(GameObject go)
        {
            var s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.65f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}

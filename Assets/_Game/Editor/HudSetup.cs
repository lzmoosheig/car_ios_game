using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
        internal const string UiDir = "Assets/_Game/Art/UI";
        internal const string RoundedFontPath = "Assets/_Game/Fonts/SFNSRounded.ttf";

        [MenuItem("Overhaul/Build HUD")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            ImportIcon($"{UiDir}/cash.png");
            ImportIcon($"{UiDir}/gold.png");
            ImportIcon($"{UiDir}/menu_serve.png");

            var old = GameObject.Find("HUD");
            if (old != null) Object.DestroyImmediate(old);

            EnsureEventSystem();

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // sane on both phone orientations

            var font = AssetDatabase.LoadAssetAtPath<Font>(RoundedFontPath)
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                       ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // UI skin sprites live in builtin_extra, not builtin_resources, so
            // Resources.GetBuiltinResource can't see them - this is the editor-side lookup.
            var panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var safeRoot = new GameObject("SafeArea", typeof(RectTransform));
            safeRoot.transform.SetParent(canvasGo.transform, false);
            var safeRect = safeRoot.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            canvasGo.AddComponent<HUDRoot>().Configure(safeRect);

            // ---- Currency bar: compact top-left safe-area pills ----
            var bar = new GameObject("CurrencyBar", typeof(RectTransform));
            bar.transform.SetParent(safeRoot.transform, false);
            Anchor(bar.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(22f, -20f), new Vector2(0f, 58f));
            var barLayout = bar.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 10f;
            barLayout.childAlignment = TextAnchor.MiddleLeft;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = false;
            barLayout.childForceExpandHeight = false;
            var barFitter = bar.AddComponent<ContentSizeFitter>();
            barFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            barFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var cashDisplay = MakeCurrencyItem(bar.transform, font, panelSprite, $"{UiDir}/cash.png", "Cash");
            var goldDisplay = MakeCurrencyItem(bar.transform, font, panelSprite, $"{UiDir}/gold.png", "Gold");
            var currencyBar = bar.AddComponent<CurrencyBar>();
            currencyBar.Configure(cashDisplay, goldDisplay);

            var serveButton = MakeServeCustomerButton(safeRoot.transform, font, panelSprite, $"{UiDir}/menu_serve.png");

            // Debug panel (F3), hidden by default.
            var debugRoot = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image));
            debugRoot.transform.SetParent(safeRoot.transform, false);
            var dbgRect = debugRoot.GetComponent<RectTransform>();
            Anchor(dbgRect, new Vector2(0f, 1f), new Vector2(28f, -244f), new Vector2(860f, 76f));
            var dbgBg = debugRoot.GetComponent<Image>();
            dbgBg.sprite = panelSprite;
            dbgBg.type = Image.Type.Sliced;
            dbgBg.color = new Color(0f, 0f, 0f, 0.55f);

            var debugText = MakeText(debugRoot.transform, font, "DebugText", new Vector2(12f, -8f),
                new Vector2(736f, 54f), 18, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f, 0.95f));
            debugRoot.SetActive(false);

            var eco = Object.FindAnyObjectByType<EconomyManager>();
            var village = Object.FindAnyObjectByType<VillageController>();
            var bay = Object.FindAnyObjectByType<ServiceBay>();
            var rack = Object.FindAnyObjectByType<ResourceRack>();

            var hud = canvasGo.AddComponent<HudView>();
            hud.Configure(eco, village, bay, rack, currencyBar, serveButton, debugText, debugRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Overhaul] HUD built (currency pills, serve-customer action, F3 debug). Roads untouched.");
        }

        /// <summary>UI textures: sprite, no mips, modest size — Doc 09 §13.4 mobile budgets.</summary>
        internal static void ImportIcon(string path)
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

        internal const float CurrencyIconSize = 42f;
        internal const float ServeIconButtonSize = 88f;

        internal static CurrencyDisplay MakeCurrencyItem(Transform parent, Font font, Sprite panelSprite, string iconPath, string name)
        {
            var item = new GameObject($"{name}Item", typeof(RectTransform), typeof(Image), typeof(CurrencyDisplay));
            item.transform.SetParent(parent, false);
            var bg = item.GetComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.04f, 0.055f, 0.075f, 0.72f);
            bg.raycastTarget = false;

            var layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 14, 7, 7);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = item.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(item.transform, false);
            var iconImg = icon.GetComponent<Image>();
            iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iconImg.preserveAspect = true;   // square icons stay square whatever the source
            iconImg.raycastTarget = false;
            var iconLayout = icon.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = CurrencyIconSize;
            iconLayout.preferredHeight = CurrencyIconSize;

            var textGo = new GameObject("Value", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(item.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.fontSize = 32;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "0";
            AddShadow(textGo);
            var textLayout = textGo.AddComponent<LayoutElement>();
            textLayout.minWidth = 28f;
            textLayout.preferredHeight = 42f;

            var display = item.GetComponent<CurrencyDisplay>();
            display.Configure(iconImg, text);
            return display;
        }

        private static ServeCustomerHUDButton MakeServeCustomerButton(Transform parent, Font font, Sprite panelSprite, string iconPath)
        {
            var card = new GameObject("ServeCustomerHUDButton", typeof(RectTransform), typeof(ServeCustomerHUDButton));
            card.transform.SetParent(parent, false);
            var cardRect = card.GetComponent<RectTransform>();
            Anchor(cardRect, new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(320f, ServeIconButtonSize));
            cardRect.pivot = new Vector2(1f, 0.5f);

            var layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var status = new GameObject("StatusPanel", typeof(RectTransform), typeof(Image));
            status.transform.SetParent(card.transform, false);
            var statusBg = status.GetComponent<Image>();
            statusBg.sprite = panelSprite;
            statusBg.type = Image.Type.Sliced;
            statusBg.color = new Color(0.04f, 0.055f, 0.075f, 0.76f);
            statusBg.raycastTarget = false;
            var statusLayoutElement = status.AddComponent<LayoutElement>();
            statusLayoutElement.preferredWidth = 210f;
            statusLayoutElement.preferredHeight = 74f;
            var statusLayout = status.AddComponent<VerticalLayoutGroup>();
            statusLayout.padding = new RectOffset(14, 14, 10, 9);
            statusLayout.spacing = 1f;
            statusLayout.childAlignment = TextAnchor.MiddleLeft;
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = false;
            statusLayout.childForceExpandWidth = true;
            statusLayout.childForceExpandHeight = false;

            var title = MakeLabel(status.transform, font, "Title", "Serve customer", 24, FontStyle.Bold, Color.white);
            var detail = MakeLabel(status.transform, font, "Detail", "Queue slot 4 · $80", 18, FontStyle.Bold, new Color(0.84f, 0.9f, 1f, 0.96f));

            var iconButton = new GameObject("IconButton", typeof(RectTransform), typeof(Image), typeof(Button));
            iconButton.transform.SetParent(card.transform, false);
            var buttonLayout = iconButton.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = ServeIconButtonSize;
            buttonLayout.preferredHeight = ServeIconButtonSize;
            var buttonBg = iconButton.GetComponent<Image>();
            buttonBg.sprite = panelSprite;
            buttonBg.type = Image.Type.Sliced;
            buttonBg.color = new Color(0.05f, 0.07f, 0.1f, 0.86f);
            var button = iconButton.GetComponent<Button>();
            button.targetGraphic = buttonBg;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.08f);
            colors.pressedColor = new Color(0.82f, 0.9f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(iconButton.transform, false);
            Stretch(glow.GetComponent<RectTransform>(), new Vector2(-4f, -4f), new Vector2(4f, 4f));
            var glowImage = glow.GetComponent<Image>();
            glowImage.sprite = panelSprite;
            glowImage.type = Image.Type.Sliced;
            glowImage.color = new Color(1f, 0.82f, 0.26f, 0.16f);
            glowImage.raycastTarget = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(iconButton.transform, false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(66f, 66f);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var badge = new GameObject("WaitingBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(iconButton.transform, false);
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-6f, -6f);
            badgeRect.sizeDelta = new Vector2(24f, 24f);
            var badgeBg = badge.GetComponent<Image>();
            badgeBg.sprite = panelSprite;
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = new Color(1f, 0.29f, 0.18f, 0.98f);
            badgeBg.raycastTarget = false;
            var badgeText = MakeLabel(badge.transform, font, "Text", "!", 16, FontStyle.Bold, Color.white);
            var badgeTextRect = badgeText.GetComponent<RectTransform>();
            Stretch(badgeTextRect, Vector2.zero, Vector2.zero);
            badgeText.alignment = TextAnchor.MiddleCenter;
            badge.SetActive(false);

            var serve = card.GetComponent<ServeCustomerHUDButton>();
            serve.Configure(cardRect, status, title, detail, badge, glowImage);
            return serve;
        }

        internal static Text MakeLabel(Transform parent, Font font, string name, string value, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            AddShadow(go);
            return text;
        }

        internal static Text MakeText(Transform parent, Font font, string name, Vector2 pos, Vector2 size,
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

        internal static void AddShadow(GameObject go)
        {
            var s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.5f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }

        internal static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        internal static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        internal static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                return;
            }

            eventSystem.gameObject.SetActive(true);
            if (eventSystem.GetComponent<BaseInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }
}

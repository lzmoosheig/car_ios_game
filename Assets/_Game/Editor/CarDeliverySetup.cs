using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Builds the Car Delivery menu, its HUD shortcut and in-world building interaction
    /// into the OPEN scene, following the same "build into the live scene, don't
    /// rebuild the world" approach as <see cref="HudSetup"/>. Placeholder item icons are
    /// generated procedurally (no external art pipeline available) and saved once under
    /// Assets/_Game/Art/UI so re-runs simply reuse them.
    /// </summary>
    public static class CarDeliverySetup
    {
        private const string UiDir = HudSetup.UiDir;
        private static readonly (string Id, string File)[] IconFiles =
        {
            ("tire", "tire_icon.png"),
            ("oil", "oil_bottle_icon.png"),
            ("battery", "battery_icon.png"),
            ("paint", "paint_supplies_icon.png"),
            ("crate", "delivery_crate.png"),
            ("truck", "delivery_truck.png"),
            ("lock", "delivery_lock.png"),
        };

        [MenuItem("Overhaul/Build Car Delivery")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();

            var hud = GameObject.Find("HUD");
            var hudRoot = hud != null ? hud.GetComponent<HUDRoot>() : null;
            var safeRoot = hudRoot != null ? hudRoot.SafeAreaRoot : null;
            if (safeRoot == null)
            {
                Debug.LogError("[Overhaul] Car Delivery needs the HUD built first (Overhaul/Build HUD).");
                return;
            }

            HudSetup.EnsureEventSystem();
            var icons = GenerateAndImportIcons();

            var font = AssetDatabase.LoadAssetAtPath<Font>(HudSetup.RoundedFontPath)
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                       ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var economy = Object.FindAnyObjectByType<EconomyManager>();

            // Re-running this builder must be idempotent. GameObject.Find only ever returns
            // one (arbitrary) match, so a naive "find and destroy" leaves every earlier run's
            // copy behind - destroy ALL matches by name before building fresh ones.
            DestroyAllNamed("CarDeliveryMenu", "CarDeliveryHUDButton", "CarDeliverySystem", "CarDeliveryIcons");

            var system = BuildOrGetSystem(economy);
            BuildOrGetIcons(icons);

            var menu = BuildMenu(safeRoot, font, panelSprite, icons, system, economy);
            BuildHudButton(safeRoot, font, panelSprite, icons["truck"], menu, system);

            var saveManager = Object.FindAnyObjectByType<SaveManager>();
            saveManager?.ConfigureCarDelivery(system);

            BuildBuildingFeatures(font, menu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Overhaul] Car Delivery built: menu, HUD shortcut, building display + interaction.");
        }

        // ------------------------------------------------------------------ system/icons

        /// <summary>Destroys every root-level GameObject matching any of the given names.
        /// GameObject.Find only returns one arbitrary match, which silently leaves earlier
        /// runs' duplicates behind - this is the only safe way to make a re-run idempotent.</summary>
        private static void DestroyAllNamed(params string[] names)
        {
            var nameSet = new HashSet<string>(names);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                // Destroying one match can cascade-destroy a child that a later match also
                // covers (e.g. CarDeliveryMenu's own children); Unity's overloaded == treats
                // that "fake null" safely, but a property access like .scene throws instead.
                if (go == null) continue;
                if (go.scene.IsValid() && nameSet.Contains(go.name))
                    Object.DestroyImmediate(go);
            }
        }

        private static CarDeliverySystem BuildOrGetSystem(EconomyManager economy)
        {
            var go = new GameObject("CarDeliverySystem");
            var system = go.GetComponent<CarDeliverySystem>() ?? go.AddComponent<CarDeliverySystem>();
            system.Configure(economy);
            return system;
        }

        private static void BuildOrGetIcons(Dictionary<string, Sprite> icons)
        {
            var go = new GameObject("CarDeliveryIcons");
            var comp = go.GetComponent<CarDeliveryIcons>() ?? go.AddComponent<CarDeliveryIcons>();
            var entries = new List<CarDeliveryIcons.Entry>();
            foreach (var kv in icons) entries.Add(new CarDeliveryIcons.Entry { id = kv.Key, sprite = kv.Value });
            comp.Configure(entries);
        }

        // ------------------------------------------------------------------ menu overlay

        private static CarDeliveryMenu BuildMenu(Transform safeRoot, Font font, Sprite panelSprite,
            Dictionary<string, Sprite> icons, CarDeliverySystem system, EconomyManager economy)
        {
            var menuGo = new GameObject("CarDeliveryMenu", typeof(RectTransform), typeof(CarDeliveryMenu));
            menuGo.transform.SetParent(safeRoot, false);
            var menuRect = menuGo.GetComponent<RectTransform>();
            HudSetup.Stretch(menuRect, Vector2.zero, Vector2.zero);

            var blocker = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
            blocker.transform.SetParent(menuGo.transform, false);
            HudSetup.Stretch(blocker.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var blockerImg = blocker.GetComponent<Image>();
            blockerImg.color = new Color(0.02f, 0.03f, 0.04f, 0.22f);
            blocker.GetComponent<Button>().transition = Selectable.Transition.None;

            var panelSize = new Vector2(1400f, 840f);
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(menuGo.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = Vector2.zero;
            var panelImg = panel.GetComponent<Image>();
            panelImg.sprite = panelSprite;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.075f, 0.085f, 0.105f, 0.985f);
            var panelLayout = panel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(26, 26, 20, 20);
            panelLayout.spacing = 12f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            BuildHeader(panel.transform, font, panelSprite, icons, out var cashDisplay, out var goldDisplay);

            var overviewSlots = BuildOverviewSection(panel.transform, font, panelSprite, icons);

            // Two sibling containers, each with exactly one LayoutGroup (Unity allows only
            // one per GameObject): a wide side-by-side row and a narrow stacked column. The
            // slots/buy sections get reparented between them at runtime as the safe-area
            // width crosses the breakpoint (see CarDeliveryMenu.ApplyResponsiveLayout).
            var content = new GameObject("Content", typeof(RectTransform), typeof(LayoutElement));
            content.transform.SetParent(panel.transform, false);
            // Explicit preferredHeight rather than flexibleHeight=1: when a sibling's own
            // LayoutElement leaves a property at -1 ("ignore me"), Unity's layout resolver
            // falls through to that sibling's OWN LayoutGroup for the property instead of
            // just using 0, which silently steals space from a flexible-sized neighbor.
            // Fully explicit sizes for both Header and Content sidestep that fallthrough.
            const float headerHeight = 72f;
            const float overviewHeight = 154f;
            float contentHeight = panelSize.y - 20f - 20f - 12f - 12f - headerHeight - overviewHeight;
            var contentLe = content.GetComponent<LayoutElement>();
            contentLe.preferredHeight = contentHeight;
            contentLe.flexibleHeight = 0f;

            var wideRow = new GameObject("WideRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            wideRow.transform.SetParent(content.transform, false);
            HudSetup.Stretch(wideRow.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var wideLayout = wideRow.GetComponent<HorizontalLayoutGroup>();
            wideLayout.spacing = 22f;
            wideLayout.childControlWidth = true;
            wideLayout.childControlHeight = true;
            wideLayout.childForceExpandWidth = true;
            wideLayout.childForceExpandHeight = true;

            // Narrow mode stacks both sections in one column; a fixed-size panel can't
            // guarantee both a full delivery-slot grid and five buy rows fit at once on
            // every aspect ratio, so this column scrolls instead of squeezing content
            // below a readable size (Viewport+Content is the standard uGUI ScrollRect shape).
            var narrowColumn = new GameObject("NarrowColumn", typeof(RectTransform), typeof(ScrollRect));
            narrowColumn.transform.SetParent(content.transform, false);
            HudSetup.Stretch(narrowColumn.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(narrowColumn.transform, false);
            HudSetup.Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f); // RectMask2D needs a Graphic to clip against

            var narrowContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            narrowContent.transform.SetParent(viewport.transform, false);
            var narrowContentRect = narrowContent.GetComponent<RectTransform>();
            narrowContentRect.anchorMin = new Vector2(0f, 1f);
            narrowContentRect.anchorMax = new Vector2(1f, 1f);
            narrowContentRect.pivot = new Vector2(0.5f, 1f);
            narrowContentRect.anchoredPosition = Vector2.zero;
            narrowContentRect.sizeDelta = Vector2.zero;
            var narrowLayout = narrowContent.GetComponent<VerticalLayoutGroup>();
            narrowLayout.spacing = 16f;
            narrowLayout.childControlWidth = true;
            narrowLayout.childControlHeight = true;
            narrowLayout.childForceExpandWidth = true;
            narrowLayout.childForceExpandHeight = false;
            var narrowFitter = narrowContent.GetComponent<ContentSizeFitter>();
            narrowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = narrowColumn.GetComponent<ScrollRect>();
            scrollRect.content = narrowContentRect;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            narrowColumn.SetActive(false);

            var slotViews = BuildSlotsSection(wideRow.transform, font, panelSprite, out var buySlotButton,
                out var buySlotCostText, out var startDeliveryButton, out var slotsSection);
            var itemRows = BuildBuySection(wideRow.transform, font, panelSprite, icons, out var buySection);

            var closeButton = MakeCloseButton(menuGo.transform, font, panelRect);

            // The global HUD style uses drop shadows, but on a scaled full-screen panel
            // they soften every glyph. This overlay keeps text crisp and unblurred.
            foreach (var shadow in panel.GetComponentsInChildren<Shadow>(true))
                Object.DestroyImmediate(shadow);

            var refs = new CarDeliveryMenu.Refs
            {
                Root = menuGo,
                Blocker = blocker,
                CloseButton = closeButton,
                BuySlotButton = buySlotButton,
                BuySlotCostText = buySlotCostText,
                StartDeliveryButton = startDeliveryButton,
                CashDisplay = cashDisplay,
                GoldDisplay = goldDisplay,
                OverviewSlots = overviewSlots,
                SlotViews = slotViews,
                ItemRows = itemRows,
                WideRow = wideRow.transform,
                NarrowColumn = narrowColumn.transform,
                NarrowContent = narrowContent.transform,
                SlotsSection = slotsSection,
                BuySection = buySection,
                NarrowBreakpoint = 900f
            };

            // Nothing has actually run a uGUI layout pass yet at this point - editor-time
            // object construction never ticks Update/CanvasUpdateRegistry, so every
            // LayoutGroup/LayoutElement size set above is still unresolved. Force it now
            // (while still active - LayoutRebuilder skips inactive rects) so the *correct*
            // sizes, not default 100x100 RectTransform stubs, get baked into the saved scene.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);

            var menu = menuGo.GetComponent<CarDeliveryMenu>();
            menu.Configure(refs, system, economy);
            return menu;
        }

        private static void BuildHeader(Transform parent, Font font, Sprite panelSprite,
            Dictionary<string, Sprite> icons, out CurrencyDisplay cashDisplay, out CurrencyDisplay goldDisplay)
        {
            var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(parent, false);
            var headerLe = header.GetComponent<LayoutElement>();
            headerLe.preferredHeight = 72f;
            headerLe.flexibleHeight = 0f;
            var headerLayout = header.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 14f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            var truckIcon = new GameObject("TruckIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            truckIcon.transform.SetParent(header.transform, false);
            var truckImg = truckIcon.GetComponent<Image>();
            truckImg.sprite = icons.TryGetValue("truck", out var truckSprite) ? truckSprite : null;
            truckImg.preserveAspect = true;
            truckIcon.GetComponent<LayoutElement>().preferredWidth = 56f;
            truckIcon.GetComponent<LayoutElement>().preferredHeight = 56f;

            var title = HudSetup.MakeLabel(header.transform, font, "Title", "Car Delivery", 38, FontStyle.Bold, Color.white);
            var titleLe = title.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredWidth = 340f;

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(header.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;

            cashDisplay = HudSetup.MakeCurrencyItem(header.transform, font, panelSprite, $"{UiDir}/cash.png", "Cash");
            goldDisplay = HudSetup.MakeCurrencyItem(header.transform, font, panelSprite, $"{UiDir}/gold.png", "Gold");
        }

        private static DeliveryPartOverviewSlot[] BuildOverviewSection(Transform parent, Font font, Sprite panelSprite,
            Dictionary<string, Sprite> icons)
        {
            var section = new GameObject("PartsOverview", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            section.transform.SetParent(parent, false);
            var sectionLe = section.GetComponent<LayoutElement>();
            sectionLe.preferredHeight = 154f;
            sectionLe.flexibleHeight = 0f;

            var layout = section.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = HudSetup.MakeLabel(section.transform, font, "OverviewLabel", "PARTS DASHBOARD", 20, FontStyle.Bold,
                new Color(0.64f, 0.80f, 1f));
            title.alignment = TextAnchor.MiddleLeft;

            var row = new GameObject("OverviewRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(section.transform, false);
            var rowLe = row.GetComponent<LayoutElement>();
            rowLe.preferredHeight = 120f;
            rowLe.flexibleHeight = 0f;
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            string[] ids = { "tire", "battery", "oil", "paint" };
            var slots = new DeliveryPartOverviewSlot[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                slots[i] = MakeOverviewCard(row.transform, font, panelSprite, icons, ids[i]);
            return slots;
        }

        private static DeliveryPartOverviewSlot MakeOverviewCard(Transform parent, Font font, Sprite panelSprite,
            Dictionary<string, Sprite> icons, string itemId)
        {
            var card = new GameObject($"Overview_{itemId}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement), typeof(DeliveryPartOverviewSlot));
            card.transform.SetParent(parent, false);
            var cardLe = card.GetComponent<LayoutElement>();
            cardLe.flexibleWidth = 1f;
            cardLe.minWidth = 180f;
            var bg = card.GetComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.11f, 0.125f, 0.15f, 0.98f);

            var cardLayout = card.GetComponent<HorizontalLayoutGroup>();
            cardLayout.padding = new RectOffset(14, 14, 12, 12);
            cardLayout.spacing = 12f;
            cardLayout.childAlignment = TextAnchor.MiddleLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = true;

            var iconShell = new GameObject("IconShell", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconShell.transform.SetParent(card.transform, false);
            var shellLe = iconShell.GetComponent<LayoutElement>();
            shellLe.preferredWidth = 78f;
            shellLe.preferredHeight = 78f;
            shellLe.flexibleWidth = 0f;
            var shellImg = iconShell.GetComponent<Image>();
            shellImg.sprite = panelSprite;
            shellImg.type = Image.Type.Sliced;
            shellImg.color = new Color(0.16f, 0.18f, 0.21f, 1f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(iconShell.transform, false);
            HudSetup.Stretch(icon.GetComponent<RectTransform>(), new Vector2(7f, 7f), new Vector2(-7f, -7f));
            var iconImg = icon.GetComponent<Image>();
            iconImg.sprite = icons.TryGetValue(itemId, out var sp) ? sp : null;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var info = new GameObject("Info", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            info.transform.SetParent(card.transform, false);
            info.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var infoLayout = info.GetComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 2f;
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;

            string displayName = CarDeliveryCatalog.TryFind(itemId, out var def) ? def.DisplayName : ResourceCatalog.DisplayName(itemId);
            var nameText = HudSetup.MakeLabel(info.transform, font, "Name", displayName, 17,
                FontStyle.Bold, Color.white);
            nameText.alignment = TextAnchor.MiddleLeft;

            var subtitle = HudSetup.MakeLabel(info.transform, font, "Subtitle", "In stock", 12, FontStyle.Normal,
                new Color(0.62f, 0.70f, 0.82f));
            subtitle.alignment = TextAnchor.MiddleLeft;

            var quantityText = HudSetup.MakeLabel(info.transform, font, "Quantity", "0", 30, FontStyle.Bold,
                new Color(0.76f, 0.96f, 0.58f));
            quantityText.alignment = TextAnchor.MiddleLeft;

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            accent.transform.SetParent(card.transform, false);
            accent.GetComponent<LayoutElement>().ignoreLayout = true;
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(1f, 0f);
            accentRect.pivot = new Vector2(0.5f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 4f);
            accent.GetComponent<Image>().color = new Color(0.36f, 0.78f, 0.42f, 0.88f);

            var view = card.GetComponent<DeliveryPartOverviewSlot>();
            view.Configure(iconImg, nameText, quantityText);
            return view;
        }

        private static Button MakeCloseButton(Transform parent, Font font, RectTransform panelRect)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(48f, 48f);
            rect.anchoredPosition = new Vector2(panelRect.sizeDelta.x / 2f - 16f, panelRect.sizeDelta.y / 2f - 16f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.16f, 0.18f, 0.21f, 1f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
            colors.pressedColor = new Color(0.72f, 0.76f, 0.82f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var label = HudSetup.MakeLabel(go.transform, font, "X", "✕", 22, FontStyle.Bold, Color.white);
            label.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)label.transform, Vector2.zero, Vector2.zero);
            return btn;
        }

        // ------------------------------------------------------------------ slots section

        private static DeliverySlotView[] BuildSlotsSection(Transform parent, Font font, Sprite panelSprite,
            out Button buySlotButton, out Text buySlotCostText, out Button startDeliveryButton, out Transform sectionTransform)
        {
            var section = new GameObject("SlotsSection", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            section.transform.SetParent(parent, false);
            sectionTransform = section.transform;
            var sectionLe = section.GetComponent<LayoutElement>();
            sectionLe.flexibleWidth = 1.3f;
            // Explicit (not left at -1): this section is reparented at runtime between
            // WideRow (forces full height anyway) and NarrowColumn, whose VerticalLayoutGroup
            // does NOT force-expand height - an unset flexibleHeight there falls through to
            // this object's own VerticalLayoutGroup's tiny computed preferred size instead.
            sectionLe.flexibleHeight = 1f;
            sectionLe.flexibleHeight = 1f;
            var sectionLayout = section.GetComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 12f;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var label = HudSetup.MakeLabel(section.transform, font, "SlotsLabel", "DELIVERY SLOTS", 22, FontStyle.Bold, new Color(0.55f, 0.75f, 1f));

            var grid = new GameObject("SlotsGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            grid.transform.SetParent(section.transform, false);
            grid.GetComponent<LayoutElement>().flexibleHeight = 1f;
            var gridLayout = grid.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(320f, 158f);
            gridLayout.spacing = new Vector2(14f, 14f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            var slotViews = new DeliverySlotView[CarDeliverySystem.SlotCount];
            for (int i = 0; i < slotViews.Length; i++)
                slotViews[i] = MakeSlotCell(grid.transform, font, panelSprite, i);

            var bottomRow = new GameObject("BottomRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            bottomRow.transform.SetParent(section.transform, false);
            var bottomRowLe = bottomRow.GetComponent<LayoutElement>();
            bottomRowLe.preferredHeight = 72f;
            bottomRowLe.flexibleHeight = 0f;
            var bottomLayout = bottomRow.GetComponent<HorizontalLayoutGroup>();
            bottomLayout.spacing = 14f;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = true;
            bottomLayout.childForceExpandHeight = true;

            buySlotButton = MakeActionButton(bottomRow.transform, font, "BuySlotButton", "Buy Slot",
                new Color(0.16f, 0.45f, 0.85f, 0.95f), true, out buySlotCostText);
            startDeliveryButton = MakeActionButton(bottomRow.transform, font, "StartDeliveryButton", "Start Delivery",
                new Color(0.20f, 0.72f, 0.30f, 0.95f), false, out _);

            return slotViews;
        }

        private static Button MakeActionButton(Transform parent, Font font, string name, string label, Color color,
            bool includeCost, out Text costText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.08f);
            colors.pressedColor = new Color(0.85f, 0.9f, 1f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var labelText = HudSetup.MakeLabel(go.transform, font, "Label", label, 22, FontStyle.Bold, Color.white);
            labelText.alignment = TextAnchor.MiddleCenter;

            costText = null;
            if (includeCost)
            {
                costText = HudSetup.MakeLabel(go.transform, font, "Cost", "$0", 16, FontStyle.Bold, new Color(1f, 1f, 1f, 0.85f));
                costText.alignment = TextAnchor.MiddleCenter;
            }
            return btn;
        }

        private static DeliverySlotView MakeSlotCell(Transform parent, Font font, Sprite panelSprite, int index)
        {
            var cell = new GameObject($"Slot{index}", typeof(RectTransform), typeof(Image), typeof(DeliverySlotView));
            cell.transform.SetParent(parent, false);
            var bg = cell.GetComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.105f, 0.12f, 0.145f, 0.98f);

            var chip = new GameObject("Chip", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(cell.transform, false);
            var chipRect = chip.GetComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0f, 1f);
            chipRect.anchorMax = new Vector2(0f, 1f);
            chipRect.pivot = new Vector2(0f, 1f);
            chipRect.anchoredPosition = new Vector2(8f, -8f);
            chipRect.sizeDelta = new Vector2(28f, 28f);
            chip.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.85f, 0.95f);
            var chipText = HudSetup.MakeLabel(chip.transform, font, "Num", (index + 1).ToString(), 16, FontStyle.Bold, Color.white);
            chipText.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)chipText.transform, Vector2.zero, Vector2.zero);

            var unlocked = new GameObject("Unlocked", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            unlocked.transform.SetParent(cell.transform, false);
            HudSetup.Stretch(unlocked.GetComponent<RectTransform>(), new Vector2(12f, 10f), new Vector2(-12f, -10f));
            var unlockedLayout = unlocked.GetComponent<HorizontalLayoutGroup>();
            unlockedLayout.spacing = 10f;
            unlockedLayout.childAlignment = TextAnchor.MiddleLeft;
            unlockedLayout.childControlWidth = true;
            unlockedLayout.childControlHeight = true;
            unlockedLayout.childForceExpandWidth = false;
            unlockedLayout.childForceExpandHeight = true;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(unlocked.transform, false);
            var iconImg = icon.GetComponent<Image>();
            iconImg.preserveAspect = true;
            icon.GetComponent<LayoutElement>().preferredWidth = 62f;
            icon.GetComponent<LayoutElement>().preferredHeight = 62f;

            var infoCol = new GameObject("Info", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            infoCol.transform.SetParent(unlocked.transform, false);
            infoCol.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var infoLayout = infoCol.GetComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 4f;
            infoLayout.childAlignment = TextAnchor.UpperLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;

            var nameText = HudSetup.MakeLabel(infoCol.transform, font, "Name", "Item", 21, FontStyle.Bold, Color.white);
            var timerText = HudSetup.MakeLabel(infoCol.transform, font, "Timer", "00m 00s", 16, FontStyle.Normal, new Color(0.8f, 0.86f, 1f));

            var barBg = new GameObject("ProgressBg", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            barBg.transform.SetParent(infoCol.transform, false);
            barBg.GetComponent<LayoutElement>().preferredHeight = 10f;
            barBg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var barFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            HudSetup.Stretch(barFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var fillImg = barFill.GetComponent<Image>();
            fillImg.color = new Color(0.25f, 0.75f, 0.35f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            var qtyText = HudSetup.MakeLabel(infoCol.transform, font, "Qty", "x0", 15, FontStyle.Normal, new Color(0.75f, 0.8f, 0.9f));

            var actionBtn = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            actionBtn.transform.SetParent(cell.transform, false);
            var actionRect = actionBtn.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(1f, 1f);
            actionRect.anchorMax = new Vector2(1f, 1f);
            actionRect.pivot = new Vector2(1f, 1f);
            actionRect.anchoredPosition = new Vector2(-8f, -8f);
            actionRect.sizeDelta = new Vector2(46f, 34f);
            var actionImg = actionBtn.GetComponent<Image>();
            actionImg.color = new Color(0.92f, 0.56f, 0.16f, 0.96f);
            var actionButtonComp = actionBtn.GetComponent<Button>();
            actionButtonComp.targetGraphic = actionImg;
            var actionLabel = HudSetup.MakeLabel(actionBtn.transform, font, "Icon", "▶▶", 15, FontStyle.Bold, Color.white);
            actionLabel.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)actionLabel.transform, Vector2.zero, Vector2.zero);

            var actionBadge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            actionBadge.transform.SetParent(actionBtn.transform, false);
            var badgeRect = actionBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(4f, 8f);
            badgeRect.sizeDelta = new Vector2(20f, 20f);
            actionBadge.GetComponent<Image>().color = new Color(1f, 0.82f, 0.26f, 0.96f);
            var actionBadgeText = HudSetup.MakeLabel(actionBadge.transform, font, "Text", "1", 13, FontStyle.Bold, new Color(0.2f, 0.12f, 0f));
            actionBadgeText.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)actionBadgeText.transform, Vector2.zero, Vector2.zero);
            actionBadge.SetActive(false);

            var locked = new GameObject("Locked", typeof(RectTransform), typeof(VerticalLayoutGroup));
            locked.transform.SetParent(cell.transform, false);
            HudSetup.Stretch(locked.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            var lockedLayout = locked.GetComponent<VerticalLayoutGroup>();
            lockedLayout.childAlignment = TextAnchor.MiddleCenter;
            lockedLayout.spacing = 8f;
            lockedLayout.childControlWidth = true;
            lockedLayout.childControlHeight = true;
            lockedLayout.childForceExpandWidth = true;
            lockedLayout.childForceExpandHeight = false;

            var lockIcon = new GameObject("LockIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            lockIcon.transform.SetParent(locked.transform, false);
            var lockImg = lockIcon.GetComponent<Image>();
            lockImg.preserveAspect = true;
            lockImg.color = new Color(1f, 1f, 1f, 0.7f);
            lockIcon.GetComponent<LayoutElement>().preferredWidth = 34f;
            lockIcon.GetComponent<LayoutElement>().preferredHeight = 34f;
            var lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiDir}/delivery_lock.png");
            lockImg.sprite = lockSprite;

            var lockText = HudSetup.MakeLabel(locked.transform, font, "LockText", "Requires\nLevel 0", 16, FontStyle.Bold, new Color(0.65f, 0.7f, 0.78f));
            lockText.alignment = TextAnchor.MiddleCenter;
            locked.SetActive(false);

            var view = cell.GetComponent<DeliverySlotView>();
            view.Configure(iconImg, nameText, qtyText, timerText, fillImg, actionButtonComp, actionBadge, actionBadgeText,
                unlocked, locked, lockText);
            return view;
        }

        // ------------------------------------------------------------------ buy section

        private static DeliveryItemRow[] BuildBuySection(Transform parent, Font font, Sprite panelSprite,
            Dictionary<string, Sprite> icons, out Transform sectionTransform)
        {
            var section = new GameObject("BuySection", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            section.transform.SetParent(parent, false);
            sectionTransform = section.transform;
            var buySectionLe = section.GetComponent<LayoutElement>();
            buySectionLe.flexibleWidth = 1f;
            buySectionLe.flexibleHeight = 1f;
            var layout = section.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            HudSetup.MakeLabel(section.transform, font, "BuyLabel", "BUY ITEMS", 22, FontStyle.Bold, new Color(0.55f, 0.75f, 1f));

            var ids = new[] { "tire", "oil", "battery", "paint", "crate" };
            var rows = new DeliveryItemRow[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                rows[i] = MakeItemRow(section.transform, font, panelSprite, icons, ids[i]);
            return rows;
        }

        private static DeliveryItemRow MakeItemRow(Transform parent, Font font, Sprite panelSprite,
            Dictionary<string, Sprite> icons, string itemId)
        {
            var row = new GameObject($"Row_{itemId}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement), typeof(DeliveryItemRow));
            row.transform.SetParent(parent, false);
            var rowLe = row.GetComponent<LayoutElement>();
            rowLe.preferredHeight = 84f;
            rowLe.flexibleHeight = 0f;
            var bg = row.GetComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.105f, 0.12f, 0.145f, 0.98f);
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 8, 8);
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(row.transform, false);
            var iconImg = icon.GetComponent<Image>();
            iconImg.sprite = icons.TryGetValue(itemId, out var sp) ? sp : null;
            iconImg.preserveAspect = true;
            var iconLe = icon.GetComponent<LayoutElement>();
            iconLe.preferredWidth = 54f;
            iconLe.preferredHeight = 54f;
            iconLe.flexibleWidth = 0f;

            var infoCol = new GameObject("Info", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            infoCol.transform.SetParent(row.transform, false);
            infoCol.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var infoLayout = infoCol.GetComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 2f;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            var nameText = HudSetup.MakeLabel(infoCol.transform, font, "Name", "Item", 19, FontStyle.Bold, Color.white);
            var descText = HudSetup.MakeLabel(infoCol.transform, font, "Desc", "Description", 13, FontStyle.Normal, new Color(0.75f, 0.8f, 0.9f));
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var qtyCol = new GameObject("QtyCol", typeof(RectTransform), typeof(LayoutElement));
            qtyCol.transform.SetParent(row.transform, false);
            var qtyColLe = qtyCol.GetComponent<LayoutElement>();
            qtyColLe.preferredWidth = 48f;
            qtyColLe.flexibleWidth = 0f;
            var qtyText = HudSetup.MakeLabel(qtyCol.transform, font, "Qty", "x100", 15, FontStyle.Bold, new Color(0.8f, 0.85f, 0.95f));
            qtyText.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)qtyText.transform, Vector2.zero, Vector2.zero);

            var priceCol = new GameObject("PriceCol", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            priceCol.transform.SetParent(row.transform, false);
            var priceColLe = priceCol.GetComponent<LayoutElement>();
            priceColLe.preferredWidth = 128f;
            priceColLe.flexibleWidth = 0f;
            var priceLayout = priceCol.GetComponent<VerticalLayoutGroup>();
            priceLayout.childAlignment = TextAnchor.MiddleCenter;
            priceLayout.spacing = 4f;
            priceLayout.childControlWidth = true;
            priceLayout.childControlHeight = true;
            priceLayout.childForceExpandWidth = true;
            priceLayout.childForceExpandHeight = false;
            var priceText = HudSetup.MakeLabel(priceCol.transform, font, "Price", "$0", 17, FontStyle.Bold, new Color(0.6f, 0.95f, 0.6f));
            priceText.alignment = TextAnchor.MiddleCenter;

            var buyBtn = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buyBtn.transform.SetParent(priceCol.transform, false);
            var buyBtnLe = buyBtn.GetComponent<LayoutElement>();
            buyBtnLe.preferredHeight = 32f;
            buyBtnLe.flexibleHeight = 0f;
            buyBtnLe.flexibleWidth = 0f;
            var buyImg = buyBtn.GetComponent<Image>();
            buyImg.color = new Color(0.20f, 0.72f, 0.30f, 0.95f);
            var buyButtonComp = buyBtn.GetComponent<Button>();
            buyButtonComp.targetGraphic = buyImg;
            var buyLabel = HudSetup.MakeLabel(buyBtn.transform, font, "Label", "Buy", 15, FontStyle.Bold, Color.white);
            buyLabel.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)buyLabel.transform, Vector2.zero, Vector2.zero);

            var view = row.GetComponent<DeliveryItemRow>();
            view.Configure(iconImg, nameText, descText, qtyText, priceText, buyButtonComp);
            return view;
        }

        // ------------------------------------------------------------------ HUD button

        private const float HudButtonSize = 88f;

        private static void BuildHudButton(Transform safeRoot, Font font, Sprite panelSprite, Sprite truckSprite,
            CarDeliveryMenu menu, CarDeliverySystem system)
        {
            var go = new GameObject("CarDeliveryHUDButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CarDeliveryHUDButton));
            go.transform.SetParent(safeRoot, false);
            var rect = go.GetComponent<RectTransform>();
            HudSetup.Anchor(rect, new Vector2(1f, 0.5f), new Vector2(-34f, -108f), new Vector2(HudButtonSize, HudButtonSize));
            rect.pivot = new Vector2(1f, 0.5f);
            var bg = go.GetComponent<Image>();
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.05f, 0.07f, 0.1f, 0.86f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.08f);
            colors.pressedColor = new Color(0.82f, 0.9f, 1f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(go.transform, false);
            HudSetup.Stretch(glow.GetComponent<RectTransform>(), new Vector2(-4f, -4f), new Vector2(4f, 4f));
            var glowImg = glow.GetComponent<Image>();
            glowImg.sprite = panelSprite;
            glowImg.type = Image.Type.Sliced;
            glowImg.color = new Color(0.3f, 0.8f, 1f, 0.2f);
            glowImg.raycastTarget = false;
            glowImg.enabled = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(58f, 58f);
            var iconImg = icon.GetComponent<Image>();
            iconImg.sprite = truckSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(go.transform, false);
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-6f, -6f);
            badgeRect.sizeDelta = new Vector2(26f, 26f);
            badge.GetComponent<Image>().color = new Color(1f, 0.29f, 0.18f, 0.98f);
            var badgeText = HudSetup.MakeLabel(badge.transform, font, "Text", "1", 15, FontStyle.Bold, Color.white);
            badgeText.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)badgeText.transform, Vector2.zero, Vector2.zero);
            badge.SetActive(false);

            var comp = go.GetComponent<CarDeliveryHUDButton>();
            comp.Configure(rect, badge, badgeText, glowImg, menu, system);
        }

        // ------------------------------------------------------------------ building features

        private static void BuildBuildingFeatures(Font font, CarDeliveryMenu menu)
        {
            var station = GameObject.Find("Station_PARTS_DELIVERY");
            if (station == null)
            {
                Debug.LogWarning("[Overhaul] Station_PARTS_DELIVERY not found; skipping building display/interaction.");
                return;
            }

            // Prefix match catches every previously-built pole/prop (Unity suffixes duplicate
            // names, so an exact-name check would miss "DisplayAwningPole (1)" etc.).
            var toRemove = new List<GameObject>();
            foreach (Transform child in station.transform)
                if (child.name.StartsWith("DisplayAwning") || child.name.StartsWith("DisplayShelf") ||
                    child.name.StartsWith("DisplayProp") || child.name.StartsWith("CarDeliveryInteractionButton") ||
                    child.name.StartsWith("CarDeliverySign"))
                    toRemove.Add(child.gameObject);
            foreach (var go in toRemove) Object.DestroyImmediate(go);

            Vector3 center = station.transform.position;
            BuildDisplayStall(center, station.transform);
            BuildInteractionButton(center, station.transform, font, menu);
        }

        private static void BuildDisplayStall(Vector3 c, Transform parent)
        {
            // The station shell's detailed face and signboard both sit toward -Z (see
            // CityGarageSceneBuilder.BuildStation) - this stall mirrors that so it reads as
            // an open front stall facing the customer apron, not stuffed behind the building.
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "DisplayAwningRoof";
            roof.transform.SetParent(parent, true);
            roof.transform.position = c + new Vector3(0f, 2.6f, -2.6f);
            roof.transform.localScale = new Vector3(5.6f, 0.12f, 3.0f);
            Object.DestroyImmediate(roof.GetComponent<Collider>());
            Paint(roof, new Color(0.32f, 0.34f, 0.38f), "CarDelivery_Awning");

            Vector3[] poleOffsets =
            {
                new(-2.6f, 1.3f, -1.3f), new(2.6f, 1.3f, -1.3f),
                new(-2.6f, 1.3f, -4.0f), new(2.6f, 1.3f, -4.0f)
            };
            foreach (var off in poleOffsets)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "DisplayAwningPole";
                pole.transform.SetParent(parent, true);
                pole.transform.position = c + off;
                pole.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);
                Object.DestroyImmediate(pole.GetComponent<Collider>());
                Paint(pole, new Color(0.25f, 0.26f, 0.28f), "CarDelivery_Pole");
            }

            var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "DisplayShelf";
            shelf.transform.SetParent(parent, true);
            shelf.transform.position = c + new Vector3(0f, 0.35f, -2.6f);
            shelf.transform.localScale = new Vector3(5.2f, 0.7f, 2.6f);
            Object.DestroyImmediate(shelf.GetComponent<Collider>());
            Paint(shelf, new Color(0.42f, 0.30f, 0.20f), "CarDelivery_Shelf");

            const float top = 0.35f + 0.35f + 0.05f;
            PlaceStack(c + new Vector3(-2.0f, top, -2.0f), parent, PrimitiveType.Cylinder, new Vector3(0.5f, 0.12f, 0.5f), new Color(0.12f, 0.12f, 0.13f), 3, 0.16f);
            PlaceProp(c + new Vector3(-0.9f, top + 0.15f, -1.9f), parent, PrimitiveType.Cube, new Vector3(0.3f, 0.4f, 0.3f), new Color(0.85f, 0.65f, 0.13f));
            PlaceProp(c + new Vector3(-0.9f, top + 0.15f, -2.9f), parent, PrimitiveType.Cube, new Vector3(0.3f, 0.4f, 0.3f), new Color(0.85f, 0.65f, 0.13f));
            PlaceProp(c + new Vector3(0.2f, top + 0.2f, -2.2f), parent, PrimitiveType.Cube, new Vector3(0.45f, 0.3f, 0.25f), new Color(0.20f, 0.30f, 0.55f));
            PlaceProp(c + new Vector3(1.3f, top + 0.25f, -1.9f), parent, PrimitiveType.Cylinder, new Vector3(0.2f, 0.35f, 0.2f), new Color(0.55f, 0.20f, 0.75f));
            PlaceProp(c + new Vector3(1.3f, top + 0.25f, -2.9f), parent, PrimitiveType.Cylinder, new Vector3(0.2f, 0.35f, 0.2f), new Color(0.55f, 0.20f, 0.75f));
            PlaceProp(c + new Vector3(2.2f, top + 0.2f, -2.4f), parent, PrimitiveType.Cube, new Vector3(0.5f, 0.4f, 0.5f), new Color(0.55f, 0.38f, 0.22f));
        }

        private static void BuildInteractionButton(Vector3 c, Transform parent, Font font, CarDeliveryMenu menu)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "CarDeliveryInteractionButton";
            pad.transform.SetParent(parent, true);
            pad.transform.position = c + new Vector3(0f, 0.05f, -3.4f);
            pad.transform.localScale = new Vector3(1.3f, 0.03f, 1.3f);
            Paint(pad, new Color(0.20f, 0.55f, 0.85f), "CarDelivery_Pad");

            var button = pad.AddComponent<CarDeliveryBuildingButton>();
            button.Configure(menu);

            var signGo = new GameObject("CarDeliverySign", typeof(Canvas), typeof(CanvasScaler));
            signGo.transform.SetParent(parent, true);
            signGo.transform.position = c + new Vector3(0f, 1.7f, -3.4f);
            signGo.transform.rotation = Quaternion.identity;
            signGo.transform.localScale = Vector3.one * 0.01f;
            var signRect = (RectTransform)signGo.transform;
            signRect.sizeDelta = new Vector2(220f, 60f);
            var signCanvas = signGo.GetComponent<Canvas>();
            signCanvas.renderMode = RenderMode.WorldSpace;

            var signBg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            signBg.transform.SetParent(signGo.transform, false);
            HudSetup.Stretch(signBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            signBg.GetComponent<Image>().color = new Color(0.10f, 0.45f, 0.85f, 0.95f);

            var signLabel = HudSetup.MakeLabel(signGo.transform, font, "Label", "DELIVERY", 30, FontStyle.Bold, Color.white);
            signLabel.alignment = TextAnchor.MiddleCenter;
            HudSetup.Stretch((RectTransform)signLabel.transform, Vector2.zero, Vector2.zero);
        }

        private static void PlaceProp(Vector3 pos, Transform parent, PrimitiveType type, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "DisplayProp";
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Paint(go, color, "CarDelivery_Prop");
        }

        private static void PlaceStack(Vector3 basePos, Transform parent, PrimitiveType type, Vector3 scale, Color color, int count, float gap)
        {
            for (int i = 0; i < count; i++)
                PlaceProp(basePos + new Vector3(0f, gap * i, 0f), parent, type, scale, color);
        }

        private static void Paint(GameObject go, Color color, string name)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var mat = new Material(Shader.Find("Standard")) { name = name };
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        // ------------------------------------------------------------------ icon generation

        private static Dictionary<string, Sprite> GenerateAndImportIcons()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var result = new Dictionary<string, Sprite>();

            foreach (var (id, file) in IconFiles)
            {
                string relPath = $"{UiDir}/{file}";
                string fullPath = Path.Combine(projectRoot, relPath);
                if (!File.Exists(fullPath))
                {
                    var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                    var pixels = new Color32[128 * 128];
                    DrawIcon(id, pixels, 128);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    File.WriteAllBytes(fullPath, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    AssetDatabase.ImportAsset(relPath);
                }
                HudSetup.ImportIcon(relPath);
                result[id] = AssetDatabase.LoadAssetAtPath<Sprite>(relPath);
            }
            return result;
        }

        private static void DrawIcon(string id, Color32[] px, int size)
        {
            switch (id)
            {
                case "tire":
                    FillCircle(px, size, 64, 64, 50, new Color32(24, 24, 26, 255));
                    FillCircle(px, size, 64, 64, 36, new Color32(72, 74, 80, 255));
                    FillCircle(px, size, 64, 64, 15, new Color32(24, 24, 26, 255));
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Mathf.PI * 2f;
                        FillCircle(px, size, 64 + Mathf.Cos(a) * 15f, 64 + Mathf.Sin(a) * 15f, 3.5f, new Color32(50, 50, 54, 255));
                    }
                    break;

                case "oil":
                    FillRoundedRect(px, size, 30, 26, 68, 78, 10, new Color32(217, 166, 33, 255));
                    FillRect(px, size, 54, 10, 20, 18, new Color32(70, 52, 18, 255));
                    FillRoundedRect(px, size, 42, 52, 44, 16, 4, new Color32(255, 214, 120, 255));
                    break;

                case "battery":
                    FillRoundedRect(px, size, 18, 34, 92, 60, 8, new Color32(58, 110, 168, 255));
                    FillRect(px, size, 42, 20, 14, 16, new Color32(40, 40, 44, 255));
                    FillRect(px, size, 72, 20, 14, 16, new Color32(220, 60, 50, 255));
                    FillRect(px, size, 32, 60, 22, 8, Color.white);
                    FillRect(px, size, 88, 56, 8, 20, Color.white);
                    FillRect(px, size, 80, 60, 24, 8, Color.white);
                    break;

                case "paint":
                    FillRoundedRect(px, size, 38, 30, 52, 80, 10, new Color32(150, 75, 200, 255));
                    FillCircle(px, size, 64, 40, 20, new Color32(150, 75, 200, 255));
                    FillRect(px, size, 30, 40, 68, 20, new Color32(0, 0, 0, 0));
                    FillRoundedRect(px, size, 38, 30, 52, 80, 10, new Color32(150, 75, 200, 255));
                    FillCircle(px, size, 64, 24, 15, new Color32(95, 45, 145, 255));
                    FillRect(px, size, 58, 8, 16, 14, new Color32(60, 60, 64, 255));
                    FillRoundedRect(px, size, 44, 55, 40, 14, 4, new Color32(205, 155, 235, 255));
                    break;

                case "crate":
                    FillRoundedRect(px, size, 20, 24, 88, 82, 8, new Color32(150, 105, 60, 255));
                    FillRect(px, size, 20, 82, 88, 16, new Color32(120, 80, 45, 255));
                    FillRect(px, size, 20, 58, 88, 8, new Color32(90, 60, 30, 255));
                    FillRect(px, size, 60, 24, 8, 82, new Color32(90, 60, 30, 255));
                    break;

                case "truck":
                    FillRoundedRect(px, size, 12, 40, 62, 40, 6, new Color32(235, 235, 238, 255));
                    FillRoundedRect(px, size, 74, 50, 38, 30, 6, new Color32(58, 110, 168, 255));
                    FillRect(px, size, 82, 56, 22, 14, new Color32(190, 220, 240, 255));
                    FillCircle(px, size, 34, 82, 13, new Color32(24, 24, 26, 255));
                    FillCircle(px, size, 34, 82, 5, new Color32(150, 150, 155, 255));
                    FillCircle(px, size, 92, 82, 13, new Color32(24, 24, 26, 255));
                    FillCircle(px, size, 92, 82, 5, new Color32(150, 150, 155, 255));
                    break;

                case "lock":
                    FillCircle(px, size, 64, 42, 24, new Color32(255, 214, 90, 255));
                    FillCircle(px, size, 64, 42, 13, new Color32(0, 0, 0, 0));
                    FillRect(px, size, 26, 42, 76, 40, new Color32(0, 0, 0, 0));
                    FillRoundedRect(px, size, 32, 52, 64, 52, 10, new Color32(255, 214, 90, 255));
                    FillCircle(px, size, 64, 78, 8, new Color32(150, 110, 10, 255));
                    break;
            }
        }

        private static void SetPx(Color32[] px, int size, int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= size || y >= size) return;
            px[y * size + x] = c;
        }

        private static void FillCircle(Color32[] px, int size, float cx, float cy, float r, Color32 c)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - r));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(cx + r));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - r));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(cy + r));
            float r2 = r * r;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r2) SetPx(px, size, x, y, c);
                }
        }

        private static void FillRect(Color32[] px, int size, float x0, float y0, float w, float h, Color32 c)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(x0));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(x0 + w));
            int minY = Mathf.Max(0, Mathf.FloorToInt(y0));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(y0 + h));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    SetPx(px, size, x, y, c);
        }

        private static void FillRoundedRect(Color32[] px, int size, float x0, float y0, float w, float h, float r, Color32 c)
        {
            FillRect(px, size, x0 + r, y0, w - 2 * r, h, c);
            FillRect(px, size, x0, y0 + r, w, h - 2 * r, c);
            FillCircle(px, size, x0 + r, y0 + r, r, c);
            FillCircle(px, size, x0 + w - r, y0 + r, r, c);
            FillCircle(px, size, x0 + r, y0 + h - r, r, c);
            FillCircle(px, size, x0 + w - r, y0 + h - r, r, c);
        }
    }
}

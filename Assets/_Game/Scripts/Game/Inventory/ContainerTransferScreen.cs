using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// The modal transfer window shown when the player opens a container (e.g. the parts
    /// delivery worker). Two Minecraft-style grids side by side - the container on the left,
    /// the player on the right - each in one-click "quick move" mode, so clicking an item sends
    /// the whole stack across. It also owns the modal etiquette: raises the
    /// <see cref="InventoryUiModal"/> gate (freezing movement/look/selection) and frees the
    /// cursor while open, restoring first-person lock on close.
    ///
    /// A lazily created singleton, rebuilt per open so it adapts to each container's slot count.
    /// </summary>
    public sealed class ContainerTransferScreen : MonoBehaviour
    {
        private static ContainerTransferScreen _instance;

        public static ContainerTransferScreen Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameObject("ContainerTransferScreen").AddComponent<ContainerTransferScreen>();
                return _instance;
            }
        }

        private GameObject _canvasGo;
        private GameObject _window;
        private Text _titleText;
        private PlayerViewController _view;
        private InventoryContainer _container;

        private static readonly Color Cyan = new(0.18f, 0.86f, 0.92f, 0.95f);
        private static readonly Color Orange = new(1f, 0.55f, 0.20f, 0.95f);
        private static readonly Color WindowBlue = new(0.07f, 0.13f, 0.21f, 0.98f);
        private static readonly Color PanelBlue = new(0.04f, 0.09f, 0.15f, 0.96f);

        public bool IsOpen { get; private set; }

        public void Open(InventoryContainer container, InventoryComponent player)
        {
            if (container == null || player == null) return;
            if (IsOpen) Close();

            _container = container;
            if (_view == null) _view = FindAnyObjectByType<PlayerViewController>();
            EnsureEventSystem();
            Build(container, player);

            IsOpen = true;
            InventoryUiModal.Push();
            SetCursor(free: true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            InventoryUiModal.Pop();
            if (_window != null) Destroy(_window);
            if (_canvasGo != null) Destroy(_canvasGo);
            _window = null;
            _canvasGo = null;
            SetCursor(free: false);
        }

        public bool IsShowing(InventoryContainer c) => IsOpen && _container == c;

        private void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (kb != null && kb[Key.Escape].wasPressedThisFrame) Close();
        }

        private void SetCursor(bool free)
        {
            // Only meddle with the cursor in first person, where it is normally locked for look.
            if (_view == null || !_view.IsFirstPerson) return;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void Build(InventoryContainer container, InventoryComponent player)
        {
            _canvasGo = new GameObject("ContainerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the hotbar
            var scaler = _canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            // Dimmed full-screen backdrop; clicking it closes the window.
            var backdrop = NewImage(_canvasGo.transform, "Backdrop", new Color(0f, 0.02f, 0.04f, 0.78f), false);
            Stretch((RectTransform)backdrop.transform);
            var backdropBtn = backdrop.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.onClick.AddListener(Close);

            // Centred window.
            _window = NewImage(_canvasGo.transform, "Window", WindowBlue).gameObject;
            var wrt = (RectTransform)_window.transform;
            wrt.anchorMin = wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(1440, 940);
            AddOutline(_window, new Color(0.28f, 0.50f, 0.68f, 0.7f), 4f);
            AddShadow(_window, new Color(0f, 0f, 0f, 0.68f), new Vector2(0f, -10f));

            var vlg = _window.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(32, 32, 26, 20);
            vlg.spacing = 18;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Header row: title + close button.
            var header = NewRow(_window.transform, 118);
            var hlg = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true; hlg.childForceExpandWidth = false; hlg.childControlHeight = true;
            hlg.childForceExpandHeight = false;

            var titleIcon = BuildHeaderIcon(header.transform, "crate", Cyan, new Vector2(106f, 94f));
            titleIcon.GetComponent<LayoutElement>().preferredWidth = 128f;

            var titleColumn = new GameObject("TitleColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            titleColumn.transform.SetParent(header.transform, false);
            titleColumn.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var titleLayout = titleColumn.GetComponent<VerticalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.spacing = 0f;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = true;
            titleLayout.childForceExpandHeight = false;

            _titleText = NewText(titleColumn.transform, container.WindowTitle, font, 54, TextAnchor.MiddleLeft);
            _titleText.fontStyle = FontStyle.Bold;
            AddShadow(_titleText.gameObject, new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -3f));
            var subtitle = NewText(titleColumn.transform, container.Subtitle, font, 26, TextAnchor.MiddleLeft);
            subtitle.color = new Color(0.78f, 0.84f, 0.92f, 0.9f);

            var closeBtn = NewIconButton(header.transform, "X", font, Close);
            closeBtn.GetComponent<LayoutElement>().preferredWidth = 86f;

            // Panels row.
            var panels = NewRow(_window.transform, 682);
            var phlg = panels.gameObject.AddComponent<HorizontalLayoutGroup>();
            phlg.spacing = 24;
            phlg.childControlWidth = true; phlg.childForceExpandWidth = true;
            phlg.childControlHeight = true; phlg.childForceExpandHeight = true;

            var left = BuildPanel(panels.transform, container.ContainerPanelTitle, container.Inventory, font, Cyan, "driver",
                out var leftGrid);
            var right = BuildPanel(panels.transform, "Your Inventory", player, font, Orange, "bag", out var rightGrid);

            // One click moves a stack the other way.
            leftGrid.SetQuickMoveTarget(player);
            rightGrid.SetQuickMoveTarget(container.Inventory);

            var hintRow = NewRow(_window.transform, 48f);
            var hintRowLayout = hintRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hintRowLayout.childAlignment = TextAnchor.MiddleCenter;
            hintRowLayout.childControlWidth = true;
            hintRowLayout.childControlHeight = true;
            hintRowLayout.childForceExpandWidth = false;
            hintRowLayout.childForceExpandHeight = false;
            var hint = BuildHintBar(hintRow, font);
            var hintElement = hint.AddComponent<LayoutElement>();
            hintElement.preferredWidth = 520f;
            hintElement.preferredHeight = 48f;
        }

        // ------------------------------------------------------------------ ui helpers

        private static GameObject BuildPanel(Transform parent, string title, InventoryComponent inv, Font font,
            Color accent, string iconKind,
            out InventoryGridView grid)
        {
            var panel = NewImage(parent, $"Panel_{title}", PanelBlue).gameObject;
            AddOutline(panel, new Color(0.22f, 0.32f, 0.45f, 0.85f), 4f);
            var pvlg = panel.AddComponent<VerticalLayoutGroup>();
            pvlg.padding = new RectOffset(18, 18, 18, 18);
            pvlg.spacing = 16;
            pvlg.childControlWidth = true; pvlg.childForceExpandWidth = true;
            pvlg.childControlHeight = true; pvlg.childForceExpandHeight = false;

            var header = new GameObject("PanelHeader", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(panel.transform, false);
            header.GetComponent<LayoutElement>().preferredHeight = 86f;
            var hlg = header.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            BuildHeaderIcon(header.transform, iconKind, accent, new Vector2(72f, 72f));

            var labelCol = new GameObject("LabelColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            labelCol.transform.SetParent(header.transform, false);
            labelCol.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var labelLayout = labelCol.GetComponent<VerticalLayoutGroup>();
            labelLayout.childAlignment = TextAnchor.MiddleLeft;
            labelLayout.childControlWidth = true;
            labelLayout.childControlHeight = true;
            labelLayout.childForceExpandWidth = true;
            labelLayout.childForceExpandHeight = false;

            var label = NewText(labelCol.transform, title, font, 30, TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold;
            AddShadow(label.gameObject, new Color(0f, 0f, 0f, 0.75f), new Vector2(1.5f, -2f));

            var accentLine = NewImage(labelCol.transform, "AccentLine", accent, false);
            accentLine.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(panel.transform, false);
            gridGo.AddComponent<LayoutElement>().flexibleHeight = 1;
            grid = gridGo.AddComponent<InventoryGridView>();
            grid.SetAccent(accent);
            grid.SetLayout(4, 138f, 18f);
            grid.Bind(inv, title);
            return panel;
        }

        private static GameObject BuildHeaderIcon(Transform parent, string kind, Color accent, Vector2 size)
        {
            var shell = NewImage(parent, $"Icon_{kind}", new Color(0.07f, 0.14f, 0.21f, 0.96f)).gameObject;
            var shellLe = shell.AddComponent<LayoutElement>();
            shellLe.preferredWidth = size.x;
            shellLe.preferredHeight = size.y;
            shellLe.flexibleWidth = 0f;
            AddOutline(shell, accent, 4f);

            var icon = new GameObject("Image", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(shell.transform, false);
            var iconImg = icon.GetComponent<Image>();
            iconImg.sprite = IconFor(kind);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = iconImg.sprite != null ? Color.white : accent;
            var iconRt = (RectTransform)icon.transform;
            iconRt.anchorMin = new Vector2(0.14f, 0.14f);
            iconRt.anchorMax = new Vector2(0.86f, 0.86f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            if (iconImg.sprite == null)
            {
                var label = NewText(shell.transform, kind == "bag" ? "BAG" : kind == "driver" ? "DRV" : "BOX",
                    Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), 20, TextAnchor.MiddleCenter);
                label.fontStyle = FontStyle.Bold;
                Stretch((RectTransform)label.transform);
            }

            return shell;
        }

        private static Button NewIconButton(Transform parent, string label, Font font, System.Action onClick)
        {
            var go = NewImage(parent, "CloseButton", new Color(0.11f, 0.18f, 0.28f, 0.98f)).gameObject;
            go.AddComponent<LayoutElement>().preferredHeight = 86f;
            AddOutline(go, new Color(0.26f, 0.38f, 0.52f, 0.95f), 4f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            var t = NewText(go.transform, label, font, 46, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            AddShadow(t.gameObject, new Color(0f, 0f, 0f, 0.75f), new Vector2(2f, -2f));
            Stretch((RectTransform)t.transform);
            return btn;
        }

        private static GameObject BuildHintBar(Transform parent, Font font)
        {
            var bar = NewImage(parent, "HintBar", new Color(0.06f, 0.12f, 0.20f, 0.9f)).gameObject;
            AddOutline(bar, new Color(0.22f, 0.34f, 0.48f, 0.85f), 2f);
            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 24f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var tap = NewText(bar.transform, "Tap to transfer", font, 20, TextAnchor.MiddleCenter);
            tap.color = new Color(0.78f, 0.88f, 0.96f, 0.92f);
            var split = NewText(bar.transform, "Hold to split", font, 20, TextAnchor.MiddleCenter);
            split.color = new Color(0.78f, 0.88f, 0.96f, 0.92f);
            return bar;
        }

        private static Sprite IconFor(string kind)
        {
            if (kind == "crate" && CarDeliveryIcons.Instance != null)
                return CarDeliveryIcons.Instance.Get("crate");
            if (kind == "driver" && CarDeliveryIcons.Instance != null)
                return CarDeliveryIcons.Instance.Get("truck");
            if (kind == "bag" && ResourceCatalog.Instance != null)
                return ResourceCatalog.Instance.IconOf("battery");
            return null;
        }

        private static void AddOutline(GameObject go, Color color, float size)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, -size);
        }

        private static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static Image NewImage(Transform parent, string name, Color color, bool rounded = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            if (rounded) InventoryUiStyle.Round(img);
            return img;
        }

        private static RectTransform NewRow(Transform parent, float minHeight)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = minHeight;
            layout.preferredHeight = minHeight;
            return (RectTransform)go.transform;
        }

        private static Text NewText(Transform parent, string text, Font font, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white;
            t.text = text; t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private static Button NewButton(Transform parent, string label, Font font, System.Action onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.75f, 0.22f, 0.17f, 0.95f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            go.AddComponent<LayoutElement>();
            var t = NewText(go.transform, label, font, 18, TextAnchor.MiddleCenter);
            Stretch((RectTransform)t.transform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}

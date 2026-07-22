using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// A single shared hover tooltip for item slots: a small rounded pill that shows the
    /// item's readable name (and stack count) and follows the cursor. Lazily created, lives
    /// on its own top-most canvas so it floats above the container/hotbar UI, and is hidden
    /// whenever nothing is hovered. Driven by <see cref="InventorySlotView"/> pointer events.
    /// </summary>
    public sealed class ItemTooltip : MonoBehaviour
    {
        private static ItemTooltip _instance;

        private Canvas _canvas;
        private RectTransform _panel;
        private Text _text;
        private bool _visible;

        public static void Show(string label, Color accent)
        {
            var tip = Ensure();
            tip._text.text = label;
            tip._accent.color = accent;
            if (!tip._visible)
            {
                tip._visible = true;
                tip._panel.gameObject.SetActive(true);
            }
            tip.Reposition();
        }

        public static void Hide()
        {
            if (_instance == null || !_instance._visible) return;
            _instance._visible = false;
            _instance._panel.gameObject.SetActive(false);
        }

        private Image _accent;

        private static ItemTooltip Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("ItemTooltip");
            _instance = go.AddComponent<ItemTooltip>();
            _instance.Build();
            return _instance;
        }

        private void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500; // above the container window (100) and hotbar
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>().enabled = false;

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(transform, false);
            _panel = (RectTransform)panelGo.transform;
            _panel.pivot = new Vector2(0f, 0f);
            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.11f, 0.97f);
            bg.raycastTarget = false;
            InventoryUiStyle.Round(bg);
            var outline = panelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Accent strip on the left, tinted to the item's catalog colour.
            var accentGo = new GameObject("Accent", typeof(RectTransform));
            accentGo.transform.SetParent(panelGo.transform, false);
            var accentRt = (RectTransform)accentGo.transform;
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.anchoredPosition = new Vector2(4f, 0f);
            accentRt.sizeDelta = new Vector2(6f, -12f);
            _accent = accentGo.AddComponent<Image>();
            _accent.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(panelGo.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20f, 8f);
            textRt.offsetMax = new Vector2(-16f, -8f);
            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 26;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleLeft;
            _text.color = new Color(0.96f, 0.98f, 1f);
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;

            _panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_visible) Reposition();
        }

        private void Reposition()
        {
            var mouse = Mouse.current;
            Vector2 p = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;

            // Size the pill to its text, then place it up-and-right of the cursor, clamped
            // so it never spills off screen.
            float width = Mathf.Max(120f, _text.preferredWidth + 40f);
            float height = 48f;
            _panel.sizeDelta = new Vector2(width, height);

            float x = p.x + 22f;
            float y = p.y + 22f;
            if (x + width > Screen.width) x = p.x - width - 18f;
            if (y + height > Screen.height) y = Screen.height - height - 6f;
            _panel.anchoredPosition = new Vector2(x, y);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}

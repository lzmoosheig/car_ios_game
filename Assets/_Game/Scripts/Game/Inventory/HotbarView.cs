using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// A Minecraft-style hotbar: a single row of slots pinned to the bottom-centre of the
    /// screen, bound to a player <see cref="InventoryComponent"/> (its first
    /// <see cref="slotCount"/> slots). One slot is "selected" (the active item), chosen with
    /// the number keys 1-9 or the mouse wheel and drawn with a bright frame.
    ///
    /// It self-hosts: if it isn't already under a Canvas it creates a screen-space overlay one,
    /// and it can auto-find the player's inventory, so dropping this component on any object -
    /// or on the Player - is enough. In first person the character carries items via
    /// <see cref="CarrierView"/>, which mirrors them into the bound inventory, so the hotbar
    /// shows exactly what the player is holding.
    /// </summary>
    public sealed class HotbarView : MonoBehaviour
    {
        [Header("Binding (auto-found if left empty)")]
        [SerializeField] private InventoryComponent target;
        [Tooltip("If set, the hotbar only shows while this view is in first person.")]
        [SerializeField] private PlayerViewController viewController;

        [Header("Layout")]
        [SerializeField] private int slotCount = 9;
        [SerializeField] private float cellSize = 92f;
        [SerializeField] private float spacing = 8f;
        [SerializeField] private float bottomMargin = 28f;

        private readonly List<Image> _frames = new();
        private readonly List<Image> _icons = new();
        private readonly List<Text> _counts = new();
        private RectTransform _row;
        private CanvasGroup _group;
        private int _selected;
        private bool _built;

        public int SelectedIndex => _selected;

        /// <summary>The item id in the selected slot, or null when it's empty. For gameplay use.</summary>
        public string SelectedItemId
        {
            get
            {
                var slot = target != null ? target.Inventory.SlotAt(_selected) : null;
                return slot != null && !slot.IsEmpty ? slot.Stack.ItemId : null;
            }
        }

        public void Bind(InventoryComponent inventory)
        {
            if (target != null) target.Changed -= Refresh;
            target = inventory;
            if (target != null) target.Changed += Refresh;
            Refresh();
        }

        private void Start()
        {
            if (target == null) target = ResolvePlayerInventory();
            if (viewController == null) viewController = FindFirstObjectByType<PlayerViewController>();
            EnsureHost();
            BuildRow();
            if (target != null) target.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (target != null) target.Changed -= Refresh;
        }

        private static InventoryComponent ResolvePlayerInventory()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                var inv = player.GetComponent<InventoryComponent>();
                if (inv != null) return inv;
            }
            return null;
        }

        /// <summary>Make sure we live under a Canvas; create an overlay one if needed.</summary>
        private void EnsureHost()
        {
            if (GetComponentInParent<Canvas>() != null) return;

            var canvasGo = new GameObject("HotbarCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // above the world, below modal popups
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            transform.SetParent(canvasGo.transform, false);
        }

        private void BuildRow()
        {
            if (_built) return;
            _built = true;

            _row = (RectTransform)transform;
            // Toggle visibility via a CanvasGroup rather than SetActive: deactivating our own
            // GameObject would also stop this component's Update, leaving it stuck off.
            _group = gameObject.GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _row.anchorMin = new Vector2(0.5f, 0f);
            _row.anchorMax = new Vector2(0.5f, 0f);
            _row.pivot = new Vector2(0.5f, 0f);
            _row.anchoredPosition = new Vector2(0f, bottomMargin);
            float width = slotCount * cellSize + (slotCount - 1) * spacing;
            _row.sizeDelta = new Vector2(width, cellSize);

            var hlg = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < slotCount; i++) BuildCell(i, font);
        }

        private void BuildCell(int index, Font font)
        {
            var cellGo = new GameObject($"Slot{index + 1}", typeof(RectTransform));
            cellGo.transform.SetParent(transform, false);
            cellGo.AddComponent<LayoutElement>().preferredWidth = cellSize;
            cellGo.GetComponent<LayoutElement>().preferredHeight = cellSize;

            // Outer frame (turns bright white when the slot is selected).
            var frame = cellGo.AddComponent<Image>();
            frame.color = UnselectedFrame;
            _frames.Add(frame);

            // Inner dark panel.
            var innerGo = new GameObject("Inner", typeof(RectTransform));
            innerGo.transform.SetParent(cellGo.transform, false);
            var inner = innerGo.AddComponent<Image>();
            inner.color = new Color(0.09f, 0.10f, 0.12f, 0.85f);
            inner.raycastTarget = false;
            StretchInset((RectTransform)innerGo.transform, 3f);

            // Item icon (swatch or sprite).
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(innerGo.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.raycastTarget = false;
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0.16f, 0.16f);
            iconRt.anchorMax = new Vector2(0.84f, 0.84f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            _icons.Add(icon);

            // Stack count (bottom-right).
            var countGo = new GameObject("Count", typeof(RectTransform));
            countGo.transform.SetParent(innerGo.transform, false);
            var count = countGo.AddComponent<Text>();
            count.raycastTarget = false;
            count.font = font;
            count.fontSize = 24;
            count.fontStyle = FontStyle.Bold;
            count.alignment = TextAnchor.LowerRight;
            count.color = Color.white;
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
            var countRt = (RectTransform)countGo.transform;
            StretchInset(countRt, 4f);
            _counts.Add(count);

            // Slot number (top-left), like Minecraft.
            var numGo = new GameObject("Number", typeof(RectTransform));
            numGo.transform.SetParent(innerGo.transform, false);
            var num = numGo.AddComponent<Text>();
            num.raycastTarget = false;
            num.font = font;
            num.fontSize = 14;
            num.alignment = TextAnchor.UpperLeft;
            num.color = new Color(1f, 1f, 1f, 0.5f);
            num.text = (index + 1).ToString();
            StretchInset((RectTransform)numGo.transform, 4f);
        }

        private static readonly Color UnselectedFrame = new(1f, 1f, 1f, 0.18f);
        private static readonly Color SelectedFrame = new(1f, 1f, 1f, 0.95f);

        private static void StretchInset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private void Update()
        {
            // Show only in first person when a view controller is wired. Fade via CanvasGroup
            // so Update keeps running even while hidden.
            bool show = viewController == null || viewController.IsFirstPerson;
            if (_group != null)
            {
                _group.alpha = show ? 1f : 0f;
                _group.blocksRaycasts = show;
            }
            if (!show) return;

            HandleSelectionInput();
        }

        private void HandleSelectionInput()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                for (int i = 0; i < slotCount && i < 9; i++)
                {
                    var key = Key.Digit1 + i; // Digit1..Digit9 are contiguous
                    if (kb[key].wasPressedThisFrame) Select(i);
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.01f) Select((_selected - 1 + slotCount) % slotCount);
                else if (scroll < -0.01f) Select((_selected + 1) % slotCount);
            }
        }

        private void Select(int index)
        {
            if (index == _selected || index < 0 || index >= slotCount) return;
            _selected = index;
            Refresh();
        }

        private void Refresh()
        {
            if (!_built || target == null) return;
            var inv = target.Inventory;
            var catalog = ResourceCatalog.Instance;

            for (int i = 0; i < _frames.Count; i++)
            {
                _frames[i].color = i == _selected ? SelectedFrame : UnselectedFrame;

                var slot = inv.SlotAt(i);
                bool empty = slot == null || slot.IsEmpty;
                if (empty)
                {
                    _icons[i].enabled = false;
                    _counts[i].text = "";
                    continue;
                }

                var stack = slot.Stack;
                Sprite icon = catalog != null ? catalog.IconOf(stack.ItemId) : null;
                _icons[i].enabled = true;
                _icons[i].sprite = icon;
                _icons[i].color = icon != null ? Color.white : (catalog != null ? catalog.ColorOf(stack.ItemId) : Color.gray);
                _counts[i].text = stack.Count > 1 ? stack.Count.ToString() : "";
            }
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// One cell of an <see cref="InventoryGridView"/>: a background frame, an item icon (a
    /// coloured swatch when the catalog has no sprite), and a stack-count label. Purely a
    /// view - it formats an <see cref="Overhaul.Core.ItemStack"/> onto widgets and forwards
    /// left/right clicks to the grid. Built at runtime by the grid, so no prefab wiring.
    /// Left-click = pick up / drop (move &amp; merge). Right-click = split.
    /// </summary>
    public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _background;
        private Image _icon;
        private Image _countBadge;
        private Text _countText;
        private Text _centerText;
        private Outline _outline;
        private Color _accent = new(0.18f, 0.86f, 0.92f, 0.85f);

        // The readable item name + colour shown in the hover tooltip; null while the slot is empty.
        private string _tooltip;
        private Color _tooltipColor = Color.white;
        private bool _hovered;
        private bool _locked;

        private static readonly Color LockedTint = new(0.05f, 0.08f, 0.12f, 0.85f);
        private static readonly Color LockedBorder = new(0.30f, 0.36f, 0.44f, 0.55f);
        private static readonly Color BuyBorder = new(0.30f, 0.82f, 0.48f, 0.95f);

        public int Index { get; private set; }
        public InventoryGridView Grid { get; private set; }

        // The cell background is a frosted-glass sprite; these are hue/brightness tints applied
        // over it (alpha 1 keeps the baked glass translucency). Empty cells sit a touch dimmer,
        // the picked-up cell glows warm.
        private static readonly Color EmptyGlass = new(0.93f, 0.96f, 1f, 0.94f);
        private static readonly Color FilledGlass = Color.white;
        private static readonly Color SelectedGlass = new(1f, 0.80f, 0.45f, 1f);

        public void Init(InventoryGridView grid, int index, Image background, Image icon, Text countText,
            Image countBadge = null, Outline outline = null, Color? accent = null, Text centerText = null)
        {
            Grid = grid;
            Index = index;
            _background = background;
            _icon = icon;
            _countText = countText;
            _countBadge = countBadge;
            _outline = outline;
            _centerText = centerText;
            if (accent.HasValue) _accent = accent.Value;
        }

        /// <summary>Renders this slot as a locked (buyable) warehouse expansion slot.</summary>
        public void BindLocked(bool isNextToBuy, bool affordable, string line1, string line2)
        {
            _locked = true;
            _tooltip = null;
            if (_background != null) _background.color = LockedTint;
            if (_outline != null) _outline.effectColor = isNextToBuy && affordable ? BuyBorder : LockedBorder;
            if (_icon != null) _icon.enabled = false;
            if (_countText != null) _countText.text = "";
            if (_countBadge != null) _countBadge.gameObject.SetActive(false);
            if (_centerText != null)
            {
                _centerText.gameObject.SetActive(true);
                _centerText.text = string.IsNullOrEmpty(line2) ? line1 : $"{line1}\n{line2}";
                _centerText.color = isNextToBuy
                    ? (affordable ? new Color(0.55f, 0.95f, 0.65f) : new Color(1f, 0.72f, 0.30f))
                    : new Color(0.60f, 0.66f, 0.74f);
            }
        }

        public void Bind(bool isEmpty, Sprite icon, Color color, int count, bool selected,
            string displayName = null)
        {
            _locked = false;
            if (_centerText != null) _centerText.gameObject.SetActive(false);

            // Tooltip text: "Tires ×8" while filled, nothing when empty. Kept live so a hover
            // that is already open updates as stacks change under the cursor.
            _tooltipColor = color;
            _tooltip = isEmpty ? null
                : (count > 1 ? $"{displayName} ×{count}" : displayName);
            if (_hovered)
            {
                if (string.IsNullOrEmpty(_tooltip)) ItemTooltip.Hide();
                else ItemTooltip.Show(_tooltip, _tooltipColor);
            }

            if (_background != null)
                _background.color = selected ? SelectedGlass : (isEmpty ? EmptyGlass : FilledGlass);

            if (_icon != null)
            {
                _icon.enabled = !isEmpty;
                _icon.sprite = icon;
                // With no sprite the icon is a solid colour swatch; with a sprite, tint white.
                _icon.color = icon != null ? Color.white : color;
            }

            if (_countText != null)
                _countText.text = (!isEmpty && count > 1) ? count.ToString() : "";

            // Optional legacy badge chip (unused by the glass grid, kept for other callers).
            if (_countBadge != null)
            {
                bool showBadge = !isEmpty && count > 1;
                _countBadge.gameObject.SetActive(showBadge);
                _countBadge.color = _accent;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Grid == null) return;
            if (_locked) { Grid.OnLockedSlotClicked(this); return; }
            if (eventData.button == PointerEventData.InputButton.Right)
                Grid.OnSlotRightClicked(this);
            else
                Grid.OnSlotLeftClicked(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            if (!string.IsNullOrEmpty(_tooltip)) ItemTooltip.Show(_tooltip, _tooltipColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ItemTooltip.Hide();
        }

        private void OnDisable()
        {
            if (_hovered) { _hovered = false; ItemTooltip.Hide(); }
        }
    }
}

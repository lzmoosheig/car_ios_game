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
    public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler
    {
        private Image _background;
        private Image _icon;
        private Image _countBadge;
        private Text _countText;
        private Outline _outline;
        private Color _accent = new(0.18f, 0.86f, 0.92f, 0.85f);

        public int Index { get; private set; }
        public InventoryGridView Grid { get; private set; }

        private static readonly Color EmptyTint = new(0.06f, 0.11f, 0.17f, 0.88f);
        private static readonly Color FilledTint = new(0.09f, 0.16f, 0.23f, 0.96f);
        private static readonly Color SelectedTint = new(0.24f, 0.16f, 0.08f, 0.98f);
        private static readonly Color EmptyBorder = new(0.30f, 0.39f, 0.50f, 0.65f);
        private static readonly Color SelectedBorder = new(1.00f, 0.55f, 0.20f, 1f);

        public void Init(InventoryGridView grid, int index, Image background, Image icon, Text countText,
            Image countBadge = null, Outline outline = null, Color? accent = null)
        {
            Grid = grid;
            Index = index;
            _background = background;
            _icon = icon;
            _countText = countText;
            _countBadge = countBadge;
            _outline = outline;
            if (accent.HasValue) _accent = accent.Value;
        }

        public void Bind(bool isEmpty, Sprite icon, Color color, int count, bool selected)
        {
            if (_background != null)
                _background.color = selected ? SelectedTint : (isEmpty ? EmptyTint : FilledTint);

            if (_outline != null)
                _outline.effectColor = selected ? SelectedBorder : (isEmpty ? EmptyBorder : _accent);

            if (_icon != null)
            {
                _icon.enabled = !isEmpty;
                _icon.sprite = icon;
                // With no sprite the icon is a solid colour swatch; with a sprite, tint white.
                _icon.color = icon != null ? Color.white : color;
            }

            if (_countText != null)
                _countText.text = (!isEmpty && count > 1) ? count.ToString() : "";

            if (_countBadge != null)
            {
                bool showBadge = !isEmpty && count > 1;
                _countBadge.gameObject.SetActive(showBadge);
                _countBadge.color = selected ? SelectedBorder : _accent;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Grid == null) return;
            if (eventData.button == PointerEventData.InputButton.Right)
                Grid.OnSlotRightClicked(this);
            else
                Grid.OnSlotLeftClicked(this);
        }
    }
}

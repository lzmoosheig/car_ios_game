using UnityEngine;
using UnityEngine.UI;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// A Minecraft-style grid of <see cref="InventorySlotView"/> cells bound to one
    /// <see cref="InventoryComponent"/>. It builds its own cells at runtime (a
    /// <see cref="GridLayoutGroup"/> of frames), so nothing needs pre-authoring in a prefab,
    /// and refreshes whenever the underlying inventory changes.
    ///
    /// Interaction (click-to-move, which the project's uGUI HUD already relies on):
    /// * Left-click a filled slot to pick it up; left-click another slot to drop it there -
    ///   merging matching stacks or swapping. Works within a grid and between two grids
    ///   (e.g. player &lt;-&gt; a building's storage) via <see cref="InventoryTransfer"/>.
    /// * Right-click a slot to split its stack in half into a free slot.
    /// The selected (picked-up) slot is tracked statically so any two grids cooperate.
    /// </summary>
    public sealed class InventoryGridView : MonoBehaviour
    {
        [SerializeField] private InventoryComponent target;
        [SerializeField] private int columns = 4;
        [SerializeField] private float cellSize = 84f;
        [SerializeField] private float spacing = 6f;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Color accentColor = new(0.18f, 0.86f, 0.92f, 0.9f);

        private readonly System.Collections.Generic.List<InventorySlotView> _cells = new();
        private bool _built;

        // When set, a left-click on a filled slot moves that whole stack straight into this
        // other inventory (Minecraft "shift-click quick move"), instead of the pick-up/drop
        // flow. Used by the container transfer screen so one click takes an item.
        private InventoryComponent _quickMoveTarget;
        public void SetQuickMoveTarget(InventoryComponent other) => _quickMoveTarget = other;

        // Cross-grid pick-up selection (only one item can be "held" at a time).
        private static InventoryGridView _selGrid;
        private static int _selIndex = -1;

        public InventoryComponent Target => target;

        /// <summary>Sets grid layout. Must be called before the cells are built (before Bind/Start).</summary>
        public void SetLayout(int cols, float cell = 84f, float gap = 6f)
        {
            columns = Mathf.Max(1, cols);
            cellSize = cell;
            spacing = gap;
        }

        public void SetAccent(Color color) => accentColor = color;

        public void Bind(InventoryComponent inventory, string title = null)
        {
            if (target != null) target.Changed -= Refresh;
            target = inventory;
            if (titleLabel != null && title != null) titleLabel.text = title;
            if (!_built) BuildCells();
            if (target != null) target.Changed += Refresh;
            Refresh();
        }

        private void Start()
        {
            if (!_built) BuildCells();
            if (target != null) target.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (target != null) target.Changed -= Refresh;
            if (_selGrid == this) ClearSelection();
        }

        private void BuildCells()
        {
            _built = true;
            int count = target != null ? target.SlotCount : columns * 3;

            var grid = GetComponent<GridLayoutGroup>();
            if (grid == null) grid = gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.spacing = new Vector2(spacing, spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, columns);

            for (int i = 0; i < count; i++)
                _cells.Add(BuildCell(i));
        }

        private InventorySlotView BuildCell(int index)
        {
            var cellGo = new GameObject($"Slot{index}", typeof(RectTransform));
            cellGo.transform.SetParent(transform, false);

            var bg = cellGo.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.11f, 0.17f, 0.88f);
            InventoryUiStyle.Round(bg);

            var outline = cellGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.39f, 0.50f, 0.65f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Icon child (fills most of the cell).
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cellGo.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0.12f, 0.16f);
            iconRt.anchorMax = new Vector2(0.88f, 0.88f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            var badgeGo = new GameObject("CountBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(cellGo.transform, false);
            var badge = badgeGo.AddComponent<Image>();
            badge.color = accentColor;
            badge.raycastTarget = false;
            InventoryUiStyle.Round(badge);
            var badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(1f, 0f);
            badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot = new Vector2(1f, 0f);
            badgeRt.anchoredPosition = new Vector2(-8f, 8f);
            badgeRt.sizeDelta = new Vector2(34f, 34f);

            var countGo = new GameObject("Count", typeof(RectTransform));
            countGo.transform.SetParent(badgeGo.transform, false);
            var count = countGo.AddComponent<Text>();
            count.raycastTarget = false;
            count.alignment = TextAnchor.MiddleCenter;
            count.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            count.fontSize = 22;
            count.fontStyle = FontStyle.Bold;
            count.color = Color.white;
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
            var countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = Vector2.zero;
            countRt.anchorMax = Vector2.one;
            countRt.offsetMin = Vector2.zero;
            countRt.offsetMax = Vector2.zero;

            var view = cellGo.AddComponent<InventorySlotView>();
            view.Init(this, index, bg, icon, count, badge, outline, accentColor);
            return view;
        }

        private void Refresh()
        {
            if (target == null) return;
            var inv = target.Inventory;
            var catalog = ResourceCatalog.Instance;

            for (int i = 0; i < _cells.Count; i++)
            {
                var slot = inv.SlotAt(i);
                bool empty = slot == null || slot.IsEmpty;
                bool selected = _selGrid == this && _selIndex == i;

                Sprite icon = null;
                Color color = Color.gray;
                int amount = 0;
                if (!empty)
                {
                    amount = slot.Stack.Count;
                    if (catalog != null)
                    {
                        icon = catalog.IconOf(slot.Stack.ItemId);
                        color = catalog.ColorOf(slot.Stack.ItemId);
                    }
                }
                _cells[i].Bind(empty, icon, color, amount, selected);
            }
        }

        // ------------------------------------------------------------- click handling

        public void OnSlotLeftClicked(InventorySlotView cell)
        {
            if (target == null) return;
            var slot = target.Inventory.SlotAt(cell.Index);

            // Quick-move mode (container screens): one click sends the whole stack across.
            if (_quickMoveTarget != null)
            {
                if (slot == null || slot.IsEmpty) return;
                InventoryTransfer.Transfer(target.Inventory, _quickMoveTarget.Inventory,
                    slot.Stack.ItemId, slot.Stack.Count);
                ClearSelection();
                Refresh();
                return;
            }

            if (_selGrid == null)
            {
                // Pick up a non-empty slot.
                if (slot == null || slot.IsEmpty) return;
                _selGrid = this;
                _selIndex = cell.Index;
                Refresh();
                return;
            }

            // Drop the held stack onto this slot.
            if (_selGrid == this)
            {
                target.Inventory.MoveOrMerge(_selIndex, cell.Index);
            }
            else
            {
                // Cross-inventory move: push the held item into this inventory.
                var srcInv = _selGrid.target.Inventory;
                var held = srcInv.SlotAt(_selIndex);
                if (held != null && !held.IsEmpty)
                    InventoryTransfer.Transfer(srcInv, target.Inventory, held.Stack.ItemId, held.Stack.Count);
                _selGrid.Refresh();
            }

            ClearSelection();
            Refresh();
        }

        public void OnSlotRightClicked(InventorySlotView cell)
        {
            if (target == null) return;
            target.Inventory.SplitStack(cell.Index);
            ClearSelection();
            Refresh();
        }

        private static void ClearSelection()
        {
            var prev = _selGrid;
            _selGrid = null;
            _selIndex = -1;
            if (prev != null) prev.Refresh();
        }
    }
}

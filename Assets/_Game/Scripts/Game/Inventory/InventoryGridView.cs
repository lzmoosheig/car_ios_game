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

        // When set, slots past the inventory's UnlockedSlots render as buyable warehouse
        // expansion slots and clicking the next one purchases it.
        private PartsWarehouse _warehouse;
        public void SetWarehouse(PartsWarehouse warehouse) => _warehouse = warehouse;

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
            InventoryUiStyle.Glass(bg, 22);

            // Icon child (fills most of the cell) with a soft drop shadow so the cut-out part
            // lifts off the glass.
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cellGo.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var iconShadow = iconGo.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            iconShadow.effectDistance = new Vector2(2.5f, -3.5f);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0.14f, 0.14f);
            iconRt.anchorMax = new Vector2(0.86f, 0.86f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            // Plain white stack count in the bottom-right corner (no badge chip).
            var countGo = new GameObject("Count", typeof(RectTransform));
            countGo.transform.SetParent(cellGo.transform, false);
            var count = countGo.AddComponent<Text>();
            count.raycastTarget = false;
            count.alignment = TextAnchor.LowerRight;
            count.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            count.fontSize = 30;
            count.fontStyle = FontStyle.Bold;
            count.color = Color.white;
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
            count.verticalOverflow = VerticalWrapMode.Overflow;
            var countShadow = countGo.AddComponent<Shadow>();
            countShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            countShadow.effectDistance = new Vector2(1.5f, -1.5f);
            var countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = Vector2.zero;
            countRt.anchorMax = Vector2.one;
            countRt.offsetMin = new Vector2(0f, 8f);
            countRt.offsetMax = new Vector2(-14f, 0f);

            // Centre label, used only by locked (buyable) warehouse slots to show level/price.
            var centerGo = new GameObject("Center", typeof(RectTransform));
            centerGo.transform.SetParent(cellGo.transform, false);
            var center = centerGo.AddComponent<Text>();
            center.raycastTarget = false;
            center.alignment = TextAnchor.MiddleCenter;
            center.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            center.fontSize = 20;
            center.fontStyle = FontStyle.Bold;
            center.horizontalOverflow = HorizontalWrapMode.Overflow;
            center.verticalOverflow = VerticalWrapMode.Overflow;
            var centerRt = (RectTransform)centerGo.transform;
            centerRt.anchorMin = Vector2.zero; centerRt.anchorMax = Vector2.one;
            centerRt.offsetMin = Vector2.zero; centerRt.offsetMax = Vector2.zero;
            centerGo.SetActive(false);

            var view = cellGo.AddComponent<InventorySlotView>();
            view.Init(this, index, bg, icon, count, null, null, accentColor, center);
            return view;
        }

        private void Refresh()
        {
            if (target == null) return;
            var inv = target.Inventory;
            var catalog = ResourceCatalog.Instance;

            for (int i = 0; i < _cells.Count; i++)
            {
                // Locked warehouse expansion slot: show its level/price instead of an item.
                if (i >= inv.UnlockedSlots)
                {
                    RenderLocked(_cells[i], i);
                    continue;
                }

                var slot = inv.SlotAt(i);
                bool empty = slot == null || slot.IsEmpty;
                bool selected = _selGrid == this && _selIndex == i;

                Sprite icon = null;
                Color color = Color.gray;
                int amount = 0;
                string displayName = null;
                if (!empty)
                {
                    amount = slot.Stack.Count;
                    if (catalog != null)
                    {
                        icon = catalog.IconOf(slot.Stack.ItemId);
                        color = catalog.ColorOf(slot.Stack.ItemId);
                        displayName = catalog.NameOf(slot.Stack.ItemId);
                    }
                    else displayName = slot.Stack.ItemId;
                }
                _cells[i].Bind(empty, icon, color, amount, selected, displayName);
            }
        }

        /// <summary>Draws a locked slot: the next one to buy shows its price, the rest their level gate.</summary>
        private void RenderLocked(InventorySlotView cell, int index)
        {
            int required = PartsWarehouse.RequiredLevel(index);
            int price = PartsWarehouse.Price(index);
            bool isNext = _warehouse != null && index == _warehouse.NextSlotIndex;
            bool levelMet = _warehouse != null && _warehouse.PlayerLevel >= required;

            if (isNext && levelMet)
                cell.BindLocked(true, _warehouse.CanBuyNext, "BUY", $"${price}"); // reachable purchase
            else
                cell.BindLocked(false, false, "LOCKED", $"Lv {required}");        // still level-gated
        }

        /// <summary>Clicking a locked slot tries to buy the next one, or explains why it can't.</summary>
        public void OnLockedSlotClicked(InventorySlotView cell)
        {
            if (_warehouse == null) return;
            if (cell.Index != _warehouse.NextSlotIndex)
            {
                ScreenToast.Show($"Unlock slots in order — reach level {PartsWarehouse.RequiredLevel(cell.Index)}.");
                return;
            }
            string reason = _warehouse.BlockReason;
            if (reason != null) { ScreenToast.Show(reason + " to unlock this slot."); return; }
            if (_warehouse.TryBuyNextSlot()) Refresh();
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

using System;
using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>
    /// A Minecraft-style bank of <see cref="InventorySlot"/>s and the reusable heart of the
    /// inventory system: the player, buildings, storage containers and workers all own one.
    /// Nothing here is car-parts specific - item metadata comes from an injected
    /// <see cref="IItemDatabase"/>, and an optional inventory-wide <see cref="AllowedFilter"/>
    /// (plus per-slot filters) gates what may enter, which is how role/building rules are
    /// expressed. Engine-agnostic and fully unit-tested.
    /// </summary>
    public sealed class SlotInventory
    {
        private readonly InventorySlot[] _slots;
        private readonly IItemDatabase _db;
        private int _unlockedSlots;

        /// <summary>Inventory-wide rule (e.g. a parts courier that only carries "Part" items).
        /// Null means "accept anything the individual slots allow".</summary>
        public Func<string, bool> AllowedFilter { get; set; }

        /// <summary>Raised after any mutation so views and persistence can refresh.</summary>
        public event Action Changed;

        public SlotInventory(int slotCount, IItemDatabase db, Func<string, bool> allowedFilter = null)
        {
            if (slotCount < 1) slotCount = 1;
            _db = db;
            AllowedFilter = allowedFilter;
            _slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount; i++) _slots[i] = new InventorySlot();
            _unlockedSlots = slotCount; // every slot usable unless a container locks some
        }

        public int SlotCount => _slots.Length;
        public IReadOnlyList<InventorySlot> Slots => _slots;

        /// <summary>
        /// How many leading slots are usable. Slots at index >= this are LOCKED: nothing can be
        /// stored there and the UI shows them as buyable (Parts Warehouse expansion). Defaults to
        /// the full capacity, so ordinary inventories behave exactly as before.
        /// </summary>
        public int UnlockedSlots
        {
            get => _unlockedSlots;
            set
            {
                int v = value < 0 ? 0 : (value > _slots.Length ? _slots.Length : value);
                if (v == _unlockedSlots) return;
                _unlockedSlots = v;
                Changed?.Invoke();
            }
        }

        public bool IsSlotUnlocked(int index) => index >= 0 && index < _unlockedSlots;

        public InventorySlot SlotAt(int index)
            => (index >= 0 && index < _slots.Length) ? _slots[index] : null;

        public int MaxStackOf(string itemId) => _db.MaxStackOf(itemId);

        /// <summary>Inventory-wide acceptance (does not consider a specific slot's own filter).</summary>
        public bool Allows(string itemId)
            => !string.IsNullOrEmpty(itemId) && (AllowedFilter == null || AllowedFilter(itemId));

        public int CountOf(string itemId)
        {
            int n = 0;
            foreach (var s in _slots)
                if (!s.IsEmpty && s.Stack.ItemId == itemId) n += s.Stack.Count;
            return n;
        }

        public bool IsEmpty
        {
            get { foreach (var s in _slots) if (!s.IsEmpty) return false; return true; }
        }

        public bool IsFull
        {
            get { for (int i = 0; i < _unlockedSlots; i++) if (_slots[i].IsEmpty) return false; return true; }
        }

        public int FirstEmptyIndex()
        {
            for (int i = 0; i < _unlockedSlots; i++) if (_slots[i].IsEmpty) return i;
            return -1;
        }

        /// <summary>How many more of an item can still fit (partial matching stacks + empty slots).</summary>
        public int RoomFor(string itemId)
        {
            if (!Allows(itemId)) return 0;
            int max = MaxStackOf(itemId);
            int room = 0;
            for (int i = 0; i < _unlockedSlots; i++)
            {
                var s = _slots[i];
                if (s.IsEmpty) { if (s.Accepts(itemId)) room += max; }
                else if (s.Stack.ItemId == itemId) room += Math.Max(0, max - s.Stack.Count);
            }
            return room;
        }

        /// <summary>
        /// Adds items, topping up matching partial stacks first and then filling empty slots
        /// (the classic Minecraft merge order). Returns the remainder that did not fit.
        /// </summary>
        public int Add(string itemId, int count)
        {
            if (count <= 0 || !Allows(itemId)) return count;
            int max = MaxStackOf(itemId);
            int remaining = count;

            for (int i = 0; i < _unlockedSlots && remaining > 0; i++)
            {
                var s = _slots[i];
                if (!s.IsEmpty && s.Stack.ItemId == itemId)
                    remaining -= s.Add(itemId, remaining, max);
            }
            for (int i = 0; i < _unlockedSlots && remaining > 0; i++)
            {
                var s = _slots[i];
                if (s.IsEmpty) remaining -= s.Add(itemId, remaining, max);
            }

            if (remaining != count) Changed?.Invoke();
            return remaining;
        }

        /// <summary>All-or-nothing add: only commits if every unit fits.</summary>
        public bool TryAddAll(string itemId, int count)
        {
            if (count <= 0) return true;
            if (RoomFor(itemId) < count) return false;
            Add(itemId, count);
            return true;
        }

        /// <summary>Removes up to <paramref name="count"/> of an item; returns the number removed.</summary>
        public int Remove(string itemId, int count)
        {
            if (count <= 0) return 0;
            int removed = 0;
            for (int i = 0; i < _slots.Length && removed < count; i++)
            {
                var s = _slots[i];
                if (!s.IsEmpty && s.Stack.ItemId == itemId)
                    removed += s.Remove(count - removed);
            }
            if (removed > 0) Changed?.Invoke();
            return removed;
        }

        // ------------------------------------------------------------- slot UI operations

        /// <summary>
        /// Moves/merges the stack in slot <paramref name="from"/> onto slot <paramref name="to"/>
        /// within this inventory. Matching stacks merge up to max (any leftover stays behind);
        /// otherwise the two slots swap. Both slots' filters are respected. Returns true if
        /// anything changed. This backs click-to-move / drag-drop in the UI.
        /// </summary>
        public bool MoveOrMerge(int from, int to)
        {
            if (from == to) return false;
            if (to >= _unlockedSlots) return false; // can't drop into a locked slot
            var a = SlotAt(from);
            var b = SlotAt(to);
            if (a == null || b == null || a.IsEmpty) return false;

            if (b.IsEmpty)
            {
                if (!b.Accepts(a.Stack.ItemId)) return false;
                b.Set(a.Stack);
                a.Clear();
                Changed?.Invoke();
                return true;
            }

            if (b.Stack.ItemId == a.Stack.ItemId)
            {
                int moved = b.Add(a.Stack.ItemId, a.Stack.Count, MaxStackOf(a.Stack.ItemId));
                if (moved <= 0) return false; // destination already full, nothing to swap
                a.Remove(moved);
                Changed?.Invoke();
                return true;
            }

            return SwapIfAllowed(a, b);
        }

        private bool SwapIfAllowed(InventorySlot a, InventorySlot b)
        {
            if (!a.Accepts(b.Stack.ItemId) || !b.Accepts(a.Stack.ItemId)) return false;
            var tmp = a.Stack;
            a.Set(b.Stack);
            b.Set(tmp);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Splits <paramref name="amount"/> (default: half) out of slot <paramref name="from"/>
        /// into the first empty accepting slot. Mirrors a Minecraft right-click split. Returns
        /// true on success.
        /// </summary>
        public bool SplitStack(int from, int amount = -1)
        {
            var a = SlotAt(from);
            if (a == null || a.IsEmpty || a.Stack.Count < 2) return false;

            int move = amount > 0 ? Math.Min(amount, a.Stack.Count - 1) : a.Stack.Count / 2;
            if (move <= 0) return false;

            for (int i = 0; i < _unlockedSlots; i++)
            {
                var s = _slots[i];
                if (!s.IsEmpty || !s.Accepts(a.Stack.ItemId)) continue;
                s.Set(new ItemStack(a.Stack.ItemId, move));
                a.Remove(move);
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            foreach (var s in _slots) s.Clear();
            Changed?.Invoke();
        }

        // -------------------------------------------------------------------- persistence

        public InventorySave Capture()
        {
            var save = new InventorySave { SlotCount = _slots.Length, UnlockedSlots = _unlockedSlots };
            foreach (var s in _slots)
            {
                save.Slots.Add(s.IsEmpty
                    ? new InventorySlotSave()
                    : new InventorySlotSave { ItemId = s.Stack.ItemId, Count = s.Stack.Count });
            }
            return save;
        }

        public void Restore(InventorySave save)
        {
            if (save?.Slots == null) return;
            if (save.UnlockedSlots > 0) UnlockedSlots = save.UnlockedSlots; // 0 => legacy save, keep all unlocked
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i < save.Slots.Count)
                {
                    var slot = save.Slots[i];
                    _slots[i].Set(string.IsNullOrEmpty(slot.ItemId) || slot.Count <= 0
                        ? ItemStack.Empty
                        : new ItemStack(slot.ItemId, slot.Count));
                }
                else _slots[i].Clear();
            }
            Changed?.Invoke();
        }
    }
}

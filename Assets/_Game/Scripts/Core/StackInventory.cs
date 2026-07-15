using System;
using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>
    /// The player's / an employee's carried stack. Capacity is measured in slots;
    /// bulky resources (engine, body panel) cost more than one slot. Removal is
    /// type-filtered: a station pulls the resources it needs from anywhere in the
    /// stack, so items are never "trapped" underneath others. See Doc 02 §1.3.
    /// Engine-agnostic (no UnityEngine); the visual tower is a separate view.
    /// </summary>
    public sealed class StackInventory
    {
        private readonly List<string> _items = new();      // one entry per unit, pickup order
        private readonly Func<string, int> _slotsOf;       // resourceId -> slot cost (>=1)

        public int CapacitySlots { get; private set; }
        public IReadOnlyList<string> Items => _items;
        public int Count => _items.Count;

        public StackInventory(int capacitySlots, Func<string, int> slotsOf)
        {
            if (slotsOf == null) throw new ArgumentNullException(nameof(slotsOf));
            CapacitySlots = capacitySlots;
            _slotsOf = slotsOf;
        }

        public int UsedSlots
        {
            get
            {
                int used = 0;
                for (int i = 0; i < _items.Count; i++) used += _slotsOf(_items[i]);
                return used;
            }
        }

        public int FreeSlots => CapacitySlots - UsedSlots;

        public bool CanAdd(string resourceId) => _slotsOf(resourceId) <= FreeSlots;

        public bool TryAdd(string resourceId)
        {
            if (!CanAdd(resourceId)) return false;
            _items.Add(resourceId);
            return true;
        }

        public int CountOf(string resourceId)
        {
            int n = 0;
            for (int i = 0; i < _items.Count; i++) if (_items[i] == resourceId) n++;
            return n;
        }

        /// <summary>
        /// Removes up to <paramref name="count"/> units of a type from anywhere in the
        /// stack (newest first for a pleasing "top items fly out" visual). Returns the
        /// number actually removed.
        /// </summary>
        public int Remove(string resourceId, int count)
        {
            int removed = 0;
            for (int i = _items.Count - 1; i >= 0 && removed < count; i--)
            {
                if (_items[i] == resourceId)
                {
                    _items.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>Upgrades only ever grow capacity (Doc 02 §1.6 edge case).</summary>
        public void SetCapacity(int slots)
        {
            if (slots < CapacitySlots) return;
            CapacitySlots = slots;
        }

        public void Clear() => _items.Clear();
    }
}

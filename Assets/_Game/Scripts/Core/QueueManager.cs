using System;
using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>
    /// Slot-based customer queue. Vehicles never free-drive: each owns exactly one discrete
    /// slot and advances forward as the front frees, which makes collisions structurally
    /// impossible (Doc 02 §3.3). Slot count is an upgrade (Doc 04 §3, Doc 09 §6.9).
    /// Engine-agnostic so the whole demand pipeline is unit-testable without the play loop.
    /// </summary>
    public sealed class QueueManager
    {
        private readonly List<string> _slots = new(); // index 0 == front; null == empty

        public QueueManager(int slotCount)
        {
            if (slotCount < 1) throw new ArgumentOutOfRangeException(nameof(slotCount));
            for (int i = 0; i < slotCount; i++) _slots.Add(null);
        }

        public int SlotCount => _slots.Count;
        public string Front => _slots[0];
        public bool IsFull => Occupancy >= SlotCount;
        public bool IsEmpty => Occupancy == 0;

        public int Occupancy
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _slots.Count; i++) if (_slots[i] != null) n++;
                return n;
            }
        }

        /// <summary>Index of the slot holding this vehicle, or -1.</summary>
        public int IndexOf(string vehicleId) => _slots.IndexOf(vehicleId);

        /// <summary>
        /// Joins at the rearmost free slot. Returns the slot index, or -1 when full
        /// (the caller pauses arrivals rather than dropping the customer — Doc 02 §3.2).
        /// </summary>
        public int TryEnqueue(string vehicleId)
        {
            if (vehicleId == null) throw new ArgumentNullException(nameof(vehicleId));
            if (_slots.Contains(vehicleId)) return _slots.IndexOf(vehicleId);

            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                if (_slots[i] != null) continue;
                // Only settle here if every slot ahead is occupied; otherwise keep walking
                // forward so cars close up rather than leaving holes.
                int target = i;
                while (target > 0 && _slots[target - 1] == null) target--;
                _slots[target] = vehicleId;
                return target;
            }
            return -1;
        }

        /// <summary>Releases the front slot (vehicle dispatched to a bay) and closes the gap.</summary>
        public bool TryDequeueFront(out string vehicleId)
        {
            vehicleId = _slots[0];
            if (vehicleId == null) return false;
            _slots[0] = null;
            Advance();
            return true;
        }

        /// <summary>Removes a vehicle from anywhere in the queue (e.g. it left).</summary>
        public bool Remove(string vehicleId)
        {
            int i = _slots.IndexOf(vehicleId);
            if (i < 0) return false;
            _slots[i] = null;
            Advance();
            return true;
        }

        /// <summary>Shuffles occupants forward into empty slots, preserving order.</summary>
        public void Advance()
        {
            int write = 0;
            for (int read = 0; read < _slots.Count; read++)
            {
                if (_slots[read] == null) continue;
                var id = _slots[read];
                _slots[read] = null;
                _slots[write++] = id;
            }
        }

        /// <summary>Queue-slot upgrades only ever add capacity (Doc 04 §3).</summary>
        public void SetSlotCount(int count)
        {
            if (count <= _slots.Count) return;
            while (_slots.Count < count) _slots.Add(null);
        }

        public IReadOnlyList<string> Slots => _slots;
    }
}

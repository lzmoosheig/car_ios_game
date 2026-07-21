using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>
    /// Moves items between two <see cref="SlotInventory"/> instances - the single code path
    /// behind every hand-off in the game: building -> worker, worker -> building,
    /// building -> building, player &lt;-&gt; container. Every transfer is partial-safe: it
    /// moves only as much as the destination will hold and reports exactly how much crossed,
    /// so items are never destroyed against a full target.
    /// </summary>
    public static class InventoryTransfer
    {
        /// <summary>
        /// Transfers up to <paramref name="requested"/> of <paramref name="itemId"/> from
        /// <paramref name="source"/> to <paramref name="destination"/>, limited by what the
        /// source has and what the destination can accept (its rules, filters and stack caps).
        /// Returns the amount actually moved.
        /// </summary>
        public static int Transfer(SlotInventory source, SlotInventory destination, string itemId, int requested)
        {
            if (source == null || destination == null || requested <= 0) return 0;

            int available = source.CountOf(itemId);
            int room = destination.RoomFor(itemId);
            int toMove = Min3(requested, available, room);
            if (toMove <= 0) return 0;

            int removed = source.Remove(itemId, toMove);
            int leftover = destination.Add(itemId, removed);
            if (leftover > 0) source.Add(itemId, leftover); // safety net: never lose items
            return removed - leftover;
        }

        /// <summary>
        /// Drains everything the destination is willing to hold, across all item types the
        /// source carries. Used by workers unloading a full inventory into a building.
        /// Returns the total number of items moved.
        /// </summary>
        public static int TransferAll(SlotInventory source, SlotInventory destination)
        {
            if (source == null || destination == null) return 0;

            var ids = new List<string>();
            foreach (var s in source.Slots)
                if (!s.IsEmpty && !ids.Contains(s.Stack.ItemId)) ids.Add(s.Stack.ItemId);

            int moved = 0;
            foreach (var id in ids)
                moved += Transfer(source, destination, id, source.CountOf(id));
            return moved;
        }

        private static int Min3(int a, int b, int c)
        {
            int m = a < b ? a : b;
            return m < c ? m : c;
        }
    }
}

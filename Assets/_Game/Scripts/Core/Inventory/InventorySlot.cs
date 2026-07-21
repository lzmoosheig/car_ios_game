using System;

namespace Overhaul.Core
{
    /// <summary>
    /// One cell of a <see cref="SlotInventory"/>. Holds at most one <see cref="ItemStack"/>
    /// and, optionally, a per-slot <see cref="Filter"/> deciding which item ids it accepts
    /// (e.g. a dedicated fuel slot). The max-stack ceiling is supplied by the owning
    /// inventory's item database, so the slot itself stays data-light.
    /// </summary>
    public sealed class InventorySlot
    {
        public ItemStack Stack { get; private set; }

        /// <summary>Optional per-slot filter. Null means "accepts any item".</summary>
        public Func<string, bool> Filter { get; set; }

        public bool IsEmpty => Stack.IsEmpty;

        public bool Accepts(string itemId)
            => !string.IsNullOrEmpty(itemId) && (Filter == null || Filter(itemId));

        public void Set(ItemStack stack) => Stack = stack;
        public void Clear() => Stack = ItemStack.Empty;

        /// <summary>
        /// Adds up to <paramref name="amount"/> of <paramref name="itemId"/>, capped by the
        /// slot filter and <paramref name="maxStack"/>. Returns the number actually accepted.
        /// </summary>
        public int Add(string itemId, int amount, int maxStack)
        {
            if (amount <= 0 || !Accepts(itemId)) return 0;

            if (IsEmpty)
            {
                int put = Math.Min(amount, maxStack);
                Stack = new ItemStack(itemId, put);
                return put;
            }

            if (Stack.ItemId != itemId) return 0;
            int room = maxStack - Stack.Count;
            if (room <= 0) return 0;
            int added = Math.Min(room, amount);
            Stack = Stack.Add(added);
            return added;
        }

        /// <summary>Removes up to <paramref name="amount"/>; returns the number removed.</summary>
        public int Remove(int amount)
        {
            if (amount <= 0 || IsEmpty) return 0;
            int taken = Math.Min(amount, Stack.Count);
            int left = Stack.Count - taken;
            Stack = left > 0 ? Stack.WithCount(left) : ItemStack.Empty;
            return taken;
        }
    }
}

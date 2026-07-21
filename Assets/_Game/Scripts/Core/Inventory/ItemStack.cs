namespace Overhaul.Core
{
    /// <summary>
    /// An amount of one item type sitting in a slot. Immutable value type: mutating
    /// operations return a new stack, which keeps slot moves free of aliasing bugs. An
    /// empty stack has a null id and a zero count (<see cref="Empty"/>).
    /// </summary>
    public readonly struct ItemStack
    {
        public static readonly ItemStack Empty = default;

        public string ItemId { get; }
        public int Count { get; }

        public ItemStack(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
            {
                ItemId = null;
                Count = 0;
            }
            else
            {
                ItemId = itemId;
                Count = count;
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

        /// <summary>True when both stacks hold the same, non-empty item type (so they could merge).</summary>
        public bool CanStackWith(ItemStack other)
            => !IsEmpty && !other.IsEmpty && other.ItemId == ItemId;

        public ItemStack WithCount(int count) => new ItemStack(ItemId, count);

        public ItemStack Add(int amount) => new ItemStack(ItemId, Count + amount);

        public override string ToString() => IsEmpty ? "(empty)" : $"{ItemId} x{Count}";
    }
}

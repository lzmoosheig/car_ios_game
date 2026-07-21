using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>
    /// Serializer-agnostic snapshot of a <see cref="SlotInventory"/> (same POCO style as
    /// <see cref="SaveData"/>). One entry per slot, preserving slot order so a restored
    /// inventory looks identical. Empty slots round-trip as empty entries.
    /// </summary>
    public sealed class InventorySave
    {
        public int SlotCount { get; set; }
        public List<InventorySlotSave> Slots { get; set; } = new();
    }

    public sealed class InventorySlotSave
    {
        public string ItemId { get; set; }
        public int Count { get; set; }
    }
}

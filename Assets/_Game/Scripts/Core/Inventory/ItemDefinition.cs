namespace Overhaul.Core
{
    /// <summary>
    /// Immutable description of one item type: what it is, how many fit in a stack, which
    /// UI icon represents it and which family it belongs to. Engine-agnostic - the icon is a
    /// string key the view layer resolves to a Sprite/colour - so the whole inventory model
    /// unit-tests without Unity. Definitions come from an <see cref="IItemDatabase"/> (in this
    /// project, the scene ResourceCatalog), never hardcoded inside the inventory.
    /// </summary>
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string IconKey { get; }
        public int MaxStackSize { get; }
        public ItemCategory Category { get; }

        public ItemDefinition(string id, string displayName, int maxStackSize,
            ItemCategory category = ItemCategory.Misc, string iconKey = null)
        {
            Id = id;
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
            MaxStackSize = maxStackSize < 1 ? 1 : maxStackSize;
            Category = category;
            IconKey = string.IsNullOrEmpty(iconKey) ? id : iconKey;
        }
    }
}

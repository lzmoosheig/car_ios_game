namespace Overhaul.Core
{
    /// <summary>
    /// Lookup from an item id to its <see cref="ItemDefinition"/>. Injected into every
    /// <see cref="SlotInventory"/> so the model stays data-driven and never bakes in an item
    /// list. The Unity ResourceCatalog implements this in the Game layer; tests supply a
    /// tiny in-memory fake.
    /// </summary>
    public interface IItemDatabase
    {
        bool TryGet(string itemId, out ItemDefinition definition);
    }

    /// <summary>Null-safe convenience lookups so callers never branch on a missing catalog.</summary>
    public static class ItemDatabaseExtensions
    {
        public static int MaxStackOf(this IItemDatabase db, string itemId)
            => db != null && db.TryGet(itemId, out var d) ? d.MaxStackSize : 1;

        public static ItemCategory CategoryOf(this IItemDatabase db, string itemId)
            => db != null && db.TryGet(itemId, out var d) ? d.Category : ItemCategory.Misc;

        public static string DisplayNameOf(this IItemDatabase db, string itemId)
            => db != null && db.TryGet(itemId, out var d) ? d.DisplayName : itemId;

        public static string IconKeyOf(this IItemDatabase db, string itemId)
            => db != null && db.TryGet(itemId, out var d) ? d.IconKey : itemId;
    }
}

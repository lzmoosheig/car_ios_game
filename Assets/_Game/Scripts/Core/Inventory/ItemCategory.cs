namespace Overhaul.Core
{
    /// <summary>
    /// Broad item family, used for slot filtering and worker role rules. Deliberately
    /// generic - the inventory system is not car-parts specific, so a category describes
    /// *how* an item is handled, not what it happens to be in this game.
    /// </summary>
    public enum ItemCategory
    {
        Misc = 0,
        Part,        // raw or completed car parts (tire, engine, body panel...)
        Consumable,  // oil, paint, cleaning supplies
        Tool,        // equipment a worker or station uses
        Resource,    // generic crafting inputs
        Product,     // finished goods a building outputs / sells
    }
}

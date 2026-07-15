namespace Overhaul.Core
{
    // Payloads published on the EventBus (Doc 06 §2). Kept as plain structs so they
    // are allocation-free and engine-agnostic.

    public struct ServiceCompleted
    {
        public string StationId;
        public string RecipeId;
        public int Revenue;
        public bool Premium;
    }

    public struct ZoneFunded
    {
        public string ZoneId;
        public int TotalCost;
    }

    public struct RackStarved
    {
        public string StationId;
        public string MissingResourceId;
    }

    public struct CashCollected
    {
        public int Amount;
        public long NewWalletTotal;
    }
}

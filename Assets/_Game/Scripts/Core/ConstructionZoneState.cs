using System;

namespace Overhaul.Core
{
    /// <summary>
    /// The funding state of a physical construction zone. The player stands in the zone and
    /// cash drains in progressively; partial funding persists across visits and save/load
    /// (Doc 02 §5.3, Doc 09 §3.1). Engine-agnostic; the view handles the drain ramp and pop-in.
    /// </summary>
    public sealed class ConstructionZoneState
    {
        public string Id { get; }
        public int TotalCost { get; }
        public int Funded { get; private set; }
        public bool Built { get; private set; }

        public ConstructionZoneState(string id, int totalCost, int funded = 0, bool built = false)
        {
            if (totalCost < 0) throw new ArgumentOutOfRangeException(nameof(totalCost));
            Id = id;
            TotalCost = totalCost;
            Funded = Math.Clamp(funded, 0, totalCost);
            Built = built || Funded >= totalCost;
        }

        public int Remaining => Math.Max(0, TotalCost - Funded);
        public float Progress01 => TotalCost <= 0 ? 1f : (float)Funded / TotalCost;

        /// <summary>
        /// Pours up to <paramref name="offered"/> cash in, never more than remains.
        /// Returns the amount actually consumed so the caller can debit exactly that.
        /// </summary>
        public int Fund(int offered)
        {
            if (Built || offered <= 0) return 0;
            int take = Math.Min(offered, Remaining);
            Funded += take;
            if (Funded >= TotalCost) Built = true;
            return take;
        }

        /// <summary>Restores from save (Doc 06 §4.9 ZoneSave).</summary>
        public static ConstructionZoneState FromSave(string id, int totalCost, int funded, bool built)
            => new ConstructionZoneState(id, totalCost, funded, built);
    }
}

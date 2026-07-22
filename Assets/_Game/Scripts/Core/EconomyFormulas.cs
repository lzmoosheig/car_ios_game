using System;

namespace Overhaul.Core
{
    /// <summary>
    /// Engine-agnostic economy math. Single source of truth for all curves and
    /// revenue rules; mirrors docs/design/04-economy-and-balancing.md.
    /// No UnityEngine dependency so it is unit-testable outside the editor.
    /// </summary>
    public static class EconomyFormulas
    {
        // Round half away from zero so results match the "clean" values in Doc 04
        // (e.g. 202.5 -> 203) rather than banker's rounding.
        private static int R(double x) => (int)Math.Round(x, MidpointRounding.AwayFromZero);

        // ---- Construction costs: cost(n) = base * growth^n  (Doc 04 §2.1) ----
        // Note: per-level authored sequences (Doc 03 §1.3) may override these.
        public const double ConstructionBase = 30.0;
        public const double ConstructionGrowth = 1.45;
        public static int ConstructionCost(int index)
            => R(ConstructionBase * Math.Pow(ConstructionGrowth, index));

        // ---- Hiring: 250 * 1.7^(k-1), k is the 1-based hire index (Doc 04 §2.2) ----
        public const double HireBase = 250.0;
        public const double HireGrowth = 1.7;
        public static int HireCost(int hireIndexOneBased)
            => R(HireBase * Math.Pow(HireGrowth, hireIndexOneBased - 1));

        // ---- Employee stat tier: 60 * 1.5^t, t 0-based (Doc 04 §2.2) ----
        public static int EmployeeStatTierCost(int tier)
            => R(60.0 * Math.Pow(1.5, tier));

        // ---- Workstation upgrade tier: 100 * 1.6^t (Doc 04 §2.3) ----
        public static int WorkstationUpgradeCost(int tier)
            => R(100.0 * Math.Pow(1.6, tier));

        // ---- Office (financial) upgrade tier: 150 * 1.6^t (Doc 04 §3) ----
        public static int OfficeUpgradeCost(int tier)
            => R(150.0 * Math.Pow(1.6, tier));

        // ---- Customer arrival interval: max(6, 20 * 0.94^zonesBuilt) (Doc 04 §4) ----
        public const double ArrivalBaseInterval = 20.0;
        public const double ArrivalDecay = 0.94;
        public const double ArrivalMinInterval = 6.0;
        public static double ArrivalInterval(int zonesBuilt)
            => Math.Max(ArrivalMinInterval, ArrivalBaseInterval * Math.Pow(ArrivalDecay, zonesBuilt));

        // ---- Service revenue (Doc 04 §1.1) ----
        // tip is expressed as a fraction of basePrice, clamped to [0, 0.75].
        public const double TipCap = 0.75;
        public const double DefaultTipBase = 0.20; // 20% baseline tip before modifiers

        /// <summary>
        /// Patience-based tip multiplier. Queue-wait penalties take priority over the
        /// fast-service bonus (a fast total service implies a short queue anyway).
        /// See Doc 02 §3.2 / Doc 04 §1.1.
        /// </summary>
        public static double PatienceFactor(double totalServiceSeconds, double queueWaitSeconds)
        {
            if (queueWaitSeconds >= 75.0) return 0.0;   // furious: no tip
            if (queueWaitSeconds >= 30.0) return 0.75;  // impatient: reduced tip
            if (totalServiceSeconds < 20.0) return 1.5; // delighted: bonus tip
            return 1.0;                                  // normal
        }

        public static int ServiceRevenue(int basePrice, double locationMult, double priceUpgradeMult,
                                         double tipBase, double patienceFactor, double qualityFactor)
        {
            double tip = Math.Clamp(tipBase * patienceFactor * qualityFactor, 0.0, TipCap);
            return R(basePrice * locationMult * priceUpgradeMult * (1.0 + tip));
        }

        // ---- Player level from reputation (XP) ----
        // Cumulative reputation needed for a level is triangular: rep(L) = base * (L-1)*L/2,
        // so each level costs a little more than the last (5,15,30,50,75,105,140,180 for L2..L9).
        public const int LevelRepBase = 5;
        public static int ReputationForLevel(int level)
            => level <= 1 ? 0 : R(LevelRepBase * (level - 1) * level / 2.0);

        /// <summary>The 1-based level a given reputation total has reached (level 1 at 0 rep).</summary>
        public static int LevelForReputation(int reputation)
        {
            int level = 1;
            while (ReputationForLevel(level + 1) <= reputation) level++;
            return level;
        }

        // ---- Parts Warehouse storage expansion (buyable slots beyond the free 4) ----
        // purchaseIndex is 0-based among the buyable slots (slot 5 => 0). Cost grows 1.5x each.
        public const int WarehouseSlotBaseCost = 200;
        public static int WarehouseSlotCost(int purchaseIndex)
            => R(WarehouseSlotBaseCost * Math.Pow(1.5, Math.Max(0, purchaseIndex)));

        // ---- Offline earnings (Doc 04 §4) ----
        // Clock rollback is clamped to 0; elapsed is capped to capHours.
        public const double OfflineEfficiency = 0.4;
        public static double OfflineEarnings(double automatedRatePerSecond, double elapsedSeconds, double capHours)
        {
            double eff = Math.Max(0.0, elapsedSeconds);
            eff = Math.Min(eff, capHours * 3600.0);
            return automatedRatePerSecond * eff * OfflineEfficiency;
        }
    }
}

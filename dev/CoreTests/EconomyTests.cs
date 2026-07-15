using Overhaul.Core;

namespace Overhaul.CoreTests
{
    /// <summary>Asserts the economy formulas match the placeholder values in Doc 04.</summary>
    public static class EconomyTests
    {
        public static void Run()
        {
            // ---- Construction curve: cost(n) = 30 * 1.45^n (Doc 04 §2.1) ----
            T.Eq(EconomyFormulas.ConstructionCost(0), 30, "construction cost n=0");
            T.Eq(EconomyFormulas.ConstructionCost(1), 44, "construction cost n=1");   // 43.5 -> 44
            T.Eq(EconomyFormulas.ConstructionCost(2), 63, "construction cost n=2");   // 63.075 -> 63

            // ---- Hiring: 250 * 1.7^(k-1) (Doc 04 §2.2) ----
            T.Eq(EconomyFormulas.HireCost(1), 250, "hire cost k=1");
            T.Eq(EconomyFormulas.HireCost(2), 425, "hire cost k=2");

            // ---- Employee stat tiers: 60 * 1.5^t (Doc 04 §2.2) ----
            T.Eq(EconomyFormulas.EmployeeStatTierCost(0), 60, "stat tier 0");
            T.Eq(EconomyFormulas.EmployeeStatTierCost(1), 90, "stat tier 1");
            T.Eq(EconomyFormulas.EmployeeStatTierCost(2), 135, "stat tier 2");
            T.Eq(EconomyFormulas.EmployeeStatTierCost(3), 203, "stat tier 3");        // 202.5 -> 203

            // ---- Workstation upgrades: 100 * 1.6^t (Doc 04 §2.3) ----
            T.Eq(EconomyFormulas.WorkstationUpgradeCost(0), 100, "workstation tier 0");
            T.Eq(EconomyFormulas.WorkstationUpgradeCost(1), 160, "workstation tier 1");
            T.Eq(EconomyFormulas.WorkstationUpgradeCost(2), 256, "workstation tier 2");

            // ---- Arrival interval: max(6, 20 * 0.94^zonesBuilt) (Doc 04 §4) ----
            T.Near(EconomyFormulas.ArrivalInterval(0), 20.0, 0.001, "arrival interval, 0 zones");
            T.Near(EconomyFormulas.ArrivalInterval(200), 6.0, 0.001, "arrival interval floors at 6");

            // ---- Service revenue (Doc 04 §1.1). tire change basePrice = 20 ----
            // normal: tip 0.20 -> 20 * 1.20 = 24
            T.Eq(EconomyFormulas.ServiceRevenue(20, 1.0, 1.0, 0.20, 1.0, 1.0), 24, "tire revenue, normal");
            // fast service: patience 1.5 -> tip 0.30 -> 20 * 1.30 = 26
            T.Eq(EconomyFormulas.ServiceRevenue(20, 1.0, 1.0, 0.20, 1.5, 1.0), 26, "tire revenue, fast");
            // furious wait: patience 0 -> tip 0 -> 20
            T.Eq(EconomyFormulas.ServiceRevenue(20, 1.0, 1.0, 0.20, 0.0, 1.0), 20, "tire revenue, no tip");
            // tip clamps at 0.75: 0.5 * 1.5 * 1.2 = 0.9 -> clamp 0.75 -> 20 * 1.75 = 35
            T.Eq(EconomyFormulas.ServiceRevenue(20, 1.0, 1.0, 0.50, 1.5, 1.2), 35, "tip cap applied");
            // location multiplier: L3 ~4.8x on an oil change base 30 -> 30 * 4.8 * 1.2 = 172.8 -> 173
            T.Eq(EconomyFormulas.ServiceRevenue(30, 4.8, 1.0, 0.20, 1.0, 1.0), 173, "oil revenue at L3 mult");

            // ---- Patience factor thresholds (Doc 02 §3.2) ----
            T.Near(EconomyFormulas.PatienceFactor(15, 0), 1.5, 0.001, "patience: fast total");
            T.Near(EconomyFormulas.PatienceFactor(40, 0), 1.0, 0.001, "patience: normal");
            T.Near(EconomyFormulas.PatienceFactor(40, 35), 0.75, 0.001, "patience: 30s+ queue");
            T.Near(EconomyFormulas.PatienceFactor(40, 80), 0.0, 0.001, "patience: 75s+ queue");

            // ---- Offline earnings (Doc 04 §4) ----
            // 1 cash/sec, 1h elapsed, 2h cap -> 3600 * 0.4 = 1440
            T.Near(EconomyFormulas.OfflineEarnings(1.0, 3600, 2), 1440.0, 0.001, "offline, under cap");
            // 10h elapsed capped at 2h -> 7200 * 0.4 = 2880
            T.Near(EconomyFormulas.OfflineEarnings(1.0, 36000, 2), 2880.0, 0.001, "offline, capped at 2h");
            // clock rollback (negative elapsed) -> 0
            T.Near(EconomyFormulas.OfflineEarnings(1.0, -500, 2), 0.0, 0.001, "offline, clock rollback clamped");
        }
    }
}

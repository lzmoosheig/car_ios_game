using System;
using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>Level-local upgrades reset per location; permanent ones persist (Doc 04 §3).</summary>
    public enum UpgradeScope { LevelLocal, Permanent }

    /// <summary>Which cost curve an upgrade follows. Curves live in EconomyFormulas.</summary>
    public enum CostCurve { Workstation, Office, EmployeeStat, Construction }

    /// <summary>
    /// Data-driven upgrade definition. Curves and effects live in data, never hard-coded in
    /// UI or building scripts (Doc 09 §4.6). Authored as ScriptableObjects on the Unity side;
    /// this is the engine-agnostic shape the logic runs on.
    /// </summary>
    public sealed class UpgradeDefinition
    {
        public string Id;
        public string DisplayName;
        public UpgradeScope Scope = UpgradeScope.LevelLocal;
        public CostCurve Curve = CostCurve.Workstation;
        public int MaxTier = 5;
        /// <summary>Additive effect per tier, e.g. +0.15 work speed or +3 rack capacity.</summary>
        public float EffectPerTier = 0.15f;

        public int CostAtTier(int tier) => Curve switch
        {
            CostCurve.Workstation => EconomyFormulas.WorkstationUpgradeCost(tier),
            CostCurve.Office => EconomyFormulas.OfficeUpgradeCost(tier),
            CostCurve.EmployeeStat => EconomyFormulas.EmployeeStatTierCost(tier),
            CostCurve.Construction => EconomyFormulas.ConstructionCost(tier),
            _ => EconomyFormulas.WorkstationUpgradeCost(tier)
        };
    }

    /// <summary>
    /// Holds purchased tiers and resolves costs/effects. Purchases go through a wallet
    /// callback so this stays engine-agnostic and testable.
    /// </summary>
    public sealed class UpgradeState
    {
        private readonly Dictionary<string, UpgradeDefinition> _defs = new();
        private readonly Dictionary<string, int> _tiers = new();

        public void Register(UpgradeDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) throw new ArgumentException("bad upgrade def");
            _defs[def.Id] = def;
            if (!_tiers.ContainsKey(def.Id)) _tiers[def.Id] = 0;
        }

        public UpgradeDefinition Definition(string id) => _defs.TryGetValue(id, out var d) ? d : null;
        public int TierOf(string id) => _tiers.TryGetValue(id, out var t) ? t : 0;
        public IReadOnlyDictionary<string, int> Tiers => _tiers;

        public bool IsMaxed(string id)
        {
            var d = Definition(id);
            return d != null && TierOf(id) >= d.MaxTier;
        }

        /// <summary>Cost of the next tier, or -1 when maxed/unknown.</summary>
        public int NextCost(string id)
        {
            var d = Definition(id);
            if (d == null || IsMaxed(id)) return -1;
            return d.CostAtTier(TierOf(id));
        }

        /// <summary>Total additive effect currently bought (tier * effectPerTier).</summary>
        public float EffectOf(string id)
        {
            var d = Definition(id);
            return d == null ? 0f : TierOf(id) * d.EffectPerTier;
        }

        /// <summary>Multiplier form for speed-style upgrades: 1.0 + effect.</summary>
        public float MultiplierOf(string id) => 1f + EffectOf(id);

        /// <summary>
        /// Buys the next tier if affordable. <paramref name="trySpend"/> debits the wallet and
        /// returns false when there isn't enough cash.
        /// </summary>
        public bool TryPurchase(string id, Func<int, bool> trySpend)
        {
            int cost = NextCost(id);
            if (cost < 0) return false;
            if (trySpend == null || !trySpend(cost)) return false;
            _tiers[id] = TierOf(id) + 1;
            return true;
        }

        /// <summary>Restores tiers from save; unknown ids are ignored.</summary>
        public void LoadTiers(IReadOnlyDictionary<string, int> saved)
        {
            if (saved == null) return;
            foreach (var kv in saved)
                if (_defs.ContainsKey(kv.Key))
                    _tiers[kv.Key] = Math.Clamp(kv.Value, 0, _defs[kv.Key].MaxTier);
        }

        /// <summary>Level-local tiers are wiped when a new location starts (Doc 04 §3).</summary>
        public void ResetLevelLocal()
        {
            foreach (var kv in _defs)
                if (kv.Value.Scope == UpgradeScope.LevelLocal)
                    _tiers[kv.Key] = 0;
        }
    }
}

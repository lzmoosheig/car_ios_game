using System;
using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-side owner of level upgrades. Definitions and cost curves stay in Core while
    /// this component applies purchased effects to the live village and exposes save data.
    /// </summary>
    public sealed class VillageUpgradeManager : MonoBehaviour
    {
        public const string OfficePricingId = "office_pricing";

        [SerializeField] private EconomyManager economy;
        [SerializeField] private ServiceBay serviceBay;

        private UpgradeState _state;

        public event Action<string, int> UpgradeChanged;
        public IReadOnlyDictionary<string, int> Tiers => State.Tiers;

        private UpgradeState State
        {
            get
            {
                if (_state == null)
                {
                    _state = new UpgradeState();
                    _state.Register(new UpgradeDefinition
                    {
                        Id = OfficePricingId,
                        DisplayName = "Service pricing",
                        Scope = UpgradeScope.LevelLocal,
                        Curve = CostCurve.Office,
                        MaxTier = 5,
                        EffectPerTier = 0.1f
                    });
                }
                return _state;
            }
        }

        public void Configure(EconomyManager eco, ServiceBay bay)
        {
            economy = eco;
            serviceBay = bay;
            ApplyEffects();
        }

        private void Awake()
        {
            _ = State;
            ApplyEffects();
        }

        public int TierOf(string id) => State.TierOf(id);
        public int NextCost(string id) => State.NextCost(id);
        public bool IsMaxed(string id) => State.IsMaxed(id);

        public bool TryPurchase(string id)
        {
            if (economy == null || !State.TryPurchase(id, economy.TrySpend)) return false;
            ApplyEffects();
            UpgradeChanged?.Invoke(id, State.TierOf(id));
            return true;
        }

        public void LoadTiers(IReadOnlyDictionary<string, int> saved)
        {
            State.LoadTiers(saved);
            ApplyEffects();
            UpgradeChanged?.Invoke(OfficePricingId, State.TierOf(OfficePricingId));
        }

        private void ApplyEffects()
        {
            if (serviceBay != null)
                serviceBay.SetPriceUpgradeMultiplier(State.MultiplierOf(OfficePricingId));
        }
    }
}

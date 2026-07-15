using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.Tests
{
    /// <summary>
    /// Phase A game-layer behaviour: construction funding through the real EconomyManager,
    /// and the patience tip reaching the ServiceBay's payout.
    /// </summary>
    public sealed class PhaseAGameTests
    {
        [Test]
        public void ConstructionZone_DrainsWalletAndBuilds_WithoutOvercharging()
        {
            var eco = new GameObject("eco").AddComponent<EconomyManager>();
            eco.SetWallet(500);

            var zone = new GameObject("zone").AddComponent<ConstructionZoneView>();
            zone.Configure("zone_test", 100, eco);

            // Ramp is 5->50 cash/sec over 3s; tick well past completion.
            for (int i = 0; i < 200 && !zone.Built; i++) zone.Tick(0.1f);

            Assert.IsTrue(zone.Built, "zone completes while the player stands in it");
            Assert.AreEqual(0, zone.Remaining, "nothing left to fund");
            Assert.AreEqual(400, eco.Wallet, "wallet debited by exactly the 100 cost, never more");

            // A built zone must not keep draining cash.
            for (int i = 0; i < 20; i++) zone.Tick(0.1f);
            Assert.AreEqual(400, eco.Wallet, "built zone stops taking cash");

            Object.DestroyImmediate(eco.gameObject);
            Object.DestroyImmediate(zone.gameObject);
        }

        [Test]
        public void ConstructionZone_BrokePlayer_MakesNoProgress()
        {
            var eco = new GameObject("eco").AddComponent<EconomyManager>();
            eco.SetWallet(0);

            var zone = new GameObject("zone").AddComponent<ConstructionZoneView>();
            zone.Configure("zone_broke", 100, eco);

            for (int i = 0; i < 50; i++) zone.Tick(0.1f);

            Assert.IsFalse(zone.Built, "no funding without cash");
            Assert.AreEqual(0, zone.Funded, "funded amount stays at zero");
            Assert.AreEqual(0, eco.Wallet, "wallet cannot go negative");

            Object.DestroyImmediate(eco.gameObject);
            Object.DestroyImmediate(zone.gameObject);
        }

        [Test]
        public void ConstructionZone_RestoresPartialFundingFromSave()
        {
            var eco = new GameObject("eco").AddComponent<EconomyManager>();
            eco.SetWallet(1000);

            var zone = new GameObject("zone").AddComponent<ConstructionZoneView>();
            zone.Configure("zone_resume", 200, eco);
            zone.LoadState(150, false);

            Assert.AreEqual(150, zone.Funded, "partial funding restored");
            Assert.AreEqual(50, zone.Remaining, "only the remainder is left");
            Assert.IsFalse(zone.Built, "not built yet");

            for (int i = 0; i < 200 && !zone.Built; i++) zone.Tick(0.1f);

            Assert.IsTrue(zone.Built, "finishes from a restored partial state");
            Assert.AreEqual(950, eco.Wallet, "only the outstanding 50 was charged");

            Object.DestroyImmediate(eco.gameObject);
            Object.DestroyImmediate(zone.gameObject);
        }

        [Test]
        public void ServiceBay_ImpatientCustomer_PaysBasePriceWithNoTip()
        {
            var rack = new GameObject("rack").AddComponent<ResourceRack>();
            var eco = new GameObject("eco").AddComponent<EconomyManager>();
            var bay = new GameObject("bay").AddComponent<ServiceBay>();

            rack.SetCapacity(8);
            for (int i = 0; i < 4; i++) rack.Add("tire");
            bay.Configure(rack, eco);
            bay.ConfigureRecipe("tire", 4, 6f, 20);

            // A furious customer (75s+ wait) zeroes the tip but still pays the base price.
            bay.PatienceFactor = (float)EconomyFormulas.PatienceFactor(80f, 80f);
            bay.VehiclePresent = true;

            for (float t = 0; t < 12f && bay.ServicedCount == 0; t += 0.5f) bay.Tick(0.5f);

            Assert.AreEqual(1, bay.ServicedCount, "car still serviced");
            Assert.AreEqual(20, eco.Wallet, "base price paid, tip zeroed - never a loss");

            Object.DestroyImmediate(rack.gameObject);
            Object.DestroyImmediate(eco.gameObject);
            Object.DestroyImmediate(bay.gameObject);
        }

        [Test]
        public void OfficePricingUpgrade_PurchasesPersistsAndRaisesServiceRevenue()
        {
            var root = new GameObject("office-upgrade-test");
            var rack = root.AddComponent<ResourceRack>();
            var eco = root.AddComponent<EconomyManager>();
            var bay = root.AddComponent<ServiceBay>();
            var upgrades = root.AddComponent<VillageUpgradeManager>();

            eco.SetWallet(500);
            rack.SetCapacity(4);
            for (int i = 0; i < 4; i++) rack.Add("tire");
            bay.Configure(rack, eco);
            bay.ConfigureRecipe("tire", 4, 0.1f, 20);
            upgrades.Configure(eco, bay);

            Assert.AreEqual(150, upgrades.NextCost(VillageUpgradeManager.OfficePricingId));
            Assert.IsTrue(upgrades.TryPurchase(VillageUpgradeManager.OfficePricingId));
            Assert.AreEqual(350, eco.Wallet);
            Assert.AreEqual(1.1f, bay.PriceUpgradeMultiplier, 0.001f);

            bay.VehiclePresent = true;
            for (int i = 0; i < 5 && bay.ServicedCount == 0; i++) bay.Tick(0.2f);
            Assert.AreEqual(26, bay.LastRevenue, "10% pricing applies before the normal patience tip");

            upgrades.LoadTiers(new Dictionary<string, int> { [VillageUpgradeManager.OfficePricingId] = 3 });
            Assert.AreEqual(1.3f, bay.PriceUpgradeMultiplier, 0.001f);

            Object.DestroyImmediate(root);
        }
    }
}

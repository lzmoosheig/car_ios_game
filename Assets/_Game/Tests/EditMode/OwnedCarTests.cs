using NUnit.Framework;
using UnityEngine;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.Tests
{
    /// <summary>
    /// Covers the Phase B personal-car rules: performance-point/class math, the
    /// reputation-gated starter claim, maintenance economics (cash + delivery stock),
    /// setup presets producing distinct handling, wear clamping, and the save
    /// round-trip through Capture/Restore.
    /// </summary>
    public sealed class OwnedCarTests
    {
        [Test]
        public void PerformancePoints_And_Class_FollowTierMath()
        {
            var car = new OwnedCarState();
            Assert.AreEqual(CarMath.StarterBasePP, CarMath.PerformancePoints(car), "stock starter PP");
            Assert.AreEqual(RaceClass.C, CarMath.ClassOf(CarMath.PerformancePoints(car)), "stock starter is class C");

            car.EngineTier = 2;
            car.TireTier = 2;
            int maxedPP = CarMath.PerformancePoints(car);
            Assert.AreEqual(CarMath.StarterBasePP + 2 * CarMath.EnginePPPerTier + 2 * CarMath.TiresPPPerTier,
                maxedPP, "tiers add their PP");
            Assert.AreEqual(RaceClass.B, CarMath.ClassOf(maxedPP), "fully tuned starter reaches class B");
        }

        [Test]
        public void StarterClaim_IsGatedOnReputation()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<OwnedCarSystem>();
            system.Configure(economy, null);

            Assert.IsFalse(system.TryClaimStarter(), "claim refused below the reputation gate");
            economy.AddReputation(OwnedCarSystem.StarterReputationRequirement);
            Assert.IsTrue(system.TryClaimStarter(), "claim succeeds at the gate");
            Assert.IsTrue(system.Owned, "car is owned after the claim");
            Assert.IsFalse(system.TryClaimStarter(), "cannot claim twice");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(systemGo);
        }

        [Test]
        public void Maintain_SpendsCash_ConsumesStock_RestoresCondition()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            economy.SetWallet(10_000);
            economy.AddReputation(OwnedCarSystem.StarterReputationRequirement);
            var deliveryGo = new GameObject("delivery");
            var delivery = deliveryGo.AddComponent<CarDeliverySystem>();
            delivery.Configure(economy);
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<OwnedCarSystem>();
            system.Configure(economy, delivery);
            system.TryClaimStarter();

            delivery.TryBuy("tire"); // grants 100 tires for 4500
            long cashBeforeService = economy.Wallet;
            int tiresBefore = delivery.OwnedCountOf("tire");

            system.Car.TireCondition = 0.3f;
            system.Car.Cleanliness = 0.2f;
            Assert.IsTrue(system.TryMaintain(), "service succeeds with cash");

            Assert.AreEqual(cashBeforeService - OwnedCarSystem.MaintenanceCashCost, economy.Wallet, "cash spent");
            Assert.AreEqual(tiresBefore - OwnedCarSystem.MaintenanceTireCost, delivery.OwnedCountOf("tire"),
                "tires consumed from delivery stock");
            Assert.AreEqual(1f, system.Car.TireCondition, "tire condition restored");
            Assert.AreEqual(1f, system.Car.Cleanliness, "cleanliness restored");

            Assert.IsFalse(system.TryMaintain(), "no service sold for an already-perfect car");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(deliveryGo);
            Object.DestroyImmediate(systemGo);
        }

        [Test]
        public void SetupPresets_ProduceDistinctHandling()
        {
            var car = new OwnedCarState { Owned = true };

            car.Setup = CarSetupPreset.Stable;
            var stable = CarMath.ProfileOf(car);
            car.Setup = CarSetupPreset.Balanced;
            var balanced = CarMath.ProfileOf(car);
            car.Setup = CarSetupPreset.Agile;
            var agile = CarMath.ProfileOf(car);

            Assert.Less(stable.Steering, balanced.Steering, "stable steers slower than balanced");
            Assert.Greater(agile.Steering, balanced.Steering, "agile steers faster than balanced");
            Assert.Greater(stable.Grip, balanced.Grip, "stable grips harder than balanced");
            Assert.Less(agile.Grip, balanced.Grip, "agile slides more than balanced");
        }

        [Test]
        public void Wear_DegradesCondition_AndConditionScalesPerformance()
        {
            var car = new OwnedCarState { Owned = true };
            var freshProfile = CarMath.ProfileOf(car);

            CarMath.ApplyWear(car, 30f);
            Assert.Less(car.TireCondition, 1f, "tires wear with mileage");
            Assert.Less(car.Cleanliness, 1f, "car gets dirty with mileage");
            Assert.AreEqual(30f, car.MileageKm, 0.001f, "mileage accrues");

            var wornProfile = CarMath.ProfileOf(car);
            Assert.Less(wornProfile.TopSpeed, freshProfile.TopSpeed, "worn car loses the bonus band");
            Assert.GreaterOrEqual(CarMath.ConditionFactor(car), 0.85f,
                "condition never drops performance below the 85% floor (gentle failure)");

            CarMath.ApplyWear(car, 100000f);
            Assert.GreaterOrEqual(car.TireCondition, 0f, "condition clamps at zero");
        }

        [Test]
        public void SaveRoundTrip_RestoresOwnershipBuildAndCondition()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            economy.SetWallet(100_000);
            economy.AddReputation(OwnedCarSystem.StarterReputationRequirement);
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<OwnedCarSystem>();
            system.Configure(economy, null);

            system.TryClaimStarter();
            system.SetSetup(CarSetupPreset.Agile);
            system.TryUpgradeEngine();
            system.TryCertify();
            system.ApplyWear(12.5f);

            var save = system.Capture();

            var restoredGo = new GameObject("restored");
            var restored = restoredGo.AddComponent<OwnedCarSystem>();
            restored.Configure(economy, null);
            restored.Restore(save);

            Assert.IsTrue(restored.Owned, "ownership restored");
            Assert.AreEqual(CarSetupPreset.Agile, restored.Car.Setup, "setup preset restored");
            Assert.AreEqual(1, restored.Car.EngineTier, "engine tier restored");
            Assert.AreEqual(12.5f, restored.Car.MileageKm, 0.001f, "mileage restored");
            Assert.AreEqual(system.Car.TireCondition, restored.Car.TireCondition, 0.0001f, "condition restored");
            Assert.AreEqual(system.Car.Certified, restored.Car.Certified, "certification restored");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(systemGo);
            Object.DestroyImmediate(restoredGo);
        }

        [Test]
        public void WorkshopLaps_KeepIndependentBestForEachSetup()
        {
            var car = new OwnedCarState { Owned = true };

            Assert.IsTrue(CarMath.TryRecordWorkshopLap(car, CarSetupPreset.Stable, 42.5f));
            Assert.IsFalse(CarMath.TryRecordWorkshopLap(car, CarSetupPreset.Stable, 43f), "slower lap is not a best");
            Assert.IsTrue(CarMath.TryRecordWorkshopLap(car, CarSetupPreset.Stable, 41.25f));
            Assert.IsTrue(CarMath.TryRecordWorkshopLap(car, CarSetupPreset.Agile, 39.75f));

            Assert.AreEqual(41.25f, CarMath.WorkshopBestLap(car, CarSetupPreset.Stable), 0.001f);
            Assert.AreEqual(0f, CarMath.WorkshopBestLap(car, CarSetupPreset.Balanced), 0.001f);
            Assert.AreEqual(39.75f, CarMath.WorkshopBestLap(car, CarSetupPreset.Agile), 0.001f);
        }

        [Test]
        public void SaveRoundTrip_RestoresPerSetupWorkshopLaps()
        {
            var economyGo = new GameObject("economy-laps");
            var economy = economyGo.AddComponent<EconomyManager>();
            economy.AddReputation(OwnedCarSystem.StarterReputationRequirement);
            var sourceGo = new GameObject("source-laps");
            var source = sourceGo.AddComponent<OwnedCarSystem>();
            source.Configure(economy, null);
            source.TryClaimStarter();
            source.SetSetup(CarSetupPreset.Stable);
            source.RecordWorkshopLap(44f);
            source.SetSetup(CarSetupPreset.Agile);
            source.RecordWorkshopLap(40f);

            var restoredGo = new GameObject("restored-laps");
            var restored = restoredGo.AddComponent<OwnedCarSystem>();
            restored.Restore(source.Capture());

            Assert.AreEqual(44f, CarMath.WorkshopBestLap(restored.Car, CarSetupPreset.Stable), 0.001f);
            Assert.AreEqual(40f, CarMath.WorkshopBestLap(restored.Car, CarSetupPreset.Agile), 0.001f);

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(sourceGo);
            Object.DestroyImmediate(restoredGo);
        }
    }
}

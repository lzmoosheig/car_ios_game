using NUnit.Framework;
using UnityEngine;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.Tests
{
    /// <summary>
    /// Covers the Car Delivery placeholder economy: buying items spends currency and
    /// grows owned stock, locked slots refuse to start, unlocked slots can, and slot/owned
    /// state round-trips through Capture/Restore the same way SaveManager persists it.
    /// </summary>
    public sealed class CarDeliveryTests
    {
        [Test]
        public void BuyItem_SpendsCashAndIncreasesOwnedQuantity()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            economy.SetWallet(10_000);
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<CarDeliverySystem>();
            system.Configure(economy);

            bool bought = system.TryBuy("tire"); // 4500 cash, grants 100

            Assert.IsTrue(bought, "purchase succeeds with enough cash");
            Assert.AreEqual(10_000 - 4500, economy.Wallet, "cash was spent");
            Assert.AreEqual(100, system.OwnedCountOf("tire"), "owned stock increased by the purchase quantity");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(systemGo);
        }

        [Test]
        public void BuyItem_FailsWithoutEnoughCash()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            economy.SetWallet(100);
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<CarDeliverySystem>();
            system.Configure(economy);

            bool bought = system.TryBuy("tire");

            Assert.IsFalse(bought, "purchase fails without enough cash");
            Assert.AreEqual(100, economy.Wallet, "cash is untouched on a failed purchase");
            Assert.AreEqual(0, system.OwnedCountOf("tire"), "no stock granted on a failed purchase");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(systemGo);
        }

        [Test]
        public void LockedSlot_CannotStartDelivery()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<CarDeliverySystem>();
            system.Configure(economy);

            Assert.IsFalse(system.Slots[4].Unlocked, "slot 5 starts locked");
            bool started = system.TryStart(4);

            Assert.IsFalse(started, "a locked slot refuses to start");
            Assert.IsFalse(system.Slots[4].Running, "locked slot stays idle");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(systemGo);
        }

        [Test]
        public void UnlockedSlot_CanStartDelivery_AfterItFinishes()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<CarDeliverySystem>();
            system.Configure(economy);

            var slot = system.Slots[0];
            Assert.IsTrue(slot.Unlocked, "slot 1 starts unlocked");
            Assert.IsTrue(slot.Running, "unlocked slots start running immediately");

            // Drive the slot to completion, then collecting should restart it - the
            // "unlocked slots can start delivery" case in practice.
            CarDeliveryLogic.Finish(slot);
            Assert.IsTrue(slot.IsComplete, "elapsed reached the slot's duration");
            bool collected = system.CollectSlot(0);

            Assert.IsTrue(collected, "a completed slot can be collected");
            Assert.IsTrue(system.OwnedCountOf(slot.ItemId) > 0, "collecting granted owned stock");
            Assert.IsTrue(system.Slots[0].Running, "collecting restarts the slot's production loop");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(systemGo);
        }

        [Test]
        public void SaveRoundTrip_RestoresOwnedItemsAndSlotUnlocks()
        {
            var economyGo = new GameObject("economy");
            var economy = economyGo.AddComponent<EconomyManager>();
            economy.SetWallet(1_000_000);
            var systemGo = new GameObject("system");
            var system = systemGo.AddComponent<CarDeliverySystem>();
            system.Configure(economy);

            system.TryBuy("tire");
            system.TryBuy("oil");
            system.TryUnlockNextSlot(); // unlocks slot index 4

            CarDeliverySave save = system.Capture();

            var restoredGo = new GameObject("restored");
            var restored = restoredGo.AddComponent<CarDeliverySystem>();
            restored.Configure(economy);
            restored.Restore(save);

            Assert.AreEqual(system.OwnedCountOf("tire"), restored.OwnedCountOf("tire"), "owned tires restored");
            Assert.AreEqual(system.OwnedCountOf("oil"), restored.OwnedCountOf("oil"), "owned oil restored");
            Assert.IsTrue(restored.Slots[4].Unlocked, "purchased slot unlock restored");

            Object.DestroyImmediate(economyGo);
            Object.DestroyImmediate(systemGo);
            Object.DestroyImmediate(restoredGo);
        }
    }
}

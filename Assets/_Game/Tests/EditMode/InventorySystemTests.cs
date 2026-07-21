using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.Tests
{
    /// <summary>
    /// Validates the reusable slot-based inventory system: stacking, slot moves/splits,
    /// partial-safe transfers, and the full worker round-trip (collect from a source building,
    /// carry in the worker's own inventory, deposit into a destination building). The Core model
    /// is exercised directly with an in-memory item database; the courier test uses the real
    /// MonoBehaviour bridges.
    /// </summary>
    public sealed class InventorySystemTests
    {
        /// <summary>Tiny in-memory <see cref="IItemDatabase"/> so Core tests need no Unity scene.</summary>
        private sealed class FakeDb : IItemDatabase
        {
            private readonly Dictionary<string, ItemDefinition> _defs = new();
            public FakeDb Add(string id, int maxStack, ItemCategory cat = ItemCategory.Misc)
            {
                _defs[id] = new ItemDefinition(id, id, maxStack, cat);
                return this;
            }
            public bool TryGet(string itemId, out ItemDefinition definition) => _defs.TryGetValue(itemId, out definition);
        }

        private static FakeDb Db() => new FakeDb()
            .Add("tire", 8, ItemCategory.Part)
            .Add("engine", 4, ItemCategory.Part)
            .Add("oil", 16, ItemCategory.Consumable);

        // ------------------------------------------------------------------ stacking

        [Test]
        public void Add_StacksUpToMax_ThenSpillsIntoNextSlot()
        {
            var inv = new SlotInventory(4, Db());
            int leftover = inv.Add("tire", 10); // max stack 8

            Assert.AreEqual(0, leftover, "everything fit across two slots");
            Assert.AreEqual(10, inv.CountOf("tire"));
            Assert.AreEqual(8, inv.SlotAt(0).Stack.Count, "first slot filled to max");
            Assert.AreEqual(2, inv.SlotAt(1).Stack.Count, "remainder spilled into the next slot");
        }

        [Test]
        public void Add_ReturnsRemainder_WhenInventoryFull()
        {
            var inv = new SlotInventory(1, Db());
            int leftover = inv.Add("tire", 20); // one slot, max 8

            Assert.AreEqual(12, leftover, "only a full stack fit; the rest is reported back");
            Assert.AreEqual(8, inv.CountOf("tire"));
        }

        [Test]
        public void Add_TopsUpExistingStacksBeforeUsingEmptySlots()
        {
            var inv = new SlotInventory(3, Db());
            inv.Add("tire", 5);           // slot 0 has 5
            inv.Add("tire", 4);           // 3 top up slot 0 to 8, 1 spills to slot 1

            Assert.AreEqual(8, inv.SlotAt(0).Stack.Count);
            Assert.AreEqual(1, inv.SlotAt(1).Stack.Count);
        }

        [Test]
        public void Remove_PullsAcrossSlots_AndReportsActualRemoved()
        {
            var inv = new SlotInventory(4, Db());
            inv.Add("tire", 10);

            Assert.AreEqual(10, inv.Remove("tire", 50), "cannot remove more than present");
            Assert.IsTrue(inv.IsEmpty);
        }

        // ------------------------------------------------------------ slot operations

        [Test]
        public void MoveOrMerge_MergesMatchingStacks_LeavingOverflowBehind()
        {
            var inv = new SlotInventory(2, Db());
            inv.SlotAt(0).Set(new ItemStack("tire", 6));
            inv.SlotAt(1).Set(new ItemStack("tire", 5));

            Assert.IsTrue(inv.MoveOrMerge(1, 0));
            Assert.AreEqual(8, inv.SlotAt(0).Stack.Count, "destination filled to max");
            Assert.AreEqual(3, inv.SlotAt(1).Stack.Count, "overflow stays in the source slot");
        }

        [Test]
        public void MoveOrMerge_SwapsDifferentItems()
        {
            var inv = new SlotInventory(2, Db());
            inv.SlotAt(0).Set(new ItemStack("tire", 3));
            inv.SlotAt(1).Set(new ItemStack("oil", 2));

            Assert.IsTrue(inv.MoveOrMerge(0, 1));
            Assert.AreEqual("oil", inv.SlotAt(0).Stack.ItemId);
            Assert.AreEqual("tire", inv.SlotAt(1).Stack.ItemId);
        }

        [Test]
        public void SplitStack_MovesHalfIntoAnEmptySlot()
        {
            var inv = new SlotInventory(2, Db());
            inv.Add("tire", 8);

            Assert.IsTrue(inv.SplitStack(0));
            Assert.AreEqual(4, inv.SlotAt(0).Stack.Count);
            Assert.AreEqual(4, inv.SlotAt(1).Stack.Count);
        }

        // --------------------------------------------------------------- role rules

        [Test]
        public void AllowedFilter_RejectsDisallowedCategory()
        {
            var db = Db();
            // A parts-only inventory (a delivery worker): consumables bounce off.
            var inv = new SlotInventory(4, db, id => db.CategoryOf(id) == ItemCategory.Part);

            Assert.AreEqual(0, inv.RoomFor("oil"), "no room for a disallowed item");
            Assert.AreEqual(5, inv.Add("oil", 5), "add is fully rejected");
            Assert.AreEqual(0, inv.Add("tire", 5), "allowed item still works");
            Assert.AreEqual(5, inv.CountOf("tire"));
        }

        // ------------------------------------------------------------ partial transfer

        [Test]
        public void Transfer_PartialSafe_WhenDestinationNearlyFull()
        {
            var src = new SlotInventory(4, Db());
            var dst = new SlotInventory(1, Db()); // single slot, max 8
            src.Add("tire", 10);
            dst.Add("tire", 6); // only room for 2 more

            int moved = InventoryTransfer.Transfer(src, dst, "tire", 10);

            Assert.AreEqual(2, moved, "only what fits crosses");
            Assert.AreEqual(8, dst.CountOf("tire"));
            Assert.AreEqual(8, src.CountOf("tire"), "the rest stays at the source - nothing lost");
        }

        [Test]
        public void TransferAll_MovesEveryType_UpToDestinationRoom()
        {
            var src = new SlotInventory(4, Db());
            var dst = new SlotInventory(4, Db());
            src.Add("tire", 8);
            src.Add("oil", 5);

            int moved = InventoryTransfer.TransferAll(src, dst);

            Assert.AreEqual(13, moved);
            Assert.IsTrue(src.IsEmpty);
            Assert.AreEqual(8, dst.CountOf("tire"));
            Assert.AreEqual(5, dst.CountOf("oil"));
        }

        // -------------------------------------------------------------- persistence

        [Test]
        public void CaptureRestore_RoundTripsSlotLayout()
        {
            var a = new SlotInventory(4, Db());
            a.Add("tire", 10);
            a.Add("oil", 3);

            var save = a.Capture();
            var b = new SlotInventory(4, Db());
            b.Restore(save);

            Assert.AreEqual(10, b.CountOf("tire"));
            Assert.AreEqual(3, b.CountOf("oil"));
            Assert.AreEqual(a.SlotAt(0).Stack.Count, b.SlotAt(0).Stack.Count);
            Assert.AreEqual(a.SlotAt(1).Stack.Count, b.SlotAt(1).Stack.Count);
        }

        // ------------------------------------------- worker round-trip (MonoBehaviours)

        [Test]
        public void Worker_CollectsCarriesAndDeposits_BetweenBuildings_PartialSafe()
        {
            // Item metadata comes from a live catalog, exactly as in play.
            var catalogGo = new GameObject("catalog");
            var catalog = catalogGo.AddComponent<ResourceCatalog>();
            catalog.SeedDefaults();

            // Source building outputs completed parts; destination is a rack building; the
            // worker carries them. tire max stack is 20 (catalog default).
            var sourceGo = new GameObject("source");
            var source = sourceGo.AddComponent<InventoryComponent>();
            source.Configure(6, label: "Source Building");

            // A single slot pre-filled to leave room for only 10 more -> forces a partial deposit,
            // independent of what the catalog's per-slot stack size happens to be.
            int max = catalog.MaxStackOf("tire");
            var destGo = new GameObject("dest");
            var dest = destGo.AddComponent<InventoryComponent>();
            dest.Configure(1, label: "Dest Building");
            dest.Add("tire", max - 10); // room for exactly 10 more

            var workerGo = new GameObject("worker");
            var workerInv = workerGo.AddComponent<InventoryComponent>();
            // Parts-delivery role: only carries Part-category items.
            workerInv.Configure(2, categories: new[] { ItemCategory.Part }, label: "Parts Courier");
            var courier = workerGo.AddComponent<InventoryCourier>();
            courier.Configure(workerInv, source, dest);

            // Building produces 30 tires + some oil (which the parts worker must ignore).
            source.Add("tire", 30);
            source.Add("oil", 10);

            // Collect: worker takes every tire it has room for; consumables bounce off the role rule.
            int collected = courier.CollectStep();
            Assert.AreEqual(30, collected, "worker grabs all tires it has room for");
            Assert.AreEqual(30, workerInv.CountOf("tire"));
            Assert.AreEqual(0, workerInv.CountOf("oil"), "role rule kept consumables out");
            Assert.AreEqual(10, source.CountOf("oil"), "oil stayed in the source building");

            // Deposit into the nearly-full destination: only the 10 that fit cross over.
            int deposited = courier.DepositStep();
            Assert.AreEqual(10, deposited, "destination only had room for 10 more");
            Assert.AreEqual(max, dest.CountOf("tire"));
            Assert.AreEqual(20, workerInv.CountOf("tire"), "the remainder is still carried - nothing lost");

            Object.DestroyImmediate(catalogGo);
            Object.DestroyImmediate(sourceGo);
            Object.DestroyImmediate(destGo);
            Object.DestroyImmediate(workerGo);
        }
    }
}

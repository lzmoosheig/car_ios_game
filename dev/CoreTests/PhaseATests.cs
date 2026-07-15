using Overhaul.Core;

namespace Overhaul.CoreTests
{
    /// <summary>Covers the Doc 09 §14 Phase A backbone: queue, reception, construction, upgrades.</summary>
    public static class PhaseATests
    {
        public static void Run()
        {
            QueueTests();
            ReceptionTests();
            ConstructionTests();
            UpgradeTests();
        }

        private static void QueueTests()
        {
            // ---- fills front-first and reports occupancy ----
            var q = new QueueManager(3);
            T.Eq(q.SlotCount, 3, "queue starts with 3 slots");
            T.True(q.IsEmpty, "queue starts empty");
            T.Eq(q.TryEnqueue("carA"), 0, "first car takes the front slot");
            T.Eq(q.TryEnqueue("carB"), 1, "second car takes slot 1");
            T.Eq(q.TryEnqueue("carC"), 2, "third car takes slot 2");
            T.True(q.IsFull, "queue full at 3");
            T.Eq(q.TryEnqueue("carD"), -1, "4th car rejected when full (arrivals pause, not dropped)");
            T.Eq(q.Occupancy, 3, "occupancy is 3");

            // ---- dispatching the front closes the gap ----
            T.True(q.TryDequeueFront(out var front), "front dispatched");
            T.Eq(front, "carA", "front was carA");
            T.Eq(q.Front, "carB", "carB advanced to the front");
            T.Eq(q.IndexOf("carC"), 1, "carC advanced to slot 1");
            T.Eq(q.Occupancy, 2, "occupancy dropped to 2");
            T.True(!q.IsFull, "queue no longer full");

            // ---- a car leaving from the middle also closes the gap ----
            var q2 = new QueueManager(3);
            q2.TryEnqueue("a"); q2.TryEnqueue("b"); q2.TryEnqueue("c");
            T.True(q2.Remove("b"), "middle car removed");
            T.Eq(q2.Front, "a", "front unchanged");
            T.Eq(q2.IndexOf("c"), 1, "rear car closed the gap");

            // ---- empty queue dispatch is a no-op ----
            var q3 = new QueueManager(2);
            T.True(!q3.TryDequeueFront(out _), "dequeue on empty queue returns false");

            // ---- slot upgrades only ever add capacity ----
            var q4 = new QueueManager(2);
            q4.SetSlotCount(1);
            T.Eq(q4.SlotCount, 2, "slot count never shrinks");
            q4.SetSlotCount(5);
            T.Eq(q4.SlotCount, 5, "slot upgrade applied");
            q4.TryEnqueue("x");
            T.Eq(q4.TryEnqueue("y"), 1, "new slots are usable");
        }

        private static void ReceptionTests()
        {
            // ---- no service unlocked -> no ticket ----
            var r0 = new Reception(seed: 1);
            T.True(!r0.HasAnyService, "reception has no unlocked service yet");
            T.True(r0.CheckIn("car1", 0f) == null, "no ticket when nothing is unlocked");

            // ---- only unlocked services are generated ----
            var r = new Reception(seed: 42);
            r.SetWeight(ServiceKind.BasicRepair, 1);
            T.True(r.HasAnyService, "reception has a service");
            bool allBasic = true;
            for (int i = 0; i < 25; i++)
            {
                var t = r.CheckIn("car" + i, i);
                if (t == null || t.Kind != ServiceKind.BasicRepair) allBasic = false;
            }
            T.True(allBasic, "only the unlocked service is ever requested");

            // ---- weights gate locked services (weight 0 never appears) ----
            var r2 = new Reception(seed: 7);
            r2.SetWeight(ServiceKind.BasicRepair, 3);
            r2.SetWeight(ServiceKind.TireChange, 0); // locked
            bool sawLocked = false;
            for (int i = 0; i < 50; i++)
                if (r2.CheckIn("c" + i, i)?.Kind == ServiceKind.TireChange) sawLocked = true;
            T.True(!sawLocked, "zero-weight (locked) service never requested");
            T.Eq(r2.TotalWeight, 3, "total weight ignores locked services");

            // ---- ticket carries queue wait for the patience tip ----
            var r3 = new Reception(seed: 3);
            r3.SetWeight(ServiceKind.OilChange, 1);
            var tk = r3.CheckIn("carZ", 10f);
            T.Eq(tk.VehicleId, "carZ", "ticket bound to its vehicle");
            T.Near(tk.QueueWaitSeconds(40f), 30f, 0.001, "queue wait measured from check-in");
            T.Near(tk.QueueWaitSeconds(5f), 0f, 0.001, "negative wait clamped to 0");
        }

        private static void ConstructionTests()
        {
            // ---- partial funding persists and completes exactly ----
            var z = new ConstructionZoneState("zone_bay2", 100);
            T.Eq(z.Remaining, 100, "starts fully unfunded");
            T.True(!z.Built, "not built yet");
            T.Eq(z.Fund(30), 30, "funded 30");
            T.Near(z.Progress01, 0.30, 0.001, "progress reflects partial funding");
            T.True(!z.Built, "still not built at 30%");
            T.Eq(z.Fund(30), 30, "topped up across a second visit");
            T.Eq(z.Funded, 60, "partial funding persisted between visits");

            // ---- overpay is clamped to what remains (never overcharges the player) ----
            T.Eq(z.Fund(999), 40, "only the remaining 40 is consumed, not 999");
            T.True(z.Built, "zone built once fully funded");
            T.Eq(z.Remaining, 0, "nothing remaining");
            T.Eq(z.Fund(50), 0, "a built zone consumes no further cash");

            // ---- restore from save mid-funding ----
            var z2 = ConstructionZoneState.FromSave("zone_wash", 200, 75, false);
            T.Eq(z2.Funded, 75, "restored partial funding from save");
            T.Eq(z2.Remaining, 125, "restored remaining");
            T.True(!z2.Built, "restored as unbuilt");

            // ---- a fully funded save restores as built ----
            var z3 = ConstructionZoneState.FromSave("zone_done", 50, 50, false);
            T.True(z3.Built, "fully funded zone restores as built even if flag was stale");
        }

        private static void UpgradeTests()
        {
            var state = new UpgradeState();
            state.Register(new UpgradeDefinition
            {
                Id = "bay_speed",
                DisplayName = "Bay Work Speed",
                Scope = UpgradeScope.LevelLocal,
                Curve = CostCurve.Workstation,
                MaxTier = 3,
                EffectPerTier = 0.15f
            });
            state.Register(new UpgradeDefinition
            {
                Id = "player_speed",
                DisplayName = "Player Move Speed",
                Scope = UpgradeScope.Permanent,
                Curve = CostCurve.Office,
                MaxTier = 2,
                EffectPerTier = 0.20f
            });

            // ---- costs follow the data-driven curve (Doc 04 §2.3: 100 * 1.6^t) ----
            T.Eq(state.TierOf("bay_speed"), 0, "starts at tier 0");
            T.Eq(state.NextCost("bay_speed"), 100, "tier 0 costs 100");

            // ---- purchase debits the wallet and raises the tier ----
            // Three bay tiers cost 100 + 160 + 256 = 516, so seed enough to buy them all.
            long wallet = 1000;
            bool TrySpend(int c) { if (wallet < c) return false; wallet -= c; return true; }

            T.True(state.TryPurchase("bay_speed", TrySpend), "bought tier 1");
            T.Eq(state.TierOf("bay_speed"), 1, "tier is now 1");
            T.Eq(wallet, 900, "wallet debited by exactly 100");
            T.Near(state.MultiplierOf("bay_speed"), 1.15, 0.001, "tier 1 gives a 1.15x multiplier");
            T.Eq(state.NextCost("bay_speed"), 160, "tier 1 costs 160 (100 * 1.6)");

            T.True(state.TryPurchase("bay_speed", TrySpend), "bought tier 2");
            T.Eq(wallet, 740, "wallet debited by 160");
            T.Near(state.EffectOf("bay_speed"), 0.30, 0.001, "two tiers stack to +0.30");

            // ---- maxing out blocks further purchase ----
            T.True(state.TryPurchase("bay_speed", TrySpend), "bought tier 3 (max)");
            T.True(state.IsMaxed("bay_speed"), "bay_speed is maxed at 3");
            T.Eq(state.NextCost("bay_speed"), -1, "maxed upgrade reports no next cost");
            T.True(!state.TryPurchase("bay_speed", TrySpend), "cannot buy past max tier");

            // ---- insufficient funds leaves state untouched ----
            long broke = 10;
            bool TrySpendBroke(int c) { if (broke < c) return false; broke -= c; return true; }
            T.True(!state.TryPurchase("player_speed", TrySpendBroke), "cannot buy without cash");
            T.Eq(state.TierOf("player_speed"), 0, "tier unchanged after failed purchase");
            T.Eq(broke, 10, "wallet untouched after failed purchase");

            // ---- level-local resets, permanent survives (Doc 04 §3) ----
            state.TryPurchase("player_speed", TrySpend); // 150 from the 240 remaining
            T.Eq(state.TierOf("player_speed"), 1, "permanent upgrade bought");
            state.ResetLevelLocal();
            T.Eq(state.TierOf("bay_speed"), 0, "level-local upgrade reset on new location");
            T.Eq(state.TierOf("player_speed"), 1, "permanent upgrade survives the reset");

            // ---- save round-trip of tiers ----
            var saved = new System.Collections.Generic.Dictionary<string, int>
            {
                { "bay_speed", 2 }, { "unknown_id", 9 }
            };
            state.LoadTiers(saved);
            T.Eq(state.TierOf("bay_speed"), 2, "tiers restored from save");
            T.Eq(state.TierOf("unknown_id"), 0, "unknown save ids are ignored");

            // ---- restored tiers are clamped to max ----
            state.LoadTiers(new System.Collections.Generic.Dictionary<string, int> { { "bay_speed", 99 } });
            T.Eq(state.TierOf("bay_speed"), 3, "corrupt/overflow tier clamped to max");
        }
    }
}

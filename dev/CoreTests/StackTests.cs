using Overhaul.Core;

namespace Overhaul.CoreTests
{
    /// <summary>Verifies mixed carrying, type-filtered removal and bulky-slot rules (Doc 02 §1.3).</summary>
    public static class StackTests
    {
        // Bulky resources cost 2 slots; everything else costs 1.
        private static int Slots(string id) => (id == "engine" || id == "body_panel") ? 2 : 1;

        public static void Run()
        {
            // ---- capacity in slots ----
            var s = new StackInventory(5, Slots);
            for (int i = 0; i < 5; i++) T.True(s.TryAdd("tire"), $"add tire #{i + 1} within capacity");
            T.True(!s.TryAdd("tire"), "6th tire rejected (capacity 5)");
            T.Eq(s.UsedSlots, 5, "used slots after 5 tires");
            T.Eq(s.Remove("tire", 4), 4, "remove 4 tires");
            T.Eq(s.Count, 1, "one tire remains");

            // ---- mixed carrying + type-filtered removal ----
            var m = new StackInventory(5, Slots);
            m.TryAdd("tire"); m.TryAdd("oil"); m.TryAdd("tire"); m.TryAdd("oil"); m.TryAdd("tire");
            T.Eq(m.CountOf("tire"), 3, "3 tires carried alongside oil");
            T.Eq(m.CountOf("oil"), 2, "2 oil carried");
            // a station pulls oil from between the tires (not trapped underneath)
            T.Eq(m.Remove("oil", 5), 2, "remove all oil regardless of position");
            T.Eq(m.CountOf("oil"), 0, "no oil left");
            T.Eq(m.CountOf("tire"), 3, "tires untouched by oil removal");
            T.Eq(m.FreeSlots, 2, "free slots after removing 2 oil from a 5-stack");

            // ---- bulky items (2 slots) ----
            var b = new StackInventory(5, Slots);
            T.True(b.TryAdd("engine"), "add engine (2 slots)");
            T.True(b.TryAdd("engine"), "add second engine (4 slots used)");
            T.True(b.TryAdd("tire"), "add tire (5 slots used)");
            T.True(!b.TryAdd("tire"), "no room for a 6th slot");
            T.True(!b.TryAdd("engine"), "no room for a bulky item either");
            T.Eq(b.UsedSlots, 5, "bulky slot accounting");

            // ---- capacity upgrade only grows ----
            var u = new StackInventory(5, Slots);
            u.SetCapacity(3); // ignored (downgrade)
            T.Eq(u.CapacitySlots, 5, "capacity never shrinks");
            u.SetCapacity(8);
            T.Eq(u.CapacitySlots, 8, "capacity upgrade applied");
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.Tests
{
    /// <summary>
    /// Exercises the gameplay MonoBehaviours through Unity's runtime (proving the
    /// Overhaul.Game assembly works in-engine, not just in the dotnet console runner).
    /// The bay is ticked deterministically via its explicit Tick method.
    /// </summary>
    public sealed class LoopEditTests
    {
        [Test]
        public void FullServiceCycle_ConsumesParts_AndPaysWallet()
        {
            var go = new GameObject("bay");
            var rack = go.AddComponent<ResourceRack>();
            var eco = go.AddComponent<EconomyManager>();
            var bay = go.AddComponent<ServiceBay>();

            rack.SetCapacity(8);
            for (int i = 0; i < 4; i++) Assert.IsTrue(rack.Add("tire"), "rack accepts tire");

            bay.Configure(rack, eco);
            bay.ConfigureRecipe("tire", 4, 6f, 20);
            bay.VehiclePresent = true;

            // Drive the FSM in 0.5s steps until the car is serviced (cap at 12s sim).
            for (float t = 0f; t < 12f && bay.ServicedCount == 0; t += 0.5f)
                bay.Tick(0.5f);

            Assert.AreEqual(1, bay.ServicedCount, "exactly one car serviced");
            Assert.AreEqual(0, rack.CountOf("tire"), "the 4 tires were consumed");
            Assert.AreEqual(24, eco.Wallet, "wallet paid $20 base + 20% tip = $24");
            Assert.IsFalse(bay.VehiclePresent, "serviced car left the bay");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bay_Starves_WhenPartsMissing_ThenRecovers()
        {
            var go = new GameObject("bay");
            var rack = go.AddComponent<ResourceRack>();
            var eco = go.AddComponent<EconomyManager>();
            var bay = go.AddComponent<ServiceBay>();

            bay.Configure(rack, eco);
            bay.ConfigureRecipe("tire", 4, 6f, 20);
            bay.VehiclePresent = true;

            bay.Tick(0.5f); // vehicle present but rack empty
            Assert.AreEqual(WorkstationState.Starved, bay.State, "empty rack -> Starved");

            rack.SetCapacity(8);
            for (int i = 0; i < 4; i++) rack.Add("tire");
            bay.Tick(0.5f); // parts arrive
            Assert.AreEqual(WorkstationState.Ready, bay.State, "parts arrive -> Ready");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Carrier_TypeFilteredRemoval_ThroughUnityAssembly()
        {
            // Confirms the engine-agnostic StackInventory is usable from the Game assembly.
            var stack = new StackInventory(5, _ => 1);
            stack.TryAdd("tire"); stack.TryAdd("oil"); stack.TryAdd("tire");
            Assert.AreEqual(1, stack.Remove("oil", 5), "pulls oil from between tires");
            Assert.AreEqual(2, stack.CountOf("tire"), "tires untouched");
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.Tests
{
    /// <summary>
    /// Covers the physical interaction layer (collect from a source, deposit into a rack)
    /// and customer-vehicle movement, all driven deterministically through explicit ticks.
    /// </summary>
    public sealed class InteractionTests
    {
        [Test]
        public void CollectZone_MovesPartsFromSourceToCarrier()
        {
            var src = new GameObject("src").AddComponent<PartsSource>();
            src.Configure("tire", 12, 2f);
            var carrier = new GameObject("carrier").AddComponent<CarrierView>();
            var zone = new GameObject("collect").AddComponent<InteractionZone>();
            zone.Configure(InteractionKind.Collect, "tire", src, null, null);

            zone.Step(carrier);
            zone.Step(carrier);

            Assert.AreEqual(2, carrier.Stack.CountOf("tire"), "carrier gained 2 tires");
            Assert.AreEqual(10, src.Stock, "source stock dropped by 2");

            Object.DestroyImmediate(src.gameObject);
            Object.DestroyImmediate(carrier.gameObject);
            Object.DestroyImmediate(zone.gameObject);
        }

        [Test]
        public void DepositZone_MovesPartsFromCarrierToRack()
        {
            var carrier = new GameObject("carrier").AddComponent<CarrierView>();
            carrier.TryCollect("tire", null);
            carrier.TryCollect("tire", null);

            var rack = new GameObject("rack").AddComponent<ResourceRack>();
            rack.SetCapacity(8);
            var zone = new GameObject("deposit").AddComponent<InteractionZone>();
            zone.Configure(InteractionKind.Deposit, "tire", null, rack, null);

            zone.Step(carrier);
            zone.Step(carrier);

            Assert.AreEqual(0, carrier.Stack.CountOf("tire"), "carrier emptied");
            Assert.AreEqual(2, rack.CountOf("tire"), "rack received 2 tires");

            Object.DestroyImmediate(carrier.gameObject);
            Object.DestroyImmediate(rack.gameObject);
            Object.DestroyImmediate(zone.gameObject);
        }

        [Test]
        public void CustomerVehicle_FollowsPath_WithoutCuttingTheCorner()
        {
            // The exit route the village builds: bay slot -> street -> exit. The car must
            // ride the street rather than cut diagonally across the station lots, which is
            // what a single straight hop used to do (driving it through the buildings).
            var go = new GameObject("car");
            go.transform.position = new Vector3(-13.7f, 0.2f, 11.8f); // in the bay
            var cv = go.AddComponent<CustomerVehicle>();

            var street = new Vector3(-13.7f, 0.2f, 6.5f);
            var exit = new Vector3(40f, 0.2f, 6.5f);
            cv.SetPath(street, exit);

            Assert.IsFalse(cv.AtTarget, "path not walked yet");

            float maxZEastOfBay = float.MinValue;
            bool arrived = false;
            for (int i = 0; i < 4000 && !arrived; i++)
            {
                arrived = cv.Tick(0.05f);
                var p = go.transform.position;
                if (p.x > -12f) maxZEastOfBay = Mathf.Max(maxZEastOfBay, p.z);
            }

            Assert.IsTrue(arrived, "car completed the full path");
            Assert.IsTrue(cv.AtTarget, "AtTarget only true at the end of the path, not each leg");
            Assert.Less(Vector3.Distance(go.transform.position, exit), 0.1f, "car finished at the exit");
            // Station pads begin around z=9.3; staying near the street proves no shortcut.
            Assert.Less(maxZEastOfBay, 9f, "car stayed on the street instead of crossing the lots");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CustomerVehicle_DrivesToTarget()
        {
            var go = new GameObject("car");
            go.transform.position = Vector3.zero;
            var cv = go.AddComponent<CustomerVehicle>();

            cv.SetTarget(new Vector3(10f, 0f, 0f));
            Assert.IsFalse(cv.AtTarget, "not arrived yet");

            bool arrived = false;
            for (int i = 0; i < 300 && !arrived; i++) arrived = cv.Tick(0.1f);

            Assert.IsTrue(cv.AtTarget, "vehicle reached its target");
            Assert.Less(Vector3.Distance(go.transform.position, new Vector3(10f, 0f, 0f)), 0.1f,
                "vehicle stopped at the target position");

            Object.DestroyImmediate(go);
        }
    }
}

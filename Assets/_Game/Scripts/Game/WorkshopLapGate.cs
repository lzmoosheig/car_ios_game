using UnityEngine;

namespace Overhaul.Game
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorkshopLapGate : MonoBehaviour
    {
        [SerializeField] private WorkshopTestDriveLoop loop;
        [SerializeField] private int gateIndex;
        private float _lastPassTime;

        public void Configure(WorkshopTestDriveLoop testLoop, int index)
        {
            loop = testLoop;
            gateIndex = index;
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastPassTime < 0.5f) return;
            var vehicle = other.GetComponentInParent<ArcadeVehicleController>();
            if (vehicle == null) return;
            _lastPassTime = Time.time;
            loop?.PassGate(gateIndex, vehicle);
        }
    }
}

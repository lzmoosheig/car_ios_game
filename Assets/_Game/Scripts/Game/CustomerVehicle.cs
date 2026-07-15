using UnityEngine;

namespace Overhaul.Game
{
    public enum VehiclePhase { ToQueue, InQueue, ToBay, InBay, ToExit, Done }

    /// <summary>
    /// A customer's car. Moves on straight hops between fixed points (entrance → bay →
    /// exit) at constant speed with eased arrival — no free pathfinding, so no collisions
    /// (Doc 02 §3.3). The <see cref="GarageController"/> assigns targets and reads
    /// <see cref="AtTarget"/>/<see cref="Phase"/>. Driven by an explicit Tick so it is
    /// unit-testable without the play loop.
    /// </summary>
    public sealed class CustomerVehicle : MonoBehaviour
    {
        [SerializeField] private float speed = 6f;
        [SerializeField] private float arriveEpsilon = 0.05f;
        [SerializeField] private float turnSpeedDegPerSec = 360f;

        private Vector3 _target;
        private bool _hasTarget;

        public VehiclePhase Phase { get; set; } = VehiclePhase.ToBay;
        public bool AtTarget { get; private set; } = true;

        public void SetTarget(Vector3 worldPos)
        {
            _target = worldPos;
            _hasTarget = true;
            AtTarget = false;
        }

        /// <summary>Advance movement by dt. Returns true on the tick arrival is reached.</summary>
        public bool Tick(float dt)
        {
            if (!_hasTarget) return false;

            Vector3 pos = transform.position;
            Vector3 flatTarget = new Vector3(_target.x, pos.y, _target.z);
            Vector3 to = flatTarget - pos;

            if (to.magnitude <= arriveEpsilon)
            {
                transform.position = flatTarget;
                _hasTarget = false;
                AtTarget = true;
                return true;
            }

            Vector3 dir = to.normalized;
            transform.position = pos + dir * Mathf.Min(speed * dt, to.magnitude);

            var look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeedDegPerSec * dt);
            return false;
        }

        private void Update() => Tick(Time.deltaTime);
    }
}

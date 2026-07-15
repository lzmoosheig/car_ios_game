using System.Collections.Generic;
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
        private readonly Queue<Vector3> _path = new();

        public VehiclePhase Phase { get; set; } = VehiclePhase.ToBay;

        /// <summary>True only once the whole path is walked, not each intermediate leg.</summary>
        public bool AtTarget { get; private set; } = true;

        public void SetTarget(Vector3 worldPos)
        {
            _path.Clear();
            _target = worldPos;
            _hasTarget = true;
            AtTarget = false;
        }

        /// <summary>
        /// Drives through each point in order. Vehicles never free-roam: routes are authored
        /// so cars turn onto the street and stay on it instead of cutting across the lots
        /// (Doc 02 §3.3 fixed lanes).
        /// </summary>
        public void SetPath(params Vector3[] points)
        {
            _path.Clear();
            if (points == null || points.Length == 0) return;
            for (int i = 1; i < points.Length; i++) _path.Enqueue(points[i]);
            _target = points[0];
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

                // More legs to drive? Take the next one; the trip isn't finished yet.
                if (_path.Count > 0)
                {
                    _target = _path.Dequeue();
                    return false;
                }

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

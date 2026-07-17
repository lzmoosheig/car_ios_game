using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>Short workshop lap used to compare each mobile tuning preset against
    /// its own best. Gate order makes shortcuts invalid while repeated start crossings
    /// naturally begin the next comparison lap.</summary>
    public sealed class WorkshopTestDriveLoop : MonoBehaviour
    {
        [SerializeField] private OwnedCarSystem ownedCar;
        [SerializeField] private int checkpointCount = 3;

        private ArcadeVehicleController _vehicle;
        private int _nextCheckpoint;
        private float _lapTime;
        private float _statusUntil;

        public bool Active { get; private set; }
        public float LapTime => _lapTime;
        public float BestLap => ownedCar != null ? ownedCar.WorkshopBestLap : 0f;
        public float LastLap { get; private set; }
        public float LastDelta { get; private set; }
        public bool LastWasBest { get; private set; }
        public string SetupName => ownedCar != null ? ownedCar.Car.Setup.ToString() : "Setup";
        public string Status => Time.unscaledTime < _statusUntil
            ? (LastWasBest ? "NEW SETUP BEST" : "LAP COMPLETE")
            : Active ? (_nextCheckpoint > checkpointCount ? "RETURN TO START" : $"CHECKPOINT {_nextCheckpoint}/{checkpointCount}")
                : "CROSS START TO TEST";

        public void Configure(OwnedCarSystem system, int checkpoints)
        {
            ownedCar = system;
            checkpointCount = Mathf.Max(1, checkpoints);
        }

        private void Update()
        {
            if (Active) _lapTime += Time.deltaTime;
            if (_vehicle == null || !_vehicle.HasDriver) CancelLap();
        }

        public void PassGate(int gateIndex, ArcadeVehicleController vehicle)
        {
            if (vehicle == null || !vehicle.HasDriver || ownedCar == null || !ownedCar.Owned) return;

            if (gateIndex == 0)
            {
                if (Active && vehicle == _vehicle && _nextCheckpoint > checkpointCount)
                    CompleteLap();
                BeginLap(vehicle);
                return;
            }

            if (!Active || vehicle != _vehicle || gateIndex != _nextCheckpoint) return;
            _nextCheckpoint++;
        }

        public void CancelLap()
        {
            if (_vehicle != null) _vehicle.ResetPerformed -= CancelLap;
            Active = false;
            _vehicle = null;
            _lapTime = 0f;
            _nextCheckpoint = 1;
        }

        private void OnDisable() => CancelLap();

        private void BeginLap(ArcadeVehicleController vehicle)
        {
            if (_vehicle != null) _vehicle.ResetPerformed -= CancelLap;
            _vehicle = vehicle;
            _vehicle.ResetPerformed -= CancelLap;
            _vehicle.ResetPerformed += CancelLap;
            _lapTime = 0f;
            _nextCheckpoint = 1;
            Active = true;
        }

        private void CompleteLap()
        {
            LastLap = _lapTime;
            float previousBest = ownedCar.WorkshopBestLap;
            LastDelta = previousBest > 0f ? LastLap - previousBest : 0f;
            LastWasBest = ownedCar.RecordWorkshopLap(LastLap);
            _statusUntil = Time.unscaledTime + 3f;
        }
    }

}

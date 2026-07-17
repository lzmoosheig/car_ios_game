using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// The starter car's scene presence (Doc 09 §7). Before it is claimed it is a
    /// tappable showpiece with an unlock requirement; after claiming it becomes a real
    /// drivable vehicle: this view adds the <see cref="ArcadeVehicleController"/> on
    /// claim (so VehicleInteractor cannot enter an unowned car), pushes the tuning
    /// profile into the physics, and accrues mileage wear while the player drives.
    /// </summary>
    public sealed class OwnedCarView : MonoBehaviour, IInteractable
    {
        [SerializeField] private OwnedCarSystem system;

        private ArcadeVehicleController _vehicle;
        private float _pendingWearKm;

        public string Title => system != null && system.Owned ? "My Car" : "Starter Car";
        public Transform PivotTransform => transform;

        public void Configure(OwnedCarSystem carSystem)
        {
            system = carSystem;
        }

        private void OnEnable()
        {
            if (system == null) return;
            system.Changed += OnCarChanged;
            OnCarChanged();
        }

        private void OnDisable()
        {
            if (system != null) system.Changed -= OnCarChanged;
        }

        private void OnCarChanged()
        {
            if (system == null || !system.Owned) return;
            EnsureDrivable();
            _vehicle.ApplyHandlingProfile(system.Profile);
        }

        /// <summary>Adding the controller only after the claim keeps the parked starter
        /// car out of VehicleInteractor's proximity scan until it is actually owned.</summary>
        private void EnsureDrivable()
        {
            if (_vehicle != null) return;
            _vehicle = GetComponent<ArcadeVehicleController>();
            if (_vehicle == null) _vehicle = gameObject.AddComponent<ArcadeVehicleController>();
        }

        private void Update()
        {
            if (_vehicle == null || !_vehicle.HasDriver || system == null) return;

            _pendingWearKm += _vehicle.Speed * Time.deltaTime / 1000f;
            // Batch wear into 50 m chunks: per-frame Changed events would make every
            // open panel and save autotimer churn for fractions of a meter.
            if (_pendingWearKm >= 0.05f)
            {
                system.ApplyWear(_pendingWearKm);
                _pendingWearKm = 0f;
            }
        }

        public void OnSelected() => SelectionRing.Show(transform, 4.2f);
        public void OnDeselected() => SelectionRing.Hide();

        public void GetInfoLines(List<string> into)
        {
            if (system == null) { into.Add("Unavailable"); return; }

            var car = system.Car;
            if (!car.Owned)
            {
                into.Add("Your first own car - earned, not bought.");
                into.Add($"Requires {OwnedCarSystem.StarterReputationRequirement} reputation");
                into.Add("Serve customers to build reputation.");
                return;
            }

            into.Add($"Class {(car.Certified ? car.CertifiedClass.ToString() : "-")} · {system.PerformancePoints} PP" +
                     (car.Certified ? "" : " (not certified)"));
            into.Add($"Setup: {car.Setup} · Mileage: {car.MileageKm:0.0} km");
            into.Add($"Engine {Percent(car.EngineCondition)} · Tires {Percent(car.TireCondition)}");
            into.Add($"Body {Percent(car.BodyCondition)} · Clean {Percent(car.Cleanliness)}");
            into.Add("Walk up and press E (or DRIVE) to drive.");
        }

        public void GetActions(List<InteractableAction> into)
        {
            if (system == null || system.Owned) return;
            into.Add(new InteractableAction("Claim starter car",
                () => system.TryClaimStarter(), system.CanClaimStarter));
        }

        private static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}

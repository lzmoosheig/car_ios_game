using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    public enum OwnedCarStationRole { Maintain, Diagnose, Tune, Inspect }

    /// <summary>
    /// In-world tap pad giving a station its owned-car function (Doc 09 §6): Basic Bay
    /// maintains condition, Diagnostic reveals the full report, Tuning changes preset
    /// and part tiers, Inspection certifies the race class. A dedicated pad per station
    /// (same pattern as CarDeliveryBuildingButton) keeps BuildingView untouched and
    /// avoids two IInteractables competing in one parent chain.
    /// </summary>
    public sealed class OwnedCarStationPad : MonoBehaviour, IInteractable
    {
        [SerializeField] private OwnedCarStationRole role;
        [SerializeField] private OwnedCarSystem system;
        [SerializeField] private CarDeliverySystem carDelivery;

        public string Title => role switch
        {
            OwnedCarStationRole.Maintain => "Car Service",
            OwnedCarStationRole.Diagnose => "Diagnostics",
            OwnedCarStationRole.Tune => "Tuning",
            _ => "Inspection"
        };

        public Transform PivotTransform => transform;

        public void Configure(OwnedCarStationRole padRole, OwnedCarSystem carSystem, CarDeliverySystem delivery)
        {
            role = padRole;
            system = carSystem;
            carDelivery = delivery;
        }

        public void OnSelected() => SelectionRing.Show(transform, 3.2f);
        public void OnDeselected() => SelectionRing.Hide();

        public void GetInfoLines(List<string> into)
        {
            if (system == null) { into.Add("Unavailable"); return; }
            if (!system.Owned)
            {
                into.Add("For your personal car.");
                into.Add("Claim the starter car first (parked out front).");
                return;
            }

            var car = system.Car;
            switch (role)
            {
                case OwnedCarStationRole.Maintain:
                    into.Add($"Condition: {Mathf.RoundToInt(CarMath.AverageCondition(car) * 100f)}%");
                    into.Add($"Full service: ${OwnedCarSystem.MaintenanceCashCost}");
                    into.Add($"Uses {OwnedCarSystem.MaintenanceTireCost}x Tires + {OwnedCarSystem.MaintenanceOilCost}x Oil from delivery stock when available.");
                    if (carDelivery != null)
                        into.Add($"Stock: {carDelivery.OwnedCountOf("tire")} tires · {carDelivery.OwnedCountOf("oil")} oil");
                    break;

                case OwnedCarStationRole.Diagnose:
                    into.Add($"Engine {Percent(car.EngineCondition)} · Tires {Percent(car.TireCondition)}");
                    into.Add($"Body {Percent(car.BodyCondition)} · Clean {Percent(car.Cleanliness)}");
                    into.Add($"Mileage: {car.MileageKm:0.0} km · Setup: {car.Setup}");
                    into.Add($"Estimated {system.PerformancePoints} PP -> Class {system.CurrentClass}");
                    if (CarMath.AverageCondition(car) < 0.7f)
                        into.Add("Recommendation: book a service at the Basic Change Bay.");
                    break;

                case OwnedCarStationRole.Tune:
                    into.Add($"Setup: {car.Setup}");
                    into.Add($"Engine tier {car.EngineTier}/{CarMath.MaxTier} · Tire tier {car.TireTier}/{CarMath.MaxTier}");
                    into.Add($"{system.PerformancePoints} PP -> Class {system.CurrentClass}");
                    if (!car.Certified) into.Add("Changed builds need re-inspection to race.");
                    break;

                default: // Inspect
                    into.Add(car.Certified
                        ? $"Certified: Class {car.CertifiedClass} · {system.PerformancePoints} PP"
                        : "Not certified for competition yet.");
                    into.Add("Inspection assigns the official race class.");
                    break;
            }
        }

        public void GetActions(List<InteractableAction> into)
        {
            if (system == null || !system.Owned) return;
            var car = system.Car;

            switch (role)
            {
                case OwnedCarStationRole.Maintain:
                    bool needsService = CarMath.AverageCondition(car) < 0.999f;
                    into.Add(new InteractableAction($"Service car (${OwnedCarSystem.MaintenanceCashCost})",
                        () => system.TryMaintain(), needsService));
                    break;

                case OwnedCarStationRole.Tune:
                    var next = NextPreset(car.Setup);
                    into.Add(new InteractableAction($"Setup -> {next}", () => system.SetSetup(next)));
                    if (car.EngineTier < CarMath.MaxTier)
                        into.Add(new InteractableAction($"Engine +1 (${system.NextEngineUpgradeCost})",
                            () => system.TryUpgradeEngine()));
                    if (car.TireTier < CarMath.MaxTier)
                        into.Add(new InteractableAction($"Tires +1 (${system.NextTireUpgradeCost})",
                            () => system.TryUpgradeTires()));
                    break;

                case OwnedCarStationRole.Inspect:
                    if (!car.Certified)
                        into.Add(new InteractableAction($"Certify (Class {system.CurrentClass})",
                            () => system.TryCertify()));
                    break;
            }
        }

        private static CarSetupPreset NextPreset(CarSetupPreset current) => current switch
        {
            CarSetupPreset.Stable => CarSetupPreset.Balanced,
            CarSetupPreset.Balanced => CarSetupPreset.Agile,
            _ => CarSetupPreset.Stable
        };

        private static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}

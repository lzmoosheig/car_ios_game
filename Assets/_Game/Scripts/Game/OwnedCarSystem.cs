using System;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-side owner of the player's personal car state (Doc 09 §7, Phase B slice).
    /// The rules live in the engine-agnostic <see cref="CarMath"/>; this holds the
    /// state, brokers cash/parts with EconomyManager and CarDeliverySystem, and raises
    /// change notifications - the same split every other system in the project uses.
    /// </summary>
    public sealed class OwnedCarSystem : MonoBehaviour
    {
        /// <summary>Doc 09 §7.1: the starter car is earned through an early milestone,
        /// not purchased. Reputation is the milestone value the village already grants.</summary>
        public const int StarterReputationRequirement = 10;

        public const int MaintenanceCashCost = 250;
        public const int MaintenanceTireCost = 4;
        public const int MaintenanceOilCost = 1;

        [SerializeField] private EconomyManager economy;
        [SerializeField] private CarDeliverySystem carDelivery;

        private OwnedCarState _car = new();

        public event Action Changed;

        public OwnedCarState Car => _car;
        public bool Owned => _car.Owned;
        public int PerformancePoints => CarMath.PerformancePoints(_car);
        public RaceClass CurrentClass => CarMath.ClassOf(PerformancePoints);
        public HandlingProfile Profile => CarMath.ProfileOf(_car);
        public float WorkshopBestLap => CarMath.WorkshopBestLap(_car, _car.Setup);

        public void Configure(EconomyManager eco, CarDeliverySystem delivery)
        {
            economy = eco;
            carDelivery = delivery;
        }

        public bool CanClaimStarter =>
            !_car.Owned && economy != null && economy.Reputation >= StarterReputationRequirement;

        public bool TryClaimStarter()
        {
            if (!CanClaimStarter) return false;
            _car.Owned = true;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Basic Bay maintenance (Doc 09 §6.10): restores all condition values.
        /// Consumes delivery-stock tires and oil when available - stock is a discount,
        /// never a hard gate, so a stockless player can still service the car for cash.</summary>
        public bool TryMaintain()
        {
            if (!_car.Owned || economy == null) return false;
            if (CarMath.AverageCondition(_car) >= 0.999f) return false;
            if (!economy.TrySpend(MaintenanceCashCost)) return false;

            if (carDelivery != null && carDelivery.OwnedCountOf("tire") >= MaintenanceTireCost)
                carDelivery.TryConsume("tire", MaintenanceTireCost);
            if (carDelivery != null && carDelivery.OwnedCountOf("oil") >= MaintenanceOilCost)
                carDelivery.TryConsume("oil", MaintenanceOilCost);

            CarMath.RestoreCondition(_car);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Tuning Station preset selection (Doc 09 §6.15) - free to change so
        /// experimentation stays cheap; the paid choices are the part tiers below.</summary>
        public void SetSetup(CarSetupPreset preset)
        {
            if (!_car.Owned || _car.Setup == preset) return;
            _car.Setup = preset;
            InvalidateCertification();
            Changed?.Invoke();
        }

        public int NextEngineUpgradeCost => CarMath.UpgradeCost(_car.EngineTier);
        public int NextTireUpgradeCost => CarMath.UpgradeCost(_car.TireTier);

        public bool TryUpgradeEngine()
        {
            if (!_car.Owned || economy == null || _car.EngineTier >= CarMath.MaxTier) return false;
            if (!economy.TrySpend(NextEngineUpgradeCost)) return false;
            _car.EngineTier++;
            InvalidateCertification();
            Changed?.Invoke();
            return true;
        }

        public bool TryUpgradeTires()
        {
            if (!_car.Owned || economy == null || _car.TireTier >= CarMath.MaxTier) return false;
            if (!economy.TrySpend(NextTireUpgradeCost)) return false;
            _car.TireTier++;
            InvalidateCertification();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Vehicle Inspection certification (Doc 09 §6.16): the Inspection
        /// building is the authority that assigns race class - builds are never
        /// self-declared, so changing the build invalidates the certificate.</summary>
        public bool TryCertify()
        {
            if (!_car.Owned) return false;
            _car.Certified = true;
            _car.CertifiedClass = CurrentClass;
            Changed?.Invoke();
            return true;
        }

        private void InvalidateCertification() => _car.Certified = false;

        /// <summary>Driving wear, forwarded by the car view while the player drives.</summary>
        public void ApplyWear(float kilometers)
        {
            if (!_car.Owned || kilometers <= 0f) return;
            CarMath.ApplyWear(_car, kilometers);
            Changed?.Invoke();
        }

        public bool RecordWorkshopLap(float seconds)
        {
            if (!_car.Owned) return false;
            bool isBest = CarMath.TryRecordWorkshopLap(_car, _car.Setup, seconds);
            if (isBest) Changed?.Invoke();
            return isBest;
        }

        // ------------------------------------------------------------------- persistence

        public OwnedCarSave Capture()
        {
            return new OwnedCarSave
            {
                Owned = _car.Owned,
                ModelId = _car.ModelId,
                EngineCondition = _car.EngineCondition,
                TireCondition = _car.TireCondition,
                BodyCondition = _car.BodyCondition,
                Cleanliness = _car.Cleanliness,
                MileageKm = _car.MileageKm,
                Setup = (int)_car.Setup,
                EngineTier = _car.EngineTier,
                TireTier = _car.TireTier,
                Certified = _car.Certified,
                CertifiedClass = (int)_car.CertifiedClass,
                WorkshopBestLapSeconds = _car.WorkshopBestLapSeconds != null
                    ? (float[])_car.WorkshopBestLapSeconds.Clone()
                    : new float[3]
            };
        }

        public void Restore(OwnedCarSave save)
        {
            if (save == null) return;
            _car = new OwnedCarState
            {
                Owned = save.Owned,
                ModelId = string.IsNullOrEmpty(save.ModelId) ? "hatchback-sports" : save.ModelId,
                EngineCondition = Mathf.Clamp01(save.EngineCondition),
                TireCondition = Mathf.Clamp01(save.TireCondition),
                BodyCondition = Mathf.Clamp01(save.BodyCondition),
                Cleanliness = Mathf.Clamp01(save.Cleanliness),
                MileageKm = Mathf.Max(0f, save.MileageKm),
                Setup = (CarSetupPreset)Mathf.Clamp(save.Setup, 0, 2),
                EngineTier = Mathf.Clamp(save.EngineTier, 0, CarMath.MaxTier),
                TireTier = Mathf.Clamp(save.TireTier, 0, CarMath.MaxTier),
                Certified = save.Certified,
                CertifiedClass = (RaceClass)Mathf.Clamp(save.CertifiedClass, 0, 4),
                WorkshopBestLapSeconds = NormalizeLapTimes(save.WorkshopBestLapSeconds)
            };
            Changed?.Invoke();
        }

        private static float[] NormalizeLapTimes(float[] source)
        {
            var result = new float[3];
            if (source == null) return result;
            for (int i = 0; i < result.Length && i < source.Length; i++)
                result[i] = source[i] > 0f && !float.IsNaN(source[i]) && !float.IsInfinity(source[i]) ? source[i] : 0f;
            return result;
        }
    }
}

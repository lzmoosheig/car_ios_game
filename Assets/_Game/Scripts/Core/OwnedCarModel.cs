using System;

namespace Overhaul.Core
{
    /// <summary>Mobile-friendly handling presets (Doc 09 §6.11): no numerical tuning UI,
    /// three named trade-offs the player can feel on a test drive.</summary>
    public enum CarSetupPreset { Stable, Balanced, Agile }

    /// <summary>Race classes from Doc 09 §7.4. R (normalized event rules) exists in the
    /// enum for later phases; PP math tops out at S until race cars ship.</summary>
    public enum RaceClass { C, B, A, S, R }

    /// <summary>
    /// One player-owned car (Doc 09 §7.2, first slice). Condition values are
    /// non-destructive (§5.3): they shave the bonus portion of performance, never
    /// disable the car. Plain serializable state; all rules live in <see cref="CarMath"/>.
    /// </summary>
    [Serializable]
    public sealed class OwnedCarState
    {
        public bool Owned;
        public string ModelId = "hatchback-sports";

        // Condition, 0..1 (Doc 09 §5.3). New cars start at full condition.
        public float EngineCondition = 1f;
        public float TireCondition = 1f;
        public float BodyCondition = 1f;
        public float Cleanliness = 1f;

        public float MileageKm;
        public CarSetupPreset Setup = CarSetupPreset.Balanced;

        // Installed-part tiers (0-2): the Doc 09 §7.2 "installed parts" list reduced to
        // two visible upgrade axes for the first slice. Engine drives speed, tires grip.
        public int EngineTier;
        public int TireTier;

        // Inspection certification (Doc 09 §6.16): class is assigned, not self-declared.
        public bool Certified;
        public RaceClass CertifiedClass = RaceClass.C;

        // Workshop test-drive personal bests, indexed by CarSetupPreset. Zero means
        // no valid lap yet. A setup is compared only with itself so tuning trade-offs
        // stay legible instead of producing one misleading global record.
        public float[] WorkshopBestLapSeconds = new float[3];
    }

    /// <summary>Handling multipliers applied on top of a vehicle's serialized base
    /// values, so presets/upgrades never overwrite hand-tuned physics constants.</summary>
    public readonly struct HandlingProfile
    {
        public readonly float Acceleration;
        public readonly float TopSpeed;
        public readonly float Steering;
        public readonly float Grip;

        public HandlingProfile(float acceleration, float topSpeed, float steering, float grip)
        {
            Acceleration = acceleration;
            TopSpeed = topSpeed;
            Steering = steering;
            Grip = grip;
        }

        public static HandlingProfile Identity => new(1f, 1f, 1f, 1f);
    }

    /// <summary>
    /// Pure owned-car rules (performance points, class thresholds, condition decay,
    /// handling profiles) - engine-agnostic and unit-testable, matching the
    /// EconomyFormulas / CarDeliveryLogic split.
    /// </summary>
    public static class CarMath
    {
        /// <summary>Starter-car baseline: bottom of class C with headroom to reach B
        /// through both upgrade axes (Doc 09 §7.4 balancing targets, not final).</summary>
        public const int StarterBasePP = 240;
        public const int EnginePPPerTier = 60;
        public const int TiresPPPerTier = 40;
        public const int MaxTier = 2;

        public static int PerformancePoints(OwnedCarState car)
        {
            if (car == null) return 0;
            return StarterBasePP + car.EngineTier * EnginePPPerTier + car.TireTier * TiresPPPerTier;
        }

        public static RaceClass ClassOf(int performancePoints)
        {
            if (performancePoints < 300) return RaceClass.C;
            if (performancePoints < 450) return RaceClass.B;
            if (performancePoints < 600) return RaceClass.A;
            return RaceClass.S;
        }

        public static float AverageCondition(OwnedCarState car)
        {
            if (car == null) return 0f;
            return Clamp01((car.EngineCondition + car.TireCondition + car.BodyCondition + car.Cleanliness) / 4f);
        }

        /// <summary>Condition scales only the bonus band (85%-100%) of performance:
        /// a neglected car is slower, never undrivable (Doc 09 §5.3 gentle failure).</summary>
        public static float ConditionFactor(OwnedCarState car)
            => 0.85f + 0.15f * AverageCondition(car);

        public static HandlingProfile ProfileOf(OwnedCarState car)
        {
            if (car == null) return HandlingProfile.Identity;

            float accel = 1f, top = 1f, steer = 1f, grip = 1f;
            switch (car.Setup)
            {
                case CarSetupPreset.Stable:
                    steer = 0.85f; grip = 1.3f; accel = 0.95f;
                    break;
                case CarSetupPreset.Agile:
                    steer = 1.3f; grip = 0.75f; accel = 1.05f;
                    break;
            }

            float enginePower = 1f + 0.10f * car.EngineTier;
            float tirePower = 1f + 0.08f * car.TireTier;
            float condition = ConditionFactor(car);

            return new HandlingProfile(
                accel * enginePower * condition,
                top * enginePower * condition,
                steer * tirePower,
                grip * tirePower);
        }

        /// <summary>Applies wear for a stretch of driving. Rates are per km and kept
        /// gentle: a full test session dirties the car; only long neglect dents pace.</summary>
        public static void ApplyWear(OwnedCarState car, float kilometers)
        {
            if (car == null || kilometers <= 0f) return;
            car.MileageKm += kilometers;
            car.TireCondition = Clamp01(car.TireCondition - 0.010f * kilometers);
            car.EngineCondition = Clamp01(car.EngineCondition - 0.004f * kilometers);
            car.Cleanliness = Clamp01(car.Cleanliness - 0.020f * kilometers);
            // Body condition only changes through collisions/repair, not mileage.
        }

        public static void RestoreCondition(OwnedCarState car)
        {
            if (car == null) return;
            car.EngineCondition = 1f;
            car.TireCondition = 1f;
            car.BodyCondition = 1f;
            car.Cleanliness = 1f;
        }

        public static int UpgradeCost(int currentTier) => 2000 * (currentTier + 1);

        public static bool TryRecordWorkshopLap(OwnedCarState car, CarSetupPreset setup, float seconds)
        {
            if (car == null || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            EnsureLapStorage(car);
            int index = (int)setup;
            float previous = car.WorkshopBestLapSeconds[index];
            if (previous > 0f && seconds >= previous) return false;
            car.WorkshopBestLapSeconds[index] = seconds;
            return true;
        }

        public static float WorkshopBestLap(OwnedCarState car, CarSetupPreset setup)
        {
            if (car == null) return 0f;
            EnsureLapStorage(car);
            return car.WorkshopBestLapSeconds[(int)setup];
        }

        private static void EnsureLapStorage(OwnedCarState car)
        {
            if (car.WorkshopBestLapSeconds != null && car.WorkshopBestLapSeconds.Length == 3) return;
            var previous = car.WorkshopBestLapSeconds;
            car.WorkshopBestLapSeconds = new float[3];
            if (previous != null) Array.Copy(previous, car.WorkshopBestLapSeconds, Math.Min(previous.Length, 3));
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}

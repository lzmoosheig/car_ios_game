using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>
    /// Serializer-agnostic save model (Doc 06 §4.9). Plain POCO so it round-trips
    /// through any serializer (System.Text.Json in dev tests, Newtonsoft in Unity).
    /// World state is reconstructed from definitions + these deltas, never from
    /// serialized scene objects.
    /// </summary>
    public sealed class SaveData
    {
        public int Version { get; set; } = 1;
        public string UtcLastSeen { get; set; }
        public long Wallet { get; set; }
        public int GoldenWrenches { get; set; }
        public int Reputation { get; set; }
        public string CurrentLevelId { get; set; }
        public Dictionary<string, LevelSave> Levels { get; set; } = new();
        public Dictionary<string, int> PermanentUpgrades { get; set; } = new();
        public SettingsSave Settings { get; set; } = new();
        public TutorialSave Tutorial { get; set; } = new();
    }

    public sealed class LevelSave
    {
        public Dictionary<string, ZoneSave> Zones { get; set; } = new();
        public Dictionary<string, int> Upgrades { get; set; } = new();
        public List<EmployeeSave> Employees { get; set; } = new();
        public bool Completed { get; set; }
        public long CumulativeEarnings { get; set; }
        public CarDeliverySave CarDelivery { get; set; } = new();
        public OwnedCarSave OwnedCar { get; set; } = new();
        public InventorySave Warehouse { get; set; }
    }

    /// <summary>Persisted personal-car state (Doc 09 §7.2, Phase B slice): ownership,
    /// condition, mileage, setup preset, part tiers, and inspection result.</summary>
    public sealed class OwnedCarSave
    {
        public bool Owned { get; set; }
        public string ModelId { get; set; } = "hatchback-sports";
        public float EngineCondition { get; set; } = 1f;
        public float TireCondition { get; set; } = 1f;
        public float BodyCondition { get; set; } = 1f;
        public float Cleanliness { get; set; } = 1f;
        public float MileageKm { get; set; }
        public int Setup { get; set; }
        public int EngineTier { get; set; }
        public int TireTier { get; set; }
        public bool Certified { get; set; }
        public int CertifiedClass { get; set; }
        public float[] WorkshopBestLapSeconds { get; set; } = new float[3];
    }

    /// <summary>Persisted Car Delivery state: owned item stock plus each slot's
    /// unlocked/running/elapsed state (Doc-less placeholder - see CarDeliveryModel.cs).</summary>
    public sealed class CarDeliverySave
    {
        public Dictionary<string, int> OwnedItems { get; set; } = new();
        public bool[] SlotUnlocked { get; set; } = new bool[6];
        public float[] SlotElapsed { get; set; } = new float[6];
        public bool[] SlotRunning { get; set; } = new bool[6];
    }

    public sealed class ZoneSave
    {
        public int Funded { get; set; }  // cash paid so far (partial funding persists)
        public bool Built { get; set; }
    }

    public sealed class EmployeeSave
    {
        public string RoleId { get; set; }
        public string Zone { get; set; }
        public Dictionary<string, int> Tiers { get; set; } = new();
    }

    public sealed class SettingsSave
    {
        public bool Haptics { get; set; } = true;
        public bool Sfx { get; set; } = true;
        public bool Music { get; set; } = true;
    }

    public sealed class TutorialSave
    {
        public int CompletedBeats { get; set; }
    }
}

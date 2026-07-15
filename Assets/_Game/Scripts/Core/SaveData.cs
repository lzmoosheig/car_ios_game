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

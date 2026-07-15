using System.Text.Json;
using Overhaul.Core;

namespace Overhaul.CoreTests
{
    /// <summary>
    /// Save round-trip using System.Text.Json (ships with the runtime; no NuGet).
    /// Proves the POCO model survives serialize -> deserialize with nested dictionaries,
    /// which is the invariant the Unity save system relies on (Doc 06 §5).
    /// </summary>
    public static class SaveDataTests
    {
        public static void Run()
        {
            var save = new SaveData
            {
                Version = 1,
                UtcLastSeen = "2026-07-13T18:04:11Z",
                Wallet = 1240,
                GoldenWrenches = 3,
                CurrentLevelId = "rustys_roadside",
            };

            var level = new LevelSave { Completed = false, CumulativeEarnings = 3180 };
            level.Zones["oil_bay"] = new ZoneSave { Funded = 120, Built = true };
            level.Upgrades["station.oil_bay.speed"] = 2;
            level.Employees.Add(new EmployeeSave
            {
                RoleId = "transporter",
                Zone = "parts",
                Tiers = { ["moveSpeed"] = 1 }
            });
            save.Levels["rustys_roadside"] = level;
            save.PermanentUpgrades["player.carryCapacity"] = 2;
            save.Tutorial.CompletedBeats = 10;

            var opts = new JsonSerializerOptions { WriteIndented = false };
            string json = JsonSerializer.Serialize(save, opts);
            var back = JsonSerializer.Deserialize<SaveData>(json, opts);

            T.Eq(back.Wallet, 1240, "wallet round-trips");
            T.Eq(back.GoldenWrenches, 3, "golden wrenches round-trip");
            T.Eq(back.CurrentLevelId, "rustys_roadside", "current level round-trips");
            T.Eq(back.Levels["rustys_roadside"].Zones["oil_bay"].Funded, 120, "partial zone funding round-trips");
            T.True(back.Levels["rustys_roadside"].Zones["oil_bay"].Built, "zone built flag round-trips");
            T.Eq(back.Levels["rustys_roadside"].Upgrades["station.oil_bay.speed"], 2, "upgrade tier round-trips");
            T.Eq(back.Levels["rustys_roadside"].Employees[0].RoleId, "transporter", "employee role round-trips");
            T.Eq(back.Levels["rustys_roadside"].Employees[0].Tiers["moveSpeed"], 1, "employee tier round-trips");
            T.Eq(back.PermanentUpgrades["player.carryCapacity"], 2, "permanent upgrade round-trips");
            T.Eq(back.Tutorial.CompletedBeats, 10, "tutorial progress round-trips");
        }
    }
}

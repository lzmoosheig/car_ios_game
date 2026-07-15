using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Local JSON save + offline earnings (Doc 06 §3, Doc 09 §14 Phase A). World state is
    /// rebuilt from definitions plus these deltas — never from serialized scene objects.
    /// Saves on pause/quit so an app interruption cannot lose progress.
    ///
    /// Offline income uses only automated throughput and is capped
    /// (<see cref="EconomyFormulas.OfflineEarnings"/>, Doc 09 §4.6).
    /// </summary>
    public sealed class SaveManager : MonoBehaviour
    {
        public const int CurrentVersion = 1;

        [SerializeField] private EconomyManager economy;
        [SerializeField] private string fileName = "overhaul_save.json";
        [SerializeField] private string levelId = "L01_CityGarage";
        [SerializeField] private float autosaveSeconds = 15f;

        [Header("Offline earnings")]
        [Tooltip("Cash per second produced by fully automated chains. 0 until staff are hired.")]
        [SerializeField] private double automatedRatePerSecond;
        [SerializeField] private double offlineCapHours = 2.0;

        private ConstructionZoneView[] _zones;
        private float _autosaveTimer;

        /// <summary>
        /// Writing is disabled until we know the on-disk file is either absent (genuinely
        /// a new game) or has been loaded successfully. Without this, a failed load would
        /// let autosave overwrite good progress with default state 15 seconds later.
        /// </summary>
        private bool _canSave;

        /// <summary>Cash granted for time away on the most recent load (0 if none).</summary>
        public double LastOfflineGrant { get; private set; }

        /// <summary>False when a save file exists but could not be read; saving is then blocked.</summary>
        public bool CanSave => _canSave;

        public string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        private void Awake()
        {
            RefreshZones();
            Load();
        }

        /// <summary>
        /// Re-resolves zones. Called before every load and save rather than cached once in
        /// Awake: FindObjectsByType during scene load is order-sensitive, and silently
        /// finding nothing there meant zones never restored while the wallet did.
        /// </summary>
        private void RefreshZones()
            => _zones = FindObjectsByType<ConstructionZoneView>(FindObjectsInactive.Include);

        private void Update()
        {
            _autosaveTimer += Time.deltaTime;
            if (_autosaveTimer < autosaveSeconds) return;
            _autosaveTimer = 0f;
            Save();
        }

        // Mobile: onApplicationPause is the reliable "app is going away" signal on iOS.
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();

        public void Save()
        {
            // Refuse to overwrite a file we failed to read - the player's progress is
            // worth more than this session's state.
            if (!_canSave)
            {
                Debug.LogWarning("[Overhaul] save skipped: existing save was not loaded successfully.");
                return;
            }

            try
            {
                RefreshZones();
                var data = BuildSaveData();
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Overhaul] save failed: {e.Message}");
            }
        }

        public void Load()
        {
            _canSave = false;
            try
            {
                if (!File.Exists(SavePath))
                {
                    _canSave = true; // genuinely a new game: safe to write
                    return;
                }

                var json = File.ReadAllText(SavePath);
                var data = JsonConvert.DeserializeObject<SaveData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[Overhaul] save file unreadable; saving disabled to protect it.");
                    return;
                }
                if (data.Version != CurrentVersion)
                {
                    Debug.LogWarning($"[Overhaul] save version {data.Version} != {CurrentVersion}; saving disabled until migrated.");
                    return; // future: migrate
                }

                RefreshZones();
                ApplySaveData(data);
                _canSave = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Overhaul] load failed; saving disabled to protect the file: {e.Message}");
            }
        }

        private SaveData BuildSaveData()
        {
            var data = new SaveData
            {
                Version = CurrentVersion,
                UtcLastSeen = DateTime.UtcNow.ToString("o"),
                Wallet = economy != null ? economy.Wallet : 0,
                GoldenWrenches = economy != null ? economy.Gold : 0,
                CurrentLevelId = levelId
            };

            var level = new LevelSave();
            if (_zones != null)
            {
                foreach (var z in _zones)
                {
                    if (z == null) continue;
                    level.Zones[z.ZoneId] = new ZoneSave { Funded = z.Funded, Built = z.Built };
                }
            }
            data.Levels[levelId] = level;
            return data;
        }

        private void ApplySaveData(SaveData data)
        {
            if (economy != null)
            {
                economy.SetWallet(data.Wallet);
                economy.SetGold(data.GoldenWrenches);
            }

            if (data.Levels != null && data.Levels.TryGetValue(levelId, out var level) && _zones != null)
            {
                foreach (var z in _zones)
                {
                    if (z == null) continue;
                    if (level.Zones != null && level.Zones.TryGetValue(z.ZoneId, out var zs))
                        z.LoadState(zs.Funded, zs.Built);
                }
            }

            GrantOfflineEarnings(data.UtcLastSeen);
        }

        private void GrantOfflineEarnings(string utcLastSeen)
        {
            LastOfflineGrant = 0;
            if (string.IsNullOrEmpty(utcLastSeen) || automatedRatePerSecond <= 0) return;
            if (!DateTime.TryParse(utcLastSeen, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var last)) return;

            double elapsed = (DateTime.UtcNow - last).TotalSeconds;
            // A rolled-back device clock yields a negative span; the formula clamps it to 0.
            double grant = EconomyFormulas.OfflineEarnings(automatedRatePerSecond, elapsed, offlineCapHours);
            if (grant <= 0) return;

            LastOfflineGrant = grant;
            economy?.Add((int)Math.Round(grant));
        }

        /// <summary>Set as chains become automated; drives offline income (Doc 09 §4.6).</summary>
        public void SetAutomatedRate(double perSecond) => automatedRatePerSecond = Math.Max(0, perSecond);
    }
}

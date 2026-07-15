using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    /// <summary>
    /// The management HUD. Doc 09 §12.1 keeps this deliberately sparse — cash, the current
    /// objective, progress — with "everything else contextual or in-world", so the old
    /// always-on debug dump (bay state, rack counts, arrival timer) now lives behind a
    /// toggle instead of cluttering the screen.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private EconomyManager economy;
        [SerializeField] private VillageController village;
        [SerializeField] private ServiceBay bay;
        [SerializeField] private ResourceRack rack;

        [Header("Widgets")]
        [SerializeField] private CurrencyBar currencyBar;
        [SerializeField] private ServeCustomerHUDButton serveCustomerButton;
        [SerializeField] private Text cashText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text debugText;
        [SerializeField] private GameObject debugPanel;

        [Header("Debug overlay")]
        [SerializeField] private Key debugToggleKey = Key.F3;
        [SerializeField] private string resourceId = "tire";

        private ConstructionZoneView[] _zones;
        private float _refreshTimer;

        public void Configure(EconomyManager eco, VillageController v, ServiceBay b, ResourceRack r,
                              Text cash, Text gold, Text objective, Text debug, GameObject debugRoot)
        {
            economy = eco; village = v; bay = b; rack = r;
            cashText = cash; goldText = gold; objectiveText = objective;
            debugText = debug; debugPanel = debugRoot;
        }

        public void Configure(EconomyManager eco, VillageController v, ServiceBay b, ResourceRack r,
                              CurrencyBar currencies, ServeCustomerHUDButton serveButton,
                              Text debug, GameObject debugRoot)
        {
            economy = eco; village = v; bay = b; rack = r;
            currencyBar = currencies; serveCustomerButton = serveButton;
            cashText = null; goldText = null; objectiveText = null;
            debugText = debug; debugPanel = debugRoot;
        }

        private void Awake() => _zones = FindObjectsByType<ConstructionZoneView>(FindObjectsInactive.Include);

        private void OnEnable()
        {
            if (economy != null)
            {
                economy.WalletChanged += OnWalletChanged;
                economy.GoldChanged += OnGoldChanged;
                OnWalletChanged(economy.Wallet);
                OnGoldChanged(economy.Gold);
            }
            if (debugPanel != null) debugPanel.SetActive(false);
        }

        private void OnDisable()
        {
            if (economy == null) return;
            economy.WalletChanged -= OnWalletChanged;
            economy.GoldChanged -= OnGoldChanged;
        }

        private void OnWalletChanged(long v)
        {
            if (currencyBar != null) currencyBar.SetCash(v);
            if (cashText != null) cashText.text = Format(v);
        }

        private void OnGoldChanged(int v)
        {
            if (currencyBar != null) currencyBar.SetGold(v);
            if (goldText != null) goldText.text = Format(v);
        }

        /// <summary>Compact money formatting so big balances never blow out the pill (Doc 04 §6).</summary>
        public static string Format(long v)
        {
            if (v >= 1_000_000_000) return (v / 1_000_000_000f).ToString("0.#") + "B";
            if (v >= 1_000_000) return (v / 1_000_000f).ToString("0.#") + "M";
            if (v >= 10_000) return (v / 1000f).ToString("0.#") + "K";
            return v.ToString("N0");
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[debugToggleKey].wasPressedThisFrame && debugPanel != null)
                debugPanel.SetActive(!debugPanel.activeSelf);

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < 0.25f) return;   // text churn every frame is wasted work
            _refreshTimer = 0f;

            UpdateObjectiveHud();
            if (debugPanel != null && debugPanel.activeSelf && debugText != null)
                debugText.text = DebugLine();
        }

        private void UpdateObjectiveHud()
        {
            string objective = CurrentObjective();
            if (objectiveText != null) objectiveText.text = objective;

            if (serveCustomerButton == null) return;

            var target = CheapestUnbuiltZone();
            bool hasQueue = village != null && village.QueueOccupancy > 0;
            bool actionable = hasQueue || target != null;
            string detail = target != null
                ? $"{Pretty(target.ZoneId)} · ${target.Remaining}"
                : hasQueue
                    ? $"{village.QueueOccupancy}/{village.QueueSlotCount} waiting"
                    : "No customer waiting";

            serveCustomerButton.SetStatus("Serve customer", detail, actionable);
        }

        /// <summary>
        /// One line telling the player what to do next, driven by actual world state rather
        /// than a scripted list: fix the starving bay, otherwise fund the cheapest thing
        /// within reach.
        /// </summary>
        private string CurrentObjective()
        {
            if (bay != null && bay.State == Overhaul.Core.WorkstationState.Starved)
                return "Bay is out of tires — carry some over";

            var target = CheapestUnbuiltZone();
            if (target != null)
            {
                long cash = economy != null ? economy.Wallet : 0;
                return cash >= target.Remaining
                    ? $"Stand in the {Pretty(target.ZoneId)} blueprint to build it"
                    : $"Serve customers — {Pretty(target.ZoneId)} needs ${target.Remaining}";
            }
            return "Serve customers";
        }

        private ConstructionZoneView CheapestUnbuiltZone()
        {
            if (_zones == null) return null;
            ConstructionZoneView best = null;
            foreach (var z in _zones)
            {
                if (z == null || z.Built) continue;
                if (best == null || z.Remaining < best.Remaining) best = z;
            }
            return best;
        }

        private static string Pretty(string zoneId) => zoneId
            .Replace("zone_", "").Replace("hire_", "hire ").Replace('_', ' ');

        private string DebugLine()
        {
            string bayState = bay != null ? bay.State.ToString() : "-";
            int tires = rack != null ? rack.CountOf(resourceId) : 0;
            string queue = village != null ? $"{village.QueueOccupancy}/{village.QueueSlotCount}" : "-";
            string arrival = village != null ? village.CurrentArrivalInterval.ToString("0.0") : "-";
            int served = village != null ? village.ServedTotal : 0;
            return $"Bay {bayState}   tires {tires}   queue {queue}   arrival {arrival}s   served {served}\n" +
                   "WASD move/drive · E enter car · V first/third person · F3 debug";
        }
    }
}

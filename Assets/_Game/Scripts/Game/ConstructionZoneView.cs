using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// A physical construction zone: stand inside and cash drains in with an accelerating
    /// tick until the structure pops in (Doc 02 §5.3). Partial funding persists. All funding
    /// maths lives in the verified <see cref="ConstructionZoneState"/>; this only handles
    /// proximity, the drain ramp, and revealing the built object.
    /// </summary>
    public sealed class ConstructionZoneView : MonoBehaviour
    {
        [Header("Identity / cost")]
        [SerializeField] private string zoneId = "zone";
        [SerializeField] private int totalCost = 120;

        [Header("Drain ramp (Doc 02 §1.2: $5/s ramping to $50/s over 3s)")]
        [SerializeField] private float drainStart = 5f;
        [SerializeField] private float drainMax = 50f;
        [SerializeField] private float rampSeconds = 3f;

        [Header("Wiring")]
        [SerializeField] private EconomyManager economy;
        [SerializeField] private GameObject blueprintVisual; // shown while unbuilt
        [SerializeField] private GameObject builtVisual;     // revealed on completion

        private ConstructionZoneState _state;
        private bool _playerInside;
        private float _insideSeconds;
        private float _accum;

        public string ZoneId => zoneId;
        public bool Built => State.Built;
        public int Funded => State.Funded;
        public int Remaining => State.Remaining;
        public float Progress01 => State.Progress01;

        /// <summary>Raised once when the zone completes, so the village can unlock content.</summary>
        public event System.Action<string> ZoneBuilt;

        private ConstructionZoneState State => _state ??= new ConstructionZoneState(zoneId, totalCost);

        public void Configure(string id, int cost, EconomyManager eco)
        {
            zoneId = id;
            totalCost = cost;
            economy = eco;
            _state = new ConstructionZoneState(id, cost);
            Refresh();
        }

        /// <summary>Restores funding progress from save.</summary>
        public void LoadState(int funded, bool built)
        {
            _state = ConstructionZoneState.FromSave(zoneId, totalCost, funded, built);
            Refresh();
            if (_state.Built) ZoneBuilt?.Invoke(zoneId);
        }

        private void Start() => Refresh();

        private void OnTriggerEnter(Collider other)
        {
            // Player-only (Doc 02 §4.4): employees also carry a CarrierView, and a worker
            // idling on a pad once quietly drained the wallet into a zone.
            if (other.GetComponentInParent<PlayerController>() == null) return;
            _playerInside = true;
            _insideSeconds = 0f;
            _accum = 0f;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            _playerInside = false;
        }

        private void Update()
        {
            if (!_playerInside || State.Built || economy == null) return;
            Tick(Time.deltaTime);
        }

        /// <summary>One funding tick. Public so tests can drive it deterministically.</summary>
        public void Tick(float dt)
        {
            if (State.Built) return;

            _insideSeconds += dt;
            float t = rampSeconds <= 0f ? 1f : Mathf.Clamp01(_insideSeconds / rampSeconds);
            float rate = Mathf.Lerp(drainStart, drainMax, t);

            _accum += rate * dt;
            int whole = Mathf.FloorToInt(_accum);
            if (whole <= 0) return;
            _accum -= whole;

            // Never take more than remains, and only take what the wallet actually has.
            int want = Mathf.Min(whole, State.Remaining);
            if (want <= 0) return;
            if (economy == null || !economy.TrySpend(want)) return;

            State.Fund(want);
            if (State.Built)
            {
                Refresh();
                ZoneBuilt?.Invoke(zoneId);
            }
        }

        private void Refresh()
        {
            if (blueprintVisual != null) blueprintVisual.SetActive(!State.Built);
            if (builtVisual != null) builtVisual.SetActive(State.Built);
        }
    }
}

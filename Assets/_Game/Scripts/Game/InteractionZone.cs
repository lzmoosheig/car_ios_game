using UnityEngine;

namespace Overhaul.Game
{
    public enum InteractionKind { Collect, Deposit }

    /// <summary>
    /// A proximity trigger that moves resources to/from whatever carrier stands in it,
    /// on a fixed tick — no button press (Doc 02 §1.2). Collect pulls from a
    /// <see cref="PartsSource"/>; Deposit pushes into a <see cref="ResourceRack"/>.
    /// The visible ground ring is set up by the scene builder.
    /// </summary>
    public sealed class InteractionZone : MonoBehaviour
    {
        [SerializeField] private InteractionKind kind = InteractionKind.Collect;
        [SerializeField] private string resourceId = "tire";
        [SerializeField] private float tickInterval = 0.25f; // ~4 items/sec (Doc 02 §1.2)

        [Header("Wiring (one of these, by kind)")]
        [SerializeField] private PartsSource source;   // Collect
        [SerializeField] private ResourceRack rack;    // Deposit

        [Header("Collect visuals")]
        [SerializeField] private GameObject carriedItemPrefab;

        private CarrierView _carrier;
        private float _timer;
        private float _lastRejectTime = -999f;
        private const float RejectCooldown = 2f;

        public void Configure(InteractionKind k, string res, PartsSource src, ResourceRack rk, GameObject itemPrefab)
        {
            kind = k;
            resourceId = res;
            source = src;
            rack = rk;
            carriedItemPrefab = itemPrefab;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var c = other.GetComponentInParent<CarrierView>();
            if (c != null) { _carrier = c; _timer = 0f; }
        }

        private void OnTriggerExit(Collider other)
        {
            var c = other.GetComponentInParent<CarrierView>();
            if (c != null && c == _carrier) _carrier = null;
        }

        private void Update()
        {
            if (_carrier == null) return;
            _timer += Time.deltaTime;
            if (_timer < tickInterval) return;
            _timer = 0f;
            Step(_carrier);
        }

        /// <summary>One transfer tick. Public so tests can drive it deterministically.</summary>
        public void Step(CarrierView carrier)
        {
            if (kind == InteractionKind.Collect)
            {
                if (source == null || !source.HasSupply(resourceId)) return;
                if (carrier.TryCollect(resourceId, carriedItemPrefab)) source.Take(resourceId);
            }
            else // Deposit
            {
                if (rack == null) return;
                if (carrier.Stack.CountOf(resourceId) <= 0)
                {
                    // Wrong-item feedback (Doc 02 §1.3): carrying something, but nothing
                    // this station takes. Soft toast, throttled so standing in the ring
                    // doesn't spam.
                    if (carrier.Stack.Count > 0 && Time.time - _lastRejectTime > RejectCooldown)
                    {
                        _lastRejectTime = Time.time;
                        ScreenToast.Show($"Need {ResourceCatalog.DisplayName(resourceId)} first.");
                    }
                    return;
                }
                if (!rack.CanAdd(resourceId)) return;
                if (carrier.Deposit(resourceId, 1) > 0) rack.Add(resourceId);
            }
        }
    }
}

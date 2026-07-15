using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// A work bay. All state logic lives in the verified <see cref="WorkstationStateMachine"/>;
    /// this component only supplies world facts each tick (vehicle present, rack contents,
    /// exit clear) and reacts to its callbacks by consuming parts and paying the wallet.
    /// Exposes an explicit <see cref="Tick"/> so it can be driven deterministically in tests
    /// as well as from Update in play. Doc 02 §3 / Doc 06 §3.
    /// </summary>
    public sealed class ServiceBay : MonoBehaviour
    {
        [Header("Recipe (placeholder for ServiceRecipe SO, Doc 06 §4.2)")]
        [SerializeField] private string inputResourceId = "tire";
        [SerializeField] private int inputCount = 4;
        [SerializeField] private float workSeconds = 6f;
        [SerializeField] private int basePrice = 20;

        [Header("Wiring")]
        [SerializeField] private ResourceRack rack;
        [SerializeField] private EconomyManager economy;

        private WorkstationStateMachine _fsm;

        /// <summary>Set true when a customer vehicle occupies the bay.</summary>
        public bool VehiclePresent;

        /// <summary>
        /// Patience tip multiplier for the car currently in the bay (Doc 09 §4.4). The village
        /// sets this from the customer's ticket before service completes; 1.0 = normal service.
        /// </summary>
        public float PatienceFactor { get; set; } = 1f;

        public WorkstationState State => Fsm.State;
        public float Progress => Fsm.Progress;
        public int ServicedCount { get; private set; }
        public int LastRevenue { get; private set; }
        public float PriceUpgradeMultiplier { get; private set; } = 1f;

        private WorkstationStateMachine Fsm => _fsm ??= new WorkstationStateMachine(workSeconds);

        public void Configure(ResourceRack r, EconomyManager e)
        {
            rack = r;
            economy = e;
        }

        public void ConfigureRecipe(string resourceId, int count, float seconds, int price)
        {
            inputResourceId = resourceId;
            inputCount = count;
            workSeconds = seconds;
            basePrice = price;
            _fsm = new WorkstationStateMachine(seconds);
        }

        /// <summary>Applied by the Office pricing upgrade. Ranked driving never reads this value.</summary>
        public void SetPriceUpgradeMultiplier(float multiplier)
            => PriceUpgradeMultiplier = Mathf.Max(1f, multiplier);

        /// <summary>
        /// When true, completing a service does NOT auto-pay the wallet: the revenue is
        /// left on <see cref="LastRevenue"/> for the job system to turn into a physical
        /// reward pickup (loop step 10). Default false keeps the pre-job behaviour and the
        /// existing tests intact.
        /// </summary>
        public bool PayoutViaPickup { get; set; }

        public void Tick(float dt)
        {
            bool parts = rack != null && rack.CountOf(inputResourceId) >= inputCount;
            Fsm.Tick(dt, VehiclePresent, parts, outputClear: true,
                onConsumeParts: () => rack.Remove(inputResourceId, inputCount),
                onProduce: () =>
                {
                    LastRevenue = EconomyFormulas.ServiceRevenue(
                        basePrice, locationMult: 1.0, priceUpgradeMult: PriceUpgradeMultiplier,
                        tipBase: EconomyFormulas.DefaultTipBase,
                        patienceFactor: PatienceFactor, qualityFactor: 1.0);
                    if (!PayoutViaPickup) economy?.Add(LastRevenue);
                    ServicedCount++;
                    VehiclePresent = false; // the finished car leaves the bay
                });
        }

        private void Update() => Tick(Time.deltaTime);
    }
}

using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// The management pipeline for Doc 09 §14 Phase A: arrivals -> Reception ticket ->
    /// slot-based Queue -> Basic Bay -> exit, with many customers in flight at once.
    /// Replaces the single-car GarageController.
    ///
    /// All decision logic lives in verified Core types (<see cref="Reception"/>,
    /// <see cref="QueueManager"/>, <see cref="EconomyFormulas"/>); this component only moves
    /// GameObjects and reads the world. Arrivals pause when the queue is full rather than
    /// dropping customers, so the loop cannot deadlock (Doc 02 §3.2).
    /// </summary>
    public sealed class VillageController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private ServiceBay bay;
        [SerializeField] private ResourceRack rack;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private Transform entrance;
        [SerializeField] private Transform[] queueSlots;
        [SerializeField] private Transform baySlot;
        [SerializeField] private Transform exit;

        [Header("Customers")]
        [SerializeField] private GameObject[] carPrefabs;
        [SerializeField] private Material carMaterial;
        [SerializeField] private float carTargetLength = 2.55f;

        [Header("Economy / parts")]
        [SerializeField] private string resourceId = "tire";
        [SerializeField] private int prefillParts = 8;
        [SerializeField] private int zonesBuilt;

        [Tooltip("Slots open at the start; construction can raise this up to queueSlots.Length.")]
        [SerializeField] private int initialSlots = 3;

        [Header("Job slice (Doc 09 §2.2 first loop)")]
        [Tooltip("Disabled template person instantiated per waiting customer.")]
        [SerializeField] private GameObject customerNpcTemplate;
        [Tooltip("Disabled template for the cash+rep pickup (visual child spins).")]
        [SerializeField] private GameObject rewardTemplate;
        [SerializeField] private Transform receptionPoint;
        [SerializeField] private Transform rewardPoint;

        [Header("Broken-part demand (which part each arriving car needs)")]
        [Tooltip("The pool of repairs cars arrive with. Each car is assigned one at random; " +
                 "the bay then needs that exact part. All ids must be Part-category so the " +
                 "Parts Delivery worker actually stocks them.")]
        [SerializeField] private List<PartDemand> partPool = new()
        {
            new PartDemand { resourceId = "tire",   count = 4 },
            new PartDemand { resourceId = "brakes", count = 2 },
            new PartDemand { resourceId = "battery",count = 2 },
            new PartDemand { resourceId = "panels", count = 3 },
        };
        [Tooltip("Seconds the mechanic takes once the right parts are in the bay.")]
        [SerializeField] private float bayWorkSeconds = 6f;
        [Tooltip("Base service price before tips/patience. Scales up with how many parts the job needs.")]
        [SerializeField] private int bayBasePrice = 20;
        [Tooltip("Height of the floating 'needs part' tag above the car in the bay.")]
        [SerializeField] private float carLabelHeight = 2.4f;

        [System.Serializable]
        public struct PartDemand
        {
            public string resourceId;
            public int count;
        }

        private readonly Dictionary<string, CustomerVehicle> _vehicles = new();
        private readonly Dictionary<string, ServiceTicket> _tickets = new();
        private readonly Dictionary<string, int> _lastSlot = new();
        private readonly Dictionary<string, ServiceJob> _jobs = new();
        private readonly Dictionary<string, CustomerNpc> _npcs = new();

        private QueueManager _queue;
        private Reception _reception;
        private float _spawnTimer;
        private int _nextId;
        private int _lastServiced;
        private string _inBayId;

        // Delivery -> bay bridge, all built at runtime in Start (Doc 06 §3 carrier flow):
        private InventoryComponent _bayInput;   // the bay's input tray the player fills
        private PartDropoffZone _dropoff;        // the ring the player walks into to hand parts over
        private BuildingView _bayBuilding;       // the clickable bay lot, for its world cue
        private CarNeedLabel _carLabel;          // floating "needs part" tag on the in-bay car
        private ServiceJob _bayJob;              // the job of the car currently at the bay

        public int ServedTotal { get; private set; }
        public int QueueOccupancy => _queue?.Occupancy ?? 0;
        public int QueueSlotCount => _queue?.SlotCount ?? 0;
        public float CurrentArrivalInterval => (float)EconomyFormulas.ArrivalInterval(zonesBuilt);

        /// <summary>The part the car in the bay needs right now (null if the bay is empty).</summary>
        public string BayNeedResourceId => _bayJob != null ? _bayJob.RequiredResourceId : null;

        /// <summary>How many of that part the job needs in total.</summary>
        public int BayNeedCount => _bayJob != null ? _bayJob.RequiredCount : 0;

        /// <summary>How many of the needed part still have to be carried to the bay.</summary>
        public int BayNeedRemaining => _bayJob != null && _bayInput != null
            ? Mathf.Max(0, _bayJob.RequiredCount - _bayInput.CountOf(_bayJob.RequiredResourceId))
            : 0;

        /// <summary>True while a car sits in the bay still short of the part it needs.</summary>
        public bool BayAwaitingParts => _bayJob != null && bay != null && bay.VehiclePresent && BayNeedRemaining > 0;

        /// <summary>
        /// The job the player should look at next. Offered jobs are surfaced in QUEUE order
        /// — the front car is the one whose acceptance unblocks dispatch, so it must be the
        /// one the HUD button accepts. (A jobs-dictionary scan here once accepted a
        /// mid-queue customer and left the front car parked forever.) If nothing is
        /// offered, the oldest ready-to-collect payout is next.
        /// </summary>
        public ServiceJob ActiveJob
        {
            get
            {
                if (_queue != null)
                {
                    foreach (var custId in _queue.Slots)   // index 0 == front
                    {
                        if (custId == null) continue;
                        if (_jobs.TryGetValue(custId, out var queued) && queued.State == JobState.Offered)
                            return queued;
                    }
                }

                ServiceJob oldestReady = null;
                foreach (var job in _jobs.Values)
                    if (job.State == JobState.ReadyToCollect) oldestReady ??= job;
                return oldestReady;
            }
        }

        /// <summary>Opens more queue slots as construction completes (Doc 09 §6.9).</summary>
        public void SetQueueSlotCount(int count)
        {
            EnsureCore();
            int max = queueSlots != null ? queueSlots.Length : count;
            _queue.SetSlotCount(Mathf.Clamp(count, 1, max));
        }

        public void Configure(ServiceBay b, ResourceRack r, EconomyManager e,
                              Transform entr, Transform[] slots, Transform baySlotT, Transform exitT,
                              GameObject[] cars, Material carMat)
        {
            bay = b; rack = r; economy = e;
            entrance = entr; queueSlots = slots; baySlot = baySlotT; exit = exitT;
            carPrefabs = cars; carMaterial = carMat;
        }

        /// <summary>Reveals a service as its station is constructed (Doc 09 §6.8).</summary>
        public void UnlockService(ServiceKind kind, int weight = 1)
        {
            EnsureCore();
            _reception.SetWeight(kind, weight);
        }

        /// <summary>Construction raises demand: arrivals speed up as the village grows.</summary>
        public void SetZonesBuilt(int count) => zonesBuilt = Mathf.Max(0, count);

        private void EnsureCore()
        {
            int max = queueSlots != null && queueSlots.Length > 0 ? queueSlots.Length : 3;
            _queue ??= new QueueManager(Mathf.Clamp(initialSlots, 1, max));
            _reception ??= new Reception(seed: 1234);
        }

        private void Start()
        {
            EnsureCore();
            _reception.SetWeight(ServiceKind.BasicRepair, 1); // the only service unlocked in Phase A

            if (rack != null) rack.SetCapacity(14);
            if (bay != null)
            {
                bay.Configure(rack, economy);
                // Parts no longer sit pre-stocked: the player carries the exact part each car
                // needs from the delivery worker into a dedicated input tray (loop steps 5-9).
                _bayInput = CreateBayInputTray();
                bay.ConfigureInputInventory(_bayInput);
                // The first car's arrival sets the real recipe; seed a harmless placeholder.
                bay.ConfigureRecipe(resourceId, 4, bayWorkSeconds, bayBasePrice);
                // Payment is a physical pickup now (loop step 10), not an auto-deposit.
                bay.PayoutViaPickup = true;
                _lastServiced = bay.ServicedCount;
            }

            SetupDeliveryBridge();
        }

        /// <summary>The bay's own inventory the player deposits into; the ServiceBay consumes from it.</summary>
        private InventoryComponent CreateBayInputTray()
        {
            var go = new GameObject("BayInputTray");
            go.transform.SetParent(transform, false);
            var inv = go.AddComponent<InventoryComponent>();
            inv.Configure(12, label: "Bay Input"); // no category filter: accepts any needed part
            return inv;
        }

        /// <summary>
        /// Wires the physical delivery -> bay channel at runtime: caches the bay's clickable
        /// building, turns the old tire-only Deposit ring into a part-aware drop-off, and
        /// retires the tire-pallet auto-collect so parts flow only through the delivery worker.
        /// </summary>
        private void SetupDeliveryBridge()
        {
            foreach (var bv in FindObjectsByType<BuildingView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (bv != null && bv.Bay == bay) { _bayBuilding = bv; break; }

            var deposit = GameObject.Find("DepositZone");
            if (deposit != null)
            {
                var legacy = deposit.GetComponent<InteractionZone>();
                if (legacy != null) legacy.enabled = false;
                _dropoff = deposit.GetComponent<PartDropoffZone>() ?? deposit.AddComponent<PartDropoffZone>();
            }
            else
            {
                var go = new GameObject("PartDropoff");
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.8f;
                if (baySlot != null) go.transform.position = baySlot.position + new Vector3(2.8f, 0f, -2.0f);
                _dropoff = go.AddComponent<PartDropoffZone>();
            }
            if (_dropoff != null) _dropoff.Configure(bay, _bayInput, this);

            var collect = GameObject.Find("CollectZone");
            if (collect != null) collect.SetActive(false); // delivery is NPC-only now
        }

        /// <summary>Wires the job-slice scene pieces (called by the editor setup).</summary>
        public void ConfigureJobSlice(GameObject npcTemplate, GameObject reward,
                                      Transform reception, Transform rewardSpawn)
        {
            customerNpcTemplate = npcTemplate;
            rewardTemplate = reward;
            receptionPoint = reception;
            rewardPoint = rewardSpawn;
        }

        public void AcceptActiveJob()
        {
            var job = ActiveJob;
            if (job != null && job.State == JobState.Offered) AcceptJob(job);
        }

        public void AcceptJob(ServiceJob job)
        {
            if (job == null || !job.Accept()) return;
            string item = ResourceCatalog.DisplayName(job.RequiredResourceId);
            ScreenToast.Show($"Job accepted — get {job.RequiredCount}x {item} to the bay!");
        }

        /// <summary>Called by the walked-through <see cref="RewardPickup"/>.</summary>
        public void CollectJob(ServiceJob job)
        {
            if (job == null || !job.Collect()) return;

            economy?.Add(job.CashReward);
            economy?.AddReputation(job.ReputationReward);
            ScreenToast.Show($"+${job.CashReward}   +{job.ReputationReward} rep");

            if (_npcs.TryGetValue(job.CustomerId, out var npc) && npc != null)
                Destroy(npc.gameObject);
            _npcs.Remove(job.CustomerId);
            _jobs.Remove(job.CustomerId);
        }

        private void Update()
        {
            EnsureCore();
            TickArrivals(Time.deltaTime);
            TickQueuePositions();
            TickDispatch();
            TickBay();
            TickExits();
            RefreshCarLabel();
        }

        private void TickArrivals(float dt)
        {
            if (_queue.IsFull) return; // demand pauses; nobody is dropped
            _spawnTimer += dt;
            if (_spawnTimer < CurrentArrivalInterval) return;
            _spawnTimer = 0f;
            SpawnCustomer();
        }

        private void SpawnCustomer()
        {
            if (carPrefabs == null || carPrefabs.Length == 0 || entrance == null) return;

            string id = "cust_" + (++_nextId);
            var ticket = _reception.CheckIn(id, Time.time);
            if (ticket == null) return; // no service unlocked yet

            int slot = _queue.TryEnqueue(id);
            if (slot < 0) return;

            var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            var go = Instantiate(prefab, entrance.position, entrance.rotation);
            NormalizeCar(go);
            ApplyMaterial(go);

            var cv = go.GetComponent<CustomerVehicle>();
            if (cv == null) cv = go.AddComponent<CustomerVehicle>();
            cv.Phase = VehiclePhase.ToQueue;

            _vehicles[id] = cv;
            _tickets[id] = ticket;
            _lastSlot[id] = -1;

            // The job layer: every arrival is an offer the player must accept (loop steps
            // 2-4). Each car arrives needing one random broken part from the pool.
            var demand = PickDemand();
            var job = new ServiceJob("job_" + _nextId, id, ticket.Kind, demand.resourceId, demand.count);
            _jobs[id] = job;
            SpawnNpc(job);
        }

        private PartDemand PickDemand()
        {
            if (partPool == null || partPool.Count == 0)
                return new PartDemand { resourceId = resourceId, count = 4 };
            var demand = partPool[Random.Range(0, partPool.Count)];
            if (string.IsNullOrEmpty(demand.resourceId)) demand.resourceId = resourceId;
            if (demand.count <= 0) demand.count = 1;
            return demand;
        }

        private void SpawnNpc(ServiceJob job)
        {
            if (customerNpcTemplate == null || receptionPoint == null) return;

            var go = Instantiate(customerNpcTemplate);
            go.SetActive(true);
            // Fan waiting customers out beside the reception desk so they never overlap.
            int index = _npcs.Count;
            go.transform.position = receptionPoint.position
                + receptionPoint.right * (index * 0.9f)
                + receptionPoint.forward * ((index % 2) * 0.5f);
            go.transform.rotation = receptionPoint.rotation;
            go.name = "Customer_" + job.CustomerId;

            var npc = go.GetComponent<CustomerNpc>();
            if (npc == null) npc = go.AddComponent<CustomerNpc>();
            npc.Configure(this, job);
            _npcs[job.CustomerId] = npc;
        }

        /// <summary>Keeps each queued car driving toward its current slot as the line advances.</summary>
        private void TickQueuePositions()
        {
            if (queueSlots == null || queueSlots.Length == 0) return;

            foreach (var kv in _vehicles)
            {
                var cv = kv.Value;
                if (cv == null) continue;
                if (cv.Phase != VehiclePhase.ToQueue && cv.Phase != VehiclePhase.InQueue) continue;

                int idx = _queue.IndexOf(kv.Key);
                if (idx < 0 || idx >= queueSlots.Length) continue;

                if (_lastSlot.TryGetValue(kv.Key, out var last) && last == idx)
                {
                    if (cv.AtTarget && cv.Phase == VehiclePhase.ToQueue) cv.Phase = VehiclePhase.InQueue;
                    continue;
                }

                _lastSlot[kv.Key] = idx;
                cv.Phase = VehiclePhase.ToQueue;
                cv.SetTarget(queueSlots[idx].position);
            }
        }

        /// <summary>Sends the front car into the bay once the bay is free.</summary>
        private void TickDispatch()
        {
            if (_inBayId != null || bay == null || baySlot == null) return;
            if (bay.VehiclePresent) return;

            var frontId = _queue.Front;
            if (frontId == null) return;
            if (!_vehicles.TryGetValue(frontId, out var cv) || cv == null) return;
            if (cv.Phase != VehiclePhase.InQueue && cv.Phase != VehiclePhase.ToQueue) return;
            // Only dispatch a car that has actually arrived at the front slot.
            if (!cv.AtTarget) return;
            // ...and whose job the player has accepted (loop step 3). Unaccepted customers
            // wait at the front; the HUD points at them.
            if (_jobs.TryGetValue(frontId, out var frontJob) && frontJob.State == JobState.Offered) return;

            _queue.TryDequeueFront(out _);
            _lastSlot.Remove(frontId);
            _inBayId = frontId;
            cv.Phase = VehiclePhase.ToBay;
            // Drive along the street to the bay's mouth, then turn in square.
            cv.SetPath(StreetPointFor(baySlot.position), baySlot.position);

            // Point the whole bay at this car's part as it heads in, so the requirement is
            // visible (tag on the car, bay cue, HUD) before the car even parks.
            if (_jobs.TryGetValue(frontId, out var dispatchJob)) ConfigureBayForJob(cv, dispatchJob);
        }

        /// <summary>
        /// Retargets the bay to the part the given car needs: sets the recipe, clears any
        /// leftover parts from the previous car, updates the bay's world cue, and raises a
        /// floating tag over the car. Called the moment a car is dispatched to the bay.
        /// </summary>
        private void ConfigureBayForJob(CustomerVehicle car, ServiceJob job)
        {
            _bayJob = job;
            int price = bayBasePrice * Mathf.Max(1, job.RequiredCount) / 2; // bigger jobs pay more
            bay?.ConfigureRecipe(job.RequiredResourceId, job.RequiredCount, bayWorkSeconds, Mathf.Max(bayBasePrice, price));
            if (_bayInput != null) ClearTray();
            if (_bayBuilding != null) _bayBuilding.SetActiveRequirement(job.RequiredResourceId, job.RequiredCount);

            if (_carLabel != null) { Destroy(_carLabel.gameObject); _carLabel = null; }
            if (car != null) _carLabel = CarNeedLabel.Attach(car.transform, carLabelHeight);
            RefreshCarLabel();
        }

        /// <summary>Empties the bay tray so a new car's part count starts from zero.</summary>
        private void ClearTray()
        {
            foreach (var e in ResourceCatalog.DefaultItems) _bayInput.Remove(e.id, 9999);
        }

        /// <summary>Keeps the floating tag counting down as the player delivers parts.</summary>
        private void RefreshCarLabel()
        {
            if (_carLabel == null || _bayJob == null) return;
            int remaining = BayNeedRemaining;
            string part = ResourceCatalog.DisplayName(_bayJob.RequiredResourceId).ToUpperInvariant();
            Color partColor = ResourceCatalog.Instance != null
                ? ResourceCatalog.Instance.ColorOf(_bayJob.RequiredResourceId)
                : Color.white;
            if (remaining > 0)
                _carLabel.Set($"NEEDS {remaining}× {part}", partColor);
            else
                _carLabel.Set("REPAIRING…", new Color(0.25f, 0.82f, 0.48f));
        }

        /// <summary>Tears down the bay's per-car requirement once the car is served.</summary>
        private void ClearBayNeed()
        {
            if (_carLabel != null) { Destroy(_carLabel.gameObject); _carLabel = null; }
            if (_bayBuilding != null)
                _bayBuilding.SetActiveRequirement(_bayJob != null ? _bayJob.RequiredResourceId : "", 0);
            _bayJob = null;
        }

        private void TickBay()
        {
            if (_inBayId == null || bay == null) return;
            if (!_vehicles.TryGetValue(_inBayId, out var cv) || cv == null) { _inBayId = null; return; }

            if (cv.Phase == VehiclePhase.ToBay && cv.AtTarget)
            {
                cv.Phase = VehiclePhase.InBay;
                // Pay the tip this customer earned by how long they actually waited.
                if (_tickets.TryGetValue(_inBayId, out var ticket))
                {
                    float wait = ticket.QueueWaitSeconds(Time.time);
                    bay.PatienceFactor = (float)EconomyFormulas.PatienceFactor(wait, wait);
                }
                if (_jobs.TryGetValue(_inBayId, out var job)) job.StartService();
                bay.VehiclePresent = true;
                return;
            }

            if (cv.Phase == VehiclePhase.InBay && bay.ServicedCount != _lastServiced)
            {
                _lastServiced = bay.ServicedCount;
                ServedTotal++;
                if (_tickets.TryGetValue(_inBayId, out var ticket)) ticket.Served = true;

                // Service done -> the payout becomes a physical pickup (loop step 10).
                if (_jobs.TryGetValue(_inBayId, out var doneJob) && doneJob.CompleteService(bay.LastRevenue))
                    SpawnReward(doneJob);

                ClearBayNeed(); // the car's part requirement is satisfied; drop the tag/cue
                cv.Phase = VehiclePhase.ToExit;
                // Back out to the street first, then leave along it - never diagonally
                // across the station lots.
                if (exit != null) cv.SetPath(StreetPointFor(cv.transform.position), exit.position);
                _inBayId = null;
            }
        }

        private void SpawnReward(ServiceJob job)
        {
            if (rewardTemplate == null)
            {
                // No template wired: degrade gracefully to the old auto-pay behaviour
                // rather than silently losing the revenue. (job is already ReadyToCollect.)
                CollectJob(job);
                return;
            }

            var spawnAt = rewardPoint != null ? rewardPoint.position
                : (baySlot != null ? baySlot.position + Vector3.right * 3f : transform.position);
            var go = Instantiate(rewardTemplate, spawnAt, Quaternion.identity);
            go.SetActive(true);
            go.name = "Reward_" + job.Id;
            var pickup = go.GetComponent<RewardPickup>();
            if (pickup == null) pickup = go.AddComponent<RewardPickup>();
            pickup.Configure(this, job);
        }


        private void TickExits()
        {
            List<string> done = null;
            foreach (var kv in _vehicles)
            {
                var cv = kv.Value;
                if (cv == null) { (done ??= new List<string>()).Add(kv.Key); continue; }
                if (cv.Phase == VehiclePhase.ToExit && cv.AtTarget) cv.Phase = VehiclePhase.Done;
                if (cv.Phase == VehiclePhase.Done)
                {
                    Destroy(cv.gameObject);
                    (done ??= new List<string>()).Add(kv.Key);
                }
            }
            if (done == null) return;
            foreach (var id in done)
            {
                _vehicles.Remove(id);
                _tickets.Remove(id);
                _lastSlot.Remove(id);
                _queue.Remove(id);
                if (_inBayId == id) _inBayId = null;
            }
        }

        /// <summary>
        /// The point on the customer street directly in front of a world position. The
        /// street line is read from the entrance/exit markers rather than hard-coded, so it
        /// follows the scene's road layout instead of assuming one.
        /// </summary>
        private Vector3 StreetPointFor(Vector3 worldPos)
        {
            if (entrance == null) return worldPos;
            return new Vector3(worldPos.x, entrance.position.y, entrance.position.z);
        }

        private void NormalizeCar(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            float longest = Mathf.Max(b.size.x, b.size.z);
            if (longest > 0.001f) go.transform.localScale *= carTargetLength / longest;
        }

        private void ApplyMaterial(GameObject go)
        {
            if (carMaterial == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = carMaterial;
                r.sharedMaterials = arr;
            }
        }

        // The on-screen readout lives in HudView now: Doc 09 §12.1 keeps the HUD to cash,
        // objective and progress, with everything else contextual or in-world.
    }
}

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

        private readonly Dictionary<string, CustomerVehicle> _vehicles = new();
        private readonly Dictionary<string, ServiceTicket> _tickets = new();
        private readonly Dictionary<string, int> _lastSlot = new();

        private QueueManager _queue;
        private Reception _reception;
        private float _spawnTimer;
        private int _nextId;
        private int _lastServiced;
        private string _inBayId;

        public int ServedTotal { get; private set; }
        public int QueueOccupancy => _queue?.Occupancy ?? 0;
        public float CurrentArrivalInterval => (float)EconomyFormulas.ArrivalInterval(zonesBuilt);

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
            int slots = queueSlots != null && queueSlots.Length > 0 ? queueSlots.Length : 3;
            _queue ??= new QueueManager(slots);
            _reception ??= new Reception(seed: 1234);
        }

        private void Start()
        {
            EnsureCore();
            _reception.SetWeight(ServiceKind.BasicRepair, 1); // the only service unlocked in Phase A

            if (rack != null)
            {
                rack.SetCapacity(14);
                for (int i = 0; i < prefillParts; i++) rack.Add(resourceId);
            }
            if (bay != null)
            {
                bay.Configure(rack, economy);
                bay.ConfigureRecipe(resourceId, 4, 6f, 20);
                _lastServiced = bay.ServicedCount;
            }
        }

        private void Update()
        {
            EnsureCore();
            TickArrivals(Time.deltaTime);
            TickQueuePositions();
            TickDispatch();
            TickBay();
            TickExits();
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

            _queue.TryDequeueFront(out _);
            _lastSlot.Remove(frontId);
            _inBayId = frontId;
            cv.Phase = VehiclePhase.ToBay;
            cv.SetTarget(baySlot.position);
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
                bay.VehiclePresent = true;
                return;
            }

            if (cv.Phase == VehiclePhase.InBay && bay.ServicedCount != _lastServiced)
            {
                _lastServiced = bay.ServicedCount;
                ServedTotal++;
                if (_tickets.TryGetValue(_inBayId, out var ticket)) ticket.Served = true;
                cv.Phase = VehiclePhase.ToExit;
                if (exit != null) cv.SetTarget(exit.position);
                _inBayId = null;
            }
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

        private void OnGUI()
        {
            long cash = economy != null ? economy.Wallet : 0;
            var state = bay != null ? bay.State.ToString() : "-";
            int parts = rack != null ? rack.CountOf(resourceId) : 0;

            GUI.Label(new Rect(12, 10, 620, 22), $"Cash: ${cash}    Served: {ServedTotal}    Queue: {QueueOccupancy}/{(queueSlots?.Length ?? 0)}");
            GUI.Label(new Rect(12, 32, 620, 22), $"Bay: {state}    Rack {resourceId}s: {parts}    Next arrival every {CurrentArrivalInterval:0.0}s");
            GUI.Label(new Rect(12, 54, 720, 22), "WASD/arrows: move or drive. Carry tires to the bay. Stand in a blueprint to fund it.");
        }
    }
}

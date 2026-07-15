using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Orchestrates the visible customer loop around the tested <see cref="ServiceBay"/>:
    /// spawns a car at the entrance, drives it to the bay, flags the bay occupied, and on
    /// service completion drives it to the exit and recycles it. Parts still come from the
    /// player carrying them into the bay's rack (Collect/Deposit zones); the rack is
    /// pre-filled so a passive viewer still sees cars served. Replaces the timer-only
    /// GarageDemo. A scaffold for the full queue/AI systems (Doc 02 §3–4).
    /// </summary>
    public sealed class GarageController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private ServiceBay bay;
        [SerializeField] private ResourceRack rack;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private Transform entrance;
        [SerializeField] private Transform baySlot;
        [SerializeField] private Transform exit;

        [Header("Customers")]
        [SerializeField] private GameObject[] carPrefabs;
        [SerializeField] private Material carMaterial;
        // Kenney car-kit vehicles are ~2.55m natively, which is already correct against
        // the 7m station lots of the city campus (Graybox keeps its own serialized 1.6
        // for the small KayKit diorama grid).
        [SerializeField] private float carTargetLength = 2.55f;
        [SerializeField] private float spawnInterval = 5f;

        [Header("Economy / parts")]
        [SerializeField] private string resourceId = "tire";
        [SerializeField] private int prefillParts = 8;

        private CustomerVehicle _active;
        private float _spawnTimer;
        private int _lastServiced;

        public void Configure(ServiceBay b, ResourceRack r, EconomyManager e,
                              Transform entr, Transform baySlotT, Transform exitT,
                              GameObject[] cars, Material carMat)
        {
            bay = b; rack = r; economy = e;
            entrance = entr; baySlot = baySlotT; exit = exitT;
            carPrefabs = cars; carMaterial = carMat;
        }

        private void Start()
        {
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
            if (_active == null)
            {
                _spawnTimer += Time.deltaTime;
                if (_spawnTimer >= spawnInterval) { _spawnTimer = 0f; SpawnCustomer(); }
                return;
            }

            switch (_active.Phase)
            {
                case VehiclePhase.ToBay:
                    if (_active.AtTarget)
                    {
                        _active.Phase = VehiclePhase.InBay;
                        if (bay != null) bay.VehiclePresent = true;
                    }
                    break;

                case VehiclePhase.InBay:
                    if (bay != null && bay.ServicedCount != _lastServiced)
                    {
                        _lastServiced = bay.ServicedCount;
                        _active.Phase = VehiclePhase.ToExit;
                        if (exit != null) _active.SetTarget(exit.position);
                    }
                    break;

                case VehiclePhase.ToExit:
                    if (_active.AtTarget) _active.Phase = VehiclePhase.Done;
                    break;

                case VehiclePhase.Done:
                    Destroy(_active.gameObject);
                    _active = null;
                    break;
            }
        }

        private void SpawnCustomer()
        {
            if (carPrefabs == null || carPrefabs.Length == 0 || entrance == null || baySlot == null) return;

            var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            var go = Instantiate(prefab, entrance.position, entrance.rotation);
            NormalizeCar(go);
            ApplyMaterial(go);

            var cv = go.GetComponent<CustomerVehicle>();
            if (cv == null) cv = go.AddComponent<CustomerVehicle>();
            cv.Phase = VehiclePhase.ToBay;
            cv.SetTarget(baySlot.position);
            _active = cv;
        }

        private void NormalizeCar(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            float longest = Mathf.Max(b.size.x, b.size.z);
            if (longest > 0.001f)
            {
                float s = carTargetLength / longest;
                go.transform.localScale *= s;
            }
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
            int serviced = bay != null ? bay.ServicedCount : 0;
            var state = bay != null ? bay.State.ToString() : "-";
            int parts = rack != null ? rack.CountOf(resourceId) : 0;

            GUI.Label(new Rect(12, 10, 520, 22), $"Cash: ${cash}    Serviced cars: {serviced}");
            GUI.Label(new Rect(12, 32, 520, 22), $"Bay: {state}    Rack {resourceId}s: {parts}");
            GUI.Label(new Rect(12, 54, 720, 22), "WASD/arrows: move or drive. Press E beside a visitor car to enter or exit.");
        }
    }
}

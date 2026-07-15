using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Drives the graybox so the core loop is visibly alive on Play without the full
    /// interaction/customer systems yet: it periodically "delivers" parts to the rack
    /// and sends a new customer to the bay, then draws a minimal OnGUI readout of cash,
    /// serviced count and bay state. A scaffold for manual inspection, not shipping UI.
    /// </summary>
    [RequireComponent(typeof(ResourceRack))]
    [RequireComponent(typeof(ServiceBay))]
    [RequireComponent(typeof(EconomyManager))]
    public sealed class GarageDemo : MonoBehaviour
    {
        [SerializeField] private string resourceId = "tire";
        [SerializeField] private int partsPerDelivery = 4;
        [SerializeField] private float deliveryInterval = 3f;
        [SerializeField] private float customerInterval = 4f;

        private ResourceRack _rack;
        private ServiceBay _bay;
        private EconomyManager _eco;
        private float _deliveryTimer;
        private float _customerTimer;

        private void Start()
        {
            _rack = GetComponent<ResourceRack>();
            _bay = GetComponent<ServiceBay>();
            _eco = GetComponent<EconomyManager>();

            _rack.SetCapacity(12);
            _bay.Configure(_rack, _eco);
            _bay.ConfigureRecipe(resourceId, 4, 6f, 20);

            for (int i = 0; i < partsPerDelivery; i++) _rack.Add(resourceId);
            _bay.VehiclePresent = true; // first customer waiting
        }

        private void Update()
        {
            _deliveryTimer += Time.deltaTime;
            if (_deliveryTimer >= deliveryInterval)
            {
                _deliveryTimer = 0f;
                for (int i = 0; i < partsPerDelivery; i++) _rack.Add(resourceId);
            }

            _customerTimer += Time.deltaTime;
            if (!_bay.VehiclePresent && _customerTimer >= customerInterval)
            {
                _customerTimer = 0f;
                _bay.VehiclePresent = true;
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(12, 10, 480, 22), $"Cash: ${_eco.Wallet}    Serviced cars: {_bay.ServicedCount}");
            GUI.Label(new Rect(12, 32, 480, 22),
                $"Bay: {_bay.State}    Progress: {_bay.Progress:P0}    Rack {resourceId}s: {_rack.CountOf(resourceId)}");
            GUI.Label(new Rect(12, 54, 480, 22), "Move the capsule with WASD / arrow keys.");
        }
    }
}

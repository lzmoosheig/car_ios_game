using UnityEngine;
using UnityEngine.AI;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// A mobile inventory: a worker that collects items from a source inventory, carries them
    /// in its <em>own</em> <see cref="InventoryComponent"/>, walks to a destination and deposits
    /// them there. It is deliberately role-agnostic - which items it will carry is decided by the
    /// rules on its own inventory (e.g. a Parts Delivery worker whose inventory only accepts the
    /// <see cref="ItemCategory.Part"/> category). All hand-offs go through
    /// <see cref="InventoryTransfer"/>, so they are partial-safe: a nearly-full destination takes
    /// what it can and the rest stays with the worker.
    ///
    /// Movement uses a <see cref="NavMeshAgent"/> when one is present; the collect/deposit logic
    /// (<see cref="CollectStep"/> / <see cref="DepositStep"/>) is exposed as plain methods so it
    /// can be unit-tested without a scene, exactly like <see cref="EmployeeAgent"/>.
    /// </summary>
    public sealed class InventoryCourier : MonoBehaviour
    {
        public enum State { Idle, ToSource, Loading, ToDestination, Unloading }

        [Header("Wiring")]
        [SerializeField] private InventoryComponent ownInventory;
        [SerializeField] private InventoryComponent source;
        [SerializeField] private InventoryComponent destination;

        [Header("Cargo (blank = carry anything this worker's inventory allows)")]
        [SerializeField] private string carriedItemId = "";

        [Header("Movement")]
        [SerializeField] private float transferInterval = 0.25f;
        [SerializeField] private float arriveDistance = 1.5f;

        private NavMeshAgent _agent;
        private State _state = State.Idle;
        private float _timer;

        public State CurrentState => _state;
        public InventoryComponent Own => ownInventory;
        public InventoryComponent Source { get => source; set => source = value; }
        public InventoryComponent Destination { get => destination; set => destination = value; }

        public void Configure(InventoryComponent own, InventoryComponent from, InventoryComponent to, string itemId = "")
        {
            ownInventory = own;
            source = from;
            destination = to;
            carriedItemId = itemId ?? "";
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (ownInventory == null) ownInventory = GetComponent<InventoryComponent>();
        }

        // ----------------------------------------------------------- atomic, testable steps

        /// <summary>Pulls as much as the worker can carry from the source. Returns units moved.</summary>
        public int CollectStep()
        {
            if (source == null || ownInventory == null) return 0;
            return string.IsNullOrEmpty(carriedItemId)
                ? InventoryTransfer.TransferAll(source.Inventory, ownInventory.Inventory)
                : InventoryTransfer.Transfer(source.Inventory, ownInventory.Inventory, carriedItemId, int.MaxValue);
        }

        /// <summary>Pushes as much as the destination will hold from the worker. Returns units moved.</summary>
        public int DepositStep()
        {
            if (destination == null || ownInventory == null) return 0;
            return string.IsNullOrEmpty(carriedItemId)
                ? InventoryTransfer.TransferAll(ownInventory.Inventory, destination.Inventory)
                : InventoryTransfer.Transfer(ownInventory.Inventory, destination.Inventory, carriedItemId, int.MaxValue);
        }

        private bool CarryingAnything => ownInventory != null && !ownInventory.IsEmpty;

        private bool SourceHasCargo
        {
            get
            {
                if (source == null) return false;
                if (!string.IsNullOrEmpty(carriedItemId)) return source.CountOf(carriedItemId) > 0;
                foreach (var s in source.Inventory.Slots)
                    if (!s.IsEmpty && ownInventory.Inventory.Allows(s.Stack.ItemId)) return true;
                return false;
            }
        }

        // ------------------------------------------------------------------- movement loop

        private void Update() => Tick(Time.deltaTime);

        /// <summary>Explicit tick so the round-trip is drivable from tests.</summary>
        public void Tick(float dt)
        {
            switch (_state)
            {
                case State.Idle:
                    if (CarryingAnything && (ownInventory.IsFull || !SourceHasCargo)) GoTo(destination, State.ToDestination);
                    else if (SourceHasCargo) GoTo(source, State.ToSource);
                    break;

                case State.ToSource:
                    if (source == null) { _state = State.Idle; break; }
                    if (Arrived(source.transform.position)) _state = State.Loading;
                    break;

                case State.Loading:
                    _timer += dt;
                    if (_timer < transferInterval) break;
                    _timer = 0f;
                    if (ownInventory.IsFull || !SourceHasCargo || CollectStep() <= 0) _state = State.Idle;
                    break;

                case State.ToDestination:
                    if (destination == null) { _state = State.Idle; break; }
                    if (Arrived(destination.transform.position)) _state = State.Unloading;
                    break;

                case State.Unloading:
                    _timer += dt;
                    if (_timer < transferInterval) break;
                    _timer = 0f;
                    if (!CarryingAnything || DepositStep() <= 0) _state = State.Idle;
                    break;
            }
        }

        private void GoTo(InventoryComponent target, State next)
        {
            if (target == null) return;
            _state = next;
            if (_agent != null && _agent.isOnNavMesh) _agent.SetDestination(target.transform.position);
        }

        private bool Arrived(Vector3 worldPos)
        {
            // No agent -> the courier is stationary/adjacent, so treat it as always in range.
            if (_agent == null || !_agent.isOnNavMesh) return true;
            var a = new Vector2(transform.position.x, transform.position.z);
            var b = new Vector2(worldPos.x, worldPos.z);
            return Vector2.Distance(a, b) <= arriveDistance;
        }
    }
}

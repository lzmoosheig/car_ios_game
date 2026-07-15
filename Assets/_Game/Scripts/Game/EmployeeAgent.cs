using UnityEngine;
using UnityEngine.AI;
using Overhaul.Core;

namespace Overhaul.Game
{
    public enum EmployeeRole { Transporter, Mechanic, Cashier }

    /// <summary>
    /// A hired worker. Phase A ships the Parts Transporter (Doc 02 §4.1): it hauls parts
    /// from a source into the station racks the player used to feed by hand, which is what
    /// turns the village into an automated chain and unlocks offline earnings.
    ///
    /// Two design rules from Doc 02 §4 are load-bearing here:
    /// - Employees reuse the player's <see cref="CarrierView"/>, so collecting, the visible
    ///   carried stack and capacity all behave identically to the player (§4.5).
    /// - Targets are chosen dynamically by the verified utility scorer
    ///   (<see cref="TaskBoard.Score"/>): urgency x starvation / distance (§4.2), rather
    ///   than a fixed assignment that would idle beside an emergency.
    ///
    /// Movement uses NavMesh so workers path around the buildings instead of through them.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EmployeeAgent : MonoBehaviour
    {
        public enum State { Idle, ToSource, Collecting, ToRack, Depositing }

        [Header("Role")]
        [SerializeField] private EmployeeRole role = EmployeeRole.Transporter;
        [SerializeField] private string resourceId = "tire";

        [Header("Stats (Doc 02 §4.3 base values; upgradeable)")]
        [SerializeField] private float moveSpeed = 3.0f;
        [SerializeField] private int carryCapacity = 3;
        [SerializeField] private float transferInterval = 0.25f;

        [Header("Wiring")]
        [SerializeField] private PartsSource[] sources;
        [SerializeField] private ResourceRack[] racks;
        [SerializeField] private GameObject carriedItemPrefab;
        [Tooltip("Stop hauling to a rack once it holds this many; keeps workers from hoarding.")]
        [SerializeField] private int rackTargetLevel = 8;

        private NavMeshAgent _agent;
        private CarrierView _carrier;
        private State _state = State.Idle;
        private PartsSource _source;
        private ResourceRack _rack;
        private float _timer;

        public State CurrentState => _state;
        public EmployeeRole Role => role;
        public int Carrying => _carrier != null ? _carrier.Stack.CountOf(resourceId) : 0;
        public bool IsFull => Carrying >= carryCapacity;

        public void Configure(EmployeeRole r, string resource, PartsSource[] src, ResourceRack[] rk, GameObject itemPrefab)
        {
            role = r;
            resourceId = resource;
            sources = src;
            racks = rk;
            carriedItemPrefab = itemPrefab;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = moveSpeed;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 20f;
            _agent.stoppingDistance = 1.2f;
            _agent.autoBraking = true;

            _carrier = GetComponent<CarrierView>();
            if (_carrier == null) _carrier = gameObject.AddComponent<CarrierView>();
            _carrier.SetCapacity(carryCapacity);
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
            if (_agent != null) _agent.speed = speed;
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>Explicit tick so the decision logic is drivable from tests.</summary>
        public void Tick(float dt)
        {
            switch (_state)
            {
                case State.Idle: Decide(); break;

                case State.ToSource:
                    if (_source == null) { _state = State.Idle; break; }
                    if (Arrived(_source.transform.position)) _state = State.Collecting;
                    break;

                case State.Collecting:
                    if (_source == null) { _state = State.Idle; break; }
                    _timer += dt;
                    if (_timer < transferInterval) break;
                    _timer = 0f;
                    // Same carrier API the player's collect zone uses.
                    if (IsFull || !_source.HasSupply(resourceId)) { _state = State.Idle; break; }
                    if (_carrier.TryCollect(resourceId, carriedItemPrefab)) _source.Take(resourceId);
                    break;

                case State.ToRack:
                    if (_rack == null) { _state = State.Idle; break; }
                    if (Arrived(_rack.transform.position)) _state = State.Depositing;
                    break;

                case State.Depositing:
                    if (_rack == null) { _state = State.Idle; break; }
                    _timer += dt;
                    if (_timer < transferInterval) break;
                    _timer = 0f;
                    if (Carrying <= 0 || !_rack.CanAdd(resourceId)) { _state = State.Idle; break; }
                    if (_carrier.Deposit(resourceId, 1) > 0) _rack.Add(resourceId);
                    break;
            }
        }

        /// <summary>Haul what we carry, otherwise go fetch more.</summary>
        private void Decide()
        {
            if (Carrying > 0)
            {
                _rack = BestRack();
                if (_rack != null) { Go(_rack.transform.position); _state = State.ToRack; return; }
            }

            if (!IsFull)
            {
                _source = BestSource();
                if (_source != null) { Go(_source.transform.position); _state = State.ToSource; return; }
            }

            // Nothing worth doing: visible idling is intentional feedback of over-hiring
            // (Doc 02 §4.5 edge cases).
        }

        /// <summary>Neediest rack wins, discounted by how far away it is (Doc 02 §4.2).</summary>
        private ResourceRack BestRack()
        {
            if (racks == null) return null;
            ResourceRack best = null;
            float bestScore = 0f;
            foreach (var r in racks)
            {
                // Skip stations that aren't built yet (their objects exist but are disabled).
                if (r == null || !r.isActiveAndEnabled || !r.CanAdd(resourceId)) continue;
                float need = rackTargetLevel - r.CountOf(resourceId);
                if (need <= 0f) continue;
                float dist = Vector3.Distance(transform.position, r.transform.position);
                float score = TaskBoard.Score(need, starvationSeconds: 1f, distanceMeters: dist);
                if (score > bestScore) { bestScore = score; best = r; }
            }
            return best;
        }

        private PartsSource BestSource()
        {
            if (sources == null) return null;
            PartsSource best = null;
            float bestScore = 0f;
            foreach (var s in sources)
            {
                // An unbuilt pallet's object is disabled; don't walk to a source that
                // doesn't exist yet just because it's in the wiring array.
                if (s == null || !s.isActiveAndEnabled || !s.HasSupply(resourceId)) continue;
                float dist = Vector3.Distance(transform.position, s.transform.position);
                float score = TaskBoard.Score(s.Stock, starvationSeconds: 1f, distanceMeters: dist);
                if (score > bestScore) { bestScore = score; best = s; }
            }
            return best;
        }

        private void Go(Vector3 worldPos)
        {
            if (_agent == null || !_agent.isOnNavMesh) return;
            _agent.SetDestination(worldPos);
        }

        private bool Arrived(Vector3 worldPos)
        {
            // Compare on the ground plane: racks/sources sit slightly above the agent.
            var a = new Vector2(transform.position.x, transform.position.z);
            var b = new Vector2(worldPos.x, worldPos.z);
            return Vector2.Distance(a, b) <= _agent.stoppingDistance + 0.6f;
        }
    }
}

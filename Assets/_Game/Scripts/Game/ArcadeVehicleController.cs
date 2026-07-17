using UnityEngine;

namespace Overhaul.Game
{
    [DisallowMultipleComponent]
    public sealed class ArcadeVehicleController : MonoBehaviour
    {
        [Header("Arcade handling")]
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float reverseAcceleration = 10f;
        [SerializeField] private float maxForwardSpeed = 15f;
        [SerializeField] private float maxReverseSpeed = 6f;
        [SerializeField] private float steeringDegreesPerSecond = 125f;
        [SerializeField, Range(0.1f, 1f)] private float lowSpeedSteering = 0.55f;
        [SerializeField] private float lateralGrip = 10f;
        [SerializeField] private float momentumAlignment = 8f;
        [SerializeField] private float coastDeceleration = 1.8f;
        [SerializeField] private float brakeStrength = 22f;
        [Header("Recovery")]
        [SerializeField] private float playableRadius = 180f;
        [SerializeField] private float minimumWorldHeight = -2.5f;

        private Rigidbody _body;
        private Vector2 _driveInput;
        private bool _braking;
        private Vector3 _safePosition;
        private Quaternion _safeRotation;
        private float _safeSampleTimer;
        private float _unsafeTimer;
        private float _stuckTimer;
        private bool _hasSafePose;

        // Owned-car handling profile (Overhaul.Core.CarMath): multipliers layered over
        // the serialized base values so tuning presets and part tiers never overwrite
        // the hand-tuned physics constants above. Identity (1,1,1,1) for plain cars.
        private float _accelerationScale = 1f;
        private float _topSpeedScale = 1f;
        private float _steeringScale = 1f;
        private float _gripScale = 1f;

        public bool HasDriver { get; private set; }
        public event System.Action ResetPerformed;
        public float Speed => _body != null ? _body.linearVelocity.magnitude : 0f;
        public float SpeedKph => Speed * 3.6f;
        public float ForwardSpeed => _body != null ? Vector3.Dot(_body.linearVelocity, transform.forward) : 0f;
        public string GearDirection => ForwardSpeed < -0.35f || (_driveInput.y < -0.1f && Speed < 0.5f) ? "R"
            : ForwardSpeed > 0.35f || (_driveInput.y > 0.1f && Speed < 0.5f) ? "D" : "N";

        /// <summary>Applies a tuning/condition profile on top of the base handling.</summary>
        public void ApplyHandlingProfile(Overhaul.Core.HandlingProfile profile)
        {
            _accelerationScale = Mathf.Max(0.1f, profile.Acceleration);
            _topSpeedScale = Mathf.Max(0.1f, profile.TopSpeed);
            _steeringScale = Mathf.Max(0.1f, profile.Steering);
            _gripScale = Mathf.Max(0.1f, profile.Grip);
        }

        private void Awake()
        {
            EnsurePhysics();
            RememberSafePose();
        }

        public void SetDriverPresent(bool present)
        {
            HasDriver = present;
            if (!present) SetDriveInput(Vector2.zero, true);
            if (_body != null) _body.WakeUp();
            if (present && !_hasSafePose) RememberSafePose();
        }

        public void SetDriveInput(Vector2 input, bool braking)
        {
            // Steering and throttle must remain independent. ClampMagnitude weakens
            // both axes when a player holds forward and turn at the same time.
            _driveInput = new Vector2(
                Mathf.Clamp(input.x, -1f, 1f),
                Mathf.Clamp(input.y, -1f, 1f));
            _braking = braking;
        }

        private void FixedUpdate()
        {
            if (_body == null || !HasDriver) return;

            float dt = Time.fixedDeltaTime;
            float effectiveMaxForward = maxForwardSpeed * _topSpeedScale;
            float effectiveSteering = steeringDegreesPerSecond * _steeringScale;
            float effectiveGrip = lateralGrip * _gripScale;
            float effectiveAlignment = momentumAlignment * _gripScale;
            Vector3 velocity = _body.linearVelocity;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float verticalSpeed = velocity.y;
            float forwardSpeed = Vector3.Dot(planarVelocity, transform.forward);
            float throttle = _driveInput.y;

            bool withinForwardLimit = throttle <= 0f || forwardSpeed < effectiveMaxForward;
            bool withinReverseLimit = throttle >= 0f || forwardSpeed > -maxReverseSpeed;
            if (Mathf.Abs(throttle) > 0.01f && withinForwardLimit && withinReverseLimit)
            {
                float force = (throttle >= 0f ? acceleration : reverseAcceleration) * _accelerationScale;
                _body.AddForce(transform.forward * (throttle * force), ForceMode.Acceleration);
            }

            float planarSpeed = planarVelocity.magnitude;
            bool wantsToMove = Mathf.Abs(throttle) > 0.01f;
            Quaternion gripRotation = _body.rotation;
            if ((planarSpeed > 0.05f || wantsToMove) && Mathf.Abs(_driveInput.x) > 0.01f)
            {
                float speedSteering = Mathf.Lerp(lowSpeedSteering, 1f, Mathf.InverseLerp(0f, 8f, planarSpeed));
                float travelDirection = forwardSpeed < -0.1f || (planarSpeed < 0.1f && throttle < 0f) ? -1f : 1f;
                float yaw = _driveInput.x * effectiveSteering * speedSteering * travelDirection * dt;
                Quaternion nextRotation = _body.rotation * Quaternion.Euler(0f, yaw, 0f);
                _body.MoveRotation(nextRotation);
                gripRotation = nextRotation;

                // Arcade grip rotates existing momentum into the turn. Without this,
                // the body turns visually but continues sliding along its old heading.
                if (planarSpeed > 0.05f)
                {
                    Vector3 nextForward = nextRotation * Vector3.forward;
                    float direction = forwardSpeed < -0.1f ? -1f : 1f;
                    Vector3 alignedVelocity = nextForward * (planarSpeed * direction);
                    float alignment = 1f - Mathf.Exp(-effectiveAlignment * dt);
                    planarVelocity = Vector3.Lerp(planarVelocity, alignedVelocity, alignment);
                }
            }

            Vector3 localVelocity = Quaternion.Inverse(gripRotation) * planarVelocity;
            localVelocity.x = Mathf.MoveTowards(localVelocity.x, 0f, effectiveGrip * dt);
            planarVelocity = gripRotation * localVelocity;

            if (!wantsToMove)
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, coastDeceleration * dt);

            float speedLimit = forwardSpeed < 0f ? maxReverseSpeed : effectiveMaxForward;
            if (planarVelocity.magnitude > speedLimit)
                planarVelocity = planarVelocity.normalized * speedLimit;

            _body.linearVelocity = planarVelocity + Vector3.up * verticalSpeed;

            if (_braking)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, brakeStrength * dt);
                _body.linearVelocity = planarVelocity + Vector3.up * verticalSpeed;
            }

            UpdateRecoveryState(dt);
        }

        public void ResetToLastSafePose()
        {
            if (_body == null) return;
            if (!_hasSafePose) RememberSafePose();
            transform.SetPositionAndRotation(_safePosition + Vector3.up * 0.35f, _safeRotation);
            _body.position = transform.position;
            _body.rotation = transform.rotation;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _driveInput = Vector2.zero;
            _braking = true;
            _unsafeTimer = 0f;
            _stuckTimer = 0f;
            _body.WakeUp();
            ResetPerformed?.Invoke();
        }

        private void UpdateRecoveryState(float dt)
        {
            float upright = Vector3.Dot(transform.up, Vector3.up);
            bool belowWorld = transform.position.y < minimumWorldHeight;
            bool outsideBoundary = new Vector2(transform.position.x, transform.position.z).sqrMagnitude > playableRadius * playableRadius;
            bool overturned = upright < 0.35f;
            _unsafeTimer = belowWorld || outsideBoundary || overturned ? _unsafeTimer + dt : 0f;

            bool tryingToMove = Mathf.Abs(_driveInput.y) > 0.55f;
            _stuckTimer = tryingToMove && Speed < 0.45f ? _stuckTimer + dt : 0f;
            if (belowWorld || outsideBoundary || _unsafeTimer >= 2f || _stuckTimer >= 3f)
            {
                ResetToLastSafePose();
                return;
            }

            _safeSampleTimer += dt;
            if (_safeSampleTimer < 0.5f || upright < 0.92f || Speed < 1f) return;
            _safeSampleTimer = 0f;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)
                && IsRoadSurface(hit.collider.transform))
                RememberSafePose();
        }

        private static bool IsRoadSurface(Transform surface)
        {
            for (var current = surface; current != null; current = current.parent)
                if (current.name.IndexOf("road", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private void RememberSafePose()
        {
            _safePosition = transform.position;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            _safeRotation = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward.normalized, Vector3.up) : Quaternion.identity;
            _hasSafePose = true;
        }

        private void EnsurePhysics()
        {
            _body = GetComponent<Rigidbody>();
            if (_body == null) _body = gameObject.AddComponent<Rigidbody>();
            _body.mass = 850f;
            _body.linearDamping = 0.35f;
            _body.angularDamping = 3f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if (GetComponentInChildren<Collider>() != null) return;
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(bounds.center);
            Vector3 scale = transform.lossyScale;
            box.center += Vector3.down * (0.08f / Mathf.Max(0.001f, Mathf.Abs(scale.y)));
            box.size = new Vector3(
                bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            box.size = new Vector3(box.size.x * 0.9f, Mathf.Max(0.45f, box.size.y * 0.95f), box.size.z * 0.9f);
        }
    }
}

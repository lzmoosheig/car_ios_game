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

        private Rigidbody _body;
        private Vector2 _driveInput;
        private bool _braking;

        public bool HasDriver { get; private set; }
        public float Speed => _body != null ? _body.linearVelocity.magnitude : 0f;

        private void Awake()
        {
            EnsurePhysics();
        }

        public void SetDriverPresent(bool present)
        {
            HasDriver = present;
            if (!present) SetDriveInput(Vector2.zero, true);
            if (_body != null) _body.WakeUp();
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
            Vector3 velocity = _body.linearVelocity;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float verticalSpeed = velocity.y;
            float forwardSpeed = Vector3.Dot(planarVelocity, transform.forward);
            float throttle = _driveInput.y;

            bool withinForwardLimit = throttle <= 0f || forwardSpeed < maxForwardSpeed;
            bool withinReverseLimit = throttle >= 0f || forwardSpeed > -maxReverseSpeed;
            if (Mathf.Abs(throttle) > 0.01f && withinForwardLimit && withinReverseLimit)
            {
                float force = throttle >= 0f ? acceleration : reverseAcceleration;
                _body.AddForce(transform.forward * (throttle * force), ForceMode.Acceleration);
            }

            float planarSpeed = planarVelocity.magnitude;
            bool wantsToMove = Mathf.Abs(throttle) > 0.01f;
            Quaternion gripRotation = _body.rotation;
            if ((planarSpeed > 0.05f || wantsToMove) && Mathf.Abs(_driveInput.x) > 0.01f)
            {
                float speedSteering = Mathf.Lerp(lowSpeedSteering, 1f, Mathf.InverseLerp(0f, 8f, planarSpeed));
                float travelDirection = forwardSpeed < -0.1f || (planarSpeed < 0.1f && throttle < 0f) ? -1f : 1f;
                float yaw = _driveInput.x * steeringDegreesPerSecond * speedSteering * travelDirection * dt;
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
                    float alignment = 1f - Mathf.Exp(-momentumAlignment * dt);
                    planarVelocity = Vector3.Lerp(planarVelocity, alignedVelocity, alignment);
                }
            }

            Vector3 localVelocity = Quaternion.Inverse(gripRotation) * planarVelocity;
            localVelocity.x = Mathf.MoveTowards(localVelocity.x, 0f, lateralGrip * dt);
            planarVelocity = gripRotation * localVelocity;

            if (!wantsToMove)
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, coastDeceleration * dt);

            float speedLimit = forwardSpeed < 0f ? maxReverseSpeed : maxForwardSpeed;
            if (planarVelocity.magnitude > speedLimit)
                planarVelocity = planarVelocity.normalized * speedLimit;

            _body.linearVelocity = planarVelocity + Vector3.up * verticalSpeed;

            if (_braking)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, brakeStrength * dt);
                _body.linearVelocity = planarVelocity + Vector3.up * verticalSpeed;
            }
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

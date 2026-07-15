using UnityEngine;

namespace Overhaul.Game
{
    [DisallowMultipleComponent]
    public sealed class ArcadeVehicleController : MonoBehaviour
    {
        [Header("Arcade handling")]
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float reverseAcceleration = 8f;
        [SerializeField] private float maxForwardSpeed = 15f;
        [SerializeField] private float maxReverseSpeed = 6f;
        [SerializeField] private float steeringDegreesPerSecond = 95f;
        [SerializeField] private float lateralGrip = 7f;
        [SerializeField] private float brakeStrength = 18f;

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
            _driveInput = Vector2.ClampMagnitude(input, 1f);
            _braking = braking;
        }

        private void FixedUpdate()
        {
            if (_body == null || !HasDriver) return;

            float dt = Time.fixedDeltaTime;
            Vector3 localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            float forwardSpeed = localVelocity.z;
            float throttle = _driveInput.y;

            bool withinForwardLimit = throttle <= 0f || forwardSpeed < maxForwardSpeed;
            bool withinReverseLimit = throttle >= 0f || forwardSpeed > -maxReverseSpeed;
            if (Mathf.Abs(throttle) > 0.01f && withinForwardLimit && withinReverseLimit)
            {
                float force = throttle >= 0f ? acceleration : reverseAcceleration;
                _body.AddForce(transform.forward * (throttle * force), ForceMode.Acceleration);
            }

            float speedFactor = Mathf.InverseLerp(0.1f, 3f, Mathf.Abs(forwardSpeed));
            if (speedFactor > 0f && Mathf.Abs(_driveInput.x) > 0.01f)
            {
                float travelDirection = forwardSpeed < -0.1f ? -1f : 1f;
                float yaw = _driveInput.x * steeringDegreesPerSecond * speedFactor * travelDirection * dt;
                _body.MoveRotation(_body.rotation * Quaternion.Euler(0f, yaw, 0f));
            }

            localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            localVelocity.x = Mathf.MoveTowards(localVelocity.x, 0f, lateralGrip * dt);
            _body.linearVelocity = transform.TransformDirection(localVelocity);

            if (_braking)
                _body.linearVelocity = Vector3.MoveTowards(_body.linearVelocity, Vector3.zero, brakeStrength * dt);
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

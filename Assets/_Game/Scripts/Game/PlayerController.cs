using UnityEngine;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    /// <summary>
    /// One-thumb movement: a floating virtual joystick drives a CharacterController.
    /// No jump, no sprint, no interaction button — every interaction is proximity-based
    /// (see InteractionZone). Speed is read live so permanent upgrades apply instantly.
    /// Doc 02 §1.1. Camera-relative so movement matches the fixed isometric view.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.0f;       // Doc 02 §1.1 base; upgrade to ~6.5
        [SerializeField] private float turnSpeedDegPerSec = 720f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _cc;
        private Vector2 _moveInput;     // set by the on-screen joystick (Input System)
        private float _verticalVel;
        private Transform _cam;

        // Camera-plane basis (fixed 45° yaw isometric). Cached from the main camera.
        private Vector3 _camForward = Vector3.forward;
        private Vector3 _camRight = Vector3.right;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (Camera.main != null)
            {
                _cam = Camera.main.transform;
                CacheCameraBasis();
            }
        }

        /// <summary>Hook this to the on-screen stick's PlayerInput "Move" action.</summary>
        public void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

        /// <summary>Direct move injection (used by the on-screen stick or the debug keyboard driver).</summary>
        public void SetMoveInput(Vector2 v) => _moveInput = v;

        public void SetMoveSpeed(float speed) => moveSpeed = speed; // upgrade hook

        private void Update()
        {
            if (_cc == null || !_cc.enabled) return;

            Vector3 planar = (_camRight * _moveInput.x + _camForward * _moveInput.y);
            if (planar.sqrMagnitude > 1f) planar.Normalize();

            Vector3 velocity = planar * moveSpeed;

            _verticalVel = _cc.isGrounded ? -1f : _verticalVel + gravity * Time.deltaTime;
            velocity.y = _verticalVel;

            _cc.Move(velocity * Time.deltaTime);

            if (planar.sqrMagnitude > 0.0001f)
            {
                var target = Quaternion.LookRotation(new Vector3(planar.x, 0f, planar.z));
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, turnSpeedDegPerSec * Time.deltaTime);
            }
        }

        private void CacheCameraBasis()
        {
            _camForward = Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized;
            _camRight = Vector3.ProjectOnPlane(_cam.right, Vector3.up).normalized;
        }
    }
}

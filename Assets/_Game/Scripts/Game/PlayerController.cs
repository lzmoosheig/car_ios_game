using UnityEngine;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    /// <summary>
    /// One-thumb movement: a floating virtual joystick drives a CharacterController.
    /// No jump, no sprint, no interaction button — every interaction is proximity-based
    /// (see InteractionZone). Speed is read live so permanent upgrades apply instantly.
    /// Doc 02 §1.1.
    ///
    /// Movement is always relative to where the camera currently looks. The basis is
    /// recomputed every frame rather than cached once: the isometric camera never rotates
    /// (so that view is unaffected), but the first-person camera does, and a stale basis
    /// made W push toward fixed world-north no matter which way the player faced.
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

        // Ground-plane basis taken from the live camera each frame.
        private Vector3 _camForward = Vector3.forward;
        private Vector3 _camRight = Vector3.right;

        // First person pins the body to the look direction instead of the walk direction,
        // so strafing doesn't spin the character.
        private bool _hasFacingOverride;
        private float _facingYaw;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (Camera.main != null) _cam = Camera.main.transform;
            UpdateCameraBasis();
        }

        /// <summary>First person: face this yaw and treat it as the movement frame.</summary>
        public void SetFacingYaw(float yaw)
        {
            _hasFacingOverride = true;
            _facingYaw = yaw;
        }

        /// <summary>Back to third person: face the walk direction again.</summary>
        public void ClearFacingYaw() => _hasFacingOverride = false;

        /// <summary>Hook this to the on-screen stick's PlayerInput "Move" action.</summary>
        public void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();

        /// <summary>Direct move injection (used by the on-screen stick or the debug keyboard driver).</summary>
        public void SetMoveInput(Vector2 v) => _moveInput = v;

        public void SetMoveSpeed(float speed) => moveSpeed = speed; // upgrade hook

        private void Update()
        {
            if (_cc == null || !_cc.enabled) return;

            UpdateCameraBasis();

            Vector3 planar = (_camRight * _moveInput.x + _camForward * _moveInput.y);
            if (planar.sqrMagnitude > 1f) planar.Normalize();

            Vector3 velocity = planar * moveSpeed;

            _verticalVel = _cc.isGrounded ? -1f : _verticalVel + gravity * Time.deltaTime;
            velocity.y = _verticalVel;

            _cc.Move(velocity * Time.deltaTime);

            if (_hasFacingOverride)
            {
                // Snap to the look direction: the camera is authoritative in first person.
                transform.rotation = Quaternion.Euler(0f, _facingYaw, 0f);
            }
            else if (planar.sqrMagnitude > 0.0001f)
            {
                var target = Quaternion.LookRotation(new Vector3(planar.x, 0f, planar.z));
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, turnSpeedDegPerSec * Time.deltaTime);
            }
        }

        /// <summary>
        /// Flattens the camera's look direction onto the ground plane. Right is derived from
        /// forward via a cross product so any camera roll can't skew strafing.
        /// </summary>
        private void UpdateCameraBasis()
        {
            if (_cam == null)
            {
                if (Camera.main == null) return;
                _cam = Camera.main.transform;
            }

            Vector3 forward = Vector3.ProjectOnPlane(_cam.forward, Vector3.up);
            // Looking almost straight up/down leaves nothing to project; fall back to the
            // camera's up vector, which still encodes the yaw we want.
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(_cam.up, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) return; // degenerate: keep the last good basis

            _camForward = forward.normalized;
            _camRight = Vector3.Cross(Vector3.up, _camForward);
        }
    }
}

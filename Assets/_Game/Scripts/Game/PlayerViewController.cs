using UnityEngine;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    /// <summary>
    /// Toggles the on-foot camera between the default elevated management view
    /// ("third person") and a first-person head camera, with mouse look.
    ///
    /// Driving is deliberately untouched: Doc 09 §3.2 specifies a third-person chase
    /// camera only for vehicles, so this yields control entirely to
    /// <see cref="ThirdPersonDriveCamera"/> whenever a car is being followed.
    ///
    /// While walking this component is the authority on the camera pose, so it also
    /// heals any drift left behind by the drive camera handing back control.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerViewController : MonoBehaviour
    {
        public enum ViewMode { ThirdPerson, FirstPerson }

        [Header("Wiring (auto-resolved if left empty)")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera cam;

        [Header("Toggle")]
        [SerializeField] private Key toggleKey = Key.V;

        [Header("First person")]
        [SerializeField] private float eyeHeight = 1.6f;   // character is 1.7m tall
        [SerializeField] private float forwardOffset = 0.12f;
        [SerializeField] private float firstPersonFov = 70f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float pitchMin = -70f;
        [SerializeField] private float pitchMax = 80f;
        [SerializeField] private bool lockCursor = true;

        private ThirdPersonDriveCamera _driveCam;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _restFov;
        private float _yaw, _pitch;

        public ViewMode Mode { get; private set; } = ViewMode.ThirdPerson;
        public bool IsFirstPerson => Mode == ViewMode.FirstPerson;

        private void Awake()
        {
            if (player == null) player = transform;
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                _driveCam = cam.GetComponent<ThirdPersonDriveCamera>();
                RememberRestPose();
            }
        }

        private void OnDisable() => ApplyCursor(false);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || cam == null) return;

            // Ignore the toggle while driving; the chase camera owns the view there.
            if (IsDriving) return;

            if (keyboard[toggleKey].wasPressedThisFrame) Toggle();

            if (Mode != ViewMode.FirstPerson) return;

            var mouse = Mouse.current;
            if (mouse == null) return;
            var delta = mouse.delta.ReadValue();
            _yaw += delta.x * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, pitchMin, pitchMax);
        }

        private void LateUpdate()
        {
            if (cam == null) return;

            // Driving: hand the camera over completely.
            if (IsDriving) return;

            if (Mode == ViewMode.FirstPerson)
            {
                Vector3 head = player.position + Vector3.up * eyeHeight + player.forward * forwardOffset;
                cam.transform.SetPositionAndRotation(head, Quaternion.Euler(_pitch, _yaw, 0f));
                cam.fieldOfView = firstPersonFov;
            }
            else
            {
                cam.transform.SetPositionAndRotation(_restPosition, _restRotation);
                cam.fieldOfView = _restFov;
            }
        }

        private bool IsDriving => _driveCam != null && _driveCam.IsFollowing;

        public void Toggle() => SetMode(IsFirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson);

        public void SetMode(ViewMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;

            if (mode == ViewMode.FirstPerson)
            {
                // Start looking where the character currently faces, so the view doesn't snap.
                _yaw = player != null ? player.eulerAngles.y : 0f;
                _pitch = 0f;
            }

            SetPlayerVisible(mode != ViewMode.FirstPerson);
            ApplyCursor(mode == ViewMode.FirstPerson);
        }

        /// <summary>Hides the character (and anything carried) so we don't render the inside of its head.</summary>
        private void SetPlayerVisible(bool visible)
        {
            if (player == null) return;
            // Queried each toggle so runtime-spawned carried items are included.
            foreach (var r in player.GetComponentsInChildren<Renderer>(true))
                r.enabled = visible;
        }

        private void ApplyCursor(bool firstPerson)
        {
            if (!lockCursor) return;
            Cursor.lockState = firstPerson ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !firstPerson;
        }

        private void RememberRestPose()
        {
            _restPosition = cam.transform.position;
            _restRotation = cam.transform.rotation;
            _restFov = cam.fieldOfView;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    [DisallowMultipleComponent]
    public sealed class VehicleInteractor : MonoBehaviour
    {
        [SerializeField] private float enterDistance = 3f;

        private PlayerController _playerController;
        private KeyboardDriver _keyboardDriver;
        private CharacterController _characterController;
        private Transform _playerVisual;
        private ThirdPersonDriveCamera _driveCamera;
        private ArcadeVehicleController _currentVehicle;
        private ArcadeVehicleController _nearbyVehicle;
        private float _nextVehicleScan;
        private bool _leftHeld;
        private bool _rightHeld;
        private bool _forwardHeld;
        private bool _reverseHeld;

        public bool IsDriving => _currentVehicle != null;
        public bool CanEnterVehicle => _nearbyVehicle != null;
        public ArcadeVehicleController CurrentVehicle => _currentVehicle;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _keyboardDriver = GetComponent<KeyboardDriver>();
            _characterController = GetComponent<CharacterController>();
            _playerVisual = transform.Find("Visual");

            if (Camera.main != null)
            {
                _driveCamera = Camera.main.GetComponent<ThirdPersonDriveCamera>();
                if (_driveCamera == null) _driveCamera = Camera.main.gameObject.AddComponent<ThirdPersonDriveCamera>();
            }
        }

        private void Update()
        {
            if (_currentVehicle == null && Time.unscaledTime >= _nextVehicleScan)
            {
                _nextVehicleScan = Time.unscaledTime + 0.2f;
                FindNearbyVehicle();
            }

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            bool togglePressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                                 || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
            if (togglePressed) RequestToggleVehicle();

            if (_currentVehicle != null) UpdateDrivingInput(keyboard, gamepad);
        }

        public void RequestToggleVehicle()
        {
            if (_currentVehicle != null) ExitVehicle();
            else if (_nearbyVehicle != null) EnterVehicle(_nearbyVehicle);
        }

        public void SetMobileControl(VehicleMobileControl control, bool held)
        {
            switch (control)
            {
                case VehicleMobileControl.Left: _leftHeld = held; break;
                case VehicleMobileControl.Right: _rightHeld = held; break;
                case VehicleMobileControl.Forward: _forwardHeld = held; break;
                case VehicleMobileControl.Reverse: _reverseHeld = held; break;
            }
        }

        private void UpdateDrivingInput(Keyboard keyboard, Gamepad gamepad)
        {
            float steering = (_rightHeld ? 1f : 0f) - (_leftHeld ? 1f : 0f);
            float throttle = (_forwardHeld ? 1f : 0f) - (_reverseHeld ? 1f : 0f);
            bool brake = false;

            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steering -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steering += 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) throttle += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) throttle -= 1f;
                brake = keyboard.spaceKey.isPressed;
            }
            if (gamepad != null)
            {
                steering += gamepad.leftStick.x.ReadValue();
                throttle += gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();
                brake |= gamepad.buttonWest.isPressed;
            }

            _currentVehicle.SetDriveInput(new Vector2(Mathf.Clamp(steering, -1f, 1f), Mathf.Clamp(throttle, -1f, 1f)), brake);
        }

        private void FindNearbyVehicle()
        {
            _nearbyVehicle = null;
            float bestDistance = enterDistance * enterDistance;
            foreach (var vehicle in FindObjectsByType<ArcadeVehicleController>(FindObjectsInactive.Exclude))
            {
                if (vehicle.HasDriver) continue;
                float distance = (vehicle.transform.position - transform.position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                _nearbyVehicle = vehicle;
            }
        }

        private void EnterVehicle(ArcadeVehicleController vehicle)
        {
            _currentVehicle = vehicle;
            _nearbyVehicle = null;
            _playerController.SetMoveInput(Vector2.zero);
            _playerController.enabled = false;
            if (_keyboardDriver != null) _keyboardDriver.enabled = false;
            if (_characterController != null) _characterController.enabled = false;
            if (_playerVisual != null) _playerVisual.gameObject.SetActive(false);

            transform.SetParent(vehicle.transform, true);
            transform.position = vehicle.transform.position;
            vehicle.SetDriverPresent(true);
            if (_driveCamera != null) _driveCamera.Follow(vehicle.transform);
        }

        private void ExitVehicle()
        {
            var vehicle = _currentVehicle;
            vehicle.SetDriveInput(Vector2.zero, true);
            vehicle.SetDriverPresent(false);
            transform.SetParent(null, true);
            transform.position = vehicle.transform.position + vehicle.transform.right * 2.2f + Vector3.up * 0.15f;

            if (_playerVisual != null) _playerVisual.gameObject.SetActive(true);
            if (_characterController != null) _characterController.enabled = true;
            _playerController.enabled = true;
            if (_keyboardDriver != null) _keyboardDriver.enabled = true;
            if (_driveCamera != null) _driveCamera.StopFollowing();

            _currentVehicle = null;
            _nextVehicleScan = Time.unscaledTime + 0.3f;
            _leftHeld = _rightHeld = _forwardHeld = _reverseHeld = false;
        }
    }
}

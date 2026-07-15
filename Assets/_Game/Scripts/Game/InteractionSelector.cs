using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    /// <summary>
    /// Turns taps/clicks into world selection: raycasts the camera through the pointer,
    /// finds the nearest <see cref="IInteractable"/> in the hit's parents, highlights it
    /// and opens the shared <see cref="InfoPanelView"/>. Tapping empty ground deselects.
    ///
    /// Selection is disabled while driving (the chase camera owns the screen) and ignored
    /// when the pointer is over uGUI, so HUD buttons don't click through into the world.
    /// </summary>
    public sealed class InteractionSelector : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private InfoPanelView panel;
        [SerializeField] private float maxDistance = 400f;

        private IInteractable _selected;
        private ThirdPersonDriveCamera _driveCam;

        public IInteractable Selected => _selected;

        public void Configure(Camera c, InfoPanelView p)
        {
            cam = c;
            panel = p;
        }

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) _driveCam = cam.GetComponent<ThirdPersonDriveCamera>();
        }

        private void Update()
        {
            if (cam == null) return;

            // Driving: the panel would fight the chase camera for attention. Drop selection.
            if (_driveCam != null && _driveCam.IsFollowing)
            {
                if (_selected != null) Deselect();
                return;
            }

            if (!PointerPressedThisFrame(out Vector2 screenPos)) return;
            if (IsPointerOverUi()) return;

            var ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, maxDistance))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null) { Select(interactable); return; }
            }
            Deselect();
        }

        private void Select(IInteractable target)
        {
            if (ReferenceEquals(target, _selected)) return;
            _selected?.OnDeselected();
            _selected = target;
            target.OnSelected();
            if (panel != null) panel.Open(target);
        }

        public void Deselect()
        {
            _selected?.OnDeselected();
            _selected = null;
            if (panel != null) panel.Close();
        }

        private static bool PointerPressedThisFrame(out Vector2 pos)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                pos = mouse.position.ReadValue();
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                pos = touch.primaryTouch.position.ReadValue();
                return true;
            }

            pos = default;
            return false;
        }

        private static bool IsPointerOverUi()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

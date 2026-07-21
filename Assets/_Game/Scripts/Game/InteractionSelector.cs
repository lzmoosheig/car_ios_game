using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    /// <summary>
    /// Turns taps/clicks (or, in first person, a center-screen "look at it" click) into
    /// world selection: raycasts through the target point, finds the nearest
    /// <see cref="IInteractable"/> in the hit's parents, highlights it and opens the shared
    /// <see cref="InfoPanelView"/>. Clicking empty ground deselects.
    ///
    /// In third person the pointer position IS the target (a free, visible cursor). In
    /// first person the cursor is locked to the center of the screen for mouse-look, so a
    /// pointer-position raycast would always hit whatever the OS last reported before the
    /// lock engaged - effectively dead. First person instead raycasts from the screen
    /// center on a left click/tap, the standard "crosshair" pattern.
    ///
    /// Selection is disabled while driving (the chase camera owns the screen) and, in third
    /// person, ignored when the pointer is over uGUI so HUD buttons don't click through.
    /// </summary>
    public sealed class InteractionSelector : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private InfoPanelView panel;
        [SerializeField] private float maxDistance = 400f;

        private IInteractable _selected;
        private ThirdPersonDriveCamera _driveCam;
        private PlayerViewController _viewController;

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
            _viewController = FindFirstObjectByType<PlayerViewController>();
        }

        private void Update()
        {
            if (cam == null) return;

            // A modal inventory screen is open; its clicks aren't world taps.
            if (InventoryUiModal.IsOpen) return;

            // Driving: the panel would fight the chase camera for attention. Drop selection.
            if (_driveCam != null && _driveCam.IsFollowing)
            {
                if (_selected != null) Deselect();
                return;
            }

            bool firstPerson = _viewController != null && _viewController.IsFirstPerson;

            Ray ray;
            if (firstPerson)
            {
                // Crosshair pick: any left click/tap fires, regardless of stale cursor
                // position, since the cursor is locked and not meaningfully "pointing" at
                // anything on screen.
                if (!ClickedThisFrame()) return;
                ray = cam.ScreenPointToRay(new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f));
            }
            else
            {
                if (!PointerPressedThisFrame(out Vector2 screenPos)) return;
                if (IsPointerOverUi()) return;
                ray = cam.ScreenPointToRay(screenPos);
            }

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

        /// <summary>Left click or a fresh touch, ignoring where either currently reports as being.</summary>
        private static bool ClickedThisFrame()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
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

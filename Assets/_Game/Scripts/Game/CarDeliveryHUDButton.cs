using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// The right-side square HUD shortcut that opens the Car Delivery menu. Mirrors
    /// <see cref="ServeCustomerHUDButton"/>'s press-scale/glow motion but stays a plain
    /// icon button (no status panel) and drives the menu directly via a serialized
    /// reference rather than an event, since it has no HudView-style owner to subscribe.
    /// </summary>
    public sealed class CarDeliveryHUDButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private GameObject badge;
        [SerializeField] private Text badgeText;
        [SerializeField] private Image glow;
        [SerializeField] private CarDeliveryMenu menu;
        [SerializeField] private CarDeliverySystem system;

        [Header("Motion")]
        [SerializeField] private float pulseScale = 1.035f;
        [SerializeField] private float pulseSpeed = 3.25f;
        [SerializeField] private float pressedScale = 0.94f;

        private bool _actionable;
        private bool _pressed;
        private Vector3 _baseScale = Vector3.one;
        private float _pollTimer;

        public void Configure(RectTransform rootRect, GameObject badgeGo, Text badgeLabel, Image glowImage,
            CarDeliveryMenu deliveryMenu, CarDeliverySystem deliverySystem)
        {
            root = rootRect;
            badge = badgeGo;
            badgeText = badgeLabel;
            glow = glowImage;
            menu = deliveryMenu;
            system = deliverySystem;
            _baseScale = root != null ? root.localScale : Vector3.one;
        }

        private void Awake()
        {
            if (root == null) root = transform as RectTransform;
            _baseScale = root != null ? root.localScale : Vector3.one;
        }

        private void Update()
        {
            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer >= 0.25f)
            {
                _pollTimer = 0f;
                RefreshBadge();
            }

            if (root == null) return;
            float target = 1f;
            if (_pressed) target = pressedScale;
            else if (_actionable) target = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * (pulseScale - 1f);
            root.localScale = Vector3.Lerp(root.localScale, _baseScale * target, Time.unscaledDeltaTime * 12f);
        }

        /// <summary>Counts ready-to-collect slots so the badge reflects real state
        /// ("deliveries are ready") instead of a scripted flag.</summary>
        private void RefreshBadge()
        {
            if (system == null) return;
            int ready = 0;
            foreach (var slot in system.Slots)
                if (slot != null && slot.IsComplete) ready++;

            _actionable = ready > 0;
            if (glow != null) glow.enabled = _actionable;
            if (badge != null) badge.SetActive(ready > 0);
            if (badgeText != null && ready > 0) badgeText.text = ready > 9 ? "9+" : ready.ToString();
        }

        public void OnPointerDown(PointerEventData eventData) => _pressed = true;

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pressed) menu?.Toggle();
            _pressed = false;
        }

        public void OnPointerExit(PointerEventData eventData) => _pressed = false;
    }
}

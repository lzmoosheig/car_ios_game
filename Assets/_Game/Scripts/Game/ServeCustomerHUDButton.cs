using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Overhaul.Game
{
    public sealed class ServeCustomerHUDButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private GameObject badge;
        [SerializeField] private Image glow;

        [Header("Responsive")]
        [SerializeField] private float collapseBelowWidth = 760f;

        [Header("Motion")]
        [SerializeField] private float pulseScale = 1.035f;
        [SerializeField] private float pulseSpeed = 3.25f;
        [SerializeField] private float pressedScale = 0.94f;

        private bool _actionable;
        private bool _pressed;
        private Vector3 _baseScale = Vector3.one;

        public void Configure(RectTransform root, GameObject panel, Text title, Text detail, GameObject waitingBadge, Image glowImage)
        {
            cardRoot = root;
            statusPanel = panel;
            titleText = title;
            detailText = detail;
            badge = waitingBadge;
            glow = glowImage;
            _baseScale = cardRoot != null ? cardRoot.localScale : Vector3.one;
        }

        public void SetStatus(string title, string detail, bool actionable)
        {
            if (titleText != null) titleText.text = title;
            if (detailText != null) detailText.text = detail;
            _actionable = actionable;
            if (badge != null) badge.SetActive(actionable);
            if (glow != null) glow.enabled = actionable;
        }

        private void Awake()
        {
            if (cardRoot == null) cardRoot = transform as RectTransform;
            _baseScale = cardRoot != null ? cardRoot.localScale : Vector3.one;
        }

        private void Update()
        {
            if (statusPanel != null)
            {
                float width = Screen.safeArea.width > 0f ? Screen.safeArea.width : Screen.width;
                statusPanel.SetActive(width >= collapseBelowWidth);
            }

            if (cardRoot == null) return;

            float target = 1f;
            if (_pressed)
            {
                target = pressedScale;
            }
            else if (_actionable)
            {
                target = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * (pulseScale - 1f);
            }

            cardRoot.localScale = Vector3.Lerp(cardRoot.localScale, _baseScale * target, Time.unscaledDeltaTime * 12f);
        }

        public void OnPointerDown(PointerEventData eventData) => _pressed = true;
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
        public void OnPointerExit(PointerEventData eventData) => _pressed = false;
    }
}

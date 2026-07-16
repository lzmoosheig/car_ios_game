using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// One Car Delivery slot cell: either an active production slot (icon, quantity,
    /// timer, progress bar, action button) or a locked placeholder with an unlock
    /// requirement. Built by the editor tool; this only formats data onto widgets.
    /// </summary>
    public sealed class DeliverySlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text quantityText;
        [SerializeField] private Text timerText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Button actionButton;
        [SerializeField] private GameObject actionBadge;
        [SerializeField] private Text actionBadgeText;
        [SerializeField] private GameObject unlockedGroup;
        [SerializeField] private GameObject lockedGroup;
        [SerializeField] private Text lockRequirementText;

        public event System.Action<DeliverySlotView> Clicked;

        private void Awake() => BindButton();

        public void Configure(Image iconImg, Text name, Text qty, Text timer, Image fill,
            Button action, GameObject badge, Text badgeText,
            GameObject unlocked, GameObject locked, Text lockRequirement)
        {
            icon = iconImg;
            nameText = name;
            quantityText = qty;
            timerText = timer;
            progressFill = fill;
            actionButton = action;
            actionBadge = badge;
            actionBadgeText = badgeText;
            unlockedGroup = unlocked;
            lockedGroup = locked;
            lockRequirementText = lockRequirement;
            BindButton();
        }

        private void BindButton()
        {
            if (actionButton == null) return;
            actionButton.onClick.RemoveListener(NotifyClicked);
            actionButton.onClick.AddListener(NotifyClicked);
        }

        private void NotifyClicked() => Clicked?.Invoke(this);

        public void ShowLocked(string requirementLabel)
        {
            if (unlockedGroup != null) unlockedGroup.SetActive(false);
            if (lockedGroup != null) lockedGroup.SetActive(true);
            if (lockRequirementText != null) lockRequirementText.text = requirementLabel;
        }

        public void ShowActive(Sprite iconSprite, string name, int quantity, float progress01,
            string timerLabel, bool actionEnabled, string actionBadgeLabel)
        {
            if (unlockedGroup != null) unlockedGroup.SetActive(true);
            if (lockedGroup != null) lockedGroup.SetActive(false);
            if (icon != null) icon.sprite = iconSprite;
            if (nameText != null) nameText.text = name;
            if (quantityText != null) quantityText.text = $"x{quantity}";
            if (progressFill != null) progressFill.fillAmount = progress01;
            if (timerText != null) timerText.text = timerLabel;
            if (actionButton != null) actionButton.interactable = actionEnabled;
            bool showBadge = !string.IsNullOrEmpty(actionBadgeLabel);
            if (actionBadge != null) actionBadge.SetActive(showBadge);
            if (actionBadgeText != null && showBadge) actionBadgeText.text = actionBadgeLabel;
        }
    }
}

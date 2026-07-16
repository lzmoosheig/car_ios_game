using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>One row in the Car Delivery "Buy Items" list: icon, name, description,
    /// purchase quantity, price and a Buy button. Built by the editor tool.</summary>
    public sealed class DeliveryItemRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text quantityText;
        [SerializeField] private Text priceText;
        [SerializeField] private Button buyButton;

        public event System.Action<DeliveryItemRow> Clicked;

        private void Awake() => BindButton();

        public void Configure(Image iconImg, Text name, Text description, Text qty, Text price, Button buy)
        {
            icon = iconImg;
            nameText = name;
            descriptionText = description;
            quantityText = qty;
            priceText = price;
            buyButton = buy;
            BindButton();
        }

        private void BindButton()
        {
            if (buyButton == null) return;
            buyButton.onClick.RemoveListener(NotifyClicked);
            buyButton.onClick.AddListener(NotifyClicked);
        }

        private void NotifyClicked() => Clicked?.Invoke(this);

        public void Set(Sprite iconSprite, string name, string description, string quantityLabel,
            string priceLabel, bool affordable)
        {
            if (icon != null) icon.sprite = iconSprite;
            if (nameText != null) nameText.text = name;
            if (descriptionText != null) descriptionText.text = description;
            if (quantityText != null) quantityText.text = quantityLabel;
            if (priceText != null) priceText.text = priceLabel;
            if (buyButton != null) buyButton.interactable = affordable;
        }
    }
}

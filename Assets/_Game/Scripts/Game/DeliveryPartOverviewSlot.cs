using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>One stock overview card in the Car Delivery dashboard.</summary>
    public sealed class DeliveryPartOverviewSlot : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text quantityText;

        public void Configure(Image iconImg, Text name, Text quantity)
        {
            icon = iconImg;
            nameText = name;
            quantityText = quantity;
        }

        public void Set(Sprite iconSprite, string displayName, int quantity)
        {
            if (icon != null) icon.sprite = iconSprite;
            if (nameText != null) nameText.text = displayName;
            if (quantityText != null) quantityText.text = quantity.ToString("N0");
        }
    }
}

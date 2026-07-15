using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    public sealed class CurrencyDisplay : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text valueText;

        public void Configure(Image iconImage, Text valueLabel)
        {
            icon = iconImage;
            valueText = valueLabel;
        }

        public void SetValue(long value)
        {
            if (valueText != null) valueText.text = HudView.Format(value);
        }
    }
}

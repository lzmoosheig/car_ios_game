using UnityEngine;

namespace Overhaul.Game
{
    public sealed class CurrencyBar : MonoBehaviour
    {
        [SerializeField] private CurrencyDisplay cashDisplay;
        [SerializeField] private CurrencyDisplay goldDisplay;

        public void Configure(CurrencyDisplay cash, CurrencyDisplay gold)
        {
            cashDisplay = cash;
            goldDisplay = gold;
        }

        public void SetCash(long value)
        {
            if (cashDisplay != null) cashDisplay.SetValue(value);
        }

        public void SetGold(int value)
        {
            if (goldDisplay != null) goldDisplay.SetValue(value);
        }
    }
}

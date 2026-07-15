using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>A physical, one-purchase-per-entry management pad for a village upgrade.</summary>
    public sealed class UpgradePurchasePad : MonoBehaviour
    {
        [SerializeField] private VillageUpgradeManager upgrades;
        [SerializeField] private string upgradeId = VillageUpgradeManager.OfficePricingId;
        [SerializeField] private string displayName = "Service pricing";
        [SerializeField] private TextMesh statusText;
        [SerializeField] private Renderer padRenderer;
        [SerializeField] private Color readyColor = new(0.2f, 1f, 0.55f, 1f);
        [SerializeField] private Color idleColor = new(0.55f, 0.65f, 0.7f, 1f);

        public void Configure(VillageUpgradeManager manager, string id, string label,
                              TextMesh text, Renderer renderer)
        {
            if (isActiveAndEnabled && upgrades != null)
                upgrades.UpgradeChanged -= OnUpgradeChanged;
            upgrades = manager;
            upgradeId = id;
            displayName = label;
            statusText = text;
            padRenderer = renderer;
            if (isActiveAndEnabled && upgrades != null)
                upgrades.UpgradeChanged += OnUpgradeChanged;
            Refresh();
        }

        private void OnEnable()
        {
            if (upgrades != null) upgrades.UpgradeChanged += OnUpgradeChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (upgrades != null) upgrades.UpgradeChanged -= OnUpgradeChanged;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<CarrierView>() != null) TryPurchase();
        }

        public bool TryPurchase()
        {
            bool bought = upgrades != null && upgrades.TryPurchase(upgradeId);
            Refresh();
            return bought;
        }

        public void Refresh()
        {
            if (upgrades == null) return;
            int tier = upgrades.TierOf(upgradeId);
            int cost = upgrades.NextCost(upgradeId);
            bool maxed = upgrades.IsMaxed(upgradeId);

            if (statusText != null)
                statusText.text = maxed
                    ? $"{displayName.ToUpperInvariant()}  MAX"
                    : $"{displayName.ToUpperInvariant()}  LV {tier + 1}  ${cost}";

            if (padRenderer != null && padRenderer.sharedMaterial != null)
            {
                var block = new MaterialPropertyBlock();
                padRenderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", maxed ? idleColor : readyColor);
                block.SetColor("_Color", maxed ? idleColor : readyColor);
                padRenderer.SetPropertyBlock(block);
            }
        }

        private void OnUpgradeChanged(string id, int _) { if (id == upgradeId) Refresh(); }
    }
}

using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Marks a GameObject (an NPC like the parts delivery worker, or a chest/crate) as an
    /// inventory the player can open and take from. It just exposes the
    /// <see cref="InventoryComponent"/>, a display title and an interaction radius; the actual
    /// "walk up and press a key" flow lives in <see cref="PlayerContainerInteractor"/>, and the
    /// transfer UI in <see cref="ContainerTransferScreen"/>. Kept dumb so any entity can be a
    /// container without new code.
    /// </summary>
    [RequireComponent(typeof(InventoryComponent))]
    public sealed class InventoryContainer : MonoBehaviour
    {
        [SerializeField] private string displayTitle = "Parts Delivery";
        [SerializeField] private string windowTitle = "";
        [SerializeField] private string containerPanelTitle = "";
        [SerializeField] private string subtitle = "Tap items to move them";
        [Tooltip("How close the player must be (metres) to open this container.")]
        [SerializeField] private float interactRadius = 3.5f;

        private InventoryComponent _inventory;

        public string DisplayTitle => string.IsNullOrEmpty(displayTitle) ? name : displayTitle;
        public string WindowTitle => string.IsNullOrEmpty(windowTitle) ? DisplayTitle : windowTitle;
        public string ContainerPanelTitle => string.IsNullOrEmpty(containerPanelTitle) ? "Delivery Crate" : containerPanelTitle;
        public string Subtitle => string.IsNullOrEmpty(subtitle) ? "Tap items to move them" : subtitle;
        public float InteractRadius => interactRadius;
        public Vector3 Position => transform.position;

        public InventoryComponent Inventory =>
            _inventory != null ? _inventory : (_inventory = GetComponent<InventoryComponent>());

        public void Configure(string title, float radius)
        {
            displayTitle = title;
            interactRadius = radius;
        }

        public void Configure(string title, float radius, string panelTitle, string screenTitle = null,
            string screenSubtitle = null)
        {
            displayTitle = title;
            interactRadius = radius;
            containerPanelTitle = panelTitle;
            windowTitle = screenTitle ?? title;
            subtitle = screenSubtitle ?? "Tap items to move them";
        }
    }
}

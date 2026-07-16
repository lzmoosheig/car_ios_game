using System.Collections.Generic;
using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// In-world tap target near the Parts Delivery building that opens the Car Delivery
    /// menu. Kept as its own pad rather than folded into <see cref="BuildingView"/> so
    /// tapping the building keeps its existing status panel behaviour untouched while this
    /// dedicated pad offers the delivery menu action (Doc 09 §3.3 tap-to-interact pattern).
    /// </summary>
    public sealed class CarDeliveryBuildingButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private CarDeliveryMenu menu;

        public string Title => "Delivery";
        public Transform PivotTransform => transform;

        public void Configure(CarDeliveryMenu deliveryMenu) => menu = deliveryMenu;

        public void OnSelected() => SelectionRing.Show(transform, 3.2f);
        public void OnDeselected() => SelectionRing.Hide();

        public void GetInfoLines(List<string> into) => into.Add("Tap to open the Car Delivery menu");

        public void GetActions(List<InteractableAction> into)
            => into.Add(new InteractableAction("Open Delivery Menu", () => menu?.Open()));
    }
}

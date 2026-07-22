using System;
using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Attaches a reusable <see cref="SlotInventory"/> to any GameObject - the player, a
    /// building, a storage container or a worker. This is the one component every item-holding
    /// entity shares, so transfer / UI / debug code treats them all identically. Role- and
    /// building-specific rules are expressed as allowed item ids and/or categories; nothing
    /// here is car-parts specific. Item metadata (names, stack caps, icons) comes from the
    /// scene <see cref="ResourceCatalog"/>, which implements <see cref="IItemDatabase"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryComponent : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField] private int slotCount = 12;
        [Tooltip("Leading usable slots at build time; the rest are locked (Parts Warehouse). -1 = all unlocked.")]
        [SerializeField] private int unlockedSlots = -1;

        [Header("Access rules (leave both empty to accept everything)")]
        [Tooltip("Explicit item ids this inventory accepts.")]
        [SerializeField] private List<string> allowedItemIds = new();
        [Tooltip("Item categories this inventory accepts (e.g. a parts courier -> Part).")]
        [SerializeField] private List<ItemCategory> allowedCategories = new();

        [Header("Debug")]
        [Tooltip("Label shown in the inventory inspector overlay. Falls back to the object name.")]
        [SerializeField] private string ownerLabel = "";

        private SlotInventory _inv;

        /// <summary>The engine-agnostic model. Built lazily so it works in EditMode tests too.</summary>
        public SlotInventory Inventory => _inv ??= BuildInventory();

        public string OwnerLabel => string.IsNullOrEmpty(ownerLabel) ? name : ownerLabel;
        public int SlotCount => slotCount;

        /// <summary>Leading usable slots; the rest are locked (Parts Warehouse expansion).</summary>
        public int UnlockedSlots
        {
            get => Inventory.UnlockedSlots;
            set => Inventory.UnlockedSlots = value;
        }

        public bool IsSlotUnlocked(int index) => Inventory.IsSlotUnlocked(index);

        /// <summary>Mirrors <see cref="SlotInventory.Changed"/> for view/HUD subscribers.</summary>
        public event Action Changed;

        private SlotInventory BuildInventory()
        {
            var inv = new SlotInventory(Mathf.Max(1, slotCount), Catalog, IsAllowed);
            if (unlockedSlots >= 0) inv.UnlockedSlots = unlockedSlots; // warehouse starts partly locked
            inv.Changed += RaiseChanged;
            return inv;
        }

        private void RaiseChanged() => Changed?.Invoke();

        private static IItemDatabase Catalog => ResourceCatalog.Instance;

        private bool IsAllowed(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (allowedItemIds != null && allowedItemIds.Count > 0 && !allowedItemIds.Contains(itemId))
                return false;
            if (allowedCategories != null && allowedCategories.Count > 0)
            {
                var cat = Catalog.CategoryOf(itemId);
                if (!allowedCategories.Contains(cat)) return false;
            }
            return true;
        }

        /// <summary>Runtime/editor wiring (mirrors the Configure(...) pattern used across the project).</summary>
        public void Configure(int slots, IEnumerable<string> ids = null,
            IEnumerable<ItemCategory> categories = null, string label = null, int unlocked = -1)
        {
            slotCount = Mathf.Max(1, slots);
            allowedItemIds = ids != null ? new List<string>(ids) : new List<string>();
            allowedCategories = categories != null ? new List<ItemCategory>(categories) : new List<ItemCategory>();
            if (label != null) ownerLabel = label;
            unlockedSlots = unlocked;
            _inv = BuildInventory();
        }

        // --------------------------------------------------------- convenience pass-throughs
        public int Add(string itemId, int count) => Inventory.Add(itemId, count);
        public int Remove(string itemId, int count) => Inventory.Remove(itemId, count);
        public int CountOf(string itemId) => Inventory.CountOf(itemId);
        public int RoomFor(string itemId) => Inventory.RoomFor(itemId);
        public bool IsFull => Inventory.IsFull;
        public bool IsEmpty => Inventory.IsEmpty;

        // --------------------------------------------------------- persistence
        public InventorySave Capture() => Inventory.Capture();
        public void Restore(InventorySave save) => Inventory.Restore(save);
    }
}

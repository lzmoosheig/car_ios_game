using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// The Parts Warehouse: a general storage the player can deposit any item into. It starts
    /// with 4 usable slots; the remaining 16 are locked and bought one at a time with cash,
    /// gated by player level (level 2 unlocks buying slots 5-6, level 3 slots 7-8, and so on).
    ///
    /// State is the leading-unlocked-slots count on the storage <see cref="InventoryComponent"/>;
    /// all the "which slot, what level, what price" rules live here so the UI stays dumb.
    /// </summary>
    [RequireComponent(typeof(InventoryComponent))]
    public sealed class PartsWarehouse : MonoBehaviour
    {
        public const int TotalSlots = 20;
        public const int FreeSlots = 4;

        [SerializeField] private InventoryComponent storage;
        [SerializeField] private EconomyManager economy;

        private InventoryComponent Storage => storage != null ? storage : (storage = GetComponent<InventoryComponent>());

        public int UnlockedSlots => Storage.UnlockedSlots;
        public bool IsMaxed => UnlockedSlots >= TotalSlots;

        private void Awake()
        {
            if (storage == null) storage = GetComponent<InventoryComponent>();
            if (economy == null) economy = FindFirstObjectByType<EconomyManager>();
            if (Storage.SlotCount < TotalSlots) Storage.Configure(TotalSlots, label: "Parts Warehouse", unlocked: FreeSlots);
            // Safety net for a clearly-broken value; the serialized unlockedSlots is the real
            // source of truth, and a save restore (in Start) raises this to what was bought.
            if (Storage.UnlockedSlots < FreeSlots) Storage.UnlockedSlots = FreeSlots;
        }

        public void Configure(EconomyManager eco) => economy = eco;

        /// <summary>The level required before slot <paramref name="index"/> (0-based) may be bought.
        /// Slots 5-6 need level 2, 7-8 level 3, … (the 4 free slots need level 1).</summary>
        public static int RequiredLevel(int index)
            => index < FreeSlots ? 1 : (index - FreeSlots) / 2 + 2;

        /// <summary>Cash price of slot <paramref name="index"/> (0-based). Free slots cost 0.</summary>
        public static int Price(int index)
            => index < FreeSlots ? 0 : EconomyFormulas.WarehouseSlotCost(index - FreeSlots);

        /// <summary>The next locked slot the player would buy (== current unlocked count), or -1 if maxed.</summary>
        public int NextSlotIndex => IsMaxed ? -1 : UnlockedSlots;

        public int NextSlotPrice => IsMaxed ? 0 : Price(NextSlotIndex);
        public int NextSlotRequiredLevel => IsMaxed ? 0 : RequiredLevel(NextSlotIndex);

        public int PlayerLevel => economy != null ? economy.Level : 1;

        /// <summary>Why the next slot can't be bought yet (null when it can, or when maxed).</summary>
        public string BlockReason
        {
            get
            {
                if (IsMaxed) return null;
                if (PlayerLevel < NextSlotRequiredLevel) return $"Reach level {NextSlotRequiredLevel}";
                if (economy != null && economy.Wallet < NextSlotPrice) return $"Need ${NextSlotPrice}";
                return null;
            }
        }

        public bool CanBuyNext => !IsMaxed && PlayerLevel >= NextSlotRequiredLevel
                                  && economy != null && economy.Wallet >= NextSlotPrice;

        public InventorySave Capture() => Storage.Capture();

        public void Restore(InventorySave save)
        {
            if (save == null) return;
            Storage.Restore(save);
            if (Storage.UnlockedSlots < FreeSlots) Storage.UnlockedSlots = FreeSlots;
        }

        /// <summary>Buys the next locked slot if level + cash allow. Returns true on success.</summary>
        public bool TryBuyNextSlot()
        {
            if (!CanBuyNext) return false;
            if (!economy.TrySpend(NextSlotPrice)) return false;
            Storage.UnlockedSlots = UnlockedSlots + 1;
            ScreenToast.Show($"Warehouse slot {UnlockedSlots} unlocked!");
            return true;
        }
    }
}

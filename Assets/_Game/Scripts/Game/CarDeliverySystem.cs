using System;
using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-side Car Delivery state: six production slots (four running, two locked) plus
    /// an owned-item stockpile bought with cash/gold. The math lives in the engine-agnostic
    /// <see cref="CarDeliveryLogic"/>; this only holds state, ticks timers and raises a
    /// change notification for the menu UI - the same split EconomyManager uses for the HUD.
    /// </summary>
    public sealed class CarDeliverySystem : MonoBehaviour
    {
        public const int SlotCount = 6;
        private const int BaseSlotUnlockCost = 75000;
        private static readonly string[] FallbackItemIds = { "tire", "oil", "battery", "paint" };

        [SerializeField] private EconomyManager economy;

        private readonly DeliverySlotState[] _slots = new DeliverySlotState[SlotCount];
        private readonly Dictionary<string, int> _owned = new();
        private bool _seeded;

        public event Action Changed;

        public IReadOnlyList<DeliverySlotState> Slots => _slots;
        public IReadOnlyDictionary<string, int> OwnedItems => _owned;

        public void Configure(EconomyManager eco)
        {
            economy = eco;
            EnsureSeeded();
        }

        private void Awake() => EnsureSeeded();

        private void EnsureSeeded()
        {
            if (_seeded) return;
            _seeded = true;

            int[] qty = { 120, 90, 60, 75 };
            float[] duration = { 272f, 675f, 1068f, 1505f };
            for (int i = 0; i < SlotCount; i++)
            {
                bool unlocked = i < 4;
                _slots[i] = new DeliverySlotState
                {
                    Unlocked = unlocked,
                    ItemId = unlocked ? FallbackItemIds[i] : "",
                    Quantity = unlocked ? qty[i] : 0,
                    DurationSeconds = unlocked ? duration[i] : 0f,
                    UnlockRequirementLevel = i == 4 ? 28 : i == 5 ? 34 : 0
                };
                if (unlocked) CarDeliveryLogic.Start(_slots[i]);
            }
        }

        private void Update()
        {
            bool any = false;
            foreach (var slot in _slots)
            {
                if (slot == null || !slot.Running) continue;
                float before = slot.ElapsedSeconds;
                CarDeliveryLogic.Tick(slot, Time.deltaTime);
                if (!Mathf.Approximately(slot.ElapsedSeconds, before)) any = true;
            }
            if (any) Changed?.Invoke();
        }

        public int OwnedCountOf(string itemId) => _owned.TryGetValue(itemId, out var q) ? q : 0;

        /// <summary>Consumes owned delivery stock (all-or-nothing). This is the Doc 09
        /// §6.1 racing link: the same tires/oil bought for the business maintain the
        /// player's personal car at the Basic Bay.</summary>
        public bool TryConsume(string itemId, int count)
        {
            if (count <= 0) return true;
            if (OwnedCountOf(itemId) < count) return false;
            _owned[itemId] -= count;
            Changed?.Invoke();
            return true;
        }

        public bool TryBuy(string itemId)
        {
            if (economy == null || !CarDeliveryCatalog.TryFind(itemId, out var def)) return false;

            bool spent = def.Currency == DeliveryCurrency.Gold
                ? economy.TrySpendGold(def.Price)
                : economy.TrySpend(def.Price);
            if (!spent) return false;

            _owned[itemId] = OwnedCountOf(itemId) + def.PurchaseQuantity;
            Changed?.Invoke();
            return true;
        }

        public bool TryStart(int slotIndex)
        {
            if (!IsValidIndex(slotIndex) || !CarDeliveryLogic.CanStart(_slots[slotIndex])) return false;
            CarDeliveryLogic.Start(_slots[slotIndex]);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Starts every unlocked, idle slot at once (the bottom "Start Delivery" button).</summary>
        public int StartAllIdle()
        {
            int started = 0;
            for (int i = 0; i < SlotCount; i++)
                if (TryStart(i)) started++;
            return started;
        }

        /// <summary>Gold fast-forward: instantly completes and collects a running slot.</summary>
        public bool TrySkip(int slotIndex, int goldCost)
        {
            if (!IsValidIndex(slotIndex) || economy == null) return false;
            var slot = _slots[slotIndex];
            if (slot == null || !slot.Running || slot.IsComplete) return false;
            if (!economy.TrySpendGold(goldCost)) return false;

            CarDeliveryLogic.Finish(slot);
            CollectSlot(slotIndex);
            return true;
        }

        /// <summary>Collects a finished slot's output into owned stock and restarts it.</summary>
        public bool CollectSlot(int slotIndex)
        {
            if (!IsValidIndex(slotIndex)) return false;
            var slot = _slots[slotIndex];
            int granted = CarDeliveryLogic.Collect(slot);
            if (granted <= 0) return false;

            _owned[slot.ItemId] = OwnedCountOf(slot.ItemId) + granted;
            CarDeliveryLogic.Start(slot);
            Changed?.Invoke();
            return true;
        }

        public int NextSlotUnlockCost()
        {
            int unlockedCount = 0;
            foreach (var s in _slots) if (s.Unlocked) unlockedCount++;
            return BaseSlotUnlockCost * Mathf.Max(1, unlockedCount - 3);
        }

        public bool TryUnlockNextSlot()
        {
            if (economy == null) return false;

            int idx = -1;
            for (int i = 0; i < SlotCount; i++)
                if (!_slots[i].Unlocked) { idx = i; break; }
            if (idx < 0) return false;

            if (!economy.TrySpend(NextSlotUnlockCost())) return false;

            var slot = _slots[idx];
            slot.Unlocked = true;
            slot.ItemId = FallbackItemIds[idx % FallbackItemIds.Length];
            slot.Quantity = 80;
            slot.DurationSeconds = 600f;
            CarDeliveryLogic.Start(slot);
            Changed?.Invoke();
            return true;
        }

        private static bool IsValidIndex(int i) => i >= 0 && i < SlotCount;

        // ------------------------------------------------------------------- persistence

        public CarDeliverySave Capture()
        {
            var save = new CarDeliverySave();
            foreach (var kv in _owned) save.OwnedItems[kv.Key] = kv.Value;
            for (int i = 0; i < SlotCount; i++)
            {
                var s = _slots[i];
                save.SlotUnlocked[i] = s.Unlocked;
                save.SlotElapsed[i] = s.ElapsedSeconds;
                save.SlotRunning[i] = s.Running;
            }
            return save;
        }

        public void Restore(CarDeliverySave save)
        {
            EnsureSeeded();
            if (save == null) return;

            if (save.OwnedItems != null)
                foreach (var kv in save.OwnedItems) _owned[kv.Key] = kv.Value;

            for (int i = 0; i < SlotCount; i++)
            {
                var slot = _slots[i];
                bool wasUnlocked = i < save.SlotUnlocked.Length && save.SlotUnlocked[i];
                if (i >= 4 && wasUnlocked && !slot.Unlocked)
                {
                    slot.Unlocked = true;
                    slot.ItemId = FallbackItemIds[i % FallbackItemIds.Length];
                    slot.Quantity = 80;
                    slot.DurationSeconds = 600f;
                }
                slot.ElapsedSeconds = i < save.SlotElapsed.Length ? save.SlotElapsed[i] : 0f;
                slot.Running = slot.Unlocked && i < save.SlotRunning.Length && save.SlotRunning[i];
                if (slot.Unlocked && !slot.Running) CarDeliveryLogic.Start(slot);
            }
            Changed?.Invoke();
        }
    }
}

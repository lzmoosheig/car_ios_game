using System;

namespace Overhaul.Core
{
    public enum DeliveryCurrency { Cash, Gold }

    /// <summary>One buyable delivery item: price, currency and how much a single
    /// purchase grants. Placeholder for a future ScriptableObject catalog, mirroring
    /// how <see cref="Overhaul.Game.ResourceCatalog"/> stands in for one today.</summary>
    public readonly struct DeliveryItemDef
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int Price;
        public readonly DeliveryCurrency Currency;
        public readonly int PurchaseQuantity;

        public DeliveryItemDef(string id, string displayName, string description, int price,
            DeliveryCurrency currency, int purchaseQuantity)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Price = price;
            Currency = currency;
            PurchaseQuantity = purchaseQuantity;
        }
    }

    /// <summary>Scene-authored catalog for the Car Delivery menu's buy list.</summary>
    public static class CarDeliveryCatalog
    {
        public static readonly DeliveryItemDef[] Items =
        {
            new("tire", "Tires", "Every car needs a solid set.", 4500, DeliveryCurrency.Cash, 100),
            new("oil", "Oil", "Keeps engines running smooth.", 3200, DeliveryCurrency.Cash, 100),
            new("battery", "Battery", "Power up every ride.", 5000, DeliveryCurrency.Cash, 100),
            new("paint", "Paint", "Make 'em look brand new.", 4200, DeliveryCurrency.Cash, 100),
            new("crate", "Parts Crate", "A mix of random parts.", 40, DeliveryCurrency.Gold, 1),
        };

        public static bool TryFind(string id, out DeliveryItemDef def)
        {
            foreach (var item in Items)
            {
                if (item.Id != id) continue;
                def = item;
                return true;
            }
            def = default;
            return false;
        }
    }

    /// <summary>One Car Delivery slot: a running production timer producing <see cref="Quantity"/>
    /// of <see cref="ItemId"/> every <see cref="DurationSeconds"/>, or a locked placeholder
    /// awaiting <see cref="UnlockRequirementLevel"/>/a slot purchase.</summary>
    [Serializable]
    public sealed class DeliverySlotState
    {
        public bool Unlocked;
        public string ItemId = "";
        public int Quantity;
        public float DurationSeconds = 60f;
        public float ElapsedSeconds;
        public bool Running;
        public int UnlockRequirementLevel;

        public float Progress01 => DurationSeconds > 0f ? Clamp01(ElapsedSeconds / DurationSeconds) : 0f;
        public bool IsComplete => Running && ElapsedSeconds >= DurationSeconds;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>
    /// Pure delivery-slot/economy rules, unit-testable without a MonoBehaviour or scene -
    /// the same "engine-agnostic core, thin MonoBehaviour shell" split already used by
    /// <see cref="EconomyFormulas"/> and <see cref="StackInventory"/>.
    /// </summary>
    public static class CarDeliveryLogic
    {
        public static bool TryPurchase(long balance, int price, out long newBalance)
        {
            if (balance < price) { newBalance = balance; return false; }
            newBalance = balance - price;
            return true;
        }

        public static bool CanStart(DeliverySlotState slot)
            => slot != null && slot.Unlocked && !slot.Running && !string.IsNullOrEmpty(slot.ItemId);

        public static void Start(DeliverySlotState slot)
        {
            if (!CanStart(slot)) return;
            slot.Running = true;
            slot.ElapsedSeconds = 0f;
        }

        public static void Tick(DeliverySlotState slot, float deltaSeconds)
        {
            if (slot == null || !slot.Running || deltaSeconds <= 0f) return;
            slot.ElapsedSeconds = Math.Min(slot.DurationSeconds, slot.ElapsedSeconds + deltaSeconds);
        }

        /// <summary>Instantly finishes a running slot's timer (the gold fast-forward action).</summary>
        public static void Finish(DeliverySlotState slot)
        {
            if (slot == null || !slot.Running) return;
            slot.ElapsedSeconds = slot.DurationSeconds;
        }

        /// <summary>Collects a completed slot's output. Returns the quantity granted (0 if not ready).
        /// The slot keeps producing: callers should <see cref="Start"/> it again after collecting.</summary>
        public static int Collect(DeliverySlotState slot)
        {
            if (slot == null || !slot.IsComplete) return 0;
            int qty = slot.Quantity;
            slot.Running = false;
            slot.ElapsedSeconds = 0f;
            return qty;
        }
    }
}

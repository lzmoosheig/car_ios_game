using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-authored item definitions: id -> slot cost, display name, stack colour and, for
    /// the slot-based inventory system, max stack size + item category + optional icon sprite.
    /// Injected into carriers so the engine-agnostic <see cref="Overhaul.Core.StackInventory"/>
    /// stays data-driven, read by UI for readable item names ("Tires", not "tire"), and used as
    /// the <see cref="IItemDatabase"/> that feeds every <see cref="SlotInventory"/>.
    /// Placeholder for the ScriptableObject ResourceDefinition catalog (Doc 06 §4.1).
    /// </summary>
    public sealed class ResourceCatalog : MonoBehaviour, IItemDatabase
    {
        [System.Serializable]
        public struct Entry
        {
            public string id;
            public int slots;
            public string displayName;
            public Color color;
            [Tooltip("Max units of this item per slot. 0 = use the catalog's Default Max Stack.")]
            public int maxStack;
            [Tooltip("Item family, used for worker-role / building slot rules.")]
            public ItemCategory category;
            [Tooltip("Optional UI icon. Falls back to a coloured swatch when unset.")]
            public Sprite icon;
        }

        /// <summary>
        /// The planned item set (Doc 09 §5.1). Only "tire" flows in the first slice, but the
        /// catalog ships the full list so stations, recipes and UI can reference items
        /// without a schema change later.
        /// </summary>
        public static readonly Entry[] DefaultItems =
        {
            // maxStack = 0 -> inherit the catalog-wide DefaultMaxStack (100). Set a positive
            // value on an individual item to give it a tighter cap (e.g. bulky engines).
            new() { id = "tire",     slots = 1, displayName = "Tires",            color = new Color(0.16f, 0.16f, 0.18f), maxStack = 0, category = ItemCategory.Part },
            new() { id = "oil",      slots = 1, displayName = "Oil",              color = new Color(0.85f, 0.65f, 0.13f), maxStack = 0, category = ItemCategory.Consumable },
            new() { id = "brakes",   slots = 1, displayName = "Brake Kits",       color = new Color(0.75f, 0.22f, 0.17f), maxStack = 0, category = ItemCategory.Part },
            new() { id = "engine",   slots = 2, displayName = "Engine Parts",     color = new Color(0.55f, 0.57f, 0.62f), maxStack = 0, category = ItemCategory.Part },
            new() { id = "paint",    slots = 1, displayName = "Paint Supplies",   color = new Color(0.20f, 0.45f, 0.85f), maxStack = 0, category = ItemCategory.Consumable },
            new() { id = "cleaning", slots = 1, displayName = "Cleaning Supplies",color = new Color(0.30f, 0.75f, 0.85f), maxStack = 0, category = ItemCategory.Consumable },
            new() { id = "panels",   slots = 2, displayName = "Body Panels",      color = new Color(0.80f, 0.80f, 0.84f), maxStack = 0, category = ItemCategory.Part },
            new() { id = "battery",  slots = 1, displayName = "Batteries",        color = new Color(0.20f, 0.70f, 0.35f), maxStack = 0, category = ItemCategory.Part },
        };

        [Tooltip("Stack ceiling used by any item whose own Max Stack is 0. This is the 'pile size' - how many identical items fit in one slot.")]
        [SerializeField] private int defaultMaxStack = 100;

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<string, Entry> _byId;

        private void Awake() => Build();

        public void Build()
        {
            _byId = new Dictionary<string, Entry>();
            foreach (var e in entries) _byId[e.id] = e;
        }

        /// <summary>Fills the catalog with the standard item set (idempotent).</summary>
        public void SeedDefaults()
        {
            foreach (var item in DefaultItems)
            {
                bool exists = false;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].id != item.id) continue;
                    entries[i] = item; // refresh stale data (e.g. entries added before names existed)
                    exists = true;
                    break;
                }
                if (!exists) entries.Add(item);
            }
            Build();
        }

        public int SlotsOf(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out var e) ? Mathf.Max(1, e.slots) : 1;
        }

        public string NameOf(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out var e) && !string.IsNullOrEmpty(e.displayName)
                ? e.displayName : id;
        }

        public Color ColorOf(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out var e) && e.color.a > 0f ? e.color : Color.gray;
        }

        /// <summary>How many identical items fit in one slot: the item's own cap, or the
        /// catalog default when it declares none. Unknown items also use the default.</summary>
        public int MaxStackOf(string id)
        {
            if (_byId == null) Build();
            int fallback = Mathf.Max(1, defaultMaxStack);
            return _byId.TryGetValue(id, out var e) && e.maxStack > 0 ? e.maxStack : fallback;
        }

        public ItemCategory CategoryOf(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out var e) ? e.category : ItemCategory.Misc;
        }

        public Sprite IconOf(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out var e) ? e.icon : null;
        }

        /// <summary>
        /// <see cref="IItemDatabase"/> entry point: hands the engine-agnostic inventory model a
        /// full <see cref="ItemDefinition"/>. Unknown ids resolve to a sensible single-stack
        /// default so a missing catalog entry never breaks a transfer.
        /// </summary>
        public bool TryGet(string itemId, out ItemDefinition definition)
        {
            if (_byId == null) Build();
            if (_byId.TryGetValue(itemId, out var e))
            {
                definition = new ItemDefinition(e.id, e.displayName, MaxStackOf(e.id), e.category, e.id);
                return true;
            }
            definition = null;
            return false;
        }

        public void AddEntry(string id, int slots)
        {
            entries.Add(new Entry { id = id, slots = slots, displayName = id, color = Color.gray, maxStack = 0, category = ItemCategory.Misc });
            if (_byId != null) Build();
        }

        private static ResourceCatalog _instance;

        /// <summary>Scene-wide lookup for UI code; null-safe fallbacks keep missing catalogs harmless.</summary>
        public static ResourceCatalog Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<ResourceCatalog>();
                return _instance;
            }
        }

        public static string DisplayName(string id) => Instance != null ? Instance.NameOf(id) : id;
    }
}

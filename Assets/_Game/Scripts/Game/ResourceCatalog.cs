using System.Collections.Generic;
using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-authored item definitions: id -> slot cost, display name, stack color.
    /// Injected into carriers so the engine-agnostic <see cref="Overhaul.Core.StackInventory"/>
    /// stays data-driven, and read by UI for readable item names ("Tires", not "tire").
    /// Placeholder for the ScriptableObject ResourceDefinition catalog (Doc 06 §4.1).
    /// </summary>
    public sealed class ResourceCatalog : MonoBehaviour
    {
        [System.Serializable]
        public struct Entry
        {
            public string id;
            public int slots;
            public string displayName;
            public Color color;
        }

        /// <summary>
        /// The planned item set (Doc 09 §5.1). Only "tire" flows in the first slice, but the
        /// catalog ships the full list so stations, recipes and UI can reference items
        /// without a schema change later.
        /// </summary>
        public static readonly Entry[] DefaultItems =
        {
            new() { id = "tire",     slots = 1, displayName = "Tires",            color = new Color(0.16f, 0.16f, 0.18f) },
            new() { id = "oil",      slots = 1, displayName = "Oil",              color = new Color(0.85f, 0.65f, 0.13f) },
            new() { id = "brakes",   slots = 1, displayName = "Brake Kits",       color = new Color(0.75f, 0.22f, 0.17f) },
            new() { id = "engine",   slots = 2, displayName = "Engine Parts",     color = new Color(0.55f, 0.57f, 0.62f) },
            new() { id = "paint",    slots = 1, displayName = "Paint Supplies",   color = new Color(0.20f, 0.45f, 0.85f) },
            new() { id = "cleaning", slots = 1, displayName = "Cleaning Supplies",color = new Color(0.30f, 0.75f, 0.85f) },
            new() { id = "panels",   slots = 2, displayName = "Body Panels",      color = new Color(0.80f, 0.80f, 0.84f) },
        };

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

        public void AddEntry(string id, int slots)
        {
            entries.Add(new Entry { id = id, slots = slots, displayName = id, color = Color.gray });
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

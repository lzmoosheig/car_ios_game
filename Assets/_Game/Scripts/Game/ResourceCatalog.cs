using System.Collections.Generic;
using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-authored lookup of resource id -> slot cost, injected into carriers so the
    /// engine-agnostic <see cref="Overhaul.Core.StackInventory"/> stays data-driven.
    /// Placeholder for the ScriptableObject ResourceDefinition catalog (Doc 06 §4.1).
    /// </summary>
    public sealed class ResourceCatalog : MonoBehaviour
    {
        [System.Serializable]
        public struct Entry
        {
            public string id;
            public int slots;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<string, int> _slots;

        private void Awake() => Build();

        public void Build()
        {
            _slots = new Dictionary<string, int>();
            foreach (var e in entries) _slots[e.id] = Mathf.Max(1, e.slots);
        }

        public int SlotsOf(string id)
        {
            if (_slots == null) Build();
            return _slots.TryGetValue(id, out var s) ? s : 1;
        }

        public void AddEntry(string id, int slots)
        {
            entries.Add(new Entry { id = id, slots = slots });
            if (_slots != null) _slots[id] = Mathf.Max(1, slots);
        }
    }
}

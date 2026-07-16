using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-side icon lookup for Car Delivery items, wired by the editor build tool.
    /// Placeholder until item sprites move into a full ScriptableObject catalog -
    /// mirrors the stand-in role <see cref="ResourceCatalog"/> already plays for names/colors.
    /// </summary>
    public sealed class CarDeliveryIcons : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> entries = new();
        private Dictionary<string, Sprite> _byId;

        public static CarDeliveryIcons Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(List<Entry> icons)
        {
            entries = icons ?? new List<Entry>();
            Build();
        }

        private void Build()
        {
            _byId = new Dictionary<string, Sprite>();
            foreach (var e in entries) _byId[e.id] = e.sprite;
        }

        public Sprite Get(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out var s) ? s : null;
        }
    }
}

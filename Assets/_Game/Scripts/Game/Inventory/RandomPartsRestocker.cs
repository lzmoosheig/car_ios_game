using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Continuously trickles random items into an <see cref="InventoryComponent"/> - the parts
    /// delivery worker's stock topping itself up over time. The pool defaults to every
    /// <see cref="ItemCategory.Part"/> item in the catalog, but can be set explicitly. Adds are
    /// partial-safe, so once the inventory is full the extra simply doesn't fit.
    /// </summary>
    [RequireComponent(typeof(InventoryComponent))]
    public sealed class RandomPartsRestocker : MonoBehaviour
    {
        [SerializeField] private InventoryComponent target;

        [Tooltip("Item ids to draw from. Empty = every Part-category item in the catalog.")]
        [SerializeField] private List<string> itemPool = new();

        [Header("Cadence")]
        [SerializeField] private float minInterval = 1.5f;
        [SerializeField] private float maxInterval = 4f;
        [SerializeField] private int minBatch = 1;
        [SerializeField] private int maxBatch = 6;

        [Header("Startup")]
        [Tooltip("How many add-bursts to pre-fill on Start, so he isn't empty when first met.")]
        [SerializeField] private int initialBursts = 4;

        private readonly System.Random _rng = new();
        private float _timer;
        private float _next;

        private void Awake()
        {
            if (target == null) target = GetComponent<InventoryComponent>();
        }

        private void Start()
        {
            EnsurePool();
            for (int i = 0; i < initialBursts; i++) AddRandom();
            _next = RollInterval();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < _next) return;
            _timer = 0f;
            _next = RollInterval();
            AddRandom();
        }

        private float RollInterval() => Mathf.Lerp(minInterval, maxInterval, (float)_rng.NextDouble());

        private void EnsurePool()
        {
            if (itemPool != null && itemPool.Count > 0) return;
            itemPool = new List<string>();
            foreach (var e in ResourceCatalog.DefaultItems)
                if (e.category == ItemCategory.Part) itemPool.Add(e.id);
            if (itemPool.Count == 0) itemPool.Add("tire");
        }

        /// <summary>Adds one random batch of one random item (respecting capacity).</summary>
        public void AddRandom()
        {
            if (target == null || itemPool == null || itemPool.Count == 0) return;
            string id = itemPool[_rng.Next(itemPool.Count)];
            int n = _rng.Next(minBatch, maxBatch + 1);
            target.Add(id, n);
        }
    }
}

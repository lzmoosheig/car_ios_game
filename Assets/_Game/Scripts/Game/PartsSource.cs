using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// The delivery pallet: holds a stock of one resource that a Collect
    /// <see cref="InteractionZone"/> lets the player scoop up. Slowly replenishes so
    /// the graybox never dead-ends (Doc 04 §4: ~1 part / 2 s, capped). A stand-in for
    /// the full Parts Delivery Zone.
    /// </summary>
    public sealed class PartsSource : MonoBehaviour
    {
        [SerializeField] private string resourceId = "tire";
        [SerializeField] private int capacity = 12;
        [SerializeField] private float produceInterval = 2f;

        private int _stock;
        private float _timer;

        public string ResourceId => resourceId;
        public int Stock => _stock;

        public void Configure(string id, int cap, float interval)
        {
            resourceId = id;
            capacity = cap;
            produceInterval = interval;
            _stock = cap;
        }

        private void Start()
        {
            if (_stock == 0) _stock = capacity;
        }

        private void Update()
        {
            if (_stock >= capacity) return;
            _timer += Time.deltaTime;
            if (_timer >= produceInterval)
            {
                _timer = 0f;
                _stock = Mathf.Min(capacity, _stock + 1);
            }
        }

        public bool HasSupply(string id) => id == resourceId && _stock > 0;

        public void Take(string id)
        {
            if (id == resourceId && _stock > 0) _stock--;
        }
    }
}

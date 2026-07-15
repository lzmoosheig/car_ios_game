using System.Collections.Generic;
using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// A per-station input buffer keyed by resource id (Doc 02 §5.2, "buffer racks
    /// beside every consumer"). Thin scene component; capacity only ever grows.
    /// </summary>
    public sealed class ResourceRack : MonoBehaviour
    {
        [SerializeField] private int capacityPerType = 8;

        private readonly Dictionary<string, int> _counts = new();

        public int CountOf(string id) => _counts.TryGetValue(id, out var n) ? n : 0;
        public bool CanAdd(string id) => CountOf(id) < capacityPerType;

        public bool Add(string id)
        {
            if (!CanAdd(id)) return false;
            _counts[id] = CountOf(id) + 1;
            return true;
        }

        public int Remove(string id, int count)
        {
            int have = CountOf(id);
            int take = Mathf.Min(have, count);
            if (take > 0) _counts[id] = have - take;
            return take;
        }

        public void SetCapacity(int c)
        {
            if (c > capacityPerType) capacityPerType = c;
        }
    }
}

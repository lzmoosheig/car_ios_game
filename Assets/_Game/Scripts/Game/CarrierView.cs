using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Unity bridge over the engine-agnostic <see cref="StackInventory"/>. This single
    /// component is shared by the player and every employee (Doc 06 §3, "Carrier
    /// abstraction") so collect/deposit logic has one code path. The MonoBehaviour owns
    /// only the visuals (the wobbling item tower) and the scene-side slot lookup; all
    /// rules live in Core and are unit-tested outside the editor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarrierView : MonoBehaviour
    {
        [Tooltip("Anchor where the visible item tower is built (usually above the character's back).")]
        [SerializeField] private Transform stackAnchor;
        [SerializeField] private float itemHeight = 0.35f;
        [SerializeField] private float itemScale = 0.7f;
        [SerializeField] private Vector3 itemRotation = new(0f, 0f, 90f);
        [SerializeField] private int baseCapacitySlots = 5;

        // Resolves a resource id to its slot cost. Wired from the resource catalog at spawn.
        // Defaults to 1 slot until the catalog injects real values.
        public System.Func<string, int> SlotsOf = _ => 1;

        private StackInventory _stack;
        private readonly List<GameObject> _visuals = new();

        // Lazy so the carrier works in EditMode tests (no Awake) as well as in play.
        public StackInventory Stack => _stack ??= new StackInventory(baseCapacitySlots, id => SlotsOf(id));

        /// <summary>Called by an InteractionZone tick while standing on a source.</summary>
        public bool TryCollect(string resourceId, GameObject itemPrefab)
        {
            if (!Stack.TryAdd(resourceId)) return false;
            SpawnVisual(itemPrefab);
            return true;
        }

        /// <summary>Called by an InteractionZone tick while standing on a consumer.</summary>
        public int Deposit(string resourceId, int max)
        {
            int removed = Stack.Remove(resourceId, max);
            for (int i = 0; i < removed && _visuals.Count > 0; i++) DespawnTopVisual();
            return removed;
        }

        /// <summary>Applied when a permanent carry-capacity upgrade is purchased (Doc 04 §3).</summary>
        public void SetCapacity(int slots) => Stack.SetCapacity(slots);

        public void ConfigureVisualLayout(float spacing, float scale, Vector3 rotation)
        {
            itemHeight = Mathf.Max(0.05f, spacing);
            itemScale = Mathf.Max(0.05f, scale);
            itemRotation = rotation;
        }

        private void SpawnVisual(GameObject prefab)
        {
            if (stackAnchor == null || prefab == null) return;
            var go = Instantiate(prefab, stackAnchor);
            go.SetActive(true); // template may be inactive
            go.transform.localPosition = Vector3.up * (itemHeight * _visuals.Count);
            go.transform.localRotation = Quaternion.Euler(itemRotation);
            go.transform.localScale = Vector3.one * itemScale;
            _visuals.Add(go);
        }

        private void DespawnTopVisual()
        {
            int last = _visuals.Count - 1;
            var go = _visuals[last];
            _visuals.RemoveAt(last);
            if (go != null) Destroy(go); // pooling replaces this in the perf pass (Doc 06 §3)
        }
    }
}

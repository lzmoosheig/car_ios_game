using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Turns finished construction into gameplay effects, so every zone the player funds
    /// immediately changes how the village runs (Doc 02 §5.3: "new mechanic, new capacity,
    /// or new revenue — visible within 30 seconds").
    ///
    /// Effects are re-applied in Start from each zone's <c>Built</c> flag rather than only
    /// from the event, because SaveManager restores zones during Awake — before anything
    /// here could have subscribed.
    /// </summary>
    public sealed class VillageUnlocks : MonoBehaviour
    {
        [SerializeField] private VillageController village;

        [Header("Queue expansion")]
        [SerializeField] private ConstructionZoneView queueZone;
        [SerializeField] private int queueSlotsWhenBuilt = 4;

        [Header("All zones (drives arrival rate: demand grows with the village)")]
        [SerializeField] private ConstructionZoneView[] allZones;

        public void Configure(VillageController v, ConstructionZoneView queue, ConstructionZoneView[] zones)
        {
            village = v;
            queueZone = queue;
            allZones = zones;
        }

        private void Start()
        {
            if (queueZone != null) queueZone.ZoneBuilt += OnQueueBuilt;
            if (allZones != null)
                foreach (var z in allZones)
                    if (z != null) z.ZoneBuilt += OnAnyBuilt;

            ApplyAll(); // covers both a fresh scene and a restored save
        }

        private void OnDestroy()
        {
            if (queueZone != null) queueZone.ZoneBuilt -= OnQueueBuilt;
            if (allZones != null)
                foreach (var z in allZones)
                    if (z != null) z.ZoneBuilt -= OnAnyBuilt;
        }

        private void OnQueueBuilt(string _) => ApplyQueue();
        private void OnAnyBuilt(string _) => ApplyZoneCount();

        private void ApplyAll()
        {
            ApplyQueue();
            ApplyZoneCount();
        }

        private void ApplyQueue()
        {
            if (village == null || queueZone == null || !queueZone.Built) return;
            village.SetQueueSlotCount(queueSlotsWhenBuilt);
        }

        private void ApplyZoneCount()
        {
            if (village == null || allZones == null) return;
            int built = 0;
            foreach (var z in allZones) if (z != null && z.Built) built++;
            village.SetZonesBuilt(built);
        }
    }
}

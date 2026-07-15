using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Contextual world markers for the two Phase A bottlenecks. They stay hidden during
    /// healthy operation and use no canvas/layout work while inactive.
    /// </summary>
    public sealed class WorldBottleneckCues : MonoBehaviour
    {
        [SerializeField] private ServiceBay bay;
        [SerializeField] private VillageController village;
        [SerializeField] private GameObject bayStarvedCue;
        [SerializeField] private GameObject queueFullCue;
        [SerializeField] private float pulseAmount = 0.08f;
        [SerializeField] private float pulseSpeed = 3f;

        private Vector3 _bayScale = Vector3.one;
        private Vector3 _queueScale = Vector3.one;
        private float _refreshTimer;

        public bool BayCueVisible => bayStarvedCue != null && bayStarvedCue.activeSelf;
        public bool QueueCueVisible => queueFullCue != null && queueFullCue.activeSelf;

        public void Configure(ServiceBay serviceBay, VillageController controller,
                              GameObject starvedCue, GameObject fullCue)
        {
            bay = serviceBay;
            village = controller;
            bayStarvedCue = starvedCue;
            queueFullCue = fullCue;
            if (bayStarvedCue != null) _bayScale = bayStarvedCue.transform.localScale;
            if (queueFullCue != null) _queueScale = queueFullCue.transform.localScale;
            RefreshNow();
        }

        private void Update()
        {
            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer >= 0.2f)
            {
                _refreshTimer = 0f;
                RefreshNow();
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            AnimateCue(bayStarvedCue, _bayScale, pulse);
            AnimateCue(queueFullCue, _queueScale, pulse);
        }

        public void RefreshNow()
        {
            if (bayStarvedCue != null)
                bayStarvedCue.SetActive(bay != null && bay.State == WorkstationState.Starved);
            if (queueFullCue != null)
                queueFullCue.SetActive(village != null && village.QueueSlotCount > 0 &&
                                       village.QueueOccupancy >= village.QueueSlotCount);
        }

        private static void AnimateCue(GameObject cue, Vector3 baseScale, float pulse)
        {
            if (cue == null || !cue.activeSelf) return;
            cue.transform.localScale = baseScale * pulse;
            var camera = Camera.main;
            if (camera != null)
                cue.transform.rotation = Quaternion.LookRotation(cue.transform.position - camera.transform.position,
                                                                 camera.transform.up);
        }
    }
}

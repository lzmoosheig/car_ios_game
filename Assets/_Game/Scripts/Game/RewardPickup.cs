using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// The physical payout: spawned when a job's service finishes, collected by walking
    /// through it (loop step 10 - collecting cash and reputation is an act in the world,
    /// not an automatic deposit). Spins and bobs so it is unmissable from the elevated view.
    /// </summary>
    public sealed class RewardPickup : MonoBehaviour
    {
        [SerializeField] private float radius = 1.6f;

        private VillageController _village;
        private ServiceJob _job;
        private Transform _visual;
        private float _baseY;

        public ServiceJob Job => _job;

        public void Configure(VillageController village, ServiceJob job)
        {
            _village = village;
            _job = job;

            var col = gameObject.GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;

            _visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            _baseY = _visual != null ? _visual.localPosition.y : 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_job == null || _job.State != JobState.ReadyToCollect) return;
            var carrier = other.GetComponentInParent<CarrierView>();
            if (carrier == null) return;
            // Only the player banks rewards; employees hauling past must not swallow them.
            if (carrier.GetComponentInParent<PlayerController>() == null) return;

            _village?.CollectJob(_job);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (_visual == null) return;
            _visual.Rotate(0f, 120f * Time.deltaTime, 0f, Space.Self);
            var p = _visual.localPosition;
            p.y = _baseY + Mathf.Abs(Mathf.Sin(Time.time * 2f)) * 0.25f;
            _visual.localPosition = p;
        }
    }
}

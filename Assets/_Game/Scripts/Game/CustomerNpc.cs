using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// The visible person attached to a customer job. Spawns at reception when their car
    /// checks in, offers "Accept job" when tapped (the first NPC interaction of Doc 09
    /// §3.3), waits while their car is serviced, and leaves when the job is collected.
    /// Not a dialogue system - one panel, one or two actions.
    /// </summary>
    public sealed class CustomerNpc : MonoBehaviour, IInteractable
    {
        private VillageController _village;
        private ServiceJob _job;
        private Transform _visual;

        public string CustomerId { get; private set; }
        public string Title => "Customer";
        public Transform PivotTransform => transform;

        public void Configure(VillageController village, ServiceJob job)
        {
            _village = village;
            _job = job;
            CustomerId = job.CustomerId;
            _visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        public ServiceJob Job => _job;

        public void OnSelected() => SelectionRing.Show(transform, 2.2f);

        public void OnDeselected() => SelectionRing.Hide();

        public void GetInfoLines(List<string> into)
        {
            if (_job == null) return;
            string item = ResourceCatalog.DisplayName(_job.RequiredResourceId);
            into.Add($"Wants: {ServiceName(_job.Kind)}");
            into.Add($"Uses: {_job.RequiredCount}x {item}");
            into.Add(_job.Describe());
        }

        public void GetActions(List<InteractableAction> into)
        {
            if (_job == null || _village == null) return;

            if (_job.State == JobState.Offered)
                into.Add(new InteractableAction("Accept job", () => _village.AcceptJob(_job)));

            into.Add(new InteractableAction("Hint", ShowHint));
        }

        private void ShowHint()
        {
            if (_job == null) return;
            string item = ResourceCatalog.DisplayName(_job.RequiredResourceId);
            ScreenToast.Show(_job.State switch
            {
                JobState.Offered => "Accept the job, then stock the bay.",
                JobState.Accepted => $"Carry {item} from Parts Delivery to the bay.",
                JobState.InService => "The mechanic is on it — wait for the timer.",
                JobState.ReadyToCollect => "Walk through the glowing pay marker!",
                _ => "All done here."
            });
        }

        private void Update()
        {
            // Small idle bob so the person reads as alive without an animator.
            if (_visual == null) return;
            var p = _visual.localPosition;
            p.y = Mathf.Abs(Mathf.Sin(Time.time * 2.2f)) * 0.06f;
            _visual.localPosition = p;
        }

        public static string ServiceName(ServiceKind kind) => kind switch
        {
            ServiceKind.BasicRepair => "Part repair",
            ServiceKind.TireChange => "Tire change",
            ServiceKind.OilChange => "Oil change",
            ServiceKind.Wash => "Car wash",
            _ => kind.ToString()
        };
    }
}

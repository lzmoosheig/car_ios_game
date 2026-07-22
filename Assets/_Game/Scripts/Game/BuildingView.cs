using System.Collections.Generic;
using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    public enum BuildingCueTone { Normal, Attention, Critical, Positive }

    /// <summary>
    /// Makes a station lot clickable (Doc 09 §3.3): tap the building to see its status,
    /// required inputs, stock, active job and progress in the shared panel. Wired by the
    /// editor setup to whichever gameplay parts the station actually has - any of the
    /// references may be null (a showroom has no rack; an unbuilt lot has only a zone).
    ///
    /// The panel informs and configures; it never teleports items (master-plan rule).
    /// </summary>
    public sealed class BuildingView : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        [SerializeField] private string buildingName = "Building";
        [SerializeField] private string statusWhenIdle = "Operational";

        [Header("Optional gameplay parts")]
        [SerializeField] private ServiceBay bay;
        [SerializeField] private ResourceRack rack;
        [SerializeField] private PartsSource source;
        [SerializeField] private ConstructionZoneView zone;
        [SerializeField] private string inputResourceId = "";
        [SerializeField] private int inputCountPerJob;

        private VillageController _village;

        public string Title => buildingName;
        public Transform PivotTransform => transform;
        public string BuildingName => buildingName;

        /// <summary>The work bay this lot presents, if any. Lets the village find the bay's
        /// clickable building so its world cue can track the current car's required part.</summary>
        public ServiceBay Bay => bay;

        /// <summary>
        /// Points the building's status cue at the part the car currently in the bay needs
        /// (Doc 09 §3.3 "make the requirement obvious"). Pass count 0 to go back to idle so
        /// an empty bay never nags for a part no car is waiting on.
        /// </summary>
        public void SetActiveRequirement(string resourceId, int count)
        {
            inputResourceId = resourceId ?? "";
            inputCountPerJob = Mathf.Max(0, count);
        }

        public void Configure(string name, ServiceBay b, ResourceRack r, PartsSource s,
                              ConstructionZoneView z, string inputResource, int inputCount)
        {
            buildingName = name;
            bay = b;
            rack = r;
            source = s;
            zone = z;
            inputResourceId = inputResource ?? "";
            inputCountPerJob = inputCount;
        }

        private void Awake() => _village = FindFirstObjectByType<VillageController>();

        public void OnSelected() => SelectionRing.Show(transform, 7.8f);

        public void OnDeselected() => SelectionRing.Hide();

        public void GetInfoLines(List<string> into)
        {
            // Unbuilt: the only story is the construction state.
            if (zone != null && !zone.Built)
            {
                into.Add("Status: Not built yet");
                into.Add($"Construction: ${zone.Funded} / ${zone.Funded + zone.Remaining}");
                into.Add("Stand in the blueprint to fund it.");
                return;
            }

            into.Add("Status: " + StatusLine());

            if (!string.IsNullOrEmpty(inputResourceId))
            {
                string item = ResourceCatalog.DisplayName(inputResourceId);
                if (inputCountPerJob > 0) into.Add($"Needs: {inputCountPerJob}x {item} per job");
                if (rack != null) into.Add($"Stock: {rack.CountOf(inputResourceId)} {item}");
            }

            if (source != null)
                into.Add($"Supply: {source.Stock} {ResourceCatalog.DisplayName(source.ResourceId)} ready");

            if (bay != null && bay.State == WorkstationState.Working)
                into.Add($"Progress: {Mathf.RoundToInt(bay.Progress * 100f)}%");

            var job = _village != null ? _village.ActiveJob : null;
            if (bay != null && job != null && job.State == JobState.InService)
                into.Add($"Job: {job.Describe()}");
        }

        public void GetActions(List<InteractableAction> into)
        {
            // Reception is the job counter: accepting the offered job lives here as well as
            // on the customer, so either tap works.
            if (_village != null && buildingName.StartsWith("Reception"))
            {
                var job = _village.ActiveJob;
                if (job != null && job.State == JobState.Offered)
                    into.Add(new InteractableAction("Accept job", () => _village.AcceptActiveJob()));
            }
        }

        public void GetWorldCue(out string detail, out BuildingCueTone tone)
        {
            tone = BuildingCueTone.Normal;

            if (zone != null && !zone.Built)
            {
                detail = $"BUILD  ${zone.Remaining:N0}";
                tone = BuildingCueTone.Attention;
                return;
            }

            if (bay != null)
            {
                if (bay.State == WorkstationState.Starved)
                {
                    detail = inputCountPerJob > 0
                        ? $"NEED {inputCountPerJob} {ResourceCatalog.DisplayName(inputResourceId).ToUpperInvariant()}"
                        : "NEEDS SUPPLIES";
                    tone = BuildingCueTone.Critical;
                    return;
                }
                if (bay.State == WorkstationState.Working)
                {
                    detail = $"WORKING  {Mathf.RoundToInt(bay.Progress * 100f)}%";
                    tone = BuildingCueTone.Positive;
                    return;
                }
            }

            if (!string.IsNullOrEmpty(inputResourceId) && rack != null && inputCountPerJob > 0)
            {
                int stock = rack.CountOf(inputResourceId);
                if (stock < inputCountPerJob)
                {
                    detail = $"NEED {inputCountPerJob - stock} {ResourceCatalog.DisplayName(inputResourceId).ToUpperInvariant()}";
                    tone = BuildingCueTone.Attention;
                }
                else
                {
                    detail = $"STOCK {stock}  READY";
                    tone = BuildingCueTone.Positive;
                }
                return;
            }

            if (source != null)
            {
                detail = source.Stock > 0
                    ? $"{source.Stock} {ResourceCatalog.DisplayName(source.ResourceId).ToUpperInvariant()} READY"
                    : "SUPPLY EMPTY";
                tone = source.Stock > 0 ? BuildingCueTone.Positive : BuildingCueTone.Critical;
                return;
            }

            if (_village != null && buildingName.IndexOf("Queue", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                detail = $"{_village.QueueOccupancy}/{_village.QueueSlotCount} CARS WAITING";
                tone = _village.QueueSlotCount > 0 && _village.QueueOccupancy >= _village.QueueSlotCount
                    ? BuildingCueTone.Critical : BuildingCueTone.Normal;
                return;
            }

            if (_village != null && buildingName.IndexOf("Reception", System.StringComparison.OrdinalIgnoreCase) >= 0
                && _village.ActiveJob != null && _village.ActiveJob.State == JobState.Offered)
            {
                detail = "NEW JOB READY";
                tone = BuildingCueTone.Attention;
                return;
            }

            detail = StatusLine().ToUpperInvariant();
        }

        private string StatusLine()
        {
            if (bay == null) return statusWhenIdle;
            return bay.State switch
            {
                WorkstationState.Idle => "Waiting for a car",
                WorkstationState.Starved => $"Out of {ResourceCatalog.DisplayName(inputResourceId)}!",
                WorkstationState.Ready => "Starting service",
                WorkstationState.Working => "Servicing",
                WorkstationState.Done => "Finishing up",
                WorkstationState.Blocked => "Exit blocked",
                _ => statusWhenIdle
            };
        }
    }
}

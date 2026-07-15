using System;
using System.Collections.Generic;

namespace Overhaul.Core
{
    public enum TaskType { Haul, Service, Checkout, MoveVehicle, Sell }

    /// <summary>An open unit of work employees can claim. See Doc 02 §4.2.</summary>
    public sealed class WorkTask
    {
        public string Id;
        public TaskType Type;
        public string ZoneId;       // employees only claim tasks in their home zone
        public float Urgency;       // data-driven per task type
        public float CreatedAtTime; // used to compute starvation age
        public string ClaimedBy;    // null => open

        public bool IsOpen => ClaimedBy == null;
    }

    /// <summary>
    /// Central registry of open work with atomic claim/release. Single-threaded
    /// (Unity main thread), so "atomic" means check-then-set is not interleaved.
    /// The employee utility scorer lives here as a static helper. See Doc 06 §3.
    /// </summary>
    public sealed class TaskBoard
    {
        private readonly Dictionary<string, WorkTask> _tasks = new();

        public IReadOnlyDictionary<string, WorkTask> Tasks => _tasks;

        public WorkTask Post(string id, TaskType type, string zoneId, float urgency, float now)
        {
            var t = new WorkTask
            {
                Id = id, Type = type, ZoneId = zoneId,
                Urgency = urgency, CreatedAtTime = now, ClaimedBy = null
            };
            _tasks[id] = t;
            return t;
        }

        /// <summary>Returns true only for the first caller; later callers get false.</summary>
        public bool TryClaim(string taskId, string workerId)
        {
            if (!_tasks.TryGetValue(taskId, out var t)) return false;
            if (!t.IsOpen) return false;
            t.ClaimedBy = workerId;
            return true;
        }

        public void Release(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var t)) t.ClaimedBy = null;
        }

        public void Complete(string taskId) => _tasks.Remove(taskId);

        /// <summary>
        /// Utility score for choosing among open tasks (higher = more attractive):
        /// score = urgency * starvationSeconds / (1 + distanceMeters / 10). Doc 02 §4.2.
        /// </summary>
        public static float Score(float urgency, float starvationSeconds, float distanceMeters)
            => urgency * starvationSeconds / (1f + distanceMeters / 10f);
    }
}

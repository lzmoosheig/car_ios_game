using System;
using System.Collections.Generic;
using System.Text;

namespace Overhaul.Core
{
    /// <summary>One line of a service recipe: how many of a given part a job consumes.</summary>
    public readonly struct PartAmount
    {
        public readonly string Id;
        public readonly int Count;
        public PartAmount(string id, int count) { Id = id; Count = Math.Max(1, count); }
    }

    public enum JobState
    {
        Offered,        // customer is at reception waiting for the player to accept
        Accepted,       // player took the job; vehicle may be dispatched to a bay
        InService,      // vehicle is in the bay being worked on
        ReadyToCollect, // work done; cash + reputation pickup is waiting
        Collected       // rewards banked; job finished
    }

    /// <summary>
    /// One customer job from offer to payout (first-slice loop, Doc 09 §2.2). The job is
    /// the player-facing objective layer on top of a <see cref="ServiceTicket"/>: tickets
    /// describe demand, jobs describe the player's commitment to it.
    ///
    /// Transitions are strict — a wrong-order call returns false and changes nothing, so
    /// UI code can wire buttons without guarding every path. Engine-agnostic and covered
    /// by the dev/CoreTests runner.
    /// </summary>
    public sealed class ServiceJob
    {
        public string Id { get; }
        public string CustomerId { get; }
        public ServiceKind Kind { get; }

        /// <summary>Every part (and how many) this job consumes — a car may need a combination,
        /// e.g. 4 tires + 2 oil + 1 battery.</summary>
        public IReadOnlyList<PartAmount> Requirements { get; }

        /// <summary>The first required part — kept for simple single-part displays and legacy code.</summary>
        public string RequiredResourceId => Requirements.Count > 0 ? Requirements[0].Id : null;
        public int RequiredCount => Requirements.Count > 0 ? Requirements[0].Count : 0;

        public JobState State { get; private set; } = JobState.Offered;

        /// <summary>Set when service completes; 0 until then.</summary>
        public int CashReward { get; private set; }
        public int ReputationReward { get; private set; }

        /// <summary>Single-part job (legacy/simple).</summary>
        public ServiceJob(string id, string customerId, ServiceKind kind,
                          string requiredResourceId, int requiredCount)
            : this(id, customerId, kind, new[] { new PartAmount(requiredResourceId, requiredCount) }) { }

        /// <summary>Multi-part job: the car needs a combination of parts.</summary>
        public ServiceJob(string id, string customerId, ServiceKind kind,
                          IReadOnlyList<PartAmount> requirements)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            Kind = kind;
            Requirements = (requirements != null && requirements.Count > 0)
                ? new List<PartAmount>(requirements)
                : new List<PartAmount> { new PartAmount("tire", 4) };
        }

        /// <summary>Total parts across the whole recipe (drives price/reward scaling).</summary>
        public int TotalPartCount
        {
            get { int n = 0; foreach (var r in Requirements) n += r.Count; return n; }
        }

        /// <summary>Readable recipe, e.g. "4x Tires, 2x Oil". <paramref name="nameOf"/> maps ids to names.</summary>
        public string DescribeParts(Func<string, string> nameOf)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Requirements.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var r = Requirements[i];
                sb.Append(r.Count).Append("x ").Append(nameOf != null ? nameOf(r.Id) : r.Id);
            }
            return sb.ToString();
        }

        public bool Accept()
        {
            if (State != JobState.Offered) return false;
            State = JobState.Accepted;
            return true;
        }

        public bool StartService()
        {
            if (State != JobState.Accepted) return false;
            State = JobState.InService;
            return true;
        }

        /// <summary>Reputation scales gently with job value: 1 rep per $20, minimum 1.</summary>
        public bool CompleteService(int revenue)
        {
            if (State != JobState.InService) return false;
            CashReward = Math.Max(0, revenue);
            ReputationReward = Math.Max(1, CashReward / 20);
            State = JobState.ReadyToCollect;
            return true;
        }

        public bool Collect()
        {
            if (State != JobState.ReadyToCollect) return false;
            State = JobState.Collected;
            return true;
        }

        /// <summary>One-line status for panels and objectives.</summary>
        public string Describe() => State switch
        {
            JobState.Offered => "Waiting for you to accept",
            JobState.Accepted => "Waiting for a free bay",
            JobState.InService => "In service",
            JobState.ReadyToCollect => "Done — collect your pay!",
            _ => "Complete"
        };
    }
}

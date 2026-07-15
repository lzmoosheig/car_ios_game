using System;

namespace Overhaul.Core
{
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
        public string RequiredResourceId { get; }
        public int RequiredCount { get; }

        public JobState State { get; private set; } = JobState.Offered;

        /// <summary>Set when service completes; 0 until then.</summary>
        public int CashReward { get; private set; }
        public int ReputationReward { get; private set; }

        public ServiceJob(string id, string customerId, ServiceKind kind,
                          string requiredResourceId, int requiredCount)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
            Kind = kind;
            RequiredResourceId = requiredResourceId;
            RequiredCount = requiredCount;
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

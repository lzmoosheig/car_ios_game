using System;
using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>The services a customer can ask for. Extend as stations unlock (Doc 09 §6).</summary>
    public enum ServiceKind
    {
        BasicRepair,
        TireChange,
        OilChange,
        Wash
    }

    /// <summary>
    /// A check-in ticket produced by Reception: what the customer wants and how long they
    /// will happily wait. Payment is always positive; impatience only reduces the tip
    /// (Doc 09 §4.4, Doc 02 §3.2) — there is no failure state.
    /// </summary>
    public sealed class ServiceTicket
    {
        public string Id;
        public string VehicleId;
        public ServiceKind Kind;
        public float CreatedAtTime;
        public float QueueEnteredAtTime;
        public bool Served;

        /// <summary>Seconds spent waiting in the queue, used for the patience tip factor.</summary>
        public float QueueWaitSeconds(float now) => Math.Max(0f, now - QueueEnteredAtTime);
    }

    /// <summary>
    /// Turns arrivals into tickets, weighted by which services are currently unlocked
    /// (Doc 09 §6.8). Deterministic when seeded, so the demand pipeline is testable.
    /// </summary>
    public sealed class Reception
    {
        private readonly Dictionary<ServiceKind, int> _weights = new();
        private readonly Random _rng;
        private int _nextId;

        public Reception(int seed = 0) => _rng = new Random(seed);

        /// <summary>Weight 0 (or absent) means the service is not yet unlocked.</summary>
        public void SetWeight(ServiceKind kind, int weight) => _weights[kind] = Math.Max(0, weight);

        public int TotalWeight
        {
            get
            {
                int t = 0;
                foreach (var kv in _weights) t += kv.Value;
                return t;
            }
        }

        public bool HasAnyService => TotalWeight > 0;

        /// <summary>Generates a ticket for a vehicle, or null if no service is unlocked.</summary>
        public ServiceTicket CheckIn(string vehicleId, float now)
        {
            int total = TotalWeight;
            if (total <= 0) return null;

            int roll = _rng.Next(total);
            foreach (var kv in _weights)
            {
                if (kv.Value <= 0) continue;
                roll -= kv.Value;
                if (roll < 0)
                {
                    return new ServiceTicket
                    {
                        Id = "ticket_" + (++_nextId),
                        VehicleId = vehicleId,
                        Kind = kv.Key,
                        CreatedAtTime = now,
                        QueueEnteredAtTime = now,
                        Served = false
                    };
                }
            }
            return null;
        }
    }
}

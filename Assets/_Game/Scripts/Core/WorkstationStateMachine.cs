using System;

namespace Overhaul.Core
{
    public enum WorkstationState
    {
        Idle,     // no vehicle in the bay
        Starved,  // vehicle present but required parts missing
        Ready,    // vehicle + parts present, about to begin
        Working,  // service in progress (Progress 0..1)
        Done,     // service finished, waiting to hand the vehicle off
        Blocked   // finished but the output/exit is not clear
    }

    /// <summary>
    /// Data-driven workstation FSM: Idle -> Starved &lt;-&gt; Ready -> Working -> Done -> (Blocked).
    /// Engine-agnostic; the owning MonoBehaviour supplies world facts each tick and
    /// reacts to the emitted callbacks. See Doc 06 §3 and Doc 05 §1.3 (visual states).
    /// </summary>
    public sealed class WorkstationStateMachine
    {
        public WorkstationState State { get; private set; } = WorkstationState.Idle;
        public float WorkSeconds { get; }
        public float Progress { get; private set; }  // 0..1 while Working

        public WorkstationStateMachine(float workSeconds)
        {
            if (workSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(workSeconds));
            WorkSeconds = workSeconds;
        }

        /// <param name="dt">delta time (seconds)</param>
        /// <param name="vehiclePresent">a vehicle occupies the bay</param>
        /// <param name="partsAvailable">the bay's input rack holds the required parts</param>
        /// <param name="outputClear">the vehicle can leave (exit lane / next node free)</param>
        /// <param name="onConsumeParts">invoked once, when work begins</param>
        /// <param name="onProduce">invoked once, when the finished vehicle is released</param>
        public void Tick(float dt, bool vehiclePresent, bool partsAvailable, bool outputClear,
                         Action onConsumeParts, Action onProduce)
        {
            switch (State)
            {
                case WorkstationState.Idle:
                    if (vehiclePresent)
                        State = partsAvailable ? WorkstationState.Ready : WorkstationState.Starved;
                    break;

                case WorkstationState.Starved:
                    if (!vehiclePresent) State = WorkstationState.Idle;
                    else if (partsAvailable) State = WorkstationState.Ready;
                    break;

                case WorkstationState.Ready:
                    if (!vehiclePresent) { State = WorkstationState.Idle; break; }
                    if (!partsAvailable) { State = WorkstationState.Starved; break; }
                    onConsumeParts?.Invoke();
                    Progress = 0f;
                    State = WorkstationState.Working;
                    break;

                case WorkstationState.Working:
                    Progress += dt / WorkSeconds;
                    if (Progress >= 1f)
                    {
                        Progress = 1f;
                        State = WorkstationState.Done;
                    }
                    break;

                case WorkstationState.Done:
                    if (outputClear) { onProduce?.Invoke(); Reset(); }
                    else State = WorkstationState.Blocked;
                    break;

                case WorkstationState.Blocked:
                    if (outputClear) { onProduce?.Invoke(); Reset(); }
                    break;
            }
        }

        private void Reset()
        {
            Progress = 0f;
            State = WorkstationState.Idle;
        }
    }
}

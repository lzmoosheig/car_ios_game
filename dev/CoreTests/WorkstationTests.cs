using Overhaul.Core;

namespace Overhaul.CoreTests
{
    /// <summary>Drives the workstation FSM through a full service and the blocked-output path (Doc 06 §3).</summary>
    public static class WorkstationTests
    {
        public static void Run()
        {
            int consumed = 0, produced = 0;
            void OnConsume() => consumed++;
            void OnProduce() => produced++;

            var w = new WorkstationStateMachine(workSeconds: 6f);
            T.True(w.State == WorkstationState.Idle, "starts Idle");

            // no vehicle -> stays Idle
            w.Tick(1f, vehiclePresent: false, partsAvailable: true, outputClear: true, OnConsume, OnProduce);
            T.True(w.State == WorkstationState.Idle, "no vehicle keeps Idle");

            // vehicle arrives, no parts -> Starved
            w.Tick(1f, true, partsAvailable: false, outputClear: true, OnConsume, OnProduce);
            T.True(w.State == WorkstationState.Starved, "vehicle without parts -> Starved");

            // parts delivered -> Ready
            w.Tick(1f, true, partsAvailable: true, outputClear: true, OnConsume, OnProduce);
            T.True(w.State == WorkstationState.Ready, "parts arrive -> Ready");

            // begins work: consumes parts once, enters Working
            w.Tick(1f, true, true, true, OnConsume, OnProduce);
            T.True(w.State == WorkstationState.Working, "Ready -> Working");
            T.Eq(consumed, 1, "parts consumed exactly once");

            // advance to completion (6s of work)
            w.Tick(3f, true, true, true, OnConsume, OnProduce);
            T.Near(w.Progress, 0.5, 0.001, "progress halfway");
            w.Tick(3f, true, true, true, OnConsume, OnProduce);
            T.True(w.State == WorkstationState.Done, "work finishes -> Done");
            T.Eq(produced, 0, "nothing produced until output clears");

            // output blocked at Done -> Blocked
            w.Tick(1f, true, true, outputClear: false, OnConsume, OnProduce);
            T.True(w.State == WorkstationState.Blocked, "blocked exit -> Blocked");
            T.Eq(produced, 0, "still not produced while Blocked");

            // output clears -> produce once, back to Idle
            w.Tick(1f, true, true, outputClear: true, OnConsume, OnProduce);
            T.True(w.State == WorkstationState.Idle, "output clears -> Idle");
            T.Eq(produced, 1, "produced exactly once");
            T.Eq(consumed, 1, "consumed exactly once over the whole service");
        }
    }
}

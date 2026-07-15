using Overhaul.Core;

namespace Overhaul.CoreTests
{
    /// <summary>Covers the ServiceJob lifecycle driving the first playable loop.</summary>
    public static class JobTests
    {
        public static void Run()
        {
            // ---- happy path: Offered -> Accepted -> InService -> ReadyToCollect -> Collected ----
            var job = new ServiceJob("job_1", "cust_1", ServiceKind.BasicRepair, "tire", 4);
            T.True(job.State == JobState.Offered, "job starts Offered");
            T.True(job.Accept(), "accept from Offered");
            T.True(job.State == JobState.Accepted, "state Accepted");
            T.True(job.StartService(), "start service from Accepted");
            T.True(job.CompleteService(24), "complete from InService");
            T.True(job.State == JobState.ReadyToCollect, "state ReadyToCollect");
            T.Eq(job.CashReward, 24, "cash reward = revenue");
            T.Eq(job.ReputationReward, 1, "rep = max(1, 24/20)");
            T.True(job.Collect(), "collect from ReadyToCollect");
            T.True(job.State == JobState.Collected, "state Collected");

            // ---- wrong-order calls are refused and change nothing ----
            var j2 = new ServiceJob("job_2", "cust_2", ServiceKind.BasicRepair, "tire", 4);
            T.True(!j2.StartService(), "cannot start an unaccepted job");
            T.True(!j2.CompleteService(10), "cannot complete an unstarted job");
            T.True(!j2.Collect(), "cannot collect an incomplete job");
            T.True(j2.State == JobState.Offered, "refused calls leave state untouched");
            T.True(j2.Accept(), "accept still works after refused calls");
            T.True(!j2.Accept(), "double-accept refused");

            // ---- reputation scaling ----
            var j3 = new ServiceJob("job_3", "cust_3", ServiceKind.OilChange, "oil", 2);
            j3.Accept(); j3.StartService(); j3.CompleteService(100);
            T.Eq(j3.ReputationReward, 5, "rep scales: 100/20 = 5");
            var j4 = new ServiceJob("job_4", "cust_4", ServiceKind.Wash, "cleaning", 1);
            j4.Accept(); j4.StartService(); j4.CompleteService(0);
            T.Eq(j4.ReputationReward, 1, "rep floor of 1 even for $0");
            T.Eq(j4.CashReward, 0, "zero revenue stays zero");
        }
    }
}

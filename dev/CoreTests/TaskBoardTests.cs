using Overhaul.Core;

namespace Overhaul.CoreTests
{
    /// <summary>Verifies atomic claim/release semantics and the utility scorer (Doc 02 §4.2).</summary>
    public static class TaskBoardTests
    {
        public static void Run()
        {
            var board = new TaskBoard();
            board.Post("t1", TaskType.Haul, zoneId: "parts", urgency: 6f, now: 0f);

            // only the first worker claims it
            T.True(board.TryClaim("t1", "worker_A"), "first claim succeeds");
            T.True(!board.TryClaim("t1", "worker_B"), "second claim on same task fails");

            // release re-opens it
            board.Release("t1");
            T.True(board.TryClaim("t1", "worker_B"), "claim succeeds after release");

            // completing removes it
            board.Complete("t1");
            T.True(!board.TryClaim("t1", "worker_C"), "cannot claim a completed task");

            // ---- utility scoring: closer + more urgent + more starved = higher ----
            float near = TaskBoard.Score(urgency: 10f, starvationSeconds: 5f, distanceMeters: 0f);   // 50
            float far = TaskBoard.Score(urgency: 10f, starvationSeconds: 5f, distanceMeters: 10f);    // 25
            T.Near(near, 50.0, 0.001, "score at 0m");
            T.Near(far, 25.0, 0.001, "score at 10m");
            T.True(near > far, "nearer task scores higher");

            float urgent = TaskBoard.Score(10f, 5f, 5f);
            float idle = TaskBoard.Score(0.1f, 5f, 5f);
            T.True(urgent > idle, "urgent task outranks idle patrol at same distance");
        }
    }
}

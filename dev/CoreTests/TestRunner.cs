using System;
using System.Collections.Generic;

namespace Overhaul.CoreTests
{
    /// <summary>
    /// Tiny dependency-free assertion harness so the core logic can be verified with
    /// `dotnet run` (no NuGet, works offline). The real Unity project would additionally
    /// wrap these cases in the Unity Test Framework, but the assertions are identical.
    /// </summary>
    public static class T
    {
        public static int Passed;
        public static readonly List<string> Failures = new();

        public static void True(bool cond, string msg)
        {
            if (cond) Passed++;
            else Failures.Add("FAIL: " + msg);
        }

        public static void Eq(long actual, long expected, string msg)
            => True(actual == expected, $"{msg} (expected {expected}, got {actual})");

        public static void Near(double actual, double expected, double tol, string msg)
            => True(Math.Abs(actual - expected) <= tol, $"{msg} (expected ~{expected}, got {actual})");

        public static void Eq(string actual, string expected, string msg)
            => True(actual == expected, $"{msg} (expected \"{expected}\", got \"{actual}\")");
    }

    public static class Program
    {
        public static int Main()
        {
            Console.WriteLine("Overhaul core test runner\n-------------------------");

            EconomyTests.Run();
            StackTests.Run();
            WorkstationTests.Run();
            TaskBoardTests.Run();
            SaveDataTests.Run();

            Console.WriteLine($"\nPassed: {T.Passed}   Failed: {T.Failures.Count}");
            foreach (var f in T.Failures) Console.WriteLine("  " + f);

            return T.Failures.Count == 0 ? 0 : 1;
        }
    }
}

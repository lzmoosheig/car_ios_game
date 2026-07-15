# dev/ — engine-agnostic core tests

The gameplay *rules* (economy math, stack inventory, workstation FSM, task board,
save model) live in `Assets/_Game/Scripts/Core/` and have **no `UnityEngine`
dependency** — the `Overhaul.Core` assembly sets `"noEngineReferences": true`.

`CoreTests/` is a zero-dependency console runner that **links those exact source
files** (`<Compile Include="..\..\Assets\_Game\Scripts\Core\**\*.cs" />`) and asserts
they match the values in `docs/design/04-economy-and-balancing.md`. It uses only the
.NET base class library (`System.Text.Json` ships with the runtime), so it runs
offline with no NuGet restore.

## Run

```bash
cd dev/CoreTests
dotnet run -c Release
```

Expected: `Passed: 81   Failed: 0` (exit code 0). Wire this into CI as the economy /
logic regression gate (Doc 07 §3.1). In the Unity project the same cases are also
runnable through the Unity Test Framework, but this runner is the fast, editor-free
check.

## Why this split

Keeping rules out of MonoBehaviours means:
- balancing formulas are verified on every commit without opening Unity;
- the same code Unity ships is the code under test (linked, not copied);
- the Unity layer (`Assets/_Game/Scripts/Game/`) only does I/O, visuals and scene glue.

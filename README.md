# Overhaul! (working title)

An original iOS idle-arcade management game: grow a tiny roadside repair bay into an automotive empire — carry parts on your back, feed service bays, fix customer cars, pour cash into construction zones, hire a crew, and automate your way up through ten locations, from a dirt-lot garage to a full automotive HQ.

One thumb. No menus for the things that matter. Everything happens in the world.

## Design documents

The full game design & development plan lives in [docs/design](docs/design/), in reading order:

| Doc | Contents |
|---|---|
| [01 — Concept & Core Loop](docs/design/01-concept-and-core-loop.md) | Executive summary · gameplay pillars · the four nested core loops |
| [02 — Gameplay Systems](docs/design/02-gameplay-systems.md) | Controls & interactions · resources & service chains · customer/vehicle flow · employee automation · garage layout & construction |
| [03 — First Level & Progression](docs/design/03-first-level-and-progression.md) | Textless 10-beat tutorial · first 20 minutes · four progression tiers · ten-location roadmap |
| [04 — Economy & Balancing](docs/design/04-economy-and-balancing.md) | Revenue/cost formulas · upgrade catalog (local vs permanent) · pacing targets · bottleneck design |
| [05 — Art, Camera, UI, Audio](docs/design/05-art-camera-ui-audio.md) | Diorama art direction · visual state language · camera spec · minimal HUD · musical SFX & haptics |
| [06 — Technical Architecture](docs/design/06-technical-architecture.md) | Unity/URP rationale · scene & system architecture · all data schemas (ScriptableObjects + save JSON) |
| [07 — MVP & Phases](docs/design/07-mvp-and-phases.md) | Essential / post-MVP / avoid lists · development phases with kill-gate · testing strategy |
| [08 — Risks & Backlog](docs/design/08-risks-and-backlog.md) | Design & technical risks with mitigations · prioritized P0–P3 backlog |

Docs 03 and 04 form one balancing unit — every price and timing in the tutorial walkthrough matches the economy tables.

## Status

Early pre-production, running on **Unity 6000.5.3f1**. The first P0 slice works end to end:

- **Engine-agnostic core** (economy, stack inventory, workstation FSM, task board, save
  model) — passes **81 assertions** with no Unity needed: `cd dev/CoreTests && dotnet run -c Release`.
- **Gameplay assemblies compile and run in Unity** — batchmode import is error-free and
  the EditMode suite passes **6/6**: the full serve → consume parts → pay loop through the
  real `ServiceBay`, plus the Collect/Deposit interaction zones and customer-vehicle driving.
- **Base starting scene** at `Assets/_Game/Scenes/CityGarage.unity`: a full automotive
  service campus built from ten curated Kenney packs — 21 labeled station lots (parts
  logistics, service bays, paint line, showrooms, admin) on a street grid with crosswalks,
  a front parking apron, entrance/exit gates and a fenced, tree-lined perimeter. The core
  loop runs in Play mode (verified live): customer cars drive the middle street into the
  Basic Change Bay, get serviced and pay, while you drive the worker (WASD) hauling tires
  from Parts Delivery. An earlier diorama version remains at `Assets/_Game/Scenes/Graybox.unity`.

See [SETUP.md](SETUP.md) to open/play and for the immediate next steps. Progress tracks
the P0 backlog in [Doc 08 §2](docs/design/08-risks-and-backlog.md).

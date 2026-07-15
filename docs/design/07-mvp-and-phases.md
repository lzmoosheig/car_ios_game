# Overhaul! — Doc 07: MVP Scope · Development Phases · Testing Strategy

---

## 1. MVP definition

**Goal:** prove that the core loop (carry → serve → collect → build → automate) is fun for 45–60 minutes on a phone, with zero backend.

### 1.1 Essential (in MVP)
- 1 environment: **Level 1 "Rusty's Roadside Garage"** exactly as specced in Doc 03 §1
- 1 player character (Carrier + joystick, rush aura)
- 3 vehicle types: city car, sedan, SUV (palette variants)
- 3 services: tire change ($20/6 s), oil change ($30/8 s), basic repair ($45/12 s)
- 5 resources: Tire, Oil Container, Part Box (stand-in for brake/battery), Cleaning Supplies (wash add-on), Money Bundle
- 3+1 workstations: Basic Repair Bay (×2 buildable), Oil-Change Bay, Car-Wash Area + Reception/pay stall
- 3 employee roles: Parts Transporter, Mechanic, Cashier (hybrid task AI, hire pads, stat board)
- ~15 upgrades: 3 source-rate tiers, 2×3 station speed tiers, 3 employee stat tracks, 2 Office tiers (price, arrival), 1 permanent carry tier (to exercise the meta path)
- Full 10-beat textless tutorial (Doc 03 §1.2)
- Construction zones with partial funding; City Contract completion + ceremony
- Local JSON save, autosave, offline earnings v1
- Audio + haptics for the core 8 events (collect, deposit, complete, cash, build, hire, reject, unlock)
- Bottleneck visual cues #1–#5 (Doc 04 §5)

### 1.2 Post-MVP (valuable, only after the loop is validated)
Locations 2–4 and their mechanics · remaining 12 resources & crafting · vehicle test loop · Vehicle Mover & remaining roles · Golden Wrench meta shop · analytics + soft-launch funnel · rewarded ads & cosmetics IAP · iCloud save · reputation stars · missions panel · additional vehicle classes.

### 1.3 Explicitly avoided until the loop is proven
Multiplayer/social anything · online backend or server save · showrooms/resale economy (L6+) · prestige/franchise (L10) · seasonal events · deep car-customization UI · ad mediation experiments · Android port · narrative/dialogue systems.

---

## 2. Development phases (2-person team or coding agent; durations indicative)

| Phase | Duration | Exit criteria |
|---|---|---|
| **0 — Graybox prototype** | 2 wks | cubes-and-capsules build of the micro loop: joystick, stack, one source, one bay, cash, one construction zone. *Kill/continue decision on feel* |
| **1 — Core systems** | 3 wks | Carrier abstraction, Workstation FSM, waypoint vehicles + queue, customer FSM w/ patience, TaskBoard employees (3 roles), data-driven definitions all in place |
| **2 — Level 1 content** | 3 wks | full Doc 03 build-out order, tutorial beats, economy tables wired, save/offline, upgrade kiosks |
| **3 — Art & juice pass** | 3 wks | style per Doc 05, all visual states, audio/haptics, pooled VFX, 60 fps on iPhone XR |
| **4 — MVP hardening** | 2 wks | device matrix, soak tests, save migration test, FTUE playtests (n≥10), App Store assets, TestFlight external beta |
| **5 — Soft-launch iteration** | ongoing | analytics funnel healthy (see §3.3), then Location 2 production begins |

---

## 3. Testing strategy

### 3.1 Automated
- **Unit tests (EditMode):** economy formulas (revenue/tip/curves vs Doc 04 tables), stack filtering rules, task-claim atomicity, save round-trip + every migration, offline-settlement math incl. clock rollback.
- **PlayMode sims:** headless "bot player" that runs the tutorial beats and a 30-min greedy strategy at 10× timescale — asserts no deadlock (a vehicle or agent stuck > 60 s sim-time fails the test), and logs economy pacing vs Doc 04 §4.1 targets as a regression report per commit.
- **Determinism guard:** waypoint/slot reservation fuzz test — 500 vehicles through random service mixes, assert zero double-occupied slots.

### 3.2 Manual / device
- Device matrix per Doc 06 §3 each milestone; 30-min thermal soak; kill-app-mid-drain and airplane-mode save tests; interruption tests (calls, notifications, backgrounding during construction/service).

### 3.3 Playtesting & metrics (from Phase 4)
- FTUE tests: 10+ fresh players, no guidance; success = 90% finish beat 10 unaided in < 12 min.
- Funnel events per tutorial beat & per construction zone; red flags: > 10% drop at any beat, median time-to-first-hire > 8 min, session 1 < 12 min.
- "Look-away test": zoom out, ask the tester what's wrong with the garage — must identify the seeded bottleneck in < 5 s (validates Doc 04 §5).

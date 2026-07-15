# Overhaul! — Doc 08: Risks · Mitigations · Prioritized Backlog

---

## 1. Main risks and recommended solutions

### Design risks

| # | Risk | Likelihood/impact | Recommended solution |
|---|---|---|---|
| D1 | **Core loop isn't fun without art** — carrying feels like a chore, not a toy | M / fatal | Phase 0 graybox kill-gate (Doc 07 §2); tune collect/deposit tick feel first; do not proceed on "it'll be fun with juice" |
| D2 | **Bottlenecks are invisible** — players feel stuck, not challenged | M / high | the diegetic cue table (Doc 04 §5) is a hard requirement, not polish; "look-away test" in every playtest |
| D3 | **Economy stalls** — a mid-level dead zone where nothing is affordable | M / high | rule "next purchase always 1–3 min away" enforced by the pacing regression sim (Doc 07 §3.1); hand-tune each level's zone sequence |
| D4 | **Automation makes the player obsolete** → churn after first hires | M / high | rush aura + zone-crossing exclusivity + construction-only-by-player (Doc 02 §4.4); watch post-automation session length in analytics |
| D5 | **Tutorial drop-off** (textless teaching fails a step) | M / high | per-beat funnel events; each beat has a 20 s idle fallback (arrow re-pulses, camera nudges) |
| D6 | **Genre similarity reads as clone** | L / high | original name/art/values audit before store submission; distinct fantasy (service chains, vehicles as actors) is the differentiator — lean into paint/tuning/showroom content early in marketing |

### Technical risks

| # | Risk | Likelihood/impact | Recommended solution |
|---|---|---|---|
| T1 | **Vehicle navigation bugs** (double-booked slots, deadlocks) | H / high | no free pathfinding — reservation table is the single source of truth; fuzz test (Doc 07 §3.1); acyclic graph rule (Doc 02 §3.3) |
| T2 | **Perf collapse with many agents/items** | M / high | pooling mandate, 5 Hz brain ticks, instanced meshes (Doc 06 §3); perf budget checked every milestone, not at the end |
| T3 | **Save corruption / migration breakage** | L / fatal | atomic writes + last-good backup + tested migration chain (Doc 06 §5); save round-trip in CI |
| T4 | **Offline-earnings exploits** (clock manipulation) | M / low | clamp negatives, cap hours, rate snapshot at save (Doc 04 §4); accept residual abuse — single-player, no economy to protect |
| T5 | **Scope creep** (locations 2–10 designed, temptation to build them early) | H / medium | Doc 07 §1.3 avoid-list is contractual; Location 2 work may not start before soft-launch funnel targets pass |
| T6 | **iOS review friction** (privacy, kids category, similarity) | L / medium | no tracking in MVP, privacy manifest from day 1, original-content audit (D6), external TestFlight beta before review |

---

## 2. Prioritized development backlog

P0 = MVP-blocking · P1 = MVP polish · P2 = post-MVP next · P3 = later. Order within a band ≈ build order.

### P0 — prove the loop
1. Boot scene, service locator, event bus, Ticker
2. Joystick + PlayerController + camera follow (Doc 05 §2 params)
3. Carrier + stack visualizer + interaction zones with priority rules
4. ResourceDefinition/ServiceRecipe/WorkstationDefinition SOs + 5 MVP resources
5. Parts Delivery source + rack + Basic Repair Bay FSM (graybox)
6. Waypoint graph, VehicleMover, slot reservation, single queue
7. Customer FSM + patience/mood bubbles + pay stall cash piles
8. EconomyManager wallet + construction zones with partial funding
9. **⛔ Phase-0 kill-gate playtest**
10. TaskBoard + EmployeeAgent + 3 roles + hire pads
11. Upgrade system (StatRegistry) + kiosk UI + Office tiers
12. LevelDefinition-driven Level 1 build-out + 10 tutorial beats
13. Save system + autosave + offline earnings v1
14. Oil chain + wash add-on + second repair bay content
15. City Contract completion ceremony + Golden Wrench stub

### P1 — make it feel great
16. Art pass: environment, 3 vehicles, characters (Doc 05 §1)
17. All visual states + bottleneck cues #1–#5
18. Audio + haptics core set; polyphony/round-robin system
19. Edge indicators, objective chip, HUD final
20. Pooling everywhere + perf pass to 60 fps on XR
21. PlayMode bot sim + economy regression report in CI
22. Device matrix + soak + interruption tests; App Store assets; TestFlight beta

### P2 — validated next steps
23. Analytics funnel + soft launch
24. Golden Wrench meta shop (permanent upgrades)
25. Location 2 (multi-bay routing, warehouse, Vehicle Mover, Tire Tech)
26. Rewarded ads (offline double, temp boosts) + cosmetics
27. iCloud save mirror
28. Location 3 (crafting + tuning + diagnostics)

### P3 — horizon
29. Locations 4–7 (quality, paint, resale, matchmaking)
30. Reputation, missions panel, premium customers
31. Locations 8–10 (bespoke orders, contracts, franchise/prestige)
32. Seasonal events, Android evaluation

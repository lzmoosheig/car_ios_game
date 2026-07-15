# Overhaul! — Doc 02: Gameplay Systems

Covers: player controls & interactions · resource and service-chain design · customer & vehicle flow · employee automation · garage layout & expansion.

Each major system ends with the standard 10-point spec block: **Purpose / Player interaction / Inputs / Outputs / States / Upgrade paths / Dependencies / Edge cases / MVP requirements / Later expansion.**

---

## 1. Player Controls & Interactions

### 1.1 Movement
- **Floating virtual joystick:** touch anywhere on screen to anchor the stick; drag to move; release to stop. No fixed joystick position, no camera control needed from the player.
- **Base movement speed:** 4.0 m/s → upgradeable to 6.5 m/s (see Doc 04 §3 upgrade table).
- Character auto-rotates toward movement direction; slight lean while carrying a tall stack for readability and charm.
- No jump, no sprint button, no manual interaction button. Ever (Pillar 2).

### 1.2 Interaction zones
Every interactive object projects a **decal ring** on the ground showing its radius and color-coded type (blue = collect, orange = deposit, green = money, yellow = construction, purple = hire).

| Parameter | Base value | Notes |
|---|---|---|
| Collection radius | 1.2 m | per-item pickup tick every 0.25 s |
| Deposit radius | 1.2 m | per-item deposit tick every 0.25 s (≈4 items/s) |
| Money collection radius | 1.5 m | magnetized: bills fly to player, 8/s |
| Construction drain radius | 1.0 m | drain accelerates: $5/s → $50/s over 3 s standing |
| Dwell interactions | 0.5–1.5 s | reception check-in assist, station rush aura ramp-up |

### 1.3 Carried stack
- Items stack on the character's back in a **visible LIFO tower**; each resource type has a distinct silhouette (see Doc 05 §1).
- **Base capacity 5 items → 14** via permanent meta upgrades (Doc 04 §3).
- **Mixed carrying:** allowed. The stack may hold multiple types. Stations pull only compatible types *from anywhere in the stack* (visual: the matching items fly out; the stack collapses down). This avoids the classic LIFO frustration of "the tires are under the oil cans."
- **Bulky items** (engine blocks, body panels) count as 2 slots and cap the stack's visual height.
- **Resource filtering / rejection feedback:** entering a station that accepts none of the carried types shows a brief red ⃠ icon over the station, a soft "nope" bounce of the stack, and a subtle haptic tick. Never a hard block, never a text popup.

### 1.4 Automatic behaviors (no button, ever)
- Auto-collect from sources with free capacity.
- Auto-deposit into stations missing that resource.
- Auto-collect cash piles (magnetized).
- Auto-fund construction zones while standing in them.
- **Priority rule when zones overlap:** deposit > collect > construct > cash. (Cash is magnetized anyway so it rarely competes.)

### 1.5 Player rush aura
When the player stands inside a working station's ring for >1 s, the station gains **+25% work speed** (sparks/glow feedback). This is the core "player stays useful after automation" mechanic (Pillar 5 of role evolution, Doc 01 §3.6).

### 1.6 System spec — Player Controller & Stack
- **Purpose:** the player's single verb ("go there") that expresses every management action physically.
- **Player interaction:** joystick drag; standing in rings.
- **Inputs:** touch input; interaction-zone triggers; upgrade values (speed, capacity, radius).
- **Outputs:** position; collect/deposit/fund events; rush aura state; stack contents.
- **States:** Idle · Moving · Collecting · Depositing · Funding · Rushing (aura) · Celebrating (level-complete only).
- **Upgrade paths:** move speed (5 tiers), carry capacity (6 tiers), collection radius (+0.3 m, 2 tiers) — all **permanent meta upgrades** (Doc 04 §3).
- **Dependencies:** interaction-zone system; resource definitions; save system (persists stack between sessions? No — stack is dropped to the nearest rack on save, simpler).
- **Edge cases:** overlapping zones (priority rule 1.4); full stack at a source (items stop flowing, source shows "full hand" icon); capacity downgrade impossible (upgrades only add); character stuck in geometry (respawn at reception after 3 s of no movement while input active).
- **MVP requirements:** joystick, base speed/capacity, collect/deposit/fund/cash automation, LIFO stack visual, rejection feedback. Rush aura included (cheap, high value).
- **Later expansion:** vehicle the player can drive between distant areas of large late-game locations; pet/drone follower that adds carry slots.

---

## 2. Resource & Service-Chain Design

### 2.1 Resource catalog

15 resource types. "Slots" = stack slots consumed. Sources/destinations use station names from §5.

| # | Resource | Slots | Source | Compatible destinations |
|---|---|---|---|---|
| R1 | Tire | 1 | Parts Delivery Zone → Tire Storage | Wheel & Tire Station, Basic Repair Bay |
| R2 | Oil Container | 1 | Parts Delivery Zone → Oil Storage | Oil-Change Bay |
| R3 | Brake Disc | 1 | Parts Delivery Zone → Parts Warehouse | Basic Repair Bay, Inspection Area |
| R4 | Battery | 1 | Parts Warehouse | Basic Repair Bay, Diagnostic Station |
| R5 | Suspension Kit | 1 | Parts Warehouse | Basic Repair Bay, Tuning Station |
| R6 | Exhaust Component | 1 | Parts Warehouse | Tuning Station, Body-Repair Area |
| R7 | Engine Component | 2 | Parts Warehouse | Engine Workshop (assembly bench) |
| R8 | Turbocharger | 2 | Engine Workshop (crafted) | Tuning Station |
| R9 | Paint Can | 1 | Parts Delivery Zone | Paint-Mixing Station |
| R10 | Mixed Paint | 1 | Paint-Mixing Station (crafted) | Paint Booth |
| R11 | Cleaning Supplies | 1 | Parts Delivery Zone | Car-Wash Area, Detailing Station |
| R12 | Body Panel | 2 | Parts Delivery Zone | Body-Repair Area |
| R13 | Electronic Module | 1 | Parts Warehouse | Diagnostic Station, Tuning Station |
| R14 | Vehicle Key | 1 | Reception (spawned per finished car) | Delivery Zone, Showrooms |
| R15 | Money Bundle | — | Cash piles at pay points | Player/cashier wallet; construction zones |

Crafted resources (R8 Turbocharger, R10 Mixed Paint) are the "advanced production" layer: a station consumes basic resources and outputs a carriable intermediate.

### 2.2 Production chains

Each chain reuses earlier stations and adds at most 1–2 new concepts (station or resource), in unlock order:

1. **Tire chain (Level 1, tutorial):**
   `Parts Delivery → Tire Storage rack → Basic Repair Bay → customer vehicle → cash pile`
   *New concepts: sources, stacks, bays, payment.*
2. **Oil chain (Level 1, minute ~3):**
   `Parts Delivery → Oil Storage → Oil-Change Bay → vehicle → cash`
   *New concept: second parallel bay (routing/queueing).*
3. **Wash & detail chain (Level 1 end / Level 2):**
   `Cleaning supplies → Car-Wash Area → (optional Detailing Station) → vehicle exits shinier, tip bonus`
   *New concept: post-service value-add step in the vehicle's path.*
4. **Paint chain (Level 5 focus, previewed at 3):**
   `Paint cans → Paint-Mixing Station (craft: 2 cans + 5 s → Mixed Paint) → Paint Booth → customized vehicle → Delivery Zone → cash`
   *New concepts: crafting, color choice (customer requests a color; booth shows swatch).* 
5. **Engine & tuning chain (Level 3):**
   `Engine components → Engine Workshop assembly bench (craft: 2 comp + 8 s → Turbocharger) → Tuning Station → vehicle test loop → customer delivery → cash`
   *New concepts: 2-slot bulky items, vehicle test drive on a small loop.*
6. **Resale chain (Level 6+):**
   `Used vehicle delivered on trailer → Car-Wash → Inspection Area → Showroom slot → buyer agent → big cash`
   *New concept: the vehicle itself is the resource; buy-low/refurbish/sell-high.*

### 2.3 System spec — Resource System
- **Purpose:** make logistics tangible; every economic flow is a visible object moving through space.
- **Player interaction:** walking into source/destination rings; watching stack composition.
- **Inputs:** generation timers at sources; recipes consuming at stations; carrier capacity.
- **Outputs:** station readiness; crafted intermediates; starvation events (drive bottleneck UX).
- **States (per resource pile/rack):** Empty · Filling · Full (source pauses) · Reserved (an employee is en route to collect — prevents two workers fetching the same item).
- **Upgrade paths:** source generation rate, rack capacity (per station, level-local).
- **Dependencies:** ResourceDefinition data (Doc 06 §4); pooling (hundreds of items).
- **Edge cases:** rack full while a carrier arrives (carrier waits max 2 s then retargets); resource type retired by level layout change (auto-refunded as cash); save/load mid-flight items (items snap to nearest valid rack on load).
- **MVP requirements:** R1, R2, R3 (as generic "Part Box"), R11, R15 only; two sources; no crafting.
- **Later expansion:** full 15-resource catalog; crafted intermediates; quality tiers on parts (bronze/silver/gold affecting service price).

---

## 3. Customer & Vehicle Flow

### 3.1 Customer flow (canonical 10 steps)
1. Customer + vehicle **spawn** at the street entrance (vehicle on the entry lane, customer driving).
2. Vehicle stops at the **check-in stall**; customer walks to **Reception Desk**.
3. Reception generates a **service request** (weighted by unlocked services; icon bubble over customer: 🛞 / 🛢 / 🔧 / 🎨 …).
4. Vehicle **joins the queue** for that service (slot-based queue lane, §3.3).
5. When a compatible bay is free, the vehicle **auto-drives into the reserved bay slot** (or a Vehicle Mover employee drives it, at higher levels where lanes get long).
6. Bay **consumes required parts** from its local rack (if missing → Starved state, red part icon).
7. A **mechanic** (or the bay itself in Level 1, see §3.5) performs the service — timed progress ring over the bay.
8. If the request includes it, the vehicle continues to **wash / inspection / detailing** waypoints.
9. Vehicle parks at the **pay stall**; customer walks to the cash point; **cash pile spawns** (base price + patience-based tip).
10. Customer re-enters the vehicle and **drives off** via the exit lane; both despawn past the map edge.

### 3.2 Patience & reactions (no failure states)
Customers carry a **patience meter** (hidden bar, visible mood):

| Trigger | Visible reaction | Economic effect |
|---|---|---|
| Waiting > 30 s in queue | 😐 bubble, checks watch | tip −25% |
| Waiting > 75 s | 😠 bubble, taps foot, red exclaim | tip = 0 |
| Waiting > 120 s | 🌩 storm cloud; walks a small angry circle | pays −20% base price, **never leaves** (v1) |
| Bay starved while their car is in it | 🛞? icon pointing at the missing part | timer pauses (no double penalty) |
| Served faster than 20 s total | ⭐ sparkle, happy hop | tip +50% |
| Premium service (paint/tuning/detail) completed | 📸 takes a photo of the car | +1 reputation star (meta) |
| Queue congestion (all queue slots full) | New arrivals slow to a stop on the street, honk softly | arrival timer pauses (soft cap, no lost customers in v1) |

Rationale: pauses and reduced tips communicate problems without ever destroying progress (Pillar 5).

### 3.3 Vehicle navigation — collision-free by construction
Vehicles never pathfind freely. Each location ships with a **hand-authored directed waypoint graph**:
- **Fixed lanes:** entry lane → check-in stall → per-service queue lanes → bay approach splines → post-service lane (wash/inspection) → pay stall → exit lane.
- **Slot-based queues:** each queue lane has N discrete slots; vehicles advance slot-to-slot with eased movement; a vehicle owns exactly one slot or one reservation at all times.
- **Bay reservation:** a bay is reserved the moment a vehicle is dispatched to it; no two vehicles ever target the same node.
- **One-way flow:** the whole graph is acyclic except the test-drive loop; vehicles never reverse (bays are drive-through or use a scripted 3-point-turn animation on rails).
- **Visual de-confusion:** lane decals on the asphalt, per-service colored curbs matching station colors (Doc 05 §1).

### 3.4 System spec — Customer & Vehicle Flow
- **Purpose:** turn demand into a legible, physical pipeline the player optimizes.
- **Player interaction:** indirect — players build capacity, supply parts, and (early game) trigger reception check-in by standing at the desk.
- **Inputs:** arrival timer (Doc 04 §2); unlocked-service weights; queue/bay availability; patience config.
- **Outputs:** service requests; part consumption; cash piles; reputation stars; congestion signals.
- **States (customer):** Arriving · CheckingIn · WaitingInQueue · InService · PostService · Paying · Leaving. **(vehicle):** Entering · Queued · MovingToBay · InBay(Starved/Working/Done) · PostServiceMove · AwaitingPickup · Exiting.
- **Upgrade paths:** arrival rate, patience duration, queue slot count, service prices — level-local (Doc 04 §3).
- **Dependencies:** waypoint graph per level; QueueManager; ServiceRecipe data; reception staffing.
- **Edge cases:** all queues full (arrival pause, honking); save mid-service (vehicle restored into same bay & progress); service unlocked while customers queued (weights refresh next spawn only); customer requests a service whose bay was demolished/moved (request re-rolls).
- **MVP requirements:** 10-step flow; one queue lane; auto-drive (no Vehicle Mover); patience with mood bubbles + tips; honk-pause soft cap.
- **Later expansion:** multi-service visits (wash + oil in one ticket); appointment/VIP customers; walk-in parts shoppers; buyer agents for showrooms.

---

## 4. Employee Automation System

### 4.1 Roles
Hiring happens at physical **hire pads** (purple ring, cost tag). Ten roles across the game; MVP ships the first three.

| Role | Automates | Key stats | First available |
|---|---|---|---|
| Parts Transporter | source → rack hauling | speed, capacity | Level 1 (MVP) |
| Mechanic | bay work (repair/oil/tire) | repair speed, quality | Level 1 (MVP) |
| Cashier / Receptionist | check-in + payment collection | checkout speed | Level 1 (MVP) |
| Tire Technician | Wheel & Tire Station | processing speed | Level 2 |
| Vehicle Mover | driving cars between areas | driving speed | Level 2 |
| Detailer | wash & detailing stations | processing speed, quality | Level 4 |
| Paint Technician | mixing + booth | processing speed, quality | Level 5 |
| Warehouse Worker | intra-warehouse restock & crafting supply | speed, capacity | Level 5 |
| Salesperson | showroom deals | close speed, sale price bonus | Level 6 |
| Diagnostic Specialist | diagnostic station (finds upsells) | accuracy (upsell rate) | Level 7 |

### 4.2 Task selection — hybrid model (decision)
- Each employee is bound to a **role + home zone** (set at the hire pad; reassignable in the employee menu — the only menu-based assignment in the game, Pillar 1 exception).
- Within that zone, tasks are chosen **dynamically** by a utility score:
  `score = urgency × starvation_time ÷ (1 + distance_meters/10)`
  where urgency is data-driven per task type (starved bay with waiting car = 10, rack below 25% = 6, rack below 75% = 2, idle patrol = 0.1).
- **Why hybrid:** pure fixed assignment makes big garages feel dumb (workers idle beside emergencies); pure global task selection makes them unreadable (who does what?). Zone-bound dynamic selection keeps behavior both smart and predictable — the player always knows "the guy in the tire zone handles tires."
- **Reservation rule:** a task is claimed before movement starts; claims expire if unreached in 10 s.

### 4.3 Employee stats & upgrades
Per-employee, upgraded at the Employee Room board (level-local):

| Stat | Base | Per tier | Tiers | Applies to |
|---|---|---|---|---|
| Move speed | 3.0 m/s | +0.5 | 5 | all |
| Carry capacity | 3 | +2 | 4 | transporter, warehouse |
| Repair/processing speed | 1.0× | +0.15× | 5 | mechanic, techs, detailer, painter |
| Service quality (tip bonus) | +0% | +5% | 4 | mechanic, detailer, painter |
| Checkout speed | 1.0× | +0.25× | 4 | cashier |
| Task-switching (claim range) | 15 m | +5 m | 3 | all |

### 4.4 Player relevance after automation
Guaranteed by design: (a) only the player crosses zones freely; (b) the **rush aura** (+25% station speed, §1.5); (c) construction funding is player-only; (d) employees are deliberately capped below optimal (max tiers still ~80% of an attentive player's throughput on any single lane); (e) bottleneck triage — deciding the *next* investment — is never automated.

### 4.5 System spec — Employee System
- **Purpose:** convert player routines into automation so attention can move up the chain (courier → manager → investor).
- **Player interaction:** hire pads (walk-in purchase); Employee Room board (assign zone, buy stat tiers); watching them work.
- **Inputs:** task events from stations/racks/queues; zone assignment; stat values.
- **Outputs:** hauling, service work, check-ins, payments collected to the shared wallet.
- **States:** Idle(patrol) · ClaimingTask · MovingToSource · Carrying · MovingToTarget · Working · Resting (brief coffee idle at Employee Room every ~3 min for charm, no economic effect).
- **Upgrade paths:** table §4.3 (level-local); meta perk "veteran staff" grants +1 base tier globally (permanent).
- **Dependencies:** utility scorer; NavMesh; interaction zones (employees use the same collect/deposit code as the player — single carrier abstraction, Doc 06 §3).
- **Edge cases:** two employees claim simultaneously (server-of-truth = TaskBoard, atomic claims); zone with nothing to do (patrol between zone anchors); employee's home station demolished (auto-reassign to nearest zone, toast notification); more employees than tasks (visible idling is *intentional feedback* of over-hiring).
- **MVP requirements:** 3 roles; hybrid scorer with 3 task types (haul, service, checkout); 3 stat types; hire pads; simple assignment menu.
- **Later expansion:** all 10 roles; named employees with rarity traits; shift scheduling; training montage animation between levels.

---

## 5. Garage Layout & Expansion

### 5.1 Station catalog and rollout
All 22 functional areas, mapped to the location where they first appear (locations defined in Doc 03 §3):

| Area | First appears | Role in flow |
|---|---|---|
| Reception Desk | L1 | check-in, request generation |
| Customer Vehicle Queue | L1 | slot lanes per service |
| Parts Delivery Zone | L1 | root source (truck drops pallets) |
| Basic Repair Bay | L1 | tire/brake/battery services |
| Oil-Change Bay | L1 | oil services |
| Tire Storage | L1 | tire rack buffer |
| Completed-Car Delivery Zone | L1 | pay stall + exit |
| Employee Room | L1 | hire management, stat board |
| Parts Warehouse | L2 | multi-resource buffer |
| Wheel & Tire Station | L2 | dedicated tire machine (frees Repair Bay) |
| Car-Wash Area | L2 | post-service value add |
| Office & Financial Area | L2 | prices/arrival upgrades, offline earnings terminal |
| Engine Workshop | L3 | crafting bench (turbos) |
| Tuning Station | L3 | performance services + test loop |
| Diagnostic Station | L3 | reveals upsell requests |
| Detailing Station | L4 | premium finish, big tips |
| Vehicle Inspection Area | L4 | quality gate before delivery/showroom |
| Body-Repair Area | L5 | panel replacement |
| Paint-Mixing Station | L5 | crafts Mixed Paint |
| Paint Booth | L5 | color customization |
| Used-Car Showroom | L6 | resale chain |
| Premium-Car Showroom | L7 | high-end resale |

### 5.2 Layout principles
- **Service spine:** vehicles flow left→right along one readable spine (entrance → queues → bays → wash/inspection → pay → exit). Humans (player, staff, customers) move on the near side; parts logistics on the far side. Crossing traffic is minimized by design.
- **Diorama framing:** each location fits a single zoomed-out screen (Doc 05 §2); expansion reveals adjacent plots (fence sections fall over, ground rolls out) rather than moving to disconnected sub-maps.
- **Buffer racks beside every consumer:** each bay has its own small input rack so haulers and mechanics never contend for the same square meter.

### 5.3 Construction zones
- A locked station appears as a **dashed blueprint outline + floating price** (e.g., `$120 🔧`).
- Standing in it drains wallet cash with an accelerating tick ($5/s ramping to $50/s), filling a radial progress ring; **partial funding persists** — players can top up across visits.
- On completion: dust-poof, the structure **springs up with a squash-and-stretch pop**, confetti of bolts, new decal rings activate, and a 1.5 s camera nudge frames it.
- Zones unlock in a **scripted order per level** (LevelDefinition, Doc 06 §4): each new zone appears only when it can matter (e.g., the hire pad appears only after the second bay exists, when hauling actually hurts).
- Every zone must satisfy the rule: **new mechanic, new capacity, or new revenue — visible within 30 seconds of construction.**

### 5.4 System spec — Construction & Expansion
- **Purpose:** physicalize upgrades and pace the level as a visible build-out.
- **Player interaction:** stand in blueprint to fund; watch the pop-in.
- **Inputs:** wallet cash; scripted unlock order; costs from LevelDefinition.
- **Outputs:** new stations/pads/lanes; camera moments; tutorial beats.
- **States:** Hidden · Revealed(affordable-highlight when wallet ≥ 30% of remaining) · PartiallyFunded · Constructing(2 s anim) · Complete.
- **Upgrade paths:** n/a (zones *are* the upgrade delivery mechanism); costs follow the curve in Doc 04 §2.
- **Dependencies:** EconomyManager wallet; LevelDefinition; save system (persist partial funding).
- **Edge cases:** funding interrupted (persists); two adjacent zones (player funds the one whose center is nearest); wallet empty mid-drain (drain stops silently, ring stays); zone revealed off-screen (edge arrow indicator, Doc 05 §2).
- **MVP requirements:** 8–10 zones in Level 1 (second bay, racks, hire pads, wash station, upgrades kiosk), partial funding, pop-in animation.
- **Later expansion:** decorative/cosmetic zones; multi-stage buildings (showroom tiers); rebuild/relocate tool.

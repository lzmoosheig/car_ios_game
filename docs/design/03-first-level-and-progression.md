# Overhaul! — Doc 03: First Level & Progression

Covers: first-level tutorial walkthrough (including the first 15–20 minutes) · four-tier progression · ten-location roadmap.

All prices, durations and timings here match the economy anchors in Doc 04 (§1–§4). Treat both docs as one balancing unit.

---

## 1. Level 1 — "Rusty's Roadside Garage"

### 1.1 Starting state
- 1 player character (no employees)
- 1 parts source: **Parts Delivery Zone** (a truck periodically drops a pallet; pallet holds tires only at start)
- 1 **Basic Repair Bay** with an empty 4-slot tire rack beside it
- 1 vehicle queue position + entry/exit lane
- 1 **pay stall** (payment point) next to the exit
- A dirt lot, a fence hiding future plots, and one waiting customer already idling at the entrance so the very first seconds have purpose

### 1.2 Tutorial — ten beats, zero text boxes
Teaching tools: a **soft yellow arrow** on the ground, a **pulsing highlight ring** on the current target, camera nudges, and short icon bubbles. No modal text. Each beat completes by *doing*, and the next begins immediately.

| # | Beat | How it's taught | Done when |
|---|---|---|---|
| 1 | **Movement** | Joystick ghost-hand animates once on first touch; arrow points to the pallet | player moves 3 m |
| 2 | **Collect a part** | Highlight ring on pallet; tires hop onto the back automatically | 3 tires carried |
| 3 | **Carry** | Stack visibly wobbles; arrow bends toward the repair bay | player walks 5 m with stack |
| 4 | **Supply the bay** | Highlight on the bay's rack; tires fly in one-by-one | rack ≥ 3 tires |
| 5 | **Complete a service** | Waiting car auto-enters bay; progress ring spins 6 s; car rolls to the pay stall | first car exits |
| 6 | **Collect payment** | Cash pile glows at pay stall; bills magnetize to player | wallet = $20 |
| 7 | **Fund construction** | Blueprint for **Tire Storage** ($30) pulses when wallet ≥ $20; second customer arrives meanwhile | zone fully funded |
| 8 | **Second workstation** | After ~2 more customers, **Oil Storage** ($80) then **Oil-Change Bay** ($120) blueprints appear in sequence | oil bay built |
| 9 | **First hire** | **Transporter hire pad** ($250) appears beside the pallet exactly when the player is visibly sprinting between two bays | transporter hired |
| 10 | **Spot a bottleneck** | Arrival rate steps up; the tire rack runs dry; the bay shows the red 🛞? starved icon and the customer's ❗ bubble — arrow points at the **Delivery Rate upgrade kiosk** ($100) | upgrade bought, bay resumes |

After beat 10 all guidance arrows retire; the objective chip (Doc 05 §3) takes over with one suggested goal at a time.

### 1.3 First 15–20 minutes — unlock order and purpose

| Time | Event / unlock | Cost | Purpose of the unlock |
|---|---|---|---|
| 0:00–1:00 | Beats 1–6; two tire customers served | — | teach the micro loop; income starts (~$40) |
| ~1:00 | **Tire Storage rack** | $30 | buffer between pallet and bay; teaches funding |
| 1:00–3:00 | 3–4 more customers; wallet grows ~$50/min | — | rhythm practice; slight overcapacity of cash |
| ~2:30 | **Oil Storage** | $80 | pre-req buffer, foreshadows second service |
| ~3:30 | **Oil-Change Bay** | $120 | second parallel service — first real *management* moment (two racks to keep filled) |
| 3:30–6:00 | Oil customers mix in ($30 each); income ~$100/min; player sprints between racks | — | manufactured transport pain → makes the next unlock feel earned |
| ~6:00 | **Transporter hire pad** | $250 | automates hauling; player feels relief immediately |
| ~7:00 | **Employee Room** | $150 | stat board; teaches employee upgrades (first tier $60) |
| ~8:00 | **Delivery Rate upgrade** (kiosk tier 1) | $100 | resolves the scripted beat-10 bottleneck |
| ~9:30 | **Reception Desk + Cashier pad** | $200 + $300 | removes player's checkout/check-in chores; arrival rate steps up as compensation |
| ~12:00 | **Second Basic Repair Bay** | $400 | doubles repair throughput; queue visibly shortens |
| ~14:00 | **Mechanic hire pad** | $400 | bays no longer need the player nearby at all |
| ~15:00 | **Car-Wash Area** | $600 | first *value-add* station: +$15 wash add-on and tip boost; introduces post-service vehicle path |
| 15:00–20:00 | Player upgrades bottlenecks freely (bay speed tiers, transporter capacity); wallet ~$150–200/min | ~$60–150 per tier | open-ended optimization; the **City Contract** sign ($2,500) is now visible as the level's finish line |

**Session exit hook:** at ~20 min a natural pause lands — everything automated, City Contract ~40% funded. Offline earnings (Doc 04 §4) make returning attractive. Total Level 1 completion: **45–60 min of play**.

### 1.4 Level 1 completion
Level 1 completes when all core zones are built **and** the player funds the **City Contract sign** ($2,500) — completion is itself a construction zone (consistent with Pillar 1). Ceremony: time-lapse camera orbit of the garage, earned **Golden Wrenches ×3** (meta currency), "Location 2 unlocked" stinger.

---

## 2. Four-Tier Progression

| Tier | Cadence | Player experiences | Systems involved |
|---|---|---|---|
| **Moment-to-moment** | 5–15 s | collect parts, fill a rack, watch a repair finish, scoop cash | stack, stations, cash piles |
| **Short-session** | 3–10 min | fund a zone, hire someone, add a service, break a bottleneck | construction, hire pads, upgrade kiosk |
| **Level** | 45–90 min | complete a garage, fund the completion contract, unlock the next location | LevelDefinition, business-value meter |
| **Meta** | days–weeks | Golden Wrench permanent upgrades, new vehicle classes, cosmetic themes, franchise prestige | meta save, HQ screen |

**Level-local vs permanent** (full table in Doc 04 §3): stations, bays, hires and their stat tiers reset per location; player speed/capacity/radius, offline-earnings cap, "veteran staff" perk, cosmetics and vehicle-class unlocks are permanent.

---

## 3. Ten-Location Roadmap

Each location = new environment + **one named new mechanic** (never just bigger prices). Completed locations keep producing a trickle of managed income (10% of their peak rate) so the empire *feels* cumulative.

| # | Location | New mechanic introduced | New stations/roles |
|---|---|---|---|
| 1 | **Rusty's Roadside Garage** | the whole core loop | repair bay, oil bay, wash, 3 roles |
| 2 | **Urban Service Center** | **multi-bay routing** — 3 parallel service lines, per-service queues, Vehicle Mover role; Parts Warehouse (multi-resource buffer); Office (financial upgrades) | Wheel & Tire Station, Warehouse, Office, Tire Tech, Vehicle Mover |
| 3 | **Tuning Workshop** | **crafting** — assembly bench turns engine components into turbochargers; vehicle test-drive loop; Diagnostic Station generates upsells | Engine Workshop, Tuning Station, Diagnostic Station |
| 4 | **Luxury Detailing Studio** | **service quality** — star rating per job (staff quality stats matter); premium customers with big tips and low patience | Detailing Station, Inspection Area, Detailer |
| 5 | **Bodywork & Paint Facility** | **two-stage production** — mix paint, then booth; customer color requests; bulky body panels (2-slot items) | Body-Repair, Paint-Mixing, Paint Booth, Paint Tech, Warehouse Worker |
| 6 | **Used-Car Dealership** | **resale loop** — buy trade-ins cheap, refurbish through existing chains, sell from showroom; the car itself becomes inventory | Used-Car Showroom, Salesperson |
| 7 | **Premium Dealership** | **customer matchmaking** — buyers have preferences (class/color); matching them multiplies sale price; reputation stars gate premium stock | Premium Showroom, Diagnostic Specialist |
| 8 | **Exotic Customization Center** | **bespoke orders** — multi-step custom builds (paint + tune + detail on one car) tracked as a single high-value ticket | all prior stations, combined tickets |
| 9 | **Motorsport Prep Facility** | **timed contracts** — race-team orders with soft deadlines and bonus payouts; pit-crew mini-moments (player rush aura matters again) | race bays, test track |
| 10 | **Automotive HQ** | **franchise management** — assign earned staff & multipliers across all previous locations; prestige (reset a location for permanent Golden Wrench yield) | HQ office, franchise board |

Vehicle classes unlock along the way (Doc 05 §1): city car / sedan / SUV (L1) → pickup (L2) → sports coupe (L3) → classic (L6) → supercar (L7) → race car (L9).

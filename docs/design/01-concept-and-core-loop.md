# Overhaul! — Doc 01: Concept & Core Loop

Working title: **Overhaul!** (original IP; all names, values, layouts and art described in these documents are our own)

---

## 1. Executive Concept Summary

**Overhaul!** is a mobile idle-arcade management game for iOS in which the player physically runs — and eventually automates — a growing automotive empire. Starting with a single roadside repair bay and a pallet of tires, the player walks a chunky stylized character through a diorama-like garage, scooping up parts into a visible stack on their back, feeding service bays, fixing customer cars, and hoovering up piles of cash that they pour straight into new construction. Over ten increasingly ambitious locations, the tiny garage becomes a service center, tuning shop, paint facility, dealership and finally a full automotive headquarters.

- **Genre:** Idle-arcade / physical management ("walk-to-interact" management sim)
- **Platform:** iOS first (iPhone primary, iPad supported), portrait-capable but designed landscape-agnostic at an elevated isometric view
- **Session length:** 3–10 minute sessions, with offline earnings rewarding returns
- **Audience:** Broad casual (age 9+), fans of arcade management and idle games; no automotive knowledge required
- **Input:** One thumb. Virtual joystick, zero action buttons for core play
- **Monetization stance:** None in MVP. Post-MVP: optional rewarded ads (boosts, offline doubling) and cosmetic IAP. Never pay-gated progression
- **Unique selling point:** The *service chain* fantasy — cars visibly roll through your hand-built pipeline (check-in → bay → wash → inspection → delivery), and every efficiency gain is something you physically constructed and can watch working

**Inspiration boundary.** The game takes only genre-level conventions from the idle-arcade management genre (world-based interaction, carried stacks, construction zones, hired helpers). Its name, setting, characters, layouts, UI, sounds, text, art and every numeric value are original.

---

## 2. Core Gameplay Pillars

Every design decision in the other documents must trace back to one of these five pillars.

### Pillar 1 — Physical, not menu-driven
Management happens in the world. Parts are objects you carry; money is piles you walk through; upgrades are construction zones you stand in. Menus exist only for settings, employee assignment and the upgrade catalog. If a mechanic can be expressed as "walk somewhere," it must be.

### Pillar 2 — One thumb, zero friction
A single floating joystick is the entire control scheme. Collection, deposit, payment and construction all trigger automatically by proximity. There is never a "press to interact" prompt in core play. Anyone who can move a thumb can play optimally.

### Pillar 3 — Always a next thing
At any moment the player can see at least one affordable or nearly-affordable goal in the world: a construction zone with a price tag, a starving station, an uncollected cash pile, a hire pad. The game never asks "what now?" — the diorama itself is the to-do list.

### Pillar 4 — Visible progress
Progress is legible at a glance: stacks grow on your back, racks fill with tires, bays sprout equipment, queues shorten, the lot gets bigger. Returning after a break, the player should be able to *see* everything they've built without opening a single screen.

### Pillar 5 — Gentle pressure, no failure
Impatient customers, starved stations and clogged queues cost efficiency and tips — never the run. The game creates "pleasant stress": enough pressure to make automation feel necessary, never enough to punish a relaxed player. There is no game-over.

---

## 3. Detailed Core Loop

The core loop is fractal: four nested loops, each feeding the next.

### 3.1 Micro loop (5–15 seconds) — "carry"
```
walk to source → auto-collect parts (stack grows) → walk to station →
auto-deposit (stack shrinks, station fills) → station works → done
```
Feel targets: collection tick every ~0.25 s per item, visible stack wobble, satisfying per-item deposit *pop*. The player is never idle for more than ~2 s without something to walk toward.

### 3.2 Station loop (30–90 seconds) — "serve"
```
customer arrives → checks in at reception → vehicle enters queue →
vehicle takes a free bay → bay consumes parts → service animation →
vehicle rolls to delivery → customer pays → cash pile spawns →
player/cashier collects cash
```
One full customer cycle in the first level takes ~30–45 s. Multiple cycles overlap once a second bay exists, which is the moment the player first feels like a *manager* rather than a courier.

### 3.3 Expansion loop (3–10 minutes) — "build"
```
cash accumulates → construction zone becomes affordable →
stand in zone, cash drains in, structure builds →
new station / bay / hire pad appears →
new demand and new bottleneck emerge →
player identifies the bottleneck → invests to fix it
```
Every construction deliberately creates its own next problem: a second bay doubles part consumption (transport bottleneck → hire a transporter), a hired transporter saturates the pallet (production bottleneck → upgrade delivery rate), and so on. See Doc 04 §5 for the designed bottleneck sequence.

### 3.4 Location loop (45–90 minutes per location) — "complete"
```
build out all stations of the location → hit the business-value target →
location complete ceremony → unlock next location →
new environment + one genuinely new mechanic (see Doc 03 §3)
```

### 3.5 Meta loop (whole game) — "empire"
```
completed locations keep earning (reduced, managed) →
meta-currency (Golden Wrenches) from location completions →
permanent player upgrades, cosmetic themes, new vehicle classes →
franchise/prestige: reset a location with permanent multipliers
```

### 3.6 The player's evolving role
- **Minutes 0–5:** the player is the only worker — courier, mechanic-enabler, cashier.
- **Minutes 5–20:** first hires take over transport and checkout; the player becomes the *bottleneck breaker*, sprinting wherever the red icons appear.
- **Hour 1+:** fully staffed stations run alone; the player is the *investor and firefighter* — funding zones, rushing stations by standing at them (player presence grants a +25% speed aura, see Doc 02 §1), and deciding what to upgrade next.

The loop diagram, resource flows and station-by-station specs are in Doc 02. The numeric pacing of these loops is specified in Doc 04.

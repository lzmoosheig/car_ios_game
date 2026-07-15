# Overhaul! - Complete Game and Racing Master Plan

Status: product design proposal. Values are balancing targets, not final constants.

This document extends Docs 01-08. It connects every building already present in
`CityGarage` to a complete playable game and adds player-owned cars, a village
circuit editor, time trials, and asynchronous ghost challenges.

---

## 1. Product Vision

The player builds a small automotive village into a complete service, tuning,
sales, and motorsport business. Everything has two complementary uses:

1. **Business use:** serve customers, move resources, automate production, earn cash.
2. **Personal use:** acquire cars, repair and tune them, build circuits, set records,
   and challenge other players' ghosts.

The game should satisfy three fantasies in the same session:

- **Owner:** build and optimize a living automotive district.
- **Driver:** take a personally prepared car and enjoy it in third-person view.
- **Competitor:** design a circuit, defend a record, and attack another village's ghost.

The social mode is asynchronous. A defender publishes an immutable circuit version,
car class, and reference ghost. Challengers race locally against that ghost. This gives
the emotional structure of an "attack" without requiring synchronized multiplayer,
network physics, or both players to be online.

### 1.1 Core promise

> Every building makes the business stronger, every business system improves the
> player's cars, and every race gives the player a reason to return to the village.

### 1.2 Design pillars

1. **Physical management:** most business actions happen by moving through the world.
2. **Readable automation:** workers visibly perform the jobs the player once did.
3. **Cars with identity:** owned cars retain history, configuration, cosmetics, and records.
4. **Fair competition:** ranked results are decided by driving and track knowledge, not spending.
5. **Creative ownership:** a player's circuit is a recognizable part of their village.
6. **Gentle failure:** losing a race costs an attempt or reward opportunity, never owned progress.
7. **Mobile clarity:** short actions, low UI density, stable performance, and resumable sessions.

---

## 2. Complete Game Loop

### 2.1 Moment-to-moment loop: 10-60 seconds

```text
spot a bottleneck -> carry a resource / rush a station / drive a car ->
complete a job -> collect cash, reputation, or a component -> choose the next action
```

### 2.2 Management loop: 3-10 minutes

```text
accept customers -> diagnose requests -> supply service stations -> finish cars ->
collect revenue -> upgrade capacity or staff -> increase throughput -> unlock demand
```

### 2.3 Personal-car loop: 5-20 minutes

```text
acquire car -> inspect -> repair -> tune -> test drive -> refine setup ->
enter event or publish circuit record -> earn reputation and cosmetics
```

### 2.4 Circuit loop: 10-30 minutes

```text
enter build mode -> place track pieces -> validate route -> drive certification lap ->
publish immutable version -> defend against challenges -> revise into a new version
```

### 2.5 Long-term loop: days to months

```text
complete village milestones -> unlock car classes and facilities -> master race leagues ->
collect cars and track cosmetics -> improve seasonal rating -> prestige the business
```

### 2.6 Typical daily session

1. Collect offline business earnings.
2. Read the village visually: empty racks, queues, completed cars, active contracts.
3. Resolve one bottleneck or purchase one upgrade.
4. Claim completed workshop or showroom work.
5. Run one personal-car test or ghost challenge.
6. Start one longer production, employee training, or dealership acquisition before leaving.

Management must remain rewarding without racing, and racing must remain playable without
waiting for idle timers. The two loops improve one another but never hard-block one another.

---

## 3. Player Verbs and Modes

### 3.1 On-foot verbs

- Move with the virtual joystick.
- Auto-collect compatible resources.
- Auto-deposit resources into nearby racks and stations.
- Collect cash and completed items.
- Fund construction and upgrades by standing in their zones.
- Rush an active workstation for a temporary speed bonus.
- Enter a parked personal car from proximity.
- Inspect a building by entering its management pad.

### 3.2 Driving verbs

- Enter and exit eligible personal or test vehicles.
- Accelerate, brake/reverse, and steer with simultaneous touch input.
- Follow third-person chase camera only; no first-person mode.
- Drive in village free-roam, test loops, circuit certification, practice, and challenges.
- Reset to the last safe road point if stuck for three seconds.
- Use auto-reset after leaving the playable boundary or overturning.

### 3.3 Management-pad verbs

Each building has one physical pad that opens only its focused panel:

- Assign workers.
- Buy speed, capacity, quality, or automation upgrades.
- Select recipes, service priorities, or stock policy.
- Review inputs, outputs, current queue, and visible bottleneck.

Panels never replace physical logistics. They configure behavior; the world performs it.

### 3.4 Track-builder verbs

- Place, rotate, replace, and remove modular road pieces.
- Paint curbs and choose decoration themes.
- Place start line, ordered checkpoints, pit lane, props, and spectator boundaries.
- Test from the selected piece without publishing.
- Validate continuity, clearance, performance budget, and lap legality.
- Save drafts and publish a version after a valid certification lap.

---

## 4. Economy Architecture

### 4.1 Currencies and progression values

Keep the economy understandable. Do not add a currency for every activity.

| Value | Type | Earned from | Spent on |
|---|---|---|---|
| Cash | soft currency | customer services, car sales, contracts, offline business | buildings, local upgrades, staff, stock, car work, circuit pieces |
| Reputation | non-spend progression XP | quality service, fast delivery, races, clean challenges | unlock gates for buildings, cars, leagues, and premium customers |
| Golden Wrenches | scarce permanent currency | major milestones, league promotion, prestige | permanent capacity, cosmetics, garage themes, convenience upgrades |
| Championship Rating | competitive score, seasonal | ranked ghost wins/losses | not spent; determines league and seasonal rewards |

No purchased currency may increase ranked car performance. If monetization is added,
it should sell cosmetics, optional rewarded boosts for the management loop, and track themes.

### 4.2 Cash sources

- Base service payment.
- Patience tip.
- Quality bonus.
- Diagnostic upsell.
- Wash, detail, paint, and tuning add-ons.
- Used and premium vehicle margin.
- Daily business contracts.
- First-win and capped daily ghost-challenge rewards.
- Defender reward when another player completes a valid challenge.
- Offline earnings from fully automated chains.

### 4.3 Cash sinks

- Construction and building tiers.
- Racks, queue slots, and service bays.
- Employee hiring and training.
- Resource delivery contracts and warehouse capacity.
- Player-car purchase, repair, tuning, and cosmetic work.
- Circuit construction budget and theme props.
- Event entry for high-value tournaments only; practice remains free.

### 4.4 Service revenue

```text
serviceRevenue = basePrice
               * locationMultiplier
               * officePriceMultiplier
               * vehicleClassMultiplier
               * conditionComplexity

tip = basePrice
    * patienceFactor
    * qualityFactor
    * optionalServiceFactor
```

Customer payment is always positive. Poor service reduces tips and reputation; it never
creates debt. Fast, high-quality multi-stage service is the best management income.

### 4.5 Racing rewards

```text
challengeReward = leagueBase
                * difficultyFactor
                * cleanDrivingFactor
                * firstDailyWinFactor
```

- Practice: unlimited, no cash after first small completion reward.
- Ranked attempts: five reward-bearing attempts per day; additional practice is unlimited.
- A failed ranked attempt does not remove cash or car parts.
- Defender rewards are capped daily and require unique challengers.
- Repeating the same easy opponent rapidly decays rewards to zero.
- Track creator receives cosmetic progression for plays, not uncapped cash.

### 4.6 Economy guardrails

- Next meaningful local purchase should be 1-3 minutes of active income away.
- A major building should be 8-15 minutes away when first revealed.
- The player should never need a race win to continue building the village.
- The player should never need an idle timer to enter a fair-spec race.
- Offline income uses automated throughput only and is capped.
- Upgrade curves and rewards live in data, never hard-coded in UI or building scripts.

---

## 5. Shared Resources and Production

### 5.1 Physical resources

| Resource | Produced or delivered by | Main consumers |
|---|---|---|
| Tires | Parts Delivery, Tire Storage | Basic Bay, Wheel & Tire, owned cars |
| Oil | Parts Delivery | Basic Bay, Engine Workshop |
| Brake components | Parts Warehouse | Basic Bay, Inspection |
| Battery/electronics | Parts Warehouse | Diagnostic, Basic Bay, Tuning |
| Suspension kit | Parts Warehouse | Tuning, owned-car setup |
| Exhaust component | Parts Warehouse | Body Repair, Tuning |
| Engine component | Parts Warehouse | Engine Workshop |
| Turbocharger | Engine Workshop | Tuning |
| Body panel | Parts Delivery/Warehouse | Body Repair |
| Paint can | Parts Delivery | Paint Mixing |
| Mixed paint | Paint Mixing | Paint Booth |
| Cleaning supplies | Parts Delivery | Car Wash, Detailing |
| Vehicle key | Reception/Delivery | Delivery, Showrooms, owned cars |
| Track materials | Warehouse contract | Circuit construction and repair |

### 5.2 Quality tiers

Resources may have Standard, Sport, and Competition quality after the basic game is proven.
Quality affects service value and personal-car performance, but the physical silhouette stays
the same with a color band. Quality is a late-game optimization, not an early tutorial burden.

### 5.3 Car condition

Owned cars have four non-destructive condition values:

- Engine condition.
- Tire condition.
- Body condition.
- Cleanliness.

Condition slowly reduces the bonus portion of performance or race rewards; the car never
becomes unusable. A player can always practice. Ranked entry requires only a safety inspection,
not perfect condition.

---

## 6. Every Building: Purpose, Actions, Economy, and Racing Link

All buildings have five levels. A building level changes its silhouette, visible equipment,
capacity, staff slots, recipes, and one meaningful behavior. Pure percentage tiers are secondary.

### 6.1 Parts Delivery

**Fantasy:** trucks unload the root materials that feed the whole village.

- Player actions: collect crates; choose the next delivery contract; rush unloading; fund loading bays.
- Inputs: cash for premium contracts, delivery time, unlocked resource catalog.
- Outputs: tires, oil, paint, cleaning supplies, panels, and generic part crates.
- Automation: transporter unloads and routes reserved stock to warehouse zones.
- Upgrades: pallet capacity, delivery interval, second dock, contract quality, auto-sort.
- Bottleneck cue: truck waiting, overflowing pallet, or empty marked delivery slot.
- Racing link: delivers replacement tires, fuel-cell cosmetics, cones, barriers, and track materials.
- Failure policy: missed capacity pauses the truck; stock is never deleted.

### 6.2 Parts Warehouse

**Fantasy:** the central logistics brain and multi-resource buffer.

- Player actions: deposit/collect; set minimum stock targets; prioritize urgent stations.
- Inputs: all delivered basic parts and crafted overflow.
- Outputs: reserved pick orders for stations, owned-car work, and circuit construction.
- Automation: warehouse workers claim restock tasks using urgency and distance.
- Upgrades: per-resource capacity, additional loading door, faster sorting, reservation range.
- Bottleneck cue: tall source piles, empty destination shelves, worker warning icon.
- Racing link: stores tuning parts and track construction inventory.
- Strategic choice: high stock protects throughput but ties up cash in inventory contracts.

### 6.3 Tire Storage

**Fantasy:** visible tire towers and fast local buffer for common jobs.

- Player actions: carry tire stacks, rotate old stock, reserve a set for a personal car.
- Inputs: Standard, Sport, and Competition tires.
- Outputs: tire sets to Basic Bay, Wheel & Tire, and owned-car garage slots.
- Automation: tire technician or transporter restocks consumers below target.
- Upgrades: rack slots, handling speed, tire-quality access, smart reservations.
- Bottleneck cue: empty racks while tire customers queue.
- Racing link: tire compound selection changes grip, wear, and class performance points.

### 6.4 Engine Workshop

**Fantasy:** build and repair the expensive heart of a vehicle.

- Player actions: supply engine components; select craft recipe; assist assembly; claim completed unit.
- Inputs: engine components, oil, electronics, cash for owned-car jobs.
- Outputs: repaired engines, sport engines, turbochargers, contract components.
- Automation: engine specialist supplies and works the active bench.
- Upgrades: second bench, craft speed, quality chance, component efficiency, dyno station.
- Bottleneck cue: open engine stand with missing-part icons or completed engines blocking output.
- Racing link: engine packages alter acceleration and top speed; dyno produces verified performance data.
- Balance rule: ranked fair-spec mode ignores owned engine power; garage-build mode uses it.

### 6.5 Body Repair

**Fantasy:** visibly restore damaged customer and trade-in cars.

- Player actions: deliver panels; select repair priority; rush bodywork; approve a restored trade-in.
- Inputs: panels, fasteners, exhaust parts, damaged vehicles.
- Outputs: repaired customer car, improved resale condition, cosmetic body slots.
- Automation: body technician handles one vehicle panel at a time.
- Upgrades: work bays, repair speed, material efficiency, restoration quality.
- Bottleneck cue: damaged car remains on lift with highlighted missing panel.
- Racing link: repairs visual damage after events; unlocks bumpers, spoilers, and weight-neutral cosmetics.
- Failure policy: collision damage never permanently destroys a personal car.

### 6.6 Paint Mixing

**Fantasy:** a colorful small production chain with visible recipe choices.

- Player actions: carry paint cans; select requested swatch; start mixing; collect mixed paint.
- Inputs: two paint cans plus a selected color recipe.
- Outputs: mixed paint batches tagged by color and finish.
- Automation: paint technician follows queued customer and personal-car orders.
- Upgrades: batch size, mixing speed, color library, metallic/pearlescent finishes.
- Bottleneck cue: booth requests a visible swatch while mixer is empty or blocked.
- Racing link: creates car liveries, curb palettes, banners, and track theme colors.

### 6.7 Paint Booth

**Fantasy:** transform a car visually and increase its value.

- Player actions: deliver mixed paint; queue customer or owned car; choose unlocked livery layers.
- Inputs: clean vehicle, mixed paint, optional decal token.
- Outputs: painted vehicle, quality bonus, cosmetic preset.
- Automation: painter performs masking, paint, and finish stages.
- Upgrades: booth speed, finish quality, second queue slot, decal layer count.
- Bottleneck cue: clean unpainted cars wait beside the booth with swatch bubbles.
- Racing link: owned livery and racing number are saved into every ghost and showcase card.
- Competitive rule: all paint and decal choices are cosmetic only.

### 6.8 Reception

**Fantasy:** convert arrivals into readable service tickets.

- Player actions: assist check-in by proximity; review priority contract; collect vehicle key.
- Inputs: arriving customer, vehicle state, unlocked recipes, customer preference.
- Outputs: service ticket, patience timer, queue reservation, vehicle key.
- Automation: receptionist checks in and later hands off completed keys.
- Upgrades: check-in speed, ticket accuracy, customer patience, premium-customer chance.
- Bottleneck cue: people line up outside while vehicles occupy check-in stalls.
- Racing link: registers the player identity, race team name, and incoming social notifications.

### 6.9 Customer Queue

**Fantasy:** a physical visualization of demand and routing efficiency.

- Player actions: build slots; choose service priority; temporarily close a saturated lane.
- Inputs: service tickets and available bay reservations.
- Outputs: ordered dispatch to compatible stations.
- Automation: QueueManager advances cars through discrete reserved slots.
- Upgrades: slot count, parallel lane, VIP lane, routing preview.
- Bottleneck cue: brake lights, mood bubbles, and soft honks.
- Racing link: event staging uses the same reservation system for starting grids and pit entry.
- Safety rule: customer AI never shares free-driving physics with the player car.

### 6.10 Basic Change Bay

**Fantasy:** fast common service and the first complete production loop.

- Player actions: supply parts; rush mechanic; choose repair order when multiple recipes are ready.
- Inputs: tires, oil, brakes, battery, assigned mechanic, customer car.
- Outputs: serviced vehicle, payment value, used-parts visual waste.
- Automation: mechanic consumes local rack stock and completes timed recipe.
- Upgrades: rack capacity, work speed, second lift, mechanic slot, quality.
- Bottleneck cue: full queue with stocked rack means bay speed is the problem.
- Racing link: performs basic maintenance on owned cars and restores condition bonuses.

### 6.11 Wheel & Tire Station

**Fantasy:** dedicated high-throughput tire fitting and setup.

- Player actions: deliver tire set; choose street/sport/competition compound; adjust simple pressure preset.
- Inputs: tire set, wheel option, vehicle.
- Outputs: fitted customer car or owned-car handling preset.
- Automation: tire technician balances and installs wheels.
- Upgrades: fitting speed, quality, compound access, alignment rig.
- Bottleneck cue: loose wheel stacks and cars waiting on marked tire lane.
- Racing link: compound and alignment trade grip, stability, and class points.
- Mobile simplification: presets are Stable, Balanced, and Agile; no numerical simulator UI.

### 6.12 Car Wash

**Fantasy:** quick, visually satisfying value-add service.

- Player actions: deliver cleaning supply; activate rush spray; route dirty owned car.
- Inputs: cleaning supply, dirty vehicle, worker or automatic gantry.
- Outputs: cleanliness, tip bonus, eligibility for Paint/Detailing.
- Automation: washer runs foam, rinse, and dry stages.
- Upgrades: speed, supply efficiency, second queue slot, automatic dryer.
- Bottleneck cue: dirty cars line up with foam icon; supply tank visibly empty.
- Racing link: clean-car bonus grants presentation XP, never lap-time performance.

### 6.13 Detailing Station

**Fantasy:** premium quality work that converts time into large tips and prestige.

- Player actions: choose interior/exterior/full package; supply two cleaning items; rush finishing.
- Inputs: cleaned car, supplies, detailer quality.
- Outputs: premium tip, reputation, showroom presentation rating.
- Automation: detailer works staged zones around the vehicle.
- Upgrades: package speed, quality, premium material access, second work position.
- Bottleneck cue: clean cars wait with gold sparkle request.
- Racing link: unlocks photo-mode poses, podium presentation, and showcase thumbnails.

### 6.14 Diagnostic Station

**Fantasy:** reveal hidden problems and profitable opportunities.

- Player actions: bring electronics; start scan; select one of the discovered recommendations.
- Inputs: vehicle, electronic module, specialist accuracy.
- Outputs: exact condition report, upsell, performance-point estimate, fault list.
- Automation: specialist scans customer, trade-in, and owned cars.
- Upgrades: scan speed, upsell accuracy, advanced telemetry, setup comparison.
- Bottleneck cue: vehicle displays unresolved warning icons.
- Racing link: telemetry compares braking, racing line, speed loss, and setup suitability.
- Fairness rule: telemetry advises; it never auto-drives a ranked lap.

### 6.15 Tuning Station

**Fantasy:** turn workshop components into a distinct driving build.

- Player actions: install parts; select handling preset; test drive; save named setup.
- Inputs: suspension, exhaust, turbo/engine package, electronics, cash.
- Outputs: owned-car setup, higher service value, performance-point rating.
- Automation: tuner completes queued installation while player is away.
- Upgrades: installation speed, setup slots, advanced parts, reversible experimentation discount.
- Bottleneck cue: prepared component waits beside an occupied tuning lift.
- Racing link: central garage-build progression for acceleration, speed, grip, braking, and stability.
- Fairness rule: every setup receives a Performance Point value and valid class.

### 6.16 Vehicle Inspection

**Fantasy:** trusted quality and competitive legality gate.

- Player actions: submit a customer, trade-in, owned car, or published track for certification.
- Inputs: vehicle condition/build or track validation report.
- Outputs: safety certificate, race class, resale quality, defect ticket.
- Automation: inspector processes standard customer jobs; player cars show detailed result.
- Upgrades: inspection speed, defect accuracy, additional lane, higher-class certification.
- Bottleneck cue: uncertified cars wait behind cones and barrier.
- Racing link: assigns class and Performance Points; validates checkpoints, boundaries, and track budget.
- Anti-cheat role: published challenge references immutable inspection and build hashes.

### 6.17 Completed Car Delivery

**Fantasy:** satisfying final handoff where the service chain pays out.

- Player actions: carry key; collect payment; optionally take completion photo.
- Inputs: completed ticket, certified car where required, customer patience.
- Outputs: cash pile, tip, reputation, customer departure.
- Automation: cashier/key runner completes handoff and collects payment.
- Upgrades: handoff speed, payment magnet range, queue slots, presentation bonus.
- Bottleneck cue: completed cars and waiting owners accumulate visibly.
- Racing link: event rewards, trophies, and returned loan cars arrive here physically.

### 6.18 Used Car Showroom

**Fantasy:** buy low, refurbish through the whole village, sell high, or keep the car.

- Player actions: inspect incoming listings; buy; choose refurb plan; list; test drive; keep.
- Inputs: cash, trade-in vehicle, inspection report, completed refurb services.
- Outputs: sale profit, collection car, parts donor, reputation.
- Automation: salesperson matches broad buyer demand and closes standard deals.
- Upgrades: display slots, listing refresh, buyer traffic, negotiation margin.
- Bottleneck cue: empty display means acquisition shortage; full display means sales shortage.
- Racing link: primary source of affordable player cars with varied starting condition.
- Anti-frustration: one free listing refresh daily; no blind real-money loot boxes.

### 6.19 Premium Car Showroom

**Fantasy:** aspirational collection, high-value buyers, and prestige presentation.

- Player actions: order unlocked models; configure showroom display; match car to buyer preference.
- Inputs: reputation gate, cash, premium stock, paint/detail/inspection quality.
- Outputs: large sales, rare owned cars, cosmetics, sponsorship offers.
- Automation: senior salesperson handles matching within player-defined minimum margin.
- Upgrades: premium slots, buyer quality, delivery cadence, showcase stage.
- Bottleneck cue: unmatched buyer preference icons appear beside displayed cars.
- Racing link: sports and race classes unlock here after league/reputation milestones.
- Fairness rule: competitive fair-spec cars are loaned even if the player does not own the model.

### 6.20 Employee Room

**Fantasy:** the human side of automation and the visible staff headquarters.

- Player actions: hire, assign zone, train stats, set uniforms, review workload.
- Inputs: cash, unlocked roles, training time.
- Outputs: employees, role assignments, automation capacity.
- Automation: resting is visual only; it does not create punitive stamina management.
- Upgrades: staff slots, training speed, veteran tier, cross-zone claim range.
- Bottleneck cue: workload board shows role-colored task backlog bars.
- Racing link: unlock pit crew roles that reduce non-driving event turnaround, not lap time.
- Management rule: workers are smart within assigned zones but never spend player currency.

### 6.21 Office & Finance

**Fantasy:** strategic control center for prices, contracts, analytics, and social play.

- Player actions: select contract; buy financial upgrades; collect offline report; publish/challenge tracks.
- Inputs: business statistics, reputation, circuit certification, online inbox.
- Outputs: price modifiers, arrival policy, contracts, challenge board, league status.
- Automation: accountant settles offline income and completed contract rewards.
- Upgrades: price tiers, offline cap, contract slots, market forecast, social board cosmetics.
- Bottleneck cue: contract cards show blocked prerequisite building icons.
- Racing link: circuit browser, attack inbox, defensive record, leagues, seasonal results.
- Security rule: server-confirmed competitive rewards enter through an idempotent reward inbox.

---

## 7. Player-Owned Cars

### 7.1 Acquisition

- Starter car earned through the first village milestone, not purchased.
- Used cars bought from rotating known listings with visible condition.
- Premium cars unlocked by reputation and purchased directly.
- Special cars earned through championships, long contracts, or collection milestones.
- Fair-spec event loaners guarantee access to ranked competition.

### 7.2 Car data

Each owned car stores:

- Stable unique ID and model ID.
- Acquisition source and ownership date.
- Engine, tire, body, and cleanliness condition.
- Installed parts and calculated Performance Points.
- Saved tuning presets.
- Paint, decals, number, wheels, and cosmetic attachments.
- Personal bests per immutable track version.
- Race statistics, mileage, wins, and favorite flag.

### 7.3 Performance stats

- Acceleration.
- Top speed.
- Braking.
- Grip.
- Steering response.
- Stability.
- Weight class.

The UI presents bars and clear deltas. Advanced underlying values remain data-driven.

### 7.4 Race classes

| Class | Example role | PP range |
|---|---|---|
| C | starter compact, stock used car | 0-299 |
| B | repaired street car | 300-449 |
| A | tuned sport car | 450-599 |
| S | premium/high-performance car | 600-749 |
| R | dedicated race car | normalized event rules |

Exact ranges require handling telemetry. A setup that exceeds a class moves up automatically;
the player can save a lower-class legal setup at the Tuning Station.

### 7.5 Driving modes

1. Village free-roam: relaxed, no rewards, instant reset.
2. Workshop test drive: compare setup changes on a short loop.
3. Circuit practice: unlimited laps, local ghost options.
4. Certification lap: validates a track version and establishes defender record.
5. Fair Challenge: normalized car, ranked skill mode.
6. Garage Challenge: owned setup within class/PP restrictions.
7. Contract event: authored objective, traffic/cones or time target.

---

## 8. Village Circuit Builder

### 8.1 Unlock and placement

The circuit plot unlocks after the player has:

- Built Tuning, Diagnostic, and Inspection.
- Acquired one owned car.
- Completed a clean test-drive contract.
- Reached the first motorsport reputation milestone.

The circuit occupies an expandable plot beside or behind the village. It is loaded as a
separate editing layer so large track layouts do not clutter management interactions.

### 8.2 Asset families from `kenney_racing-kit`

- Straights and long straights.
- Curves, wide curves, chicanes, intersections where legal.
- Start grid and start/finish line.
- Pit entry, pit straight, pit garage road.
- Red/white barriers and walls.
- Fences, flags, billboards, lights, tire stacks, grandstands, and trees.
- Race-car models for fair-spec or later race class content.

Use prefabs with shared materials and colliders. Decoration has a separate budget from
driveable track pieces and cannot change the collision envelope after publication.

### 8.3 Editing model

- Grid snap based on the racing-kit module size.
- Rotation snap at valid connector angles.
- Connector sockets highlight green when compatible.
- Drag preview shows cost, direction, and collision footprint.
- Replace operation preserves following connected pieces where possible.
- Undo/redo history of at least 30 edits.
- Draft autosave after each edit transaction.
- Three draft slots initially; more slots are permanent convenience upgrades.

### 8.4 Construction budget

Every piece has:

- Cash construction cost.
- Track Complexity units for performance control.
- Connector type and lane width.
- Driveable bounds and no-build margin.
- Mobile rendering cost category.

Track plot upgrades increase area and complexity budget. They do not improve race rewards
directly; creative and technically good layouts should beat raw spending.

### 8.5 Required objects

- Exactly one start/finish line.
- At least three ordered checkpoints.
- One continuous legal lap route.
- Spawn grid with collision-safe positions.
- Reset anchors at safe intervals.
- Closed outer boundaries where a shortcut could leave the track.

Pit lane, timing sectors, scenery, and alternate visual routes are optional.

### 8.6 Validation pipeline

Publishing requires all checks:

1. Connector graph forms one continuous start-to-finish lap.
2. No track collision overlaps or impossible height changes.
3. Checkpoints are ordered and span the full lane width.
4. Start grid and reset anchors are unobstructed.
5. No shortcut from checkpoint N to N+2 beats the legal path tolerance.
6. Track length is inside the current league range.
7. Complexity, renderer, collider, and prop budgets pass mobile limits.
8. Creator completes one clean certification lap.

An automated validation car can check continuity and approximate reachability, but only the
human certification lap proves the track is publishable.

### 8.7 Publication and versioning

- Publishing creates immutable `TrackVersionId` and content hash.
- Editing a published track creates a new draft; it never changes active challenges.
- Defender ghost, rules, car class, and collision data belong to that version.
- Old versions remain playable from challenge history for a limited retention period.
- Creator may unlist a track, but completed results and earned rewards remain.
- One track is selected as the village's active defensive circuit.

---

## 9. Asynchronous Ghost Challenges

### 9.1 Defender flow

1. Select a certified published track version.
2. Select Fair Spec or Garage Build rules.
3. Complete a clean reference lap.
4. Upload track manifest, rules, car/build hash, ghost, and validation evidence.
5. Circuit appears in matchmaking after server validation.
6. Receive challenge results through Office inbox and world notifications.

### 9.2 Challenger flow

1. Open Office challenge board.
2. Choose recommended opponent, friend, code, revenge, or league browser.
3. Download small immutable track and ghost payload.
4. Inspect track preview, class, reference time, and possible reward.
5. Select legal owned setup or accept fair-spec loan car.
6. Practice or start reward-bearing attack.
7. Race locally against translucent ghost and sector deltas.
8. Upload result evidence; receive provisional feedback immediately.
9. Server validates and settles rating/reward through inbox.

### 9.3 Ghost representation

Preferred production format:

- Fixed-rate input samples at 20 Hz.
- Key transform checkpoints at 5 Hz for correction and validation.
- Car setup hash, physics version, track version, random seed, and start state.
- Event markers for checkpoint crossing, reset, collision severity, and finish.

Playback uses input simulation where deterministic enough and smoothly corrects toward key
transforms. The ghost has no collision with the challenger.

### 9.4 Challenge rule sets

**Fair Challenge**

- Server-defined car and setup.
- Same physics version and conditions.
- Primary ranked mode.
- Best expression of track knowledge and driving skill.

**Garage Challenge**

- Player-owned car within class and PP cap.
- Build decisions matter, but matchmaking includes PP and class.
- Separate rating and leaderboard from Fair Challenge.

**Friends Challenge**

- Any agreed rules.
- No exploitable economy payout after first completion.
- Share code/deep link and optional rematch chain.

### 9.5 Matchmaking

Recommended opponent score combines:

- Championship Rating proximity.
- Track difficulty preference.
- Expected win probability of 40-60%.
- Opponent freshness and unique-player diversity.
- Download region/latency only for payload reliability, not real-time play.

Players can always challenge friends regardless of rating.

### 9.6 Rating

- Separate Fair and Garage ratings.
- Placement series of five completed challenges.
- Rating changes use expected-result logic with uncertainty for new players.
- Defending a record does not lose rating while offline; rating changes when a player actively
  enters ranked attacks. Defensive wins grant capped rewards and profile statistics instead.
- Seasonal soft reset compresses ratings, never wipes collection or village progress.

### 9.7 Race validity

A clean ranked lap requires:

- All checkpoints in order.
- No reset usage unless event explicitly allows one with time penalty.
- No boundary breach beyond tolerance.
- No impossible acceleration, velocity, rotation, or time delta.
- Correct track, car build, physics, and content hashes.
- Monotonic timer and valid start countdown.

### 9.8 Anti-cheat model

- Server owns ranked reward and rating settlement.
- Client uploads signed run envelope plus compressed evidence.
- Validation re-simulates critical sections or checks motion envelopes.
- Impossible runs are quarantined, not immediately exposed as public accusations.
- Leaderboard top percentile receives stricter replay validation.
- Physics-version changes invalidate cross-version ranked comparison but preserve historical records.
- Rate-limit uploads and repeated opponent farming.
- Local save edits cannot grant server currencies, rating, or published records.

### 9.9 Offline behavior

- Downloaded friend tracks remain available for practice.
- Offline results are personal bests only until validated online.
- No rating or ranked currency is promised before server confirmation.
- Management game remains fully playable offline.

---

## 10. Contracts, Objectives, and Retention

### 10.1 Contract types

- Serve N tire customers.
- Complete N premium services above a quality target.
- Keep all queues below a threshold for a timed period.
- Refurbish and sell a used vehicle above target margin.
- Prepare an owned car to a target class.
- Complete a clean lap.
- Beat a developer-authored ghost.
- Beat a player ghost within the recommended rating range.
- Publish a valid circuit of a target length or theme.

### 10.2 Objective hierarchy

- **Immediate objective:** one visible physical action.
- **Session objective:** one building, hire, or upgrade.
- **Daily contract:** 5-15 minute mixed objective.
- **Weekly milestone:** business plus racing goal with flexible contribution.
- **Season:** league progression and cosmetic track theme.

No daily streak should reset to zero. Missed days pause progress instead of punishing return.

### 10.3 Achievements

- First automated service chain.
- First owned car.
- First clean lap.
- First published track.
- First challenger defeated.
- Ten unique challengers.
- Track played 100 times.
- Win with each class.
- Restore a low-condition used car to premium quality.
- Fully automate every building.

---

## 11. Progression Roadmap

### Chapter 0 - First 10 minutes

- Walk, collect tires, supply Basic Bay, collect payment.
- Build Tire Storage.
- Understand visible bottlenecks.

### Chapter 1 - Service village, 10-60 minutes

- Unlock Reception, Delivery, Parts Warehouse, Employee Room, Car Wash.
- Hire transporter, mechanic, receptionist.
- Reach stable automated income.

### Chapter 2 - Ownership and driving, hour 1-2

- Receive starter car.
- Enter/exit and free-roam tutorial.
- Use Basic Bay, Wash, Diagnostic, and Inspection on the personal car.
- Complete first authored driving contract.

### Chapter 3 - Performance workshop, hours 2-5

- Unlock Engine Workshop, Tuning, Wheel & Tire.
- Create first B-class setup.
- Compare telemetry and save setup preset.

### Chapter 4 - Creative circuit, hours 4-8

- Unlock circuit plot and starter piece set.
- Build guided first loop.
- Pass validation and certification lap.
- Publish locally and race own ghost.

### Chapter 5 - Social competition, day 2+

- Unlock Office Challenge Board.
- Beat developer ghost before player matchmaking.
- Complete placement series.
- Receive first defensive challenge result.

### Chapter 6 - Dealership and collection, day 3+

- Unlock Used Showroom, refurb chain, and keep/sell choice.
- Unlock premium stock through reputation and leagues.
- Expand circuit plot and visual themes.

### Endgame

- Optimize all service chains.
- Collect and prepare every car class.
- Maintain multiple circuit drafts and one active defense.
- Compete in seasons and authored championships.
- Prestige a completed village for permanent cosmetic/convenience progression.

---

## 12. User Experience

### 12.1 Management HUD

- Cash.
- Current objective.
- Village progress/reputation.
- Menu.

Everything else remains contextual or in-world.

### 12.2 Driving HUD

- Left/right steering hold controls.
- Throttle and brake/reverse hold controls.
- Exit button.
- Speed and current gear direction.
- Race-only timer, sector delta, reset, and pause.

Do not show management alerts during an active timed lap. Queue them for return to village.

### 12.3 Track editor HUD

- Piece palette by category.
- Rotate, replace, remove, undo, redo.
- Budget and validation status.
- Test and publish commands.
- Context tooltip only for invalid placement reason.

### 12.4 Accessibility

- Steering sensitivity and left/right control layout options.
- Optional steering assist in unranked and lower leagues.
- Colorblind-safe icon shapes in addition to station colors.
- Reduced haptics, motion, particles, and camera shake.
- Ghost opacity slider and hide-ghost option after countdown.
- Larger UI scale and high-contrast racing line option.

---

## 13. Technical Architecture

### 13.1 Local systems

- Data-driven resources, recipes, buildings, cars, parts, track pieces, and events.
- Versioned local save with atomic write and backup.
- Object pooling for cars, resources, customers, VFX, and track props.
- Separate vehicle controllers for player physics and scripted customer traffic.
- Track graph generated from connector metadata, not object names.
- Fixed-timestep race timer and checkpoint authority.
- Ghost recorder isolated from rendering and UI.

### 13.2 Online services required for social release

- Authentication: platform account plus anonymous fallback upgrade path.
- Player profile and public display name moderation.
- Track manifest/object storage with versioned CDN payload.
- Ghost upload, validation queue, and replay storage.
- Matchmaking and separate ratings.
- Idempotent reward inbox.
- Leaderboards and seasonal configuration.
- Block/report controls and track unlisting.
- Remote-config balancing for rewards, not physics.

The first circuit prototype should be local-only. Backend work starts only after track building
and ghost racing are fun against developer ghosts.

### 13.3 Core data definitions

- `BuildingDefinition`: levels, recipes, workers, pads, visual stages.
- `ResourceDefinition`: slot cost, quality, source, destinations.
- `CarDefinition`: base stats, class, model, compatible parts.
- `PartDefinition`: stat modifiers, PP cost, install station.
- `OwnedCarState`: instance condition, setup, cosmetics, history.
- `TrackPieceDefinition`: connectors, bounds, costs, complexity, prefab.
- `TrackDraft`: placed pieces, props, checkpoints, local revision.
- `PublishedTrackVersion`: immutable manifest and content hash.
- `GhostRun`: samples, keyframes, hashes, events, time.
- `ChallengeResult`: opponent, mode, validity, time, rating delta, reward state.

### 13.4 Mobile budgets

- Track piece meshes use shared materials and GPU instancing where possible.
- One real-time directional light; no per-lamp real-time lights.
- Static track decoration is batched.
- Maximum published track complexity is device-tier independent for fairness.
- Target 60 fps on reference device, 30 fps floor on minimum supported device.
- Ghost playback adds no physics collision and minimal CPU work.
- Download payload target: track plus ghost below 500 KB before optional thumbnail.

---

## 14. Production Plan and Exit Criteria

### Phase A - Complete management vertical slice

Build Reception -> Queue -> Basic Bay -> Delivery, Parts Delivery, Tire Storage,
Employee Room, and Office with cash, construction, upgrades, save, and offline earnings.

Exit criteria:

- 30-minute business loop has no deadlock.
- At least one meaningful upgrade every 1-3 minutes.
- Player can identify seeded bottleneck in under five seconds.
- Full loop survives save/load and app interruption.

### Phase B - Personal-car vertical slice

Build one owned car, condition, Basic Bay maintenance, Diagnostic, Inspection, Tuning,
third-person driving, test loop, and mobile driving HUD.

Exit criteria:

- Enter, drive, reset, and exit are reliable.
- Three setup presets feel observably different.
- Stable 60 fps with management agents active.
- No customer AI uses player-car physics.

### Phase C - Local circuit builder

Import selected `kenney_racing-kit` pieces as socketed prefabs. Implement editor,
validation, draft save, certification lap, local best, and self ghost.

Exit criteria:

- New player builds a legal first track in under ten minutes.
- Invalid route explanations are clear without documentation.
- Published local version cannot change when draft is edited.
- Twenty varied generated drafts pass performance budget.

### Phase D - Developer ghost competition

Add fair-spec cars, authored tracks, sectors, ghost playback, challenge results,
and progression rewards without online services.

Exit criteria:

- Ghost remains visually stable and non-blocking.
- Race result is deterministic within accepted tolerance.
- Losing encourages retry rather than management grinding.
- Fair challenge has no owned-upgrade advantage.

### Phase E - Online asynchronous alpha

Add accounts, immutable uploads, validation, matchmaking, reward inbox, friends codes,
rating, reports, and operational dashboards.

Exit criteria:

- Duplicate result submission cannot duplicate rewards.
- Modified local save cannot grant ranked reward.
- Track/ghost download and play succeeds above 99% in test cohort.
- Invalid top results are automatically quarantined.

### Phase F - Content and soft launch

Finish all buildings, dealership loop, garage-build league, seasons, cosmetics,
analytics, device matrix, onboarding, and economy tuning.

Exit criteria:

- Tutorial completion above 90% in observed tests.
- No purchase dead zone above five active minutes.
- Day-one players reach personal driving before fatigue.
- Social mode improves return rate without reducing management engagement.

---

## 15. Recommended First Fully Playable Release

Do not attempt all systems simultaneously. The first public-quality release should contain:

- One complete village location.
- Ten functional buildings: Parts Delivery, Warehouse, Tire Storage, Reception, Queue,
  Basic Bay, Car Wash, Delivery, Employee Room, Office.
- Three resources and three customer services.
- Three employee roles.
- One starter owned car plus two unlockable cars.
- Basic maintenance, Diagnostic, Inspection, and Tuning for owned cars.
- One expandable circuit plot with 12-18 racing-kit piece types.
- Local builder, validation, certification, and self ghost.
- Six developer tracks/ghosts across C and B classes.
- Local save, offline earnings, accessibility, audio, and haptics.

Online player publishing follows as the next milestone after telemetry proves that players
finish tracks, replay ghosts, and understand fair-spec versus garage-build rules.

---

## 16. Features Explicitly Deferred

- Real-time multiplayer racing.
- Open-world traffic with shared free-driving physics.
- Real-money performance parts.
- Fuel that blocks normal play.
- Car destruction or permanent loss.
- Random paid car loot boxes.
- Complex numerical tuning before presets are proven.
- Player-to-player cash trading.
- User-authored text or images on tracks at initial social launch.
- Dynamic weather affecting ranked fairness.
- Clubs, chat, tournaments, and spectator mode before core ghost challenges are stable.

---

## 17. Product Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Management and racing feel like separate games | Every building has an explicit owned-car or racing link; shared reputation and cars connect loops |
| Track editor overwhelms casual players | Guided first circuit, socket snapping, starter templates, automatic validation |
| Pay-to-win perception | Fair Challenge is primary ranked mode; cosmetics-only monetization; garage mode separated |
| Bad or impossible user tracks | Graph validation, budgets, certification lap, reports, unlisting |
| Cheated records | Immutable hashes, evidence upload, server settlement, top-run revalidation |
| Economy farming through friends | Unique-opponent caps, reward decay, server idempotency |
| Physics update invalidates records | Physics version embedded in every ghost and leaderboard partition |
| Mobile performance collapses on large tracks | strict complexity budget, shared assets, instancing, device profiling |
| Automation makes player irrelevant | construction, tuning choices, driving, rush aura, contracts, creative track work |
| Scope becomes unshippable | local management -> owned car -> local builder -> developer ghosts -> online, each with kill gate |

---

## 18. Success Metrics

### Fun and comprehension

- 90% complete first service without text explanation.
- 80% enter and drive starter car within the intended unlock session.
- 75% of players who open builder save a valid draft.
- 60% of valid-track creators complete certification.
- At least 40% of ghost challengers immediately retry a close loss.

### Economy

- Median next-purchase wait remains 1-3 active minutes.
- No required racing gate blocks management progression.
- No building remains unused after its tutorial milestone.
- Offline earnings accelerate return but remain below active play rate.

### Social health

- Recommended challenge expected win rate stays near 50%.
- Invalid upload rate remains below 1% excluding connectivity failures.
- Unique opponent diversity increases through the week.
- Defender notifications generate returns without spam.

### Technical quality

- Zero duplicate reward grants.
- Zero published-track mutation.
- No race deadlocks or missed checkpoint false positives in regression suite.
- Stable frame budget on minimum supported iPhone with ghost and full track.

---

## 19. Final Design Decisions

1. The village management loop remains the economic foundation.
2. Personal cars are persistent collection objects, not disposable race selections.
3. Ranked competition is asynchronous ghost racing, not real-time multiplayer.
4. Fair-spec and garage-build competition use separate ratings and leaderboards.
5. A published circuit version is immutable and requires a certification lap.
6. The Inspection building is the authority for car class and circuit legality.
7. The Office is the physical home of contracts, publishing, matchmaking, and rewards.
8. Racing rewards complement business income but never gate basic construction.
9. `kenney_racing-kit` supplies modular circuit and decoration assets under strict budgets.
10. Online infrastructure begins only after local track creation and ghost racing are fun.


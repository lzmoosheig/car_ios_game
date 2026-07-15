# Overhaul! — Doc 04: Economy & Balancing

All values are **placeholders for tuning** but internally consistent with the Level 1 walkthrough (Doc 03 §1.3). Everything here lives in data (Doc 06 §4), not code.

---

## 1. Revenue model

### 1.1 Service revenue formula
```
revenue = basePrice × locationMult × priceUpgradeMult × (1 + tip)
tip     = tipBase × patienceFactor × qualityFactor        (0 … +0.75 of basePrice)
```
- `locationMult`: L1 = 1.0, roughly ×2.2 per location (L2 = 2.2, L3 = 4.8 …)
- `priceUpgradeMult`: +10% per Office price tier (level-local, 5 tiers)
- `patienceFactor`: 1.5 if served < 20 s total, 1.0 normal, 0.75 after 30 s queue wait, 0 after 75 s (Doc 02 §3.2)
- `qualityFactor`: 1.0 + staff quality bonuses (Doc 02 §4.3)

### 1.2 Base prices & durations (Level 1 values)

| Service | basePrice | Work duration | Parts consumed |
|---|---|---|---|
| Tire change | $20 | 6 s | 4× Tire |
| Oil change | $30 | 8 s | 2× Oil Container |
| Basic repair (brakes/battery) | $45 | 12 s | 2× Brake Disc or 1× Battery |
| Wash (add-on) | $15 | 10 s | 1× Cleaning Supplies |
| Detailing | $60 | 15 s | 2× Cleaning Supplies |
| Diagnostic | $25 | 5 s | 1× Electronic Module |
| Tuning | $120 | 20 s | 1× Turbocharger + 1× Suspension Kit |
| Paint job | $150 | 12 s | 2× Mixed Paint |
| Body repair | $100 | 18 s | 2× Body Panel |
| Used-car resale | buy 0.5× value → sell 1.0× | — | refurb services at cost |

## 2. Cost model

### 2.1 Construction costs
Guideline curve: `cost(n) ≈ 30 × 1.45^n` for the n-th zone in a location, then **hand-tuned per level**. Level 1's authored sequence (matches Doc 03 §1.3):

`$30 (tire rack) → $80 (oil storage) → $120 (oil bay) → $150 (employee room) → $200 (reception) → $400 (2nd repair bay) → $600 (car wash) → $2,500 (City Contract / completion)`

### 2.2 Hiring & employee upgrades
- Hire cost: `250 × 1.7^(k−1)` for the k-th hire in a location ($250, $425, $723 …); transporter/cashier/mechanic pads may carry small hand-tuned offsets ($250 / $300 / $400 in L1).
- Employee stat tier: `60 × 1.5^t` per tier ($60, $90, $135, $203, $304).

### 2.3 Workstation upgrades (kiosk / Office)
- Station speed or capacity tier: `100 × 1.6^t` ($100, $160, $256, $410, $655); effect **+15% speed** or **+3 rack capacity** per tier, 5 tiers, level-local.

## 3. Upgrade catalog — local vs permanent

| Upgrade | Scope | Curve / tiers |
|---|---|---|
| Workstation speed / capacity | **level-local** | §2.3, 5 tiers each |
| Parts production rate (per source) | **level-local** | $100 × 1.6^t, +25%/tier, 4 tiers |
| Service-bay count, queue slots | **level-local** | construction zones |
| Worker speed / capacity / quality / checkout | **level-local** | §2.2 |
| Customer arrival rate / patience / prices | **level-local** (Office) | $150 × 1.6^t, 5 tiers |
| Showroom capacity | **level-local** | construction zones |
| Player move speed (5), carry capacity (6), radius (2) | **permanent** | Golden Wrenches: 2, 3, 5, 8, 12 🔧 |
| Offline-earnings cap 2 h → 8 h (4 tiers) | **permanent** | 3, 6, 10, 15 🔧 |
| "Veteran staff" (+1 base tier all hires) | **permanent** | 10 🔧 |
| Cosmetic themes, character skins | **permanent** | 🔧 or IAP (post-MVP) |

Golden Wrenches 🔧 are earned only from location completions (3–8 per location) and prestige (L10 mechanic).

## 4. Flow rates & pacing

| Parameter | Formula / value |
|---|---|
| Customer arrival interval | `max(6 s, 20 s × 0.94^zonesBuilt)`; pauses while all queue slots are full (honk soft cap) |
| Parts generation (pallet) | 1 part / 2 s, capacity 12; +25% rate per tier |
| Player carry / speed | 5 items, 4.0 m/s base (Doc 02 §1) |
| Offline earnings | `automatedRate × min(elapsedTime, cap) × 0.4`; `automatedRate` counts only fully-staffed chains; cap 2 h base → 8 h; presented as a cash pile at the garage gate on return |
| Level completion | all core zones built **+** fund City Contract ($2,500 in L1; scales ×2.2/location) |

### 4.1 Pacing targets (Level 1, must match Doc 03 §1.3)
- First construction funded **< 60 s** ($30 rack after two $20 customers)
- Income ≈ $50/min at minute 1 → ≈ $100/min at minute 5 → ≈ $200/min at minute 15
- First hire ≈ minute 6 ($250); full automation of L1 chores ≈ minute 14
- Level 1 complete in **45–60 min**; each later location ≈ +20–30% duration
- Rule of thumb: the *next* purchase is always 1–3 minutes of income away; the *level goal* is always visible but ~10× the next purchase

## 5. Bottleneck design & visual language

Bottlenecks are the gameplay. Each has a *designed cause*, a *unique visual cue* (never text), and an *intended fix*:

| # | Bottleneck | Visual cue in world | Intended fix |
|---|---|---|---|
| 1 | Insufficient parts | bay shows red pulsing part icon (🛞?/🛢?); rack renders visibly empty | upgrade source rate; add warehouse buffer |
| 2 | Slow transport | parts pile to max at source (pallet overflows, wobbles); racks empty | hire/upgrade transporter; player hauls |
| 3 | Slow service | queue lengthens while racks stay full; progress rings crawl | station speed tiers; mechanic quality; rush aura |
| 4 | Inadequate bay capacity | all bays occupied + queue full + street honking | build next bay zone |
| 5 | Slow checkout | cash piles stack up at pay stall; customers cluster waiting to pay | hire/upgrade cashier |
| 6 | Excessive arrivals | everything above at once; honk-pause engaged repeatedly | patience upgrade; broad capacity investment (deliberate late-level pressure) |
| 7 | Routing congestion | vehicles stacked bumper-to-bumper on one lane while another sits empty | build parallel queue lane; hire Vehicle Mover (L2+) |

Diegetic rule: **the player should diagnose any bottleneck from a zoomed-out glance in < 3 seconds**, purely from piles, queues and icons.

## 6. System spec — Economy Manager
- **Purpose:** single source of truth for wallet, prices, costs, offline earnings and completion progress.
- **Player interaction:** indirect — cash piles in, construction/kiosk spending out; Office kiosk for financial upgrades.
- **Inputs:** service completion events; construction drains; upgrade purchases; time deltas (offline).
- **Outputs:** wallet value (HUD); affordability highlights on zones; business-value % toward completion; offline-earnings pile.
- **States:** Running · OfflineSettlement (on app resume) · LevelComplete ceremony.
- **Upgrade paths:** price tiers, arrival tiers, offline cap (§3).
- **Dependencies:** all definitions (Doc 06 §4); save system timestamps.
- **Edge cases:** device clock rollback (clamp offline to ≥ 0, cap unaffected); wallet overflow (int64, display 1.2K/3.4M); mid-drain app kill (partial funding persisted every tick to save cache).
- **MVP requirements:** wallet, L1 cost/price tables, offline earnings v1, completion meter.
- **Later expansion:** per-location managed income (10% trickle), franchise multipliers, rebalancing via remote config (post-MVP).

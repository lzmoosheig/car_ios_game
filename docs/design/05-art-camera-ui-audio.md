# Overhaul! — Doc 05: Art Direction · Camera & UI · Audio & Haptics

---

## 1. Art Direction

### 1.1 Style pillars
- **Chunky diorama garages:** each location reads as a hand-built model on a base plate — visible ground edges, slightly oversized props, compact footprint that fits one zoomed-out screen.
- **Smooth rounded geometry:** no hard edges; low-poly meshes with beveled corners, flat or 2-tone gradient materials, no realistic textures. One 4K atlas per location.
- **Exaggerated parts:** tires the size of the character's torso, comically large wrenches, oil cans with big friendly labels. Carried stacks must read at full zoom-out.
- **Color-coded stations:** every station family owns a hue used on its structure, ground decal, curb paint, queue lane and part icons: tires = teal, oil = amber, repair = orange, wash = sky blue, detail = lavender, diagnostics = lime, engine/tuning = red, paint = magenta, sales = gold, money/cash = green, construction = dashed yellow, hiring = purple.
- **Readable silhouettes:** customers are slim with big heads and hats/props per archetype; employees wear role-colored overalls + tool props (transporter = hand truck, mechanic = wrench, cashier = tablet); the player has a unique cap + tool-belt silhouette.
- **Minimal clutter:** decoration budget per location ≤ 15 props; nothing animated that isn't gameplay.

### 1.2 Vehicles
Original designs inspired by broad categories only — two-box/three-box archetypes with exaggerated proportions (big wheels, short overhangs, friendly headlight "faces"); never resembling an identifiable licensed model.

Categories & unlock: **compact city car, sedan, SUV** (L1) → **pickup** (L2) → **sports coupe** (L3) → **classic car** (L6) → **supercar** (L7) → **race car** (L9). Each category = 1 base mesh + palette variants + optional roof/spoiler attachments.

### 1.3 Visual state language (world tells the story)

| State | Visual treatment |
|---|---|
| Waiting vehicle | idle bob, occasional wiper/light blink; patience mood bubble above owner |
| Vehicle being serviced | raised on lift or hood open; animated tool props; progress ring above bay |
| Missing required parts | red pulsing part icon over bay; empty rack clearly bare |
| Active workstation | station hue emissive glow, moving machinery, small particle puffs |
| Completed service | green check burst; car does a happy suspension bounce; horn chirp |
| Premium service | gold sparkle trail on the vehicle until exit; customer photo flash |
| Blocked queue | cars bumper-to-bumper with subtle brake-light glow; soft honk ripples |
| Collected payment | green bill fountain arcing into collector; wallet counter ticks |
| Newly constructed station | dust poof → squash-and-stretch pop-in → bolt confetti → 1.5 s camera nudge |

---

## 2. Camera

| Parameter | Decision |
|---|---|
| Projection | **Perspective, FOV 35°** (near-ortho feel, keeps parallax depth for the diorama) |
| Angle | pitch 52° down, yaw 45° fixed (classic elevated isometric); **no player rotation** |
| Distance | default 18 m; pinch zoom 13–26 m, two detents ("work" / "overview") |
| Follow | soft-follow with 1.5 m dead zone + 0.3 s smoothing; slight look-ahead in movement direction |
| Walls/roofs | interiors are roofless by design (diorama); any occluding wall segment dither-fades below 40% when the player is behind it |
| Events | 1.5 s framing nudge on construction complete; orbit time-lapse on level complete; never takes control longer than 2 s |
| Off-screen objectives | edge-of-screen chevron indicators with the target's icon and (for zones) price; max 2 shown, nearest-priority |

---

## 3. User Interface (minimal by contract)

Persistent HUD (4 elements max):
1. **Cash counter** (top-left, animates on gain)
2. **Objective chip** (top-center: one icon + short label, e.g. "🔧 Build Oil Bay — $120"; tappable to flash an arrow)
3. **Location progress ring** (top-right, business-value % — tapping opens the level panel)
4. **Menu button** (bottom-right corner: upgrades kiosk shortcut, employees, settings)

Contextual only: temporary carried-resource icon over the stack when it changes; rewarded-ad offer chip (post-MVP, max 1 visible, dismissible); optional mission panel (post-MVP) folded into the level panel.
Everything else — bottleneck states, patience, prices, capacities — is communicated **in-world** (Doc 04 §5). Hard rule: no tutorial popups, no interstitial screens during play.

---

## 4. Audio & Haptic Feedback

Style: **musical, not realistic.** Foley is replaced by pitched, percussive UI-like sounds in a shared key (C major pentatonic) so long sessions never grate. All loops duck under one-shots. Global music = light, lo-fi garage-radio bed at low volume.

| Event | Sound | Haptic (iOS) |
|---|---|---|
| Collect part | soft pluck, pitch rises with each item in a combo (resets after 1 s) | `selectionChanged` per item |
| Stack wobble/max | rubbery boing at cap | light impact |
| Deposit items | reverse-pluck descending run | `selectionChanged` per item |
| Tighten bolts / repair | rhythmic ratchet triplet, quantized to music tempo | none (too frequent) |
| Tire change | quick pneumatic "brrt" + pop | none |
| Wash | shimmering white-noise sweep with musical tail | none |
| Spray paint | airy "pshh" arpeggio | none |
| Engine build/tuning | 3-note rising motif + one soft rev (never looping engine noise) | medium impact on finish |
| Service complete | bright 2-note chime + horn chirp | `notificationSuccess` |
| Cash collect | coin glissando, caps at 4 simultaneous voices | light impact per burst (not per bill) |
| Purchase upgrade | cha-ching + paper slide | medium impact |
| Construction complete | drumroll tick during build → pop + tada chord | heavy impact |
| New area unlocked | short fanfare + fence-fall crash | `notificationSuccess` |
| Rejected deposit ("nope") | muted dull tick | rigid light tick |

Rules: any sound that can fire > 1×/s must have round-robin variants (≥ 3) and a polyphony cap; haptics never fire more than 8×/s; all audio + haptics individually toggleable in settings.

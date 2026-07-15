# Automotive Idle Garage - Asset Usage Catalog

Workspace: `/Users/leozmoos/Downloads/Assets IOS Game`

Source design: the attached plan describes a mobile idle-arcade automotive management game: elevated isometric camera, physical resource carrying, customer vehicle queues, reception, repair bays, tire/oil service, payments, construction zones, workers, and later showroom/customization expansions.

The available packs are mostly Kenney low-poly assets. Their included license files state CC0, so they are usable for personal, educational, and commercial projects. Credit to Kenney is appreciated by the creator but not required by the license text.

## Best Overall Scene Direction

Use the assets to build the first playable level as a compact roadside repair garage:

- Exterior road and driveway loop from `kenney_city-kit-roads`.
- Open-front workshop shell from `kenney_modular-buildings` plus `kenney_racing-kit` pit garage parts.
- Player, customers, and employees from `kenney_blocky-characters_20`.
- Customer vehicles from `kenney_car-kit`.
- Repair props, tire resources, cones, boxes, debris, and wheels from `kenney_car-kit`.
- Garage signs, barriers, lights, lane boundaries, pit office, and pit garage details from `kenney_racing-kit`.
- Construction zones and tutorial arrows from `kenney_city-kit-roads`, `kenney_platformer-kit`, and `kenney_watercraft-pack`.
- Exterior context from `kenney_city-kit-commercial_2`, `kenney_city-kit-industrial_1`, `kenney_city-kit-suburban_20`, and `kenney_nature-kit`.

## First Playable Level Asset List

### Player, Customers, And Workers

Use these for the controlled character, waiting customers, mechanics, cashier, and first parts transporter. Recolor materials or attach simple hats/tools in-engine to distinguish roles.

Primary files:

```text
kenney_blocky-characters_20/Models/GLB format/character-a.glb
kenney_blocky-characters_20/Models/GLB format/character-b.glb
kenney_blocky-characters_20/Models/GLB format/character-c.glb
kenney_blocky-characters_20/Models/GLB format/character-d.glb
kenney_blocky-characters_20/Models/GLB format/character-e.glb
kenney_blocky-characters_20/Models/GLB format/character-f.glb
kenney_blocky-characters_20/Models/GLB format/character-g.glb
kenney_blocky-characters_20/Models/GLB format/character-h.glb
kenney_blocky-characters_20/Models/GLB format/character-i.glb
kenney_blocky-characters_20/Models/GLB format/character-j.glb
kenney_blocky-characters_20/Models/GLB format/character-k.glb
kenney_blocky-characters_20/Models/GLB format/character-l.glb
kenney_blocky-characters_20/Models/GLB format/character-m.glb
kenney_blocky-characters_20/Models/GLB format/character-n.glb
kenney_blocky-characters_20/Models/GLB format/character-o.glb
kenney_blocky-characters_20/Models/GLB format/character-p.glb
kenney_blocky-characters_20/Models/GLB format/character-q.glb
kenney_blocky-characters_20/Models/GLB format/character-r.glb
```

Recommended role mapping:

| Role | Suggested Asset | Notes |
|---|---|---|
| Player mechanic | `character-a.glb` or `character-b.glb` | Give the player a stronger color, e.g. blue overalls. |
| Customer variants | `character-c.glb` through `character-j.glb` | Rotate through silhouettes/colors. |
| Parts transporter | `character-k.glb` | Add crate/tire stack attachment point. |
| Mechanic employee | `character-l.glb` | Position at repair bay. |
| Cashier/receptionist | `character-m.glb` | Fixed at reception/payment point. |
| Tire technician | `character-n.glb` | Later unlock near tire station. |
| Detailer/painter | `character-o.glb` to `character-r.glb` | Use for later services. |

### Customer Vehicles

These fit the plan's compact city car, sedan, SUV, pickup, sports coupe, race car, service/delivery vehicle, and premium showroom categories.

Core MVP vehicle files:

```text
kenney_car-kit/Models/GLB format/sedan.glb
kenney_car-kit/Models/GLB format/suv.glb
kenney_car-kit/Models/GLB format/hatchback-sports.glb
```

Good first-level expansion vehicles:

```text
kenney_car-kit/Models/GLB format/sedan-sports.glb
kenney_car-kit/Models/GLB format/suv-luxury.glb
kenney_car-kit/Models/GLB format/van.glb
kenney_car-kit/Models/GLB format/truck.glb
kenney_car-kit/Models/GLB format/truck-flat.glb
kenney_car-kit/Models/GLB format/taxi.glb
kenney_car-kit/Models/GLB format/delivery.glb
kenney_car-kit/Models/GLB format/delivery-flat.glb
```

Later/premium/special vehicles:

```text
kenney_car-kit/Models/GLB format/race.glb
kenney_car-kit/Models/GLB format/race-future.glb
kenney_racing-kit/Models/GLTF format/raceCarGreen.glb
kenney_racing-kit/Models/GLTF format/raceCarOrange.glb
kenney_racing-kit/Models/GLTF format/raceCarRed.glb
kenney_racing-kit/Models/GLTF format/raceCarWhite.glb
```

Use sparingly or reserve for themed events:

```text
kenney_car-kit/Models/GLB format/ambulance.glb
kenney_car-kit/Models/GLB format/firetruck.glb
kenney_car-kit/Models/GLB format/police.glb
kenney_car-kit/Models/GLB format/garbage-truck.glb
kenney_car-kit/Models/GLB format/tractor.glb
kenney_car-kit/Models/GLB format/tractor-police.glb
kenney_car-kit/Models/GLB format/tractor-shovel.glb
```

### Vehicle Queue, Entrance, And Exit Roads

Use roads as fixed, readable vehicle lanes. For mobile idle gameplay, keep lanes wide and simple.

Best first-level road pieces:

```text
kenney_city-kit-roads/Models/GLB format/road-straight.glb
kenney_city-kit-roads/Models/GLB format/road-straight-half.glb
kenney_city-kit-roads/Models/GLB format/road-side.glb
kenney_city-kit-roads/Models/GLB format/road-side-entry.glb
kenney_city-kit-roads/Models/GLB format/road-side-exit.glb
kenney_city-kit-roads/Models/GLB format/road-driveway-single.glb
kenney_city-kit-roads/Models/GLB format/road-driveway-double.glb
kenney_city-kit-roads/Models/GLB format/road-end.glb
kenney_city-kit-roads/Models/GLB format/road-bend.glb
kenney_city-kit-roads/Models/GLB format/road-intersection.glb
kenney_city-kit-roads/Models/GLB format/road-crossing.glb
```

Useful queue-control and construction pieces:

```text
kenney_city-kit-roads/Models/GLB format/construction-barrier.glb
kenney_city-kit-roads/Models/GLB format/construction-cone.glb
kenney_city-kit-roads/Models/GLB format/construction-light.glb
kenney_city-kit-roads/Models/GLB format/road-straight-barrier.glb
kenney_city-kit-roads/Models/GLB format/road-straight-barrier-half.glb
kenney_city-kit-roads/Models/GLB format/road-side-barrier.glb
kenney_city-kit-roads/Models/GLB format/road-square.glb
kenney_city-kit-roads/Models/GLB format/tile-low.glb
```

Racing-kit road pieces can work for a tuning/workshop flavor, but the city roads are more readable for the first level:

```text
kenney_racing-kit/Models/GLTF format/roadPitEntry.glb
kenney_racing-kit/Models/GLTF format/roadPitGarage.glb
kenney_racing-kit/Models/GLTF format/roadPitStraight.glb
kenney_racing-kit/Models/GLTF format/roadPitStraightLong.glb
kenney_racing-kit/Models/GLTF format/roadStraight.glb
kenney_racing-kit/Models/GLTF format/roadStraightLong.glb
kenney_racing-kit/Models/GLTF format/roadStartPositions.glb
```

### Garage Building Shell

For the first playable level, build an open-front workshop: floor/road pad, one visible wall edge, one open bay, one office/reception corner. Avoid a full roof over gameplay unless it fades or is removed for camera visibility.

Best garage/workshop pieces:

```text
kenney_racing-kit/Models/GLTF format/pitsGarage.glb
kenney_racing-kit/Models/GLTF format/pitsGarageClosed.glb
kenney_racing-kit/Models/GLTF format/pitsGarageCorner.glb
kenney_racing-kit/Models/GLTF format/pitsOffice.glb
kenney_racing-kit/Models/GLTF format/pitsOfficeCorner.glb
kenney_racing-kit/Models/GLTF format/pitsOfficeRoof.glb
```

Use modular-building pieces if you want to assemble a custom service-center facade:

```text
kenney_modular-buildings/Models/GLB format/building-block.glb
kenney_modular-buildings/Models/GLB format/building-corner.glb
kenney_modular-buildings/Models/GLB format/building-door.glb
kenney_modular-buildings/Models/GLB format/building-door-window.glb
kenney_modular-buildings/Models/GLB format/building-door-window-narrow.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_modular-buildings/Models/GLB format/building-window-wide.glb
kenney_modular-buildings/Models/GLB format/building-window-awnings.glb
kenney_modular-buildings/Models/GLB format/door-brown-glass.glb
kenney_modular-buildings/Models/GLB format/door-white-glass.glb
kenney_modular-buildings/Models/GLB format/roof-flat-center.glb
kenney_modular-buildings/Models/GLB format/roof-flat-border-straight.glb
kenney_modular-buildings/Models/GLB format/roof-flat-border-corner.glb
kenney_modular-buildings/Models/GLB format/roof-flat-awning-a.glb
kenney_modular-buildings/Models/GLB format/roof-flat-awning-b.glb
kenney_modular-buildings/Models/GLB format/detail-ac-a.glb
kenney_modular-buildings/Models/GLB format/detail-ac-b.glb
```

### Basic Repair Bay

There is no dedicated lift model in the folder, so represent the first bay with lane paint, barriers, cones, wheels, tool debris, and a service vehicle slot. This is enough for MVP readability.

Recommended bay assets:

```text
kenney_racing-kit/Models/GLTF format/pitsGarage.glb
kenney_racing-kit/Models/GLTF format/roadPitGarage.glb
kenney_racing-kit/Models/GLTF format/barrierWhite.glb
kenney_racing-kit/Models/GLTF format/barrierRed.glb
kenney_racing-kit/Models/GLTF format/barrierWall.glb
kenney_city-kit-roads/Models/GLB format/construction-cone.glb
kenney_car-kit/Models/GLB format/cone.glb
kenney_car-kit/Models/GLB format/cone-flat.glb
kenney_car-kit/Models/GLB format/debris-bolt.glb
kenney_car-kit/Models/GLB format/debris-nut.glb
kenney_car-kit/Models/GLB format/debris-plate-a.glb
kenney_car-kit/Models/GLB format/debris-plate-b.glb
```

Suggested service state visuals:

| State | Asset Treatment |
|---|---|
| Empty bay | Road/pit tile plus white barrier edges. |
| Waiting for parts | Red cone or red barrier beside vehicle. |
| Active service | Mechanic character next to car, bolt/nut/debris props on ground. |
| Completed | Green flag or bright coin/cash pickup near payment point. |
| Locked future bay | Construction barrier, construction light, and cost marker UI. |

### Parts Source And Warehouse

Use `box.glb`, crates/barrels from platformer kit, and industrial buildings/tanks for a clear "parts delivery" area.

Primary assets:

```text
kenney_car-kit/Models/GLB format/box.glb
kenney_platformer-kit/Models/GLB format/crate.glb
kenney_platformer-kit/Models/GLB format/crate-strong.glb
kenney_platformer-kit/Models/GLB format/crate-item.glb
kenney_platformer-kit/Models/GLB format/crate-item-strong.glb
kenney_platformer-kit/Models/GLB format/barrel.glb
kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb
kenney_city-kit-industrial_1/Models/GLB format/building-a.glb
kenney_city-kit-industrial_1/Models/GLB format/building-b.glb
kenney_city-kit-industrial_1/Models/GLB format/building-c.glb
```

Use these as physical resource stand-ins:

| Game Resource | Available Asset | Notes |
|---|---|---|
| Tires | `kenney_car-kit/Models/GLB format/wheel-default.glb` | Best visible stack item. |
| Premium tires | `wheel-racing.glb`, `wheel-dark.glb` | Use for later tire station upgrades. |
| Truck tires | `wheel-truck.glb` | Later heavy vehicle service. |
| Basic parts box | `box.glb`, `crate.glb` | Generic resource for first tutorial. |
| Bolts/nuts | `debris-bolt.glb`, `debris-nut.glb` | Small repair ingredients or visual scatter. |
| Body panels | `debris-door.glb`, `debris-bumper.glb`, `debris-plate-a.glb` | Bodywork expansion. |
| Drivetrain/engine stand-in | `debris-drivetrain.glb`, `debris-drivetrain-axle.glb` | Engine/tuning expansion. |
| Spoiler/custom part | `debris-spoiler-a.glb`, `debris-spoiler-b.glb` | Customization/tuning expansion. |
| Oil/paint/cleaning supplies | `barrel.glb`, `crate-item.glb`, `detail-tank.glb` | Needs color-coded materials/icons in-engine. |
| Vehicle keys | `kenney_platformer-kit/Models/GLB format/key.glb` | Great for completed-car delivery. |
| Money bundles | No perfect model; use `coin-gold.glb` or make a simple custom cash stack. |

### Tire Storage And Tire Station

The car kit has enough wheel assets to make tire storage visually strong.

Use:

```text
kenney_car-kit/Models/GLB format/wheel-default.glb
kenney_car-kit/Models/GLB format/wheel-dark.glb
kenney_car-kit/Models/GLB format/wheel-racing.glb
kenney_car-kit/Models/GLB format/wheel-truck.glb
kenney_car-kit/Models/GLB format/wheel-tractor-front.glb
kenney_car-kit/Models/GLB format/wheel-tractor-back.glb
kenney_platformer-kit/Models/GLB format/crate.glb
kenney_racing-kit/Models/GLTF format/barrierWhite.glb
```

Recommended setup:

- Place 3-5 `wheel-default.glb` models on a simple rack made from `barrierWhite.glb` or custom primitive cylinders/cubes.
- Use `wheel-racing.glb` as the upgrade visual for a faster tire station.
- Use `wheel-truck.glb` only when larger vehicles unlock.

### Oil Change Bay

There is no oil-can model, so make oil readable through color, station layout, and barrels/tanks.

Use:

```text
kenney_platformer-kit/Models/GLB format/barrel.glb
kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb
kenney_city-kit-industrial_1/Models/GLB format/chimney-small.glb
kenney_car-kit/Models/GLB format/debris-drivetrain.glb
kenney_racing-kit/Models/GLTF format/roadPitGarage.glb
```

Recommended treatment:

- Recolor barrels/tank to blue or yellow.
- Add a droplet icon in UI/world marker rather than relying on the model alone.
- Put the bay next to the basic repair bay so the player learns shared vehicle flow.

### Reception And Payment Point

There are no desks/counters in the automotive packs, so build a low-poly reception from simple modular pieces and props.

Use:

```text
kenney_racing-kit/Models/GLTF format/pitsOffice.glb
kenney_racing-kit/Models/GLTF format/pitsOfficeCorner.glb
kenney_modular-buildings/Models/GLB format/building-door-window.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_platformer-kit/Models/GLB format/chest.glb
kenney_platformer-kit/Models/GLB format/coin-gold.glb
kenney_platformer-kit/Models/GLB format/coin-silver.glb
kenney_platformer-kit/Models/GLB format/coin-bronze.glb
```

Recommended setup:

- Use `pitsOffice.glb` as the reception room.
- Use `chest.glb` as a placeholder cash register/payment box if needed.
- Use floating `coin-gold.glb` or a custom cash-stack mesh for collectible payments.
- Put one cashier character behind the reception point after the first employee unlock.

### Construction Zones And Tutorial Guidance

The plan calls for world highlights and arrows instead of long text. Use these assets:

```text
kenney_platformer-kit/Models/GLB format/arrow.glb
kenney_platformer-kit/Models/GLB format/arrows.glb
kenney_watercraft-pack/Models/GLB format/arrow.glb
kenney_watercraft-pack/Models/GLB format/arrow-standing.glb
kenney_city-kit-roads/Models/GLB format/construction-barrier.glb
kenney_city-kit-roads/Models/GLB format/construction-light.glb
kenney_city-kit-roads/Models/GLB format/construction-cone.glb
kenney_platformer-kit/Models/GLB format/button-round.glb
kenney_platformer-kit/Models/GLB format/button-square.glb
kenney_racing-kit/Models/GLTF format/flagGreen.glb
kenney_racing-kit/Models/GLTF format/flagRed.glb
```

Use `button-round.glb` or `button-square.glb` as an in-world upgrade/funding pad. Place construction barriers around locked bay positions.

### Signs, Lights, And Readability Props

Use these to make the garage readable from an elevated isometric camera:

```text
kenney_city-kit-roads/Models/GLB format/light-square.glb
kenney_city-kit-roads/Models/GLB format/light-square-double.glb
kenney_city-kit-roads/Models/GLB format/light-curved.glb
kenney_city-kit-roads/Models/GLB format/sign-highway.glb
kenney_city-kit-roads/Models/GLB format/sign-highway-wide.glb
kenney_city-kit-roads/Models/GLB format/sign-highway-detailed.glb
kenney_racing-kit/Models/GLTF format/billboard.glb
kenney_racing-kit/Models/GLTF format/billboardLow.glb
kenney_racing-kit/Models/GLTF format/billboardLower.glb
kenney_racing-kit/Models/GLTF format/overhead.glb
kenney_racing-kit/Models/GLTF format/overheadLights.glb
kenney_racing-kit/Models/GLTF format/lightPostLarge.glb
kenney_racing-kit/Models/GLTF format/lightPostModern.glb
```

Recommended uses:

- `billboardLow.glb`: garage name sign.
- `sign-highway-wide.glb`: external roadside sign.
- `light-square-double.glb`: nighttime or parking-lot polish.
- `overheadLights.glb`: later inspection/tuning area.

### Environment Dressing

Use environment dressing lightly so the first level stays readable and performant.

Suburban roadside context:

```text
kenney_city-kit-suburban_20/Models/GLB format/tree-small.glb
kenney_city-kit-suburban_20/Models/GLB format/tree-large.glb
kenney_city-kit-suburban_20/Models/GLB format/fence.glb
kenney_city-kit-suburban_20/Models/GLB format/fence-low.glb
kenney_city-kit-suburban_20/Models/GLB format/driveway-short.glb
kenney_city-kit-suburban_20/Models/GLB format/driveway-long.glb
kenney_city-kit-suburban_20/Models/GLB format/planter.glb
kenney_city-kit-suburban_20/Models/GLB format/path-short.glb
kenney_city-kit-suburban_20/Models/GLB format/path-long.glb
```

Commercial city backdrop:

```text
kenney_city-kit-commercial_2/Models/GLB format/low-detail-building-a.glb
kenney_city-kit-commercial_2/Models/GLB format/low-detail-building-b.glb
kenney_city-kit-commercial_2/Models/GLB format/low-detail-building-wide-a.glb
kenney_city-kit-commercial_2/Models/GLB format/detail-awning.glb
kenney_city-kit-commercial_2/Models/GLB format/detail-awning-wide.glb
```

Industrial backdrop for later body/paint facility:

```text
kenney_city-kit-industrial_1/Models/GLB format/building-a.glb
kenney_city-kit-industrial_1/Models/GLB format/building-d.glb
kenney_city-kit-industrial_1/Models/GLB format/building-h.glb
kenney_city-kit-industrial_1/Models/GLB format/chimney-basic.glb
kenney_city-kit-industrial_1/Models/GLB format/chimney-medium.glb
kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb
```

Nature/ground dressing:

```text
kenney_nature-kit/Models/GLTF format/ground_grass.glb
kenney_nature-kit/Models/GLTF format/ground_pathStraight.glb
kenney_nature-kit/Models/GLTF format/ground_pathCorner.glb
kenney_nature-kit/Models/GLTF format/plant_bush.glb
kenney_nature-kit/Models/GLTF format/plant_bushSmall.glb
kenney_nature-kit/Models/GLTF format/rock_smallA.glb
kenney_nature-kit/Models/GLTF format/tree_default.glb
kenney_nature-kit/Models/GLTF format/tree_small.glb
```

## Scene Layout Recommendation

### Level 1: Roadside Repair Garage

Build the playable footprint as a small rectangular diorama:

1. Front road lane: `road-straight.glb`, `road-side-entry.glb`, `road-side-exit.glb`.
2. One customer queue slot: marked by cones/barriers and a waypoint.
3. Reception/payment point: `pitsOffice.glb`, cashier character, coin pickup.
4. Parts source: `box.glb`, crates, barrels, delivery-flat vehicle parked nearby.
5. Basic repair bay: `pitsGarage.glb` or `roadPitGarage.glb`, one customer vehicle slot.
6. Locked tire station: construction barriers, wheel stack visible behind the locked area.
7. Locked oil bay: barrels/tank behind construction barrier.
8. Exit/completed-car slot: road side exit plus green flag or coin.

### First 15-20 Minute Unlock Asset Sequence

| Time | Unlock | Assets To Reveal |
|---|---|---|
| 0:00 | Player + first car + basic parts | `character-a.glb`, `sedan.glb`, `box.glb`, `road-straight.glb` |
| 1:00 | Basic repair bay | `pitsGarage.glb`, `roadPitGarage.glb`, cones, bolts |
| 2:00 | Payment point | `pitsOffice.glb`, `coin-gold.glb`, cashier position |
| 4:00 | Second queue slot | additional road tile, cones, barrier |
| 6:00 | Tire storage | wheel stack using `wheel-default.glb` |
| 8:00 | Tire station | `wheel-racing.glb`, technician slot, barriers |
| 10:00 | First employee | `character-k.glb` as transporter |
| 12:00 | Oil bay | barrels, tank, second service slot |
| 15:00 | Reception/cashier employee | `character-m.glb`, office/payout upgrade |
| 18:00 | Larger vehicle customer | `suv.glb` or `van.glb` |

## Later Expansion Asset Mapping

### Car Wash And Detailing

Available assets are partial; you will likely need simple custom spray/foam effects.

Use:

```text
kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb
kenney_platformer-kit/Models/GLB format/barrel.glb
kenney_racing-kit/Models/GLTF format/overheadLights.glb
kenney_city-kit-roads/Models/GLB format/road-driveway-single.glb
kenney_car-kit/Models/GLB format/cone.glb
```

Need custom assets:

- Sponge/brush model.
- Foam/water particles.
- Cleaning supply bottle or bucket.

### Body Repair Area

Strong fit with existing car debris assets.

Use:

```text
kenney_car-kit/Models/GLB format/debris-bumper.glb
kenney_car-kit/Models/GLB format/debris-door.glb
kenney_car-kit/Models/GLB format/debris-door-window.glb
kenney_car-kit/Models/GLB format/debris-plate-a.glb
kenney_car-kit/Models/GLB format/debris-plate-b.glb
kenney_car-kit/Models/GLB format/debris-plate-small-a.glb
kenney_car-kit/Models/GLB format/debris-plate-small-b.glb
```

Need custom assets:

- Repair hammer/toolbox if you want clearer interactions.
- Body-panel rack.

### Paint Mixing And Paint Booth

Existing assets can block out the area, but paint cans/booth equipment should be custom or kitbashed.

Use:

```text
kenney_platformer-kit/Models/GLB format/barrel.glb
kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb
kenney_racing-kit/Models/GLTF format/pitsGarageClosed.glb
kenney_racing-kit/Models/GLTF format/overhead.glb
kenney_racing-kit/Models/GLTF format/overheadLights.glb
```

Need custom assets:

- Paint can resource.
- Spray gun.
- Paint booth wall/fan.
- Recolor material workflow for vehicles.

### Diagnostic And Inspection Station

Use racing lights/camera/radar props to imply scanning and testing.

Use:

```text
kenney_racing-kit/Models/GLTF format/radarEquipment.glb
kenney_racing-kit/Models/GLTF format/camera_exclusive.glb
kenney_racing-kit/Models/GLTF format/lightRed.glb
kenney_racing-kit/Models/GLTF format/lightRedDouble.glb
kenney_racing-kit/Models/GLTF format/overheadLights.glb
kenney_racing-kit/Models/GLTF format/roadStraightArrow.glb
```

Need custom assets:

- Diagnostic computer/screen.
- Small animated scan effect.

### Engine Workshop And Tuning Station

Good existing drivetrain/spoiler/racing assets.

Use:

```text
kenney_car-kit/Models/GLB format/debris-drivetrain.glb
kenney_car-kit/Models/GLB format/debris-drivetrain-axle.glb
kenney_car-kit/Models/GLB format/debris-spoiler-a.glb
kenney_car-kit/Models/GLB format/debris-spoiler-b.glb
kenney_car-kit/Models/GLB format/race.glb
kenney_car-kit/Models/GLB format/race-future.glb
kenney_racing-kit/Models/GLTF format/flagCheckers.glb
kenney_racing-kit/Models/GLTF format/roadStartPositions.glb
```

Need custom assets:

- Engine block/turbocharger if drivetrain pieces are not readable enough.
- Workbench/tool cabinet.

### Used-Car And Premium Showrooms

Use commercial/modular buildings plus vehicle display slots.

Use:

```text
kenney_city-kit-commercial_2/Models/GLB format/building-a.glb
kenney_city-kit-commercial_2/Models/GLB format/building-b.glb
kenney_city-kit-commercial_2/Models/GLB format/building-c.glb
kenney_city-kit-commercial_2/Models/GLB format/detail-awning-wide.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_modular-buildings/Models/GLB format/building-window-wide.glb
kenney_modular-buildings/Models/GLB format/door-white-glass.glb
kenney_car-kit/Models/GLB format/suv-luxury.glb
kenney_car-kit/Models/GLB format/sedan-sports.glb
kenney_car-kit/Models/GLB format/race-future.glb
```

Need custom assets:

- Sales desk.
- Vehicle price sign.
- Showroom floor material.

## Assets To Avoid Or Keep As Distant Background

These are not useful for the automotive idle garage core loop unless you build themed expansions:

- `kenney_marble-kit`: mostly marble-run parts; not a natural fit for garage gameplay.
- `kenney_train-kit`: only useful for distant scenery or a cargo-delivery expansion.
- `kenney_watercraft-pack`: mostly irrelevant, except `arrow.glb`, `arrow-standing.glb`, and cargo containers if you want a shipping-yard location.
- Most `kenney_nature-kit` crop/camp/tent assets: can distract from the automotive setting.
- Emergency/service vehicles from `kenney_car-kit`: reserve for special customers so the ordinary customer flow stays grounded.

## Missing Assets Worth Creating

The folder is strong for vehicles, roads, buildings, cones, wheels, and characters. It is weaker for garage-specific interactables. For a polished MVP, create or source these small custom low-poly models:

- Car lift or floor jack.
- Toolbox/tool cart.
- Workbench.
- Tire rack.
- Oil container.
- Paint can.
- Sponge/bucket/cleaning bottle.
- Cash bundle.
- Register/payment terminal.
- Service-bay floor decal or sign.
- Construction-zone cost sign.
- Simple thought/status bubble icons for customer patience and missing resources.

## Recommended MVP Asset Set

If you want the smallest scene that still matches the plan, start with this exact set:

```text
kenney_blocky-characters_20/Models/GLB format/character-a.glb
kenney_blocky-characters_20/Models/GLB format/character-c.glb
kenney_blocky-characters_20/Models/GLB format/character-k.glb
kenney_blocky-characters_20/Models/GLB format/character-m.glb
kenney_car-kit/Models/GLB format/sedan.glb
kenney_car-kit/Models/GLB format/suv.glb
kenney_car-kit/Models/GLB format/hatchback-sports.glb
kenney_car-kit/Models/GLB format/box.glb
kenney_car-kit/Models/GLB format/wheel-default.glb
kenney_car-kit/Models/GLB format/wheel-racing.glb
kenney_car-kit/Models/GLB format/debris-bolt.glb
kenney_car-kit/Models/GLB format/debris-nut.glb
kenney_car-kit/Models/GLB format/cone.glb
kenney_city-kit-roads/Models/GLB format/road-straight.glb
kenney_city-kit-roads/Models/GLB format/road-side-entry.glb
kenney_city-kit-roads/Models/GLB format/road-side-exit.glb
kenney_city-kit-roads/Models/GLB format/road-driveway-single.glb
kenney_city-kit-roads/Models/GLB format/construction-barrier.glb
kenney_city-kit-roads/Models/GLB format/construction-light.glb
kenney_racing-kit/Models/GLTF format/pitsGarage.glb
kenney_racing-kit/Models/GLTF format/pitsOffice.glb
kenney_racing-kit/Models/GLTF format/roadPitGarage.glb
kenney_racing-kit/Models/GLTF format/barrierWhite.glb
kenney_racing-kit/Models/GLTF format/flagGreen.glb
kenney_platformer-kit/Models/GLB format/arrow.glb
kenney_platformer-kit/Models/GLB format/button-round.glb
kenney_platformer-kit/Models/GLB format/coin-gold.glb
kenney_platformer-kit/Models/GLB format/crate.glb
kenney_platformer-kit/Models/GLB format/barrel.glb
```

This set supports:

- One player character.
- One customer type plus variants.
- One repair bay.
- One customer vehicle queue.
- One payment point.
- One parts source.
- Tire resource carrying.
- Basic repair resource carrying.
- Locked construction zone.
- First employee.
- Oil/tire station expansion placeholders.

## Unity Scene Generation Blueprint

Use this section as the primary input for automatic Unity scene generation. Coordinates assume Unity units, `X` = horizontal left/right, `Z` = depth, `Y` = vertical. The playable garage faces the camera from the south/front. Recommended camera: orthographic, rotation `(45, 45, 0)`, orthographic size `18-22` for Level 1.

### Global Scene Conventions

Use these conventions across all scenes:

| Convention | Value |
|---|---|
| Ground plane | X/Z plane, Y up |
| Tile size assumption | Most road/building assets should be treated as 4x4 or 5x5 visual modules, then adjusted after import |
| Vehicle forward | Positive Z for entering service area, negative Z for leaving if using a U-shaped loop |
| Player spawn | Near reception and parts source, not on vehicle lane |
| Interaction zone shape | Cylinder trigger on X/Z plane |
| Collection radius | 1.5-2.0 units |
| Deposit radius | 1.75-2.25 units |
| Construction zone radius | 2.0-2.5 units |
| Worker path lanes | Keep separate from vehicle lanes where possible |
| Asset scale | Start at `(1,1,1)`, normalize per imported model visually |

Suggested Unity object metadata for generated objects:

```json
{
  "id": "unique_scene_object_id",
  "name": "Readable Object Name",
  "assetPath": "kenney_car-kit/Models/GLB format/sedan.glb",
  "position": [0, 0, 0],
  "rotationEuler": [0, 90, 0],
  "scale": [1, 1, 1],
  "category": "Vehicle | Building | Road | Prop | Character | Resource | Zone | Decoration",
  "gameplayRole": "CustomerVehicle | RepairBay | PartsSource | PaymentPoint | ConstructionZone",
  "startsUnlocked": true,
  "unlockId": null,
  "collider": "Box | Capsule | MeshVisualOnly | TriggerCylinder",
  "notes": "Implementation hints"
}
```

### Level 1 Scene: Roadside Repair Garage

Scene purpose: teach the full idle loop with one repair bay, one parts source, one vehicle queue, payment, construction, tire station unlock, oil station unlock, and first employee.

Scene name suggestion: `L01_RoadsideRepairGarage`.

Playable footprint:

| Area | Bounds |
|---|---|
| Total scene | X `-24` to `24`, Z `-18` to `22` |
| Player walkable garage floor | X `-14` to `14`, Z `-8` to `12` |
| Vehicle road/queue lane | X `-22` to `22`, Z `-15` to `-8` |
| Parts warehouse corner | X `-14` to `-7`, Z `4` to `11` |
| Reception/payment corner | X `8` to `14`, Z `4` to `11` |
| Repair bay 1 | X `-4` to `4`, Z `-2` to `6` |
| Locked tire station | X `-13` to `-6`, Z `-5` to `2` |
| Locked oil station | X `6` to `13`, Z `-5` to `2` |

#### Level 1 Static Road Layout

Place road pieces first. Use these as visual road modules and add separate invisible waypoint nodes for navigation.

| ID | Asset | Position | Rotation | Role |
|---|---|---:|---:|---|
| `road_front_01` | `kenney_city-kit-roads/Models/GLB format/road-straight.glb` | `[-16,0,-12]` | `[0,90,0]` | Customer approach road |
| `road_front_02` | `kenney_city-kit-roads/Models/GLB format/road-straight.glb` | `[-8,0,-12]` | `[0,90,0]` | Queue road |
| `road_front_03` | `kenney_city-kit-roads/Models/GLB format/road-straight.glb` | `[0,0,-12]` | `[0,90,0]` | Queue road |
| `road_front_04` | `kenney_city-kit-roads/Models/GLB format/road-straight.glb` | `[8,0,-12]` | `[0,90,0]` | Queue road |
| `road_front_05` | `kenney_city-kit-roads/Models/GLB format/road-straight.glb` | `[16,0,-12]` | `[0,90,0]` | Exit road |
| `road_entry_driveway` | `kenney_city-kit-roads/Models/GLB format/road-driveway-single.glb` | `[-8,0,-8]` | `[0,0,0]` | Driveway from road into service |
| `road_exit_driveway` | `kenney_city-kit-roads/Models/GLB format/road-driveway-single.glb` | `[8,0,-8]` | `[0,180,0]` | Driveway out of service |
| `road_service_pad` | `kenney_racing-kit/Models/GLTF format/roadPitGarage.glb` | `[0,0,1]` | `[0,0,0]` | Repair bay floor |
| `road_queue_marker_01` | `kenney_city-kit-roads/Models/GLB format/road-straight-half.glb` | `[-8,0,-12]` | `[0,90,0]` | Visual queue slot overlay if needed |

Vehicle waypoint path:

| Waypoint ID | Position | Notes |
|---|---:|---|
| `veh_spawn` | `[-24,0,-12]` | Spawn off-screen/front-left |
| `veh_queue_01` | `[-8,0,-12]` | First visible waiting position |
| `veh_queue_02_locked` | `[-2,0,-12]` | Unlock after second queue construction |
| `veh_to_repair_01` | `[-4,0,-6]` | Turn into garage |
| `veh_repair_01` | `[0,0,1]` | Reserved bay slot |
| `veh_to_payment` | `[7,0,-5]` | Completed vehicle moves near reception |
| `veh_exit` | `[24,0,-12]` | Despawn off-screen/front-right |

#### Level 1 Garage Shell And Architecture

Use these as visible walls/office/garage shape. Do not place a full roof over the playable floor unless it fades by camera distance.

| ID | Asset | Position | Rotation | Role |
|---|---|---:|---:|---|
| `garage_bay_shell` | `kenney_racing-kit/Models/GLTF format/pitsGarage.glb` | `[0,0,4]` | `[0,180,0]` | Main open garage bay backdrop |
| `garage_left_corner` | `kenney_racing-kit/Models/GLTF format/pitsGarageCorner.glb` | `[-8,0,4]` | `[0,180,0]` | Left bay corner/wall |
| `reception_office` | `kenney_racing-kit/Models/GLTF format/pitsOffice.glb` | `[10,0,7]` | `[0,180,0]` | Reception room |
| `reception_office_corner` | `kenney_racing-kit/Models/GLTF format/pitsOfficeCorner.glb` | `[14,0,7]` | `[0,180,0]` | Office edge |
| `garage_sign_low` | `kenney_racing-kit/Models/GLTF format/billboardLow.glb` | `[0,0,11.5]` | `[0,180,0]` | Garage name sign |
| `roadside_sign` | `kenney_city-kit-roads/Models/GLB format/sign-highway-wide.glb` | `[-18,0,-7]` | `[0,90,0]` | Roadside business sign |
| `lot_light_left` | `kenney_city-kit-roads/Models/GLB format/light-square-double.glb` | `[-15,0,-7]` | `[0,45,0]` | Parking/queue readability |
| `lot_light_right` | `kenney_city-kit-roads/Models/GLB format/light-square-double.glb` | `[15,0,-7]` | `[0,-45,0]` | Parking/queue readability |

#### Level 1 Gameplay Objects

These are the core interactables. Add invisible trigger colliders for gameplay and keep visual assets separate as child objects.

| ID | Visual Asset | Position | Rotation | Gameplay Role | Starts Unlocked |
|---|---|---:|---:|---|---|
| `player_spawn` | `kenney_blocky-characters_20/Models/GLB format/character-a.glb` | `[-5,0,-4]` | `[0,45,0]` | Player start | Yes |
| `parts_source_visual_box_01` | `kenney_car-kit/Models/GLB format/box.glb` | `[-11,0,7]` | `[0,20,0]` | Parts pile visual | Yes |
| `parts_source_visual_crate_01` | `kenney_platformer-kit/Models/GLB format/crate.glb` | `[-13,0,7]` | `[0,-10,0]` | Parts pile visual | Yes |
| `parts_source_zone` | None | `[-11.5,0,6.5]` | `[0,0,0]` | Pickup zone: `BasicParts` | Yes |
| `repair_bay_01_zone` | `kenney_racing-kit/Models/GLTF format/roadPitGarage.glb` | `[0,0,1]` | `[0,0,0]` | Workstation: basic repair | Yes |
| `repair_bay_01_barrier_l` | `kenney_racing-kit/Models/GLTF format/barrierWhite.glb` | `[-4,0,1]` | `[0,0,0]` | Bay boundary | Yes |
| `repair_bay_01_barrier_r` | `kenney_racing-kit/Models/GLTF format/barrierWhite.glb` | `[4,0,1]` | `[0,0,0]` | Bay boundary | Yes |
| `repair_bay_01_tool_bolt` | `kenney_car-kit/Models/GLB format/debris-bolt.glb` | `[-2,0,4]` | `[0,35,0]` | Repair dressing | Yes |
| `repair_bay_01_tool_nut` | `kenney_car-kit/Models/GLB format/debris-nut.glb` | `[2,0,4]` | `[0,-20,0]` | Repair dressing | Yes |
| `payment_point_visual` | `kenney_platformer-kit/Models/GLB format/coin-gold.glb` | `[9,0,2]` | `[0,0,0]` | Payment pickup visual | Yes |
| `payment_point_zone` | None | `[9,0,2]` | `[0,0,0]` | Cash collection trigger | Yes |
| `construction_queue_02` | `kenney_platformer-kit/Models/GLB format/button-round.glb` | `[-2,0,-8]` | `[0,0,0]` | Buy second queue slot | No, visible locked |
| `construction_tire_station` | `kenney_platformer-kit/Models/GLB format/button-round.glb` | `[-9,0,-2]` | `[0,0,0]` | Buy tire station | No, visible locked |
| `construction_oil_station` | `kenney_platformer-kit/Models/GLB format/button-round.glb` | `[9,0,-2]` | `[0,0,0]` | Buy oil station | No, visible locked |
| `employee_hire_pad` | `kenney_platformer-kit/Models/GLB format/button-square.glb` | `[12,0,2]` | `[0,0,0]` | Hire first transporter | No, reveal after tire station |

Recommended zone data:

```json
[
  {
    "id": "parts_source_zone",
    "type": "ResourceSource",
    "resourceId": "BasicParts",
    "capacity": 12,
    "regenSeconds": 1.25,
    "position": [-11.5, 0, 6.5],
    "radius": 2.0,
    "acceptedActors": ["Player", "PartsTransporter"]
  },
  {
    "id": "repair_bay_01_zone",
    "type": "Workstation",
    "workstationId": "BasicRepairBay",
    "acceptedResources": ["BasicParts"],
    "vehicleSlot": "veh_repair_01",
    "inputCapacity": 8,
    "serviceSeconds": 4.5,
    "position": [0, 0, 1],
    "radius": 2.25
  },
  {
    "id": "payment_point_zone",
    "type": "PaymentPoint",
    "currencyId": "Cash",
    "position": [9, 0, 2],
    "radius": 2.0
  }
]
```

#### Level 1 Locked/Unlockable Stations

Use locked visuals from the start so players understand future expansion space.

| Unlock ID | Cost Suggestion | Assets Before Unlock | Assets After Unlock | Position |
|---|---:|---|---|---:|
| `queue_slot_02` | `60` | `construction-barrier.glb`, `construction-light.glb`, `button-round.glb` | second waypoint marker, cones repositioned | `[-2,0,-8]` |
| `tire_station_01` | `120` | barriers around wheel stack | `wheel-default.glb` stack, `wheel-racing.glb`, technician slot | `[-9,0,-2]` |
| `oil_station_01` | `180` | barriers around barrel/tank | `barrel.glb`, `detail-tank.glb`, service zone | `[9,0,-2]` |
| `employee_transporter_01` | `250` | hire pad only | `character-k.glb` spawns near parts source | `[12,0,2]` |

Locked tire station visuals:

| ID | Asset | Position | Rotation | Notes |
|---|---|---:|---:|---|
| `tire_locked_barrier_01` | `kenney_city-kit-roads/Models/GLB format/construction-barrier.glb` | `[-9,0,-5]` | `[0,90,0]` | South barrier |
| `tire_locked_light_01` | `kenney_city-kit-roads/Models/GLB format/construction-light.glb` | `[-12,0,-5]` | `[0,0,0]` | Construction feedback |
| `tire_preview_stack_01` | `kenney_car-kit/Models/GLB format/wheel-default.glb` | `[-11,0,0]` | `[90,0,0]` | Preview of future resource |
| `tire_preview_stack_02` | `kenney_car-kit/Models/GLB format/wheel-default.glb` | `[-11,0.6,0]` | `[90,0,0]` | Stack item |
| `tire_preview_stack_03` | `kenney_car-kit/Models/GLB format/wheel-default.glb` | `[-11,1.2,0]` | `[90,0,0]` | Stack item |

Unlocked tire station visuals:

| ID | Asset | Position | Rotation | Gameplay Role |
|---|---|---:|---:|---|
| `tire_source_zone` | None | `[-11,0,0]` | `[0,0,0]` | Pickup zone: `Tire` |
| `tire_station_zone` | None | `[-7,0,-1]` | `[0,0,0]` | Workstation: tire service |
| `tire_station_wheel_01` | `kenney_car-kit/Models/GLB format/wheel-racing.glb` | `[-7,0,-1]` | `[90,0,0]` | Station prop |
| `tire_station_barrier_l` | `kenney_racing-kit/Models/GLTF format/barrierWhite.glb` | `[-12,0,-2]` | `[0,90,0]` | Boundary |
| `tire_station_barrier_r` | `kenney_racing-kit/Models/GLTF format/barrierWhite.glb` | `[-6,0,-2]` | `[0,90,0]` | Boundary |

Unlocked oil station visuals:

| ID | Asset | Position | Rotation | Gameplay Role |
|---|---|---:|---:|---|
| `oil_source_barrel_01` | `kenney_platformer-kit/Models/GLB format/barrel.glb` | `[11,0,0]` | `[0,0,0]` | Oil resource visual |
| `oil_source_barrel_02` | `kenney_platformer-kit/Models/GLB format/barrel.glb` | `[12.2,0,0.2]` | `[0,20,0]` | Oil resource visual |
| `oil_source_tank` | `kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb` | `[12,0,2]` | `[0,90,0]` | Oil storage visual |
| `oil_source_zone` | None | `[11.5,0,0]` | `[0,0,0]` | Pickup zone: `OilCan` |
| `oil_station_zone` | None | `[7,0,-1]` | `[0,0,0]` | Workstation: oil change |

#### Level 1 Character And Vehicle Spawn Setup

| ID | Asset | Position | Rotation | Role |
|---|---|---:|---:|---|
| `player` | `kenney_blocky-characters_20/Models/GLB format/character-a.glb` | `[-5,0,-4]` | `[0,45,0]` | Controlled character |
| `customer_spawn_template` | `kenney_blocky-characters_20/Models/GLB format/character-c.glb` | `[-10,0,-10]` | `[0,90,0]` | Customer agent prefab template |
| `employee_transporter_template` | `kenney_blocky-characters_20/Models/GLB format/character-k.glb` | `[-12,0,4]` | `[0,0,0]` | First employee template |
| `cashier_template` | `kenney_blocky-characters_20/Models/GLB format/character-m.glb` | `[11,0,5]` | `[0,180,0]` | Later receptionist/cashier |
| `vehicle_sedan_template` | `kenney_car-kit/Models/GLB format/sedan.glb` | `[-24,0,-12]` | `[0,90,0]` | Basic customer car |
| `vehicle_suv_template` | `kenney_car-kit/Models/GLB format/suv.glb` | `[-24,0,-12]` | `[0,90,0]` | Larger customer car |
| `vehicle_hatchback_template` | `kenney_car-kit/Models/GLB format/hatchback-sports.glb` | `[-24,0,-12]` | `[0,90,0]` | Faster/premium early car |

Recommended vehicle service recipes:

```json
[
  {
    "serviceId": "BasicRepair",
    "vehicleTypes": ["Sedan", "Hatchback", "SUV"],
    "requiredWorkstations": ["BasicRepairBay"],
    "requiredResources": [{"resourceId": "BasicParts", "amount": 2}],
    "durationSeconds": 4.5,
    "revenue": 20,
    "visualProps": ["debris-bolt", "debris-nut"]
  },
  {
    "serviceId": "TireChange",
    "vehicleTypes": ["Sedan", "Hatchback", "SUV"],
    "requiredWorkstations": ["TireStation"],
    "requiredResources": [{"resourceId": "Tire", "amount": 4}],
    "durationSeconds": 6.0,
    "revenue": 35,
    "unlockId": "tire_station_01"
  },
  {
    "serviceId": "OilChange",
    "vehicleTypes": ["Sedan", "Hatchback", "SUV"],
    "requiredWorkstations": ["OilStation"],
    "requiredResources": [{"resourceId": "OilCan", "amount": 1}],
    "durationSeconds": 5.5,
    "revenue": 45,
    "unlockId": "oil_station_01"
  }
]
```

#### Level 1 Decoration And Boundary Dressing

Keep these outside main pathing lanes.

| ID | Asset | Position | Rotation | Role |
|---|---|---:|---:|---|
| `front_cone_01` | `kenney_car-kit/Models/GLB format/cone.glb` | `[-5,0,-8]` | `[0,0,0]` | Queue/bay boundary |
| `front_cone_02` | `kenney_car-kit/Models/GLB format/cone.glb` | `[5,0,-8]` | `[0,0,0]` | Queue/bay boundary |
| `parts_delivery_van` | `kenney_car-kit/Models/GLB format/delivery-flat.glb` | `[-16,0,5]` | `[0,90,0]` | Parked supplier vehicle |
| `fence_back_01` | `kenney_city-kit-suburban_20/Models/GLB format/fence.glb` | `[-12,0,13]` | `[0,90,0]` | Back boundary |
| `fence_back_02` | `kenney_city-kit-suburban_20/Models/GLB format/fence.glb` | `[-6,0,13]` | `[0,90,0]` | Back boundary |
| `tree_left_01` | `kenney_city-kit-suburban_20/Models/GLB format/tree-small.glb` | `[-20,0,6]` | `[0,0,0]` | Exterior decoration |
| `tree_right_01` | `kenney_city-kit-suburban_20/Models/GLB format/tree-large.glb` | `[19,0,7]` | `[0,0,0]` | Exterior decoration |
| `commercial_backdrop_01` | `kenney_city-kit-commercial_2/Models/GLB format/low-detail-building-wide-a.glb` | `[20,0,16]` | `[0,225,0]` | Distant backdrop |

### Level 2 Scene: Urban Service Center

Scene purpose: expand from one garage bay into a busier city service center with reception, two repair bays, tire/oil stations, and a clearer customer queue.

Scene name suggestion: `L02_UrbanServiceCenter`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Main road frontage | `Z -16`, X `-28..28` | `road-straight.glb`, `road-side-entry.glb`, `road-side-exit.glb` | Customer arrival and exit |
| Reception office | `[14,0,6]` | `pitsOffice.glb`, `building-window-large.glb`, `coin-gold.glb` | Check-in/payment |
| Repair bay 1 | `[-5,0,2]` | `pitsGarage.glb`, `roadPitGarage.glb`, `barrierWhite.glb` | Basic repair |
| Repair bay 2 | `[3,0,2]` | `pitsGarage.glb`, `roadPitGarage.glb`, `barrierWhite.glb` | Higher throughput |
| Tire station | `[-15,0,-2]` | `wheel-default.glb`, `wheel-racing.glb`, `barrierWhite.glb` | Tire-change chain |
| Oil station | `[14,0,-2]` | `barrel.glb`, `detail-tank.glb` | Oil-change chain |
| Parts warehouse | `[-17,0,8]` | `box.glb`, `crate.glb`, `delivery.glb` | Resource source |
| Employee room | `[18,0,10]` | `pitsOfficeCorner.glb`, `character-k.glb` | Employee unlock/upgrades |

Key asset list:

```text
kenney_city-kit-commercial_2/Models/GLB format/building-a.glb
kenney_city-kit-commercial_2/Models/GLB format/building-b.glb
kenney_city-kit-commercial_2/Models/GLB format/detail-awning-wide.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_racing-kit/Models/GLTF format/pitsGarage.glb
kenney_racing-kit/Models/GLTF format/pitsGarageCorner.glb
kenney_racing-kit/Models/GLTF format/pitsOffice.glb
kenney_car-kit/Models/GLB format/sedan.glb
kenney_car-kit/Models/GLB format/suv.glb
kenney_car-kit/Models/GLB format/taxi.glb
kenney_car-kit/Models/GLB format/van.glb
```

Generation notes:

- Add three visible queue slots on the front road: `[-12,0,-16]`, `[-6,0,-16]`, `[0,0,-16]`.
- Place two service-bay vehicle slots at `[-5,0,2]` and `[3,0,2]`.
- Use city commercial buildings as non-playable backdrop behind `Z 16`.
- Upgrade this scene by adding a cashier employee and parts transporter from the start.

### Level 3 Scene: Tuning Workshop

Scene purpose: introduce customization, spoilers, racing wheels, inspection lights, and premium service timing.

Scene name suggestion: `L03_TuningWorkshop`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Performance entrance | `[-20,0,-12]` | `roadStartPositions.glb`, `flagCheckers.glb` | Premium car arrival |
| Tuning bay | `[0,0,2]` | `pitsGarage.glb`, `overheadLights.glb`, `debris-spoiler-a.glb` | Spoiler/tuning service |
| Racing tire station | `[-10,0,0]` | `wheel-racing.glb`, `raceCarRed.glb` | High-value tire service |
| Dyno/test lane placeholder | `[10,0,0]` | `roadStraightArrow.glb`, `lightRedDouble.glb`, `radarEquipment.glb` | Vehicle test |
| Parts source | `[-15,0,8]` | `debris-drivetrain.glb`, `crate-item.glb` | Advanced parts |
| Showcase exit | `[18,0,-10]` | `billboardLow.glb`, `flagGreen.glb` | Delivery/payment |

Key asset list:

```text
kenney_car-kit/Models/GLB format/sedan-sports.glb
kenney_car-kit/Models/GLB format/hatchback-sports.glb
kenney_car-kit/Models/GLB format/race.glb
kenney_car-kit/Models/GLB format/race-future.glb
kenney_car-kit/Models/GLB format/debris-spoiler-a.glb
kenney_car-kit/Models/GLB format/debris-spoiler-b.glb
kenney_car-kit/Models/GLB format/debris-drivetrain.glb
kenney_car-kit/Models/GLB format/wheel-racing.glb
kenney_racing-kit/Models/GLTF format/flagCheckers.glb
kenney_racing-kit/Models/GLTF format/roadStartPositions.glb
kenney_racing-kit/Models/GLTF format/radarEquipment.glb
kenney_racing-kit/Models/GLTF format/overheadLights.glb
```

Generation notes:

- Make this scene visually more performance-focused: flags, overhead lights, racing road pieces.
- Use `debris-spoiler-a.glb` and `debris-spoiler-b.glb` as carried customization resources.
- Use `raceCarRed.glb`, `raceCarGreen.glb`, etc. from racing-kit as later high-value vehicles if their style matches your car-kit scale.

### Level 4 Scene: Luxury Detailing Studio

Scene purpose: introduce wash/detailing chain and premium customer satisfaction.

Scene name suggestion: `L04_LuxuryDetailingStudio`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Clean showroom entrance | `[-18,0,-12]` | `building-window-large.glb`, `door-white-glass.glb` | Customer arrival |
| Wash bay | `[-6,0,0]` | `road-driveway-single.glb`, `detail-tank.glb`, `barrel.glb` | Wash service |
| Detailing bay | `[4,0,0]` | `pitsGarageClosed.glb`, `overheadLights.glb` | Detail service |
| Supply storage | `[-14,0,7]` | `crate-item.glb`, `barrel.glb` | Cleaning supplies |
| Premium delivery area | `[14,0,-5]` | `building-window-wide.glb`, `flagGreen.glb` | Higher-value payment |

Key asset list:

```text
kenney_car-kit/Models/GLB format/suv-luxury.glb
kenney_car-kit/Models/GLB format/sedan-sports.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_modular-buildings/Models/GLB format/building-window-wide.glb
kenney_modular-buildings/Models/GLB format/door-white-glass.glb
kenney_city-kit-commercial_2/Models/GLB format/detail-awning-wide.glb
kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb
kenney_platformer-kit/Models/GLB format/barrel.glb
kenney_platformer-kit/Models/GLB format/crate-item.glb
```

Generation notes:

- Add custom water/foam particles; available assets only support the physical layout.
- Use cleaner, brighter materials than earlier garage scenes.
- Keep vehicle lanes slower and fewer, but payment values higher.

### Level 5 Scene: Bodywork And Paint Facility

Scene purpose: add body panels, paint mixing, paint booth, and a two-stage production chain.

Scene name suggestion: `L05_BodyPaintFacility`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Damaged vehicle queue | `[-18,0,-14]` | `road-straight.glb`, `construction-cone.glb` | Bodywork arrivals |
| Body repair bay | `[-6,0,0]` | `debris-door.glb`, `debris-bumper.glb`, `debris-plate-a.glb` | Body repair |
| Paint mixing station | `[4,0,6]` | `barrel.glb`, `detail-tank.glb` | Convert paint supplies |
| Paint booth | `[8,0,0]` | `pitsGarageClosed.glb`, `overheadLights.glb` | Paint service |
| Parts warehouse | `[-15,0,7]` | `crate.glb`, `box.glb`, `delivery.glb` | Body panels/paint |
| Inspection exit | `[16,0,-8]` | `lightRedDouble.glb`, `flagGreen.glb` | Completion |

Key asset list:

```text
kenney_car-kit/Models/GLB format/debris-bumper.glb
kenney_car-kit/Models/GLB format/debris-door.glb
kenney_car-kit/Models/GLB format/debris-door-window.glb
kenney_car-kit/Models/GLB format/debris-plate-a.glb
kenney_car-kit/Models/GLB format/debris-plate-b.glb
kenney_platformer-kit/Models/GLB format/barrel.glb
kenney_city-kit-industrial_1/Models/GLB format/detail-tank.glb
kenney_racing-kit/Models/GLTF format/pitsGarageClosed.glb
kenney_racing-kit/Models/GLTF format/overheadLights.glb
```

Generation notes:

- Use body debris as both inventory resources and station dressing.
- Paint cans are missing; generate simple colored cylinders or use recolored barrels as placeholders.
- Paint booth should be semi-enclosed but camera-readable.

### Level 6 Scene: Used-Car Dealership

Scene purpose: shift from servicing individual customer cars to preparing vehicles for resale.

Scene name suggestion: `L06_UsedCarDealership`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Trade-in intake | `[-18,0,-12]` | `road-side-entry.glb`, `sedan.glb`, `van.glb` | Vehicle acquisition |
| Wash/inspection | `[-8,0,0]` | `detail-tank.glb`, `radarEquipment.glb` | Prep chain |
| Small repair bay | `[0,0,0]` | `pitsGarage.glb`, `debris-bolt.glb` | Basic repair |
| Showroom row | `[10,0,2]` | `building-window-large.glb`, vehicles | Display inventory |
| Buyer/payment point | `[16,0,-6]` | `coin-gold.glb`, `character-c.glb` | Sale completion |

Key asset list:

```text
kenney_city-kit-commercial_2/Models/GLB format/building-a.glb
kenney_city-kit-commercial_2/Models/GLB format/detail-awning-wide.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_modular-buildings/Models/GLB format/door-white-glass.glb
kenney_car-kit/Models/GLB format/sedan.glb
kenney_car-kit/Models/GLB format/van.glb
kenney_car-kit/Models/GLB format/truck.glb
kenney_car-kit/Models/GLB format/suv.glb
```

Generation notes:

- Create 4-6 marked showroom slots at `[6,0,4]`, `[10,0,4]`, `[14,0,4]`, `[6,0,-1]`, `[10,0,-1]`, `[14,0,-1]`.
- Use vehicle keys as a handoff resource: `kenney_platformer-kit/Models/GLB format/key.glb`.
- Add a salesperson role using `character-p.glb` or `character-q.glb`.

### Level 7 Scene: Premium Dealership

Scene purpose: larger showroom, luxury cars, premium customization, higher-value sales.

Scene name suggestion: `L07_PremiumDealership`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Glass showroom | `[0,0,6]` | `building-window-wide.glb`, `building-window-large.glb`, `door-white-glass.glb` | Premium display |
| Delivery detailing bay | `[-12,0,-2]` | `pitsGarageClosed.glb`, `overheadLights.glb` | Final detail |
| Customer lounge/reception | `[14,0,6]` | `pitsOffice.glb`, `detail-awning-wide.glb` | Sales flow |
| Premium vehicle pads | X `-6..10`, Z `2..9` | `suv-luxury.glb`, `sedan-sports.glb`, `race-future.glb` | Inventory |
| Delivery exit | `[16,0,-10]` | `road-side-exit.glb`, `flagGreen.glb` | Vehicle handoff |

Key asset list:

```text
kenney_car-kit/Models/GLB format/suv-luxury.glb
kenney_car-kit/Models/GLB format/sedan-sports.glb
kenney_car-kit/Models/GLB format/race-future.glb
kenney_modular-buildings/Models/GLB format/building-window-wide.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_modular-buildings/Models/GLB format/door-white-glass.glb
kenney_city-kit-commercial_2/Models/GLB format/building-skyscraper-a.glb
kenney_city-kit-commercial_2/Models/GLB format/low-detail-building-wide-b.glb
```

Generation notes:

- Use fewer vehicles than the used dealership but make each slot larger and cleaner.
- Add custom floor material/primitive platform if you want a true showroom feel.
- Keep most operations world-based: staff move keys, cleaning supplies, and payment.

### Level 8 Scene: Exotic-Car Customization Center

Scene purpose: top-end paint, tuning, wheels, inspection, and display.

Scene name suggestion: `L08_ExoticCustomizationCenter`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Exotic intake lane | `[-22,0,-14]` | `roadStraightLong.glb`, `flagCheckers.glb` | Premium arrival |
| Custom paint booth | `[-8,0,0]` | `pitsGarageClosed.glb`, `barrel.glb`, `detail-tank.glb` | Paint service |
| Performance tuning | `[2,0,0]` | `debris-drivetrain.glb`, `debris-spoiler-a.glb`, `radarEquipment.glb` | Tuning |
| Racing wheel station | `[10,0,0]` | `wheel-racing.glb`, `barrierWhite.glb` | Premium tires |
| Photo/display exit | `[16,0,8]` | `billboard.glb`, `overheadLights.glb` | Completion/premium bonus |

Key asset list:

```text
kenney_car-kit/Models/GLB format/race-future.glb
kenney_car-kit/Models/GLB format/race.glb
kenney_racing-kit/Models/GLTF format/raceCarRed.glb
kenney_racing-kit/Models/GLTF format/raceCarWhite.glb
kenney_car-kit/Models/GLB format/wheel-racing.glb
kenney_car-kit/Models/GLB format/debris-spoiler-a.glb
kenney_car-kit/Models/GLB format/debris-drivetrain.glb
kenney_racing-kit/Models/GLTF format/radarEquipment.glb
kenney_racing-kit/Models/GLTF format/flagCheckers.glb
```

Generation notes:

- This scene should use the most racing-kit props.
- Keep production chain compact: paint -> tune -> wheel -> inspection -> delivery.
- Use material overrides to create multiple exotic paint variants from the same car mesh.

### Level 9 Scene: Motorsport Preparation Facility

Scene purpose: industrial/racing prep with multiple lanes, pit garages, inspection, and race-car output.

Scene name suggestion: `L09_MotorsportPrepFacility`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| Pit lane | `Z -14`, X `-28..28` | `roadPitStraightLong.glb`, `roadPitEntry.glb` | Vehicle flow |
| Pit garage row | `X -12,0,12`, Z `2` | `pitsGarage.glb`, `pitsGarageCorner.glb` | Parallel prep bays |
| Parts logistics | `[-20,0,8]` | `delivery-flat.glb`, `crate.glb`, `debris-drivetrain.glb` | Transport bottleneck |
| Inspection gate | `[18,0,-4]` | `overheadLights.glb`, `lightRedDouble.glb`, `radarEquipment.glb` | Final test |
| Launch area | `[24,0,-14]` | `roadStartPositions.glb`, `flagGreen.glb`, `flagCheckers.glb` | Completion |

Key asset list:

```text
kenney_racing-kit/Models/GLTF format/roadPitEntry.glb
kenney_racing-kit/Models/GLTF format/roadPitGarage.glb
kenney_racing-kit/Models/GLTF format/roadPitStraightLong.glb
kenney_racing-kit/Models/GLTF format/pitsGarage.glb
kenney_racing-kit/Models/GLTF format/pitsGarageCorner.glb
kenney_racing-kit/Models/GLTF format/roadStartPositions.glb
kenney_racing-kit/Models/GLTF format/flagCheckers.glb
kenney_racing-kit/Models/GLTF format/raceCarGreen.glb
kenney_racing-kit/Models/GLTF format/raceCarOrange.glb
kenney_racing-kit/Models/GLTF format/raceCarRed.glb
kenney_racing-kit/Models/GLTF format/raceCarWhite.glb
```

Generation notes:

- This is the best scene for three or more simultaneous service bays.
- Use waypoint reservations aggressively so race cars do not visually collide in pit lane.
- Make employee automation prominent: multiple mechanics, tire technicians, parts transporters.

### Level 10 Scene: Automotive Business Headquarters

Scene purpose: meta-progression hub: office, finance upgrades, franchise map, showroom preview, and light service demo.

Scene name suggestion: `L10_AutomotiveHQ`.

Layout summary:

| Area | Position | Main Assets | Gameplay |
|---|---:|---|---|
| HQ tower/backdrop | `[0,0,14]` | `building-skyscraper-a.glb`, `building-skyscraper-b.glb` | Visual headquarters |
| Executive office | `[10,0,4]` | `pitsOffice.glb`, modular windows/doors | Permanent upgrades |
| Franchise board | `[0,0,2]` | `billboard.glb`, `sign-highway-wide.glb` | Location selection |
| Showcase garage | `[-10,0,0]` | `pitsGarage.glb`, premium cars | Trophy/progression display |
| Finance collection | `[8,0,-6]` | `coin-gold.glb`, `chest.glb` | Offline earnings pickup |

Key asset list:

```text
kenney_city-kit-commercial_2/Models/GLB format/building-skyscraper-a.glb
kenney_city-kit-commercial_2/Models/GLB format/building-skyscraper-b.glb
kenney_city-kit-commercial_2/Models/GLB format/building-skyscraper-c.glb
kenney_modular-buildings/Models/GLB format/building-window-large.glb
kenney_modular-buildings/Models/GLB format/door-white-glass.glb
kenney_racing-kit/Models/GLTF format/billboard.glb
kenney_city-kit-roads/Models/GLB format/sign-highway-wide.glb
kenney_platformer-kit/Models/GLB format/chest.glb
kenney_platformer-kit/Models/GLB format/coin-gold.glb
```

Generation notes:

- This scene can be more menu-like, but still keep upgrade areas physical.
- Use construction pads as franchise unlock pads.
- Place miniature versions of earlier businesses as non-interactive display dioramas if performance allows.

## LLM-Friendly Prefab Role Dictionary

Use this dictionary to convert asset paths into Unity prefab roles automatically.

```json
{
  "PlayerCharacter": {
    "asset": "kenney_blocky-characters_20/Models/GLB format/character-a.glb",
    "components": ["CharacterController", "PlayerMovement", "StackInventory", "InteractionScanner"]
  },
  "CustomerCharacter": {
    "assetOptions": [
      "kenney_blocky-characters_20/Models/GLB format/character-c.glb",
      "kenney_blocky-characters_20/Models/GLB format/character-d.glb",
      "kenney_blocky-characters_20/Models/GLB format/character-e.glb"
    ],
    "components": ["CustomerAgent", "WaypointMover", "PatienceMeter"]
  },
  "MechanicEmployee": {
    "asset": "kenney_blocky-characters_20/Models/GLB format/character-l.glb",
    "components": ["EmployeeAgent", "WaypointMover", "WorkstationWorker"]
  },
  "TransporterEmployee": {
    "asset": "kenney_blocky-characters_20/Models/GLB format/character-k.glb",
    "components": ["EmployeeAgent", "WaypointMover", "StackInventory", "ResourceTransporter"]
  },
  "BasicCar": {
    "assetOptions": [
      "kenney_car-kit/Models/GLB format/sedan.glb",
      "kenney_car-kit/Models/GLB format/hatchback-sports.glb",
      "kenney_car-kit/Models/GLB format/suv.glb"
    ],
    "components": ["VehicleAgent", "WaypointMover", "ServiceRequestHolder"]
  },
  "BasicPartsResource": {
    "asset": "kenney_car-kit/Models/GLB format/box.glb",
    "components": ["CarryableResource"]
  },
  "TireResource": {
    "asset": "kenney_car-kit/Models/GLB format/wheel-default.glb",
    "components": ["CarryableResource"]
  },
  "OilResource": {
    "asset": "kenney_platformer-kit/Models/GLB format/barrel.glb",
    "components": ["CarryableResource"],
    "notes": "Use reduced scale and oil-colored material override."
  },
  "PaymentPickup": {
    "asset": "kenney_platformer-kit/Models/GLB format/coin-gold.glb",
    "components": ["CurrencyPickup", "FloatingBobAnimation"]
  },
  "ConstructionPad": {
    "asset": "kenney_platformer-kit/Models/GLB format/button-round.glb",
    "components": ["ConstructionZone", "CostDisplayAnchor"]
  }
}
```

## Import Notes For Development

- Prefer GLB/GLTF files for Unity/Godot import consistency.
- Keep all interactable gameplay objects separate from visual decoration in the scene hierarchy.
- Add colliders manually; do not rely on imported render mesh collision for mobile.
- Use simple primitive trigger zones for pickup/deposit/payment/construction areas.
- Attach resource stacks to the player as pooled child objects, e.g. tires stack vertically, boxes stack behind/backpack, coins fly to UI.
- Use material swaps or small world icons for missing services because several resource types are represented by generic crates/barrels.
- Use object pooling for vehicles, customers, coins, resource pickups, and tutorial arrows.

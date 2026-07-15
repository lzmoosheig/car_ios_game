# Setup & current state

This repository is in **early pre-production**. It contains the full design (`docs/design/`)
plus the first working slice of P0 (Doc 08 §2), verified against **Unity 6000.5.3f1**.

## What is verified right now

**1. Engine-agnostic core** (`Assets/_Game/Scripts/Core/`) — economy formulas, stack
inventory, workstation FSM, task board, event bus, save model — passes **81 assertions**
via the editor-free runner (no Unity needed):

```bash
cd dev/CoreTests && dotnet run -c Release   # -> Passed: 81  Failed: 0
```

**2. The gameplay assemblies compile and run inside Unity.** A batchmode import builds
`Overhaul.Core`, `Overhaul.Game`, `Overhaul.Editor` and the test assembly with zero
errors, and the EditMode suite passes **6/6** — the full serve → consume parts → pay
loop through the real `ServiceBay`, the Collect/Deposit `InteractionZone` transfers, and
`CustomerVehicle` driving:

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.5.3f1-arm64/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults results.xml -logFile tests.log
```

**3. Two model-based scenes**, both regenerable from source:
   - `Assets/_Game/Scenes/Graybox.unity` — the original tight garage diorama, built from
     the KayKit City/Forest packs + a low-poly car pack (`Overhaul ▸ Build Graybox Scene`).
   - `Assets/_Game/Scenes/CityGarage.unity` — the current **base/starting scene**: a full
     automotive service **campus** (modeled on the labeled-grid reference image): 21
     station lots in three rows (Parts Delivery/Warehouse/Tire Storage/Engine
     Workshop/Body Repair/Paint Mixing/Paint Booth · Reception/Customer Queue/Basic
     Change Bay/Wheel & Tire/Car Wash/Detailing/Diagnostic/Tuning · Inspection/Completed
     Delivery/Showrooms/Employee Room/Office), each with a tinted pad, building shell,
     colored signboard with 3D `TextMesh` label, and themed props; marked streets with
     crosswalks between rows, front parking apron with painted lines and parked cars,
     entrance/exit gates, perimeter fence and tree ring. Regenerate via
     `Overhaul ▸ Build City Garage Scene` (`CityGarageSceneBuilder.cs` — station grid is
     data-driven in its `Stations` table). The tested gameplay loop is wired in:
     `PartsSource` + Collect zone at the Parts Delivery lot, `ServiceBay` + Deposit zone
     at the Basic Change Bay lot, and customer cars route along the middle street.
     **Play-mode verified:** cars spawn, drive to the bay, get serviced and pay ($24
     each, matching the economy tests), and drive off.

## Models

Two asset sources, both staged outside `Assets/` and copied in curated:
- `Models/` (KayKit + a low-poly car pack) → `Assets/_Game/Art/Models/{Cars,City,Nature}` —
  feeds the `Graybox` scene. One Standard atlas material per pack
  (`Assets/_Game/Art/Materials/{Cars,City,Nature}.mat`).
- `Assets_IOS_Game/` (ten Kenney packs) → `Assets/_Game/Art/Models/Kenney/<Pack>/` — feeds
  the `CityGarage` scene. Materials created by `Overhaul ▸ Setup Kenney Materials`
  (`KenneyModelSetup.cs`): most packs are a single `colormap.png` atlas → one Standard
  material each. **Racing and Nature packs are the exception** — confirmed via
  `execute_code` mesh inspection that they ship real per-submesh embedded materials with
  baked flat colors (e.g. pitsGarage has "grey"/"red" slots, tree_default has
  "woodBark"/"leafsGreen"), not a UV atlas or vertex colors as first assumed — so the
  scene builder deliberately leaves their material argument `null` and keeps Unity's own
  auto-generated per-slot materials rather than overriding them.

`Assets_IOS_Game/AutomotiveIdleUnity/` is a separate, pre-existing Unity project (its own
scene + editor script + an extensive `automotive_idle_scene_asset_catalog.md` asset-usage
doc) — used only as a coordinate/layout reference, not merged in; `CityGarageSceneBuilder`
is our own implementation against our own tested architecture.

**Model placement is bounds-based, not pivot-based.** Most Kenney packs have clean
base-centered FBX pivots, but the racing-kit "pits" pieces (`pitsGarage`, `pitsOffice`,
`roadPitGarage`, `barrierWhite`) bake in large arbitrary offsets from their source kit
layout — placing them by raw `transform.position` puts them meters off target. `PlaceModel`
instantiates at the origin, measures actual world-space renderer bounds, then translates so
those bounds land centered (X/Z) and grounded (Y) at the target — correct for both clean
and offset-pivot models. Shells are fitted to their lots with `ScaleToWidth`.

Empirical facts about the racing-kit shells (established by isolated orbit captures —
worth knowing before touching the builder):
- `pitsGarage`/`pitsOffice` open detailed fronts face **-Z at rotY=0**; every other side
  is blank wall.
- `pitsGarage`'s interior contains a **raised platform** (plus a baked-in red block), so
  cars "parked inside" sink out of sight — the builder parks bay cars at the **bay mouth
  on the pad apron** instead.
- Sign labels use legacy `TextMesh` with the built-in `LegacyRuntime.ttf` (zero package
  dependencies); glyphs read correctly from **-Z at identity rotation** (a Y=180 flip
  renders them mirrored to the main camera).

## Open and play

1. Open this folder in Unity Hub (editor **6000.5.3f1**, pinned in
   `ProjectSettings/ProjectVersion.txt`; add **iOS Build Support** for device builds).
2. Open `Assets/_Game/Scenes/CityGarage.unity` (the current base scene) and press **Play**.
   Cars drive in from the avenue, park at the bay, get serviced (cash ticks up in the OnGUI
   readout) and drive off. Drive the character with **WASD / arrow keys** and carry tires
   from the crate pallet beside the office wing to the bay to keep its rack stocked.
3. Run tests any time from **Window ▸ General ▸ Test Runner** (EditMode).

## Immediate next steps (Doc 08 §2, P0)

- Hands-on feel pass: drive the player around the campus in Play (WASD), check zone
  radii and walking distances between Parts Delivery and the Basic Change Bay.
- Slot-based **queue** + patience so multiple cars line up (currently one active car at a
  time); replace the single `_active` vehicle with a queue in `GarageController` — the
  Customer Queue lot is already in the layout waiting for it.
- **Construction zones** over `EconomyManager`: most campus stations are decorative
  set-dressing today — turn them into locked construction zones that unlock per Doc 03's
  sequence, making the reference layout the level's build-out goal.
- Then the Phase-0 kill-gate playtest before building further.

> `Library/`, `bin/`, `obj/`, and IDE-generated `*.csproj`/`*.sln` are gitignored;
> Unity regenerates them.

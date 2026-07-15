# Overhaul! — Doc 06: Technical Architecture & Data Structures

---

## 1. Engine choice: Unity (Unity 6 LTS, URP, C#)

**Why Unity for an iOS-first idle-arcade game:**
- Mature iOS pipeline: one-click Xcode export, TestFlight workflow, on-device Profiler/Frame Debugger, Metal by default.
- URP hits 60 fps on this art style down to iPhone XR-class hardware with minimal tuning.
- Built-in NavMesh (customers/employees) — writing our own agent avoidance would be the single biggest avoidable cost.
- **ScriptableObjects map exactly onto the data-driven mandate** (§4): designers add resources/recipes/levels without touching code.
- Ecosystem for this genre: DOTween/PrimeTween, pooling libs, Firebase/Unity Analytics, haptics plugins (CoreHaptics bridge), rewarded-ad mediation later.
- Hiring: largest mobile-gameplay talent pool.

**Why not Godot:** viable and improving on iOS, but weaker profiling on-device, no built-in navmesh crowd of the same maturity, smaller mobile-monetization/analytics plugin ecosystem, and C# on iOS via Godot is still the less-trodden path. Godot remains the fallback if Unity licensing terms become unacceptable.

Render/perf budget: ≤ 150 draw calls, ≤ 150k tris, 1 realtime light + baked/blob shadows, 60 fps target on A14, 30 fps floor on A12.

---

## 2. Project & scene structure

```
Assets/
  _Game/
    Data/            ScriptableObjects (all §4 definitions)
    Scripts/
      Core/          GameManager, SaveSystem, EconomyManager, Ticker
      Agents/        Carrier (shared), PlayerController, EmployeeAgent, CustomerAgent
      World/         Workstation, InteractionZone, ConstructionZone, ResourceRack,
                     VehicleMover, WaypointGraph, QueueManager, TaskBoard
      UI/            HUD, Kiosk, EmployeeBoard, EdgeIndicators
    Prefabs/  Art/  Audio/  Levels/
Scenes:
  Boot        (services init, save load, → Level)
  Level_XX    (one scene per location: baked graph, zones, stations)
  HQ          (post-MVP meta scene)
```
- **Boot → additive Level scene.** All managers are plain C# services registered in a lightweight service locator (no DI framework); MonoBehaviours only where engine callbacks are needed.
- **Event bus** (typed C# events) decouples systems: `ServiceCompleted`, `ZoneFunded`, `RackStarved`, `CashCollected` — UI, audio, analytics and tutorial all subscribe rather than being called.

## 3. Core runtime systems

| System | Design |
|---|---|
| **Player controller** | CharacterController + floating joystick (Input System); no physics forces; speed/capacity read live from UpgradeState |
| **Carrier abstraction** | one `Carrier` component shared by player and employees: stack list, capacity, collect/deposit ticks, stack visualizer. Employees = Carrier + brain; player = Carrier + joystick. Single code path = half the bugs |
| **Interaction zones** | trigger colliders on a dedicated layer; zone publishes `(carrier, zoneType, station)` enter/stay/exit; priority resolver applies Doc 02 §1.4 ordering |
| **Stack inventory** | `List<ResourceId>` + capacity; type-filtered removal (Doc 02 §1.3); visualizer pools item meshes, animates with tween arcs |
| **Workstation FSM** | `Idle → Starved ⇄ Ready → Working → Done → (Blocked if output full)`; data-driven timings from WorkstationDefinition; emits state events consumed by VFX/audio/bottleneck cues |
| **Service recipes** | resolver matches a CustomerRequest to a station whose WorkstationDefinition lists the recipe; consumes inputs from the station's rack; applies revenue formula (Doc 04 §1) |
| **Customer AI** | FSM (Doc 02 §3.4 states) on NavMeshAgent; patience timer + mood bubble component; no per-frame decisions — event-driven transitions |
| **Employee AI** | FSM + utility scorer (Doc 02 §4.2) querying the **TaskBoard** (central list of open tasks with atomic claim/release) |
| **Vehicle navigation** | no NavMesh: hand-authored `WaypointGraph` per level (nodes = stalls/slots/bay positions; edges = bezier lanes); `VehicleMover` tweens along edges; **slot reservation table** guarantees collision-freedom (Doc 02 §3.3) |
| **Queue manager** | per-service ring buffer of slots; advances vehicles on slot-freed events; exposes fullness for arrival-pause and honk cues |
| **Construction zones** | funding drain, persisted partial progress, scripted reveal order from LevelDefinition; completion swaps blueprint prefab → station prefab and raises `ZoneFunded` |
| **Upgrade system** | UpgradeDefinitions applied as modifiers to a central `StatRegistry` (`playerSpeed`, `station.tireBay.speed`, …); UI kiosks are thin views over it |
| **Economy manager** | Doc 04 §6; single wallet; offline settlement on `applicationDidBecomeActive` using saved UTC timestamp |
| **Save system** | JSON (schema §4.9) → `Application.persistentDataPath`, atomic write (temp + rename), autosave every 10 s + on pause/background; versioned with explicit migration functions; optional iCloud KVS mirror post-MVP |
| **Object pooling** | mandatory for: resources, cash bills, vehicles, customers, VFX, audio voices. Zero runtime `Instantiate` after level load |
| **Analytics** (post-MVP soft launch) | Unity Analytics or Firebase: FTUE funnel per tutorial beat, time-to-unlock per zone, session length, bottleneck dwell times |
| **Performance** | URP mobile preset; GPU-instanced item meshes; agent brains tick on a 5 Hz scheduler, animation LOD by distance; physics only for triggers (no rigidbody dynamics) |
| **Device testing** | matrix: iPhone SE 2 (perf floor), XR, 13, 15 Pro, iPad 9; profile each milestone; thermal soak 30 min |
| **App Store prep** | privacy manifest + nutrition labels (no tracking in MVP), ATT not needed until ads, app slicing/thinning, launch screen, 4.3-safe original branding review, TestFlight external beta before submission |

---

## 4. Data structures (ScriptableObject schemas)

C#-flavored; every gameplay noun is data. IDs are string keys; cross-references by ID.

### 4.1 ResourceDefinition
```csharp
class ResourceDefinition : ScriptableObject {
  string id;                 // "tire"
  string displayName;
  Sprite icon;  GameObject mesh;
  int stackSlots;            // 1 or 2 (bulky)
  ResourceCategory category; // Part, Consumable, Crafted, Currency, Key
}
```

### 4.2 ServiceRecipe
```csharp
class ServiceRecipe : ScriptableObject {
  string id;                       // "oil_change"
  string displayName;  Sprite requestIcon;
  List<ItemCount> inputs;          // { resourceId, count }
  float workSeconds;               // 8
  int basePrice;                   // 30
  string requiredStationType;      // "oil_bay"
  bool isAddOn;                    // wash/detail chain into main service
  VehicleClassMask allowedClasses;
}
```

### 4.3 WorkstationDefinition
```csharp
class WorkstationDefinition : ScriptableObject {
  string id;                    // "oil_bay"
  List<string> recipeIds;
  int inputRackCapacity;        // 8
  int bayCount;                 // vehicle slots
  bool requiresWorker;          // false in L1 tutorial bays, true later
  string workerRoleId;          // "mechanic"
  UpgradeTrack speedTrack, capacityTrack;   // tiers, curve refs (Doc 04 §2.3)
  StationColor colorFamily;     // drives decals/curbs/VFX (Doc 05 §1.1)
}
```

### 4.4 EmployeeRole
```csharp
class EmployeeRole : ScriptableObject {
  string id;                    // "transporter"
  string displayName;  GameObject skin;  Color overalls;
  List<TaskType> taskTypes;     // Haul, Service, Checkout, MoveVehicle, Sell...
  StatBlock baseStats;          // moveSpeed, capacity, workSpeed, quality, ...
  List<UpgradeTrack> statTracks;
  int hireBaseCost;             // 250 (curve in Doc 04 §2.2)
}
```

### 4.5 VehicleType
```csharp
class VehicleType : ScriptableObject {
  string id;                    // "city_car"
  GameObject mesh;  List<Material> paletteVariants;
  VehicleClass vClass;          // City, Sedan, SUV, Pickup, Coupe, Classic, Super, Race
  float priceMult;              // 1.0 city ... 3.0 super
  int unlockLocation;           // 1..10
}
```

### 4.6 CustomerRequest (runtime struct, spawned from weights)
```csharp
struct CustomerRequest {
  string vehicleTypeId;
  List<string> recipeIds;       // usually 1; multi-service at L8
  string paintColorId;          // optional (paint chain)
  PatienceProfile patience;     // normal / premium (short fuse, big tip)
  int reputationReward;
}
```

### 4.7 UpgradeDefinition
```csharp
class UpgradeDefinition : ScriptableObject {
  string id;                    // "player_speed_2"
  UpgradeScope scope;           // LevelLocal | Permanent
  CurrencyType currency;        // Cash | GoldenWrench
  string targetStat;            // "player.moveSpeed" | "station.oil_bay.speed"
  float valuePerTier;  int maxTiers;
  int baseCost;  float costGrowth;   // Doc 04 curves
}
```

### 4.8 LevelDefinition
```csharp
class LevelDefinition : ScriptableObject {
  string id;  string displayName;      // "rustys_roadside"
  SceneReference scene;
  List<ZoneEntry> constructionOrder;   // { zoneId, cost, prereqZoneIds, tutorialBeat? }
  List<string> availableRecipeIds, vehicleTypeIds, employeeRoleIds;
  ArrivalCurve arrivals;               // base interval, decay per zone (Doc 04 §4)
  int completionContractCost;          // 2500
  int goldenWrenchReward;              // 3
}
```

### 4.9 SaveData (JSON, versioned)
```jsonc
{
  "version": 1,
  "utcLastSeen": "2026-07-13T18:04:11Z",
  "wallet": 1240, "goldenWrenches": 3,
  "currentLevelId": "rustys_roadside",
  "levels": { "rustys_roadside": {
      "zones":    { "oil_bay": { "funded": 120, "built": true } },
      "upgrades": { "station.oil_bay.speed": 2 },
      "employees":[ { "roleId":"transporter", "zone":"parts", "tiers":{"moveSpeed":1} } ],
      "completed": false, "cumulativeEarnings": 3180 } },
  "permanentUpgrades": { "player.carryCapacity": 2, "offline.capHours": 1 },
  "settings": { "haptics": true, "sfx": true, "music": true },
  "tutorial": { "completedBeats": 10 }
}
```
Loading rule: world state is **reconstructed from definitions + save deltas** (never serialized scene objects); in-flight items/vehicles resolve to nearest valid slot (Doc 02 §2.3).

---

## 5. System spec — Save & Offline
- **Purpose:** durable local progress + the offline-earnings return hook.
- **Player interaction:** none visible except the returning-player cash pile & summary toast.
- **Inputs:** autosave ticks, app lifecycle events, UTC clock.
- **Outputs:** SaveData JSON; offline settlement (Doc 04 §4).
- **States:** Clean · Dirty(pending write) · Writing · Migrating(on version bump).
- **Upgrade paths:** offline cap tiers (permanent, Doc 04 §3).
- **Dependencies:** EconomyManager (automated rate snapshot saved each autosave), all definitions.
- **Edge cases:** corrupted file (keep last-good backup copy, restore + toast); clock rollback (clamp to 0); schema bump (explicit `Migrate_v1_v2` chain, never silent field loss).
- **MVP requirements:** local JSON, autosave, offline v1, corruption backup. No backend, no cloud.
- **Later expansion:** iCloud sync, multi-device conflict resolution (latest-wins with confirmation), remote-config balancing.

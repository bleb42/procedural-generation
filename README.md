# Dungeon Gen

Grid-free procedural dungeon generator for Unity. You build rooms as ordinary prefabs, bake their floor and doors with one click, and the generator snaps them together door to door into a connected dungeon with optional loops, special rooms and a built-in minimap.

<!-- TODO: gif of a dungeon being generated -->
<!-- TODO: screenshot of the minimap -->

## Features

- **No grid.** Rooms can be any size (3, 3.5, 9 units) and any shape made of rectangles (L, T, plus). The baker measures them.
- **Door-to-door placement.** Rooms rotate in 90-degree steps and never overlap.
- **Loops.** Doors of neighbouring rooms that line up are connected automatically.
- **Special rooms.** Place a room anywhere or at the deepest point, with a chance and a limit.
- **Deterministic.** The same seed gives the same layout.
- **Frame budgeted.** Generation is spread over frames so the game does not freeze. An immediate mode exists for the editor and tests.
- **Wall cleanup.** Overlapping walls of neighbouring rooms are hidden automatically.
- **Minimap.** A mini map and a zoomable full map.
- **Engine-agnostic core.** The layout algorithm works on plain data and has EditMode tests.

## Requirements

- Unity 2022.3 or newer
- Unity UI (`com.unity.ugui`), which is in a new project by default

## Installation

**Git URL:** Window > Package Manager > + > Add package from git URL:

```
https://github.com/bleb42/<repository>.git
```

**Local:** copy the `com.bleb42.dungeongen` folder into your project's `Packages` folder.

## The Dungeon Gen window

Open **Tools > Dungeon Gen > Open Window**. It covers everything below with buttons:

- **Generate:** quick setup (starter rooms, profile and builder in one click), seed settings, generate, random seed, clear, validate, and a result summary.
- **Rooms:** create box rooms with a chosen size and doors (already baked), a starter set, and a library of all your rooms with a status and Bake / Bake All.
- **Profile:** create a profile, fill it from your rooms, validate it and edit it.
- **Map:** create the mini map and the full map in your Canvas.
- **About:** links and the test runner.

The manual steps are described next.

## Quick start

### 1. Build a room prefab

1. Create an empty GameObject. Add `RoomInstance` and `RoomBaker` to its root. The root scale must be (1, 1, 1).
2. Add a child with `FloorArea` and set its size to the walkable area. For an L-shaped room add several `FloorArea` children. Edges can be anywhere.
3. Add a `DoorSocket` child for every doorway. Place it on the outer edge of a floor area, with its blue arrow (`transform.forward`) pointing out of the room. Doors must face North, East, South or West.
4. Optionally add `WallSocket` children for wall pieces. Wall and door visuals are normal children with meshes.
5. On `RoomInstance`, use the context menu **Collect Sockets From Children**.

### 2. Bake it

1. Create a `RoomDefinition` asset (Create > DungeonGen > Room Definition) and assign the prefab.
2. Assign the asset to `RoomBaker` on the prefab and press **Bake**.

Bake again whenever you change the room. In the editor and development builds a warning is logged at spawn time if a room no longer matches its baked data.

### 3. Create a profile

Create > DungeonGen > Dungeon Profile, then assign:

| Field | Meaning |
|---|---|
| Root | The first room, placed at the origin. |
| Branches | Rooms with two or more doors. They form the body of the dungeon. |
| Leaf | A dead-end room used when no branch fits. Needs at least one door. |
| Special Rooms | Rooms with a chance and a placement (Anywhere or DeepestPoint). |
| Room Count Min / Max | The target size. |
| Connect Adjacent Doors | Creates loops. |
| Max Extra Rooms | How many special rooms may exceed the room count. -1 = unlimited. |

### 4. Generate

Add `DungeonBuilder` to a GameObject, assign the profile and tick **Generate On Start**. Or from code:

```csharp
using DungeonGen;
using UnityEngine;

public class Example : MonoBehaviour
{
    [SerializeField] private DungeonBuilder _builder;

    private void Start()
    {
        _builder.Generated += OnGenerated;
        _builder.Generate();            // spread over frames
        // _builder.Generate(12345);    // fixed seed
        // _builder.GenerateImmediate() // everything in one frame
    }

    private void OnGenerated(DungeonResult dungeon)
    {
        foreach (var room in dungeon.Rooms)
            Debug.Log($"{room.Name}, depth {room.Depth}, weight {room.Weight}");
    }
}
```

In the editor you can also right-click the `DungeonBuilder` and choose **Generate (Editor Preview)**.

## Adding your own content

The package only builds the rooms. Everything else (enemies, loot, NavMesh) is yours. Subscribe to `DungeonBuilder.Generated` and use the `DungeonResult`:

- `Rooms`: every `SpawnedRoom` with its `Instance` (the room prefab instance), `Depth`, `Weight` and floor `Rects`.
- `FindRoom(worldPosition)`: which room a point is in.
- `Layout`: the raw tree of placed rooms, including `Seed` and `Warnings`.

Put your own components on the room prefabs (for example spawn points) and find them with `room.Instance.GetComponentsInChildren<T>()`. `RoomDefinition.Weight` is free for your own use, for example as an enemy budget per room.

## Minimap

1. Create a Canvas with a panel for the mini map. Inside it add an empty child (centered anchors and pivot) and put `MiniMapView` on the panel, assigning the child to **Content**. Do the same for the full map with `FullMapView`.
2. Add `MapPanel` to any active object, assign the views, the `DungeonBuilder` and the player transform.
3. Call `MapPanel.Refit()` when you open the full map.

Colors, scale and gaps are in a `MapStyleData` asset (Create > DungeonGen > Map Style). `MapPanel.Model` exposes the current room and visited rooms.

## How it works

1. `DungeonLayoutGenerator` picks a room count, places the root and repeatedly attaches rooms to open doors. A candidate is rotated so one of its doors faces the open door, moved so the two door points coincide, and accepted if its floor rectangles do not overlap any placed room.
2. `RoomSpawner` instantiates the prefabs.
3. `RoomVisualCoordinator` hides duplicate walls and switches door sockets between door and wall visuals.

## Tests

Window > General > Test Runner > EditMode. The tests cover determinism, overlap, door alignment for non-grid room sizes, room counts and special rooms.

## Limitations

- Rotations are multiples of 90 degrees and doors face the four cardinal directions.
- Rooms are placed on a single floor level.
- Floor areas are axis-aligned rectangles.

## License

MIT

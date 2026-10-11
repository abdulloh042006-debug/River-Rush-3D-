# River Rush 3D

A Unity 6 **Universal 3D (URP)** prototype for Android and Windows: steer a growing river from a mountain source to the sea across seeded procedural terrain.

> **Development status:** source-code prototype. The project has **not** yet been opened or compiled in Unity, and there are **no verified APK / EXE builds**. No Unity installation is needed on the old laptop.

## Implemented in source

| Component | Purpose |
| --- | --- |
| `ProceduralTerrain.cs` | Reproducible 3D terrain; seeded mountain noise and a strictly downhill channel bed |
| `RiverRoute.cs` | Samples the same channel's centerline and elevations |
| `RiverWaterSurface.cs` | Generates a curved two-sided water mesh with flow UVs |
| `StylizedRiverURP.shader` | Low-cost URP water colors, moving ripples, shore foam |
| `RiverRunner.cs` | Prototype forward movement and keyboard/touch steering |
| `RiverGrowth.cs` + `TributaryPickup.cs` | Volume pickups and visible size growth |
| `RiverFollowCamera.cs` | Smooth chase camera |
| `RiverRushSceneBuilder.cs` | Unity editor menu to create a demo scene with placeholder objects |

The river water is **animated geometry**, not simulated fluid. Branching physics, AI, proper ocean, networking, optimization validation and builds are future phases.

## Start on a new laptop

1. Install **Unity Hub**, a **Unity 6** editor, and Android Build Support (SDK/NDK + OpenJDK) if APK export is required. Add Windows Build Support if required for your installed Unity editor / build configuration.
2. In Unity Hub create a **Universal 3D (URP)** project. Do **not** choose Built-in or HDRP: the custom shader is written for URP.
3. Download/clone this repository. Copy its `Assets/Scripts`, `Assets/Shaders` and `Assets/Editor` folders into your new Unity project's `Assets/` directory. Let Unity import the scripts and shader.
4. Use **River Rush → Create Prototype Scene** from Unity's top menu.
5. Use **River Rush → Validate Prototype Geometry** to check channel slope, water mesh and assigned shader. Then open `Assets/Scenes/RiverRushPrototype.unity` and press **Play**.
6. Windows test: A/D or left/right arrow keys. Android test: drag/hold to the left or right side of the screen. If using the new Input System exclusively, enable **Both** under Player → Active Input Handling before testing this prototype.
7. Check Console for errors and use the checklist in `docs/PROTOTYPE_TEST_CHECKLIST.md`.

### Demo expectations

- Terrain is rebuilt using the same integer seed on every run.
- A river corridor slopes from mountainous -Z toward sea-level +Z.
- A ribbon mesh follows the route with a simple animated water material.
- A placeholder sphere moves down the river and can pick up three tributary spheres to increase size.
- The camera follows the sphere.

### Mobile notes

Keep terrain resolution at 64–96 initially, use URP without expensive post-processing, and measure FPS on a real Android device before raising detail. Transparent water still causes overdraw: this shader uses one pass and no texture sampling. Actual performance has not been measured.

### Known limitations

- No ocean mesh or win condition yet.
- Channel curvature is fixed per seed, with mountain-to-sea slope; full random river network will come later.
- Water is not volumetric or physically simulated; pickups only increase a size/volume value.
- Placeholder scene is made with primitives, not final game art.
- The repository currently contains **Assets source files**, not a fully initialized Unity project (Unity will generate its own Packages/ and ProjectSettings/ when you create the URP project).
- Without testing in a matching Unity 6 project, shader import, runtime behavior and Android compatibility remain unverified.

## Repository

https://github.com/abdulloh042006-debug/River-Rush-3D-

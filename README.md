# River Rush 3D — Unity 6 (Android + Windows)

Source-code prototype of a 3D river-racing game. A seeded mountain world leads to the ocean; the player follows a hydraulic current, steers around banks, gathers tributaries, creates ripples, and aims for the sea.

**Current status:** code is pushed; source structure and numerical formulas were reviewed. **Unity compilation, the actual 3D scene, real-device performance and APK/EXE builds have not been run** (the old laptop cannot run Unity well). This is not yet a release-quality game.

## Features implemented as code

- Fresh procedural terrain seed on each new race, with an Inspector toggle to replay a saved seed; monotonic downhill river channel and regenerated pickup positions.
- World-space physical-distance spline/arc-length, samples and slopes.
- Flow speed computed from grade, gravity-like acceleration, friction and shore slowdown; turning inertia, bank boundaries and eddies.
- Dampened 1D physically-inspired surface waves with swimmer and tributary impulse events.
- Water mesh displaced by actual simulated waves, plus inexpensive URP foam/ripple shader.
- Dynamic-body buoyancy/current drag in the river and ocean, plus floating-crate test.
- Water-volume pickups, visible player growth, foam-like wake trail.
- Opaque stylized ocean with approximate ocean buoyancy/current and prototype finish state.
- **Android & Windows performance:** Auto/BatterySaver/Balanced/High/Ultra; dynamic URP render scale, shadow/LOD distance, shader detail, wave mesh update rate; FPS modes Auto/30/60/90/120/Native, on-screen FPS counter and quality menu.

This is an **arcade water-physics approximation**, not full 3D computational fluid dynamics. Real branching tributary hydrology, turbulence simulation, wet-ground interaction, physically computed liquid merging, AI and multiplayer remain future work.

## Open on a new laptop

1. Install **Unity Hub and Unity 6**.
2. Create a **Universal 3D (URP)** Unity project. Do not choose HDRP or Built-In.
3. Download or clone this repository; copy its `Assets/Scripts`, `Assets/Shaders`, and `Assets/Editor` folders into that new URP project's `Assets` directory. This repository contains Unity source files, **not** a complete Unity project including `ProjectSettings`.
4. Wait for C# / URP shader imports to finish. Ensure Unity's **Active Input Handling** is set to **Both** (or legacy Input Manager) because the current runner reads `Input.GetAxisRaw`.
5. Click **River Rush → Create Prototype Scene**.
6. Click **River Rush → Validate Prototype Geometry** and inspect all Console logs. Save the scene.
7. Press **Play**. Control with **A/D** or arrow keys; Android touch steering uses left/right halves of the screen.
8. **F3** toggles FPS counter, **F4** opens performance settings on Windows; on mobile tap **Graphics**. The settings also allow 30/60/90/120 FPS or Native refresh mode.
9. For APK, install Android Build Support / SDK / NDK / OpenJDK via Unity Hub and follow Android build settings. Test APK on an actual device. For EXE, build Windows x64 and test standalone.

## Documents

- [Water physics + FPS tiers](docs/WATER_AND_FPS.md)
- [Prototype validation checklist](docs/PROTOTYPE_TEST_CHECKLIST.md)
- [Five-round review log](docs/AUDIT_5_ROUNDS.md)

## Next milestones

1. Compile and play in Unity on the new laptop; fix any imported shader or scene errors.
2. Test physical water/river interactions on Android and Windows, measure 1% low FPS, CPU/GPU cost and thermals.
3. Upgrade the purely linear river to a branching drainage graph and procedural forks.
4. Replace the temporary IMGUI debug/settings screens and primitive art with finished UI, assets, VFX and audio.
5. Add AI races, balance and later synchronized multiplayer. Publish APK/EXE only after verified builds.

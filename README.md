# River Rush 3D — Unity 6 (Android + Windows)

Source-code prototype of a 3D river-racing game. A seeded mountain world leads to the ocean; the player follows a hydraulic current, steers around banks, gathers tributaries, creates ripples, and aims for the sea.

**Current status:** code is pushed; source structure and numerical formulas were reviewed. **Unity compilation, the actual 3D scene, real-device performance and APK/EXE builds have not been run** (the old laptop cannot run Unity well). This is not yet a release-quality game.

## Features implemented as code

- Fresh procedural terrain seed on each new race, with an Inspector toggle to replay a saved seed; mountains, valleys and a monotonic downhill main river.
- **Five generated tributaries** carved into the same terrain mesh with branching 3D water ribbons and confluence pickups; branch floating bodies receive buoyancy and current drag.
- **Procedural nature** with seeded low-poly conifers, broadleaf trees, rocks, alpine boulders, meadow shrubs and freshwater riverbank reeds, combined into four mesh batches (not hundreds of separate draw objects).
- **Terrain biomes** shaded by elevation and slope: sand, green forests, rocky hills and snowy mountain peaks.
- World-space physical-distance spline/arc-length, samples and slopes.
- Flow speed computed from grade, gravity-like acceleration, friction and shore slowdown; turning inertia, bank boundaries and eddies.
- Dampened 1D physically-inspired surface waves with swimmer and tributary impulse events.
- Water mesh displaced by actual simulated waves, plus inexpensive URP foam/ripple shader.
- Dynamic-body buoyancy/current drag in the river and ocean, plus floating-crate test.
- Water-volume pickups, visible player growth, foam-like wake trail.
- Opaque stylized ocean with approximate ocean buoyancy/current and prototype finish state.
- Five **offline AI racers**, three-second countdown, time/progress leaderboard, finish placement, and replay with a new randomized map.
- **Ambient audio:** procedurally synthesized rushing river, wind gusts, forest leaves/bird calls, and tributary pickup splashes; mute/volume preferences persist. Optional legally licensed recordings can replace synthetic clips.
- **Natural water palettes:** randomized blues, cyan and transparent glass-like droplet materials by default; at >=8 actual human players, use full-spectrum random hues via an external multiplayer-count hook (networking is not yet implemented). Bots don't count as people.
- **Graphics:** shares 12 natural or 48 crowd variant droplet materials to reduce rendering cost as player counts grow; seeded river/ocean colors always stay natural.
- A license-free GitHub Actions source audit (Python). This checks source structure and key dependency markers, **not** a Unity build.
- Editor menu shortcuts to build **Windows x64 EXE** or **Android APK** once a Unity installation is available. No binary has yet been generated.
- **Android & Windows performance:** Auto/BatterySaver/Balanced/High/Ultra; dynamic URP render scale, shadow/LOD distance, shader detail, wave mesh update rate; FPS modes Auto/30/60/90/120/Native, on-screen FPS counter and quality menu.

This is an **arcade water-physics approximation**, not full 3D computational fluid dynamics. Real branching tributary hydrology, turbulence simulation, wet-ground interaction, physically computed liquid merging, advanced AI and online multiplayer remain future work.

## Open on a new laptop

1. Install **Unity Hub and Unity 6**.
2. Create a **Universal 3D (URP)** Unity project. Do not choose HDRP or Built-In.
3. Download or clone this repository; copy its `Assets/Scripts`, `Assets/Shaders`, and `Assets/Editor` folders into that new URP project's `Assets` directory. This repository contains Unity source files, **not** a complete Unity project including `ProjectSettings`.
4. Wait for C# / URP shader imports to finish. Ensure Unity's **Active Input Handling** is set to **Both** (or legacy Input Manager) because the current runner reads `Input.GetAxisRaw`.
5. Click **River Rush → Create Prototype Scene**.
6. Click **River Rush → Validate Prototype Geometry** and inspect all Console logs. Save the scene.
7. Press **Play**. The countdown starts a race against five offline AI racers. Control with **A/D** or arrow keys; Android touch steering uses left/right halves of the screen. Reaching the ocean shows your place/time; use **New Random Race** to restart.
8. **F3** toggles FPS counter, **F4** opens performance settings on Windows; on mobile tap **Graphics**. The settings also allow 30/60/90/120 FPS or Native refresh mode.
9. For APK, install Android Build Support / SDK / NDK / OpenJDK via Unity Hub, switch active build target to Android, then click **River Rush → Build → Android APK**. For EXE, switch target to Windows x64, then click **River Rush → Build → Windows x64 EXE**. A menu build succeeds only when Unity can compile and required platform modules are installed.

## Documents

- [New laptop setup / APK + EXE build guide](docs/NEW_LAPTOP_SETUP.md)
- [Nature sound and water color system](docs/AUDIO_AND_COLORS.md)
- [Procedural forest, mountain, rocks & tributaries](docs/PROCEDURAL_WORLD.md)
- [Water physics + FPS tiers](docs/WATER_AND_FPS.md)
- [Prototype validation checklist](docs/PROTOTYPE_TEST_CHECKLIST.md)
- [Five-round review log](docs/AUDIT_5_ROUNDS.md)
- [GitHub static source audit](tools/source_audit.py) — runs automatically on commits to `main` via `.github/workflows/source-audit.yml`. No Unity license required.

## Next milestones

1. Compile and play in Unity on the new laptop; fix any imported shader or scene errors.
2. Test physical water/river interactions on Android and Windows, measure 1% low FPS, CPU/GPU cost and thermals.
3. Extend the visible branched drainage network into multiple selectable racing routes (currently the player stays on the main channel).
4. Replace the temporary IMGUI UI, primitive art and synthetic audio with polished licensed nature recordings and production-grade VFX as needed.
5. Improve AI path selection and balancing, then add synchronized multiplayer. Publish APK/EXE only after verified builds.

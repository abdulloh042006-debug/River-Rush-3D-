# River Rush 3D — Water and FPS design (Unity 6 URP)

## Water subsystem (arcade-quality physical approximation)

| System | Code | What it does |
| --- | --- | --- |
| Mountain-to-sea gradient | `ProceduralTerrain` | Seeded terrain, predictable downhill bed |
| River centerline | `RiverRoute` | Arc length, tangent and slope (in metres) |
| Flow | `RiverFlowField` | Gravity/slope current, friction, bank slowdown and lateral eddies |
| Steer and bank collisions | `RiverRunner` | Downstream inertia, lateral acceleration, bank restitution |
| Surface dynamics | `WaterWaveSimulator` | Stable 1-D damped wave propagation with gameplay impulses |
| 3D river appearance | `RiverWaterSurface` | Ribbon mesh updated with physics-driven displacement |
| Flow ripples / foam | `StylizedRiverURP.shader` | Flowing wave lines, pale shoreline and crest foam; mobile-friendly single pass |
| Sea | `StylizedOceanURP.shader` | Opaque low-cost ocean surface; no volumetric ocean waves |
| Floating physical objects | `WaterFloatBody` | Submersion-dependent buoyancy and current drag |
| Merging branches | `RiverGrowth` + `TributaryPickup` | Finite gameplay water-volume pickups and splash impulses |
| Wake | `RiverRunner` + `TrailRenderer` | Runner excites surface waves and leaves foam-like trail |

**Scope disclaimer:** these algorithms approximate an arcade river for actual gameplay. They are **not a CFD solver** for realistic 3D fluids, mass-conserving tributary networking, erosion, wave-breaking or flood simulation. Full fluid simulation is a separate high-cost technology choice and is not advisable for affordable Android hardware.

## FPS settings

Settings visible in the prototype through the top-right **Graphics** button or **F4** key.
Toggle the FPS counter with **F3** (desktop).

Graphics presets:
- **Auto** — uses memory to choose an initial quality tier, samples frame time, lowers quality after repeated slow intervals and cautiously restores if performance improves. Does not promise a fixed FPS.
- **BatterySaver** — 70% URP render scale, 12 m shadows, low wave update/detail.
- **Balanced** — 82% render scale, 30 m shadows, medium wave update/detail.
- **High** — 100% render scale, 55 m shadows, higher water detail.
- **Ultra** — 112% render scale, 85 m shadows, highest water detail.

FPS caps: **Auto, 30, 60, 90, 120, Native**. Native targets the display's detected refresh rate (144/165/240Hz monitors may benefit). The operating system / display and GPU workload determine the FPS actually achieved. On Android, rate caps are rounded to supported integer divisors of detected refresh; e.g., 90 requested on a 120Hz screen can become 60. This is intentional for smoother frame pacing.

On Windows when targeting native refresh, vSync is preferred to limit tearing; at other caps, `targetFrameRate` is used, which may cause uneven frame pacing. Changing presets adjusts URP render scale, shadow distance, LOD bias, wave shader detail and wave mesh update frequency. Values are persisted in PlayerPrefs.

## Real-device sign-off (still required)

1. **Windows**: Test on a low/mid GPU and a strong gaming GPU; check editor and standalone FPS at 1080p and 1440p. Verify Native setting with >120Hz display.
2. **Android**: Test 60Hz, 90Hz and 120Hz screens if available; verify actual rather than requested FPS; ensure foreground/background, portrait/landscape and throttling under 15-minute runs.
3. Check clipping between river mesh and terrain on sharp bends, object flotation and wake impulse; watch for jitter or wave explosion at high `samples`.
4. Profile CPU/GPU using Unity Profiler, Android GPU Inspector/Frame Debugger; log low percentile FPS and thermal changes.
5. Fix all Console and shader compiler errors **before** announcing builds.

## Limits to resolve before release

- Actual Unity Editor compilation, gameplay and FPS tests have not yet happened.
- Procedural forks, path selection, real ocean volume / ocean buoyancy, multiplayer synchronization and AAA visual VFX are unfinished.
- UI still uses prototype IMGUI, replace with responsive uGUI/UI Toolkit for production.
- Potential URP/shader cross-device differences must be checked in engine.

# Five successive review-and-improvement cycles

Date: 2026-10-12. This log records *source code audits* and the changes pushed to GitHub, **not** proof of Unity or device testing.

| Review | Found | Implemented | Source-level recheck |
| --- | --- | --- | --- |
| 1 — Terrain and movement | Route moved by sample index, so game speed depended on spline resolution; route height fallback was 0 without collider. | Arc-length route sampling, world-space tangent and slope; deterministic analytic terrain sampling, monotonic carved bed. | Code references physical metres, cached route distance, seeded height and downhill channel equations. |
| 2 — Gameplay water physics | No current-driven motion, bank drag or forces on floating bodies. | Hydraulic approximation from slope/friction, steering inertia, shoreline drag and boundaries, Rigidbody buoyancy and current force. Later optimized floating-body proximity from scan of all segments to a binary-search neighborhood. | Formula and source-dependency checks; no physics engine simulation yet. |
| 3 — Water appearance | Only a flat animated color surface; no real dynamic displacement or ocean. | Better URP ripple/foam shader, moving river mesh, ocean shader, 1-D damped wave propagation, runner wake and merging splashes; waves now affect player/buoyancy height. | Mathematically replayed an equivalent 12-second damped-wave update: finite and amplitude bounded, but not Unity graphics validation. |
| 4 — FPS and graphics | No hardware adaptation or explicit frame targets. | Auto/BatterySaver/Balanced/High/Ultra, URP render scale, shadows, LOD, water detail/update frequency, FPS overlay and menu, Auto/30/60/90/120/Native refresh, device-aware auto scaling. | Evaluated target-rate/divisor examples; verified interfaces in code, **not** real measured FPS. |
| 5 — Integration and regressions | Water code existed as isolated scripts; no reproducible integrated demo or validation. | Unity menu builds scene with terrain, flowing water, wave simulation, float test, pickup events, ocean, chase camera and quality controller. Added editor validator, detailed test checklist and onboarding. | Fetched 16 key source/shader/editor files; checked bracket/parenthesis balance, key dependencies and five subsystem invariants. |

## Additional regression fixes after the five cycles

- **Core requirement gap corrected:** a hardcoded seed made every new race use the same map. `ProceduralTerrain` now generates a new seed each play; `RiverRaceBootstrap` rebuilds the route and relocates pickups / floating physics objects to match it. Uncheck `Randomize Each Race` in Inspector for deterministic replay.
- **Ocean physics gap corrected:** floating bodies now use a low-cost ocean buoyancy/drift fallback when outside the river, through `OceanWaterPhysics`.
- Editor validation checks the seed bootstrap, wave simulator and ocean physics components.

## Independent formula sanity check

- 128 samples of the analytic centerline bed decreased monotonically toward the ocean in a direct formula check.
- A 256-point damped-wave numerical test over 12 s produced finite values and a maximum absolute height about 0.544 m (configured limit 0.65 m).
- Mobile frame-rate divisor examples: 60Hz/60→60, 90Hz/90→90, 120Hz/90→60, 120Hz/120→120, 144Hz/120→72, 144Hz/Native→144.
- Static scan found no unmatched brackets in the 16 checked source/shader files (~1,600 source lines at the time of that scan). **This does not detect missing Unity API, shader import or runtime faults.**

## Acceptance is still blocked by

1. Unity 6 URP shader import and compiler.
2. Real scene-generation test and 5-group Editor validator execution.
3. APK and EXE builds, installation, and device-level profiling.
4. Water physical realism/branching beyond the current arcade-grade 1-D waves and river corridor.

We should not mark the water system as production-complete until these are cleared. If any Unity error occurs, fix it and repeat all five checks before claiming release readiness.

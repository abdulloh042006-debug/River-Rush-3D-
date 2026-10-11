# River Rush 3D — Seeded Mountains, Forests, Rocks and Tributaries

## What is implemented in code

| Module | Role |
| --- | --- |
| `ProceduralTerrain.cs` | Builds mountain and valley terrain with cached seeded noise and a carved main river + tributaries |
| `RiverBranchLayout.cs` | Pure deterministic layout of 5 tributary centerlines, each heading toward +Z and joining the main river |
| `RiverTributaryNetwork.cs` | Generates 5 water ribbon meshes + 5 confluence pickups and supports floating-body surface/current sampling |
| `ProceduralNature.cs` | Batched low-poly pine trees, broadleaf trees, rocky outcrops, mountain boulders and bushes |
| `TerrainBiomeURP.shader` | Vertex-height/slope shading for beach sand, meadow, forest floor, exposed stone and mountain snow |
| `NatureVertexColorURP.shader` | Single-pass URP low-poly vegetation/rock shader, with seeded vertex colors |
| `RiverRaceBootstrap.cs` | Regenerates the main route after `ProceduralTerrain` gets a new race seed |
| `RiverColorSystem.cs` | Synchronizes the blue/turquoise river colors across the main channel and subsidiary rivers |
| `AdaptiveQualityController.cs` | Suppresses extra forest and shrubs at low quality; adjusts water detail along with FPS settings |
| `WaterFloatBody.cs` | Applies buoyancy/current forces in main river, subsidiary channels and ocean |

## Randomness and replay

- Every new race uses a new seed by default (`ProceduralTerrain.randomizeEachRace`).
- The same seed maps to the same main river, five tributary paths, vegetation positions, rock clusters and base water colors.
- Turn off `Randomize Each Race` on the terrain Inspector for deterministic reproduction while testing.
- River and foliage share a common seed but use separate salted `System.Random` streams, so varying foliage budgets does not modify the river.
- Tributaries have spaced downstream junctions, alternating entry sides, winding headwater courses and smoothly merging end points.

## Environment distributions

- **Alpine headwaters:** more conifer trees, exposed gray rocks and snow-colored high peaks.
- **Sheltered mid-valleys:** trees of variable heights, green forest flooring and scattered boulders.
- **Downstream plains:** broadleaf trees, shrubs and riverbank meadows.
- **Coast and mouth:** sandy low terrain; natural blue sea.
- Flora placement avoids the playable main river and generated tributary channel corridors; no artificial trunk blocks the route.

All plants and stones are simple **stylized low-poly geometry**. They are visual and do not have colliders in this prototype. Individual trees, rocks and bushes are accumulated into four `Mesh` batches. No separate tree GameObjects and no third-party model packages are required.

### Default maximum populations

- Up to **270 trees**, split into 2 mesh tiers
- Up to **180 rocks** (octahedron-based low-poly stones)
- Up to **240 bushes**
- Real generated counts may be lower due to slope, waterway buffers, biome suitability and bounded candidate attempts.

### Quality and FPS

The Auto/BatterySaver/Balanced/High/Ultra controller adjusts visual detail:

| Approximate internal detail | Nature elements |
| --- | --- |
| Low (0.12) | Essential forest and rock meshes only |
| Medium (0.40) | Essential forest, detail forest and rock meshes |
| High/Ultra (0.72/1.0) | Essential + detail forests, rocks and bushes |

Keeping 4 batched renderers is far less expensive than hundreds of independent renderers, but mesh detail remains loaded in memory at low quality. The main and branch river shaders are still transparent, so GPU overdraw should be profiled on actual devices.

## Tributary behavior

Five subsidiary rivers are generated *upstream to downstream*. Their centerlines run monotonically toward +Z; the visual meshes are aligned with the same carved positions on the terrain. The pickup spawned at each joining point can be collected to increase the water volume.

Physical Rigidbody objects placed on tributaries can experience downward flow and buoyancy using `RiverTributaryNetwork.TrySampleSurface` and `WaterFloatBody`.

**Important limit:** The player's steering remains on the main river path. Branches are not yet separate routes for choosing a race shortcut. Tributary pickups represent arcade volume gain, not mass-conserving 3D fluid merging. There are no real waterfalls, erosion, dynamic forest swaying, climate/weather cycles or actual volumetric liquid physics yet.

## New laptop validation — still pending

1. Open in **Unity 6 Universal 3D (URP)**; click **River Rush > Create Prototype Scene**.
2. Confirm mountain-to-sea terrain, five tributary water meshes, rocks, trees and bushes are visible.
3. Use **River Rush > Validate Prototype Geometry**: confirm branch centerlines are strictly +Z, meet the main river and trend downhill; verify nature mesh and pickup counts.
4. Compare two runs with the *same* seed (should be identical) and two different seeds (terrain/vegetation should differ).
5. Verify no river water strips clip through carved terrain or float above banks on tight curves.
6. Ensure physics objects float on tributaries and the main flow; collecting all five pickups increases player water volume.
7. Check terrain shader colors: green vegetation, rocky exposed slopes, sandy coast, alpine snow.
8. Compare FPS, GPU overdraw, CPU generation time and memory on low/mid/high-end Android and Windows systems.
9. Test a long session and at least 20 different map seeds; capture any protruding geometry or broken junction.

No Unity Editor compilation, real-device build, visual inspection or measured FPS has yet been completed.

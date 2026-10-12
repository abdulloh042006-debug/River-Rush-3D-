# River Rush 3D: Ryzen 7 + 8 GB RAM setup

Use this guide if the laptop has **Ryzen 7, 8 GB RAM, 256 GB SSD and 1 TB HDD**. The screenshot doesn't identify the **CPU model or GPU**, so performance cannot be guaranteed from the name "Ryzen 7" alone.

## Practical compatibility

- **Unity 6 URP:** reasonable to attempt an Editor prototype on 8 GB RAM, but expect project import, scene generation, shader compile and multitasking to consume most free memory.
- **GPU matters:** an integrated Vega GPU and dedicated NVIDIA/AMD GPU can provide very different editor FPS with the same Ryzen 7 badge. Check Task Manager > Performance > GPU and CPU for the **exact** models.
- **SSD:** install Unity, IDE and active Unity project on the 256 GB SSD. Keep at least 35–50 GB free if possible because `Library/`, caches, SDK and shader compilation expand. Move archives, ZIPs, old projects and builds to the 1 TB HDD.
- **RAM:** 16 GB is a recommended upgrade when possible, especially with Unity + browser + IDE. It is not required to start.
- **Settings:** this project's **Auto** graphics preset now starts 8 GB Windows laptops on **Balanced**. Try 720p–900p Game View / low Editor resolution, cap 30 or 60 FPS, and disable nonessential post-processing/shadows for initial tests.
- **Do not infer GPU performance from CPU class:** confirm the exact graphics device before predicting realistic resolution or FPS.

## Sequence for the first day

1. Install Unity 6 Universal 3D (URP) and only the platform modules you currently need. Install Android build tools after confirming the Windows prototype compiles.
2. Open the source folders in a new URP project and wait until imports finish.
3. Run `River Rush > Create Prototype Scene`, then `River Rush > Validate Prototype Geometry`.
4. Play the prototype and test movement, branching river, procedural forest, transparent water, AI bots, audio and finish.
5. Start with **Balanced**, optionally test **BatterySaver**, and press **F5** or tap **Benchmark** to collect the local 60-second FPS report.
6. After 10–15 minutes, note lag, fans, temperature, memory usage and average / 1%-low FPS. Increase quality only if stable.
7. If the Editor freezes or RAM paging becomes significant, lower terrain resolution, restart Unity, close browser tabs and background apps, and avoid rendering high-quality Game View simultaneously with Scene View.

## Engine constraints

- This repository is **source files**, not yet a fully initialized Unity project.
- Script and shader compilation have not been verified in a local Unity installation.
- Benchmark code is ready, but **no hardware benchmark has yet been performed**.
- Editor scene generation now removes previously serialized procedural forest/branch children before rebuilding. Tributary carving uses spatial bounding-box culling to reduce unnecessary calculations.

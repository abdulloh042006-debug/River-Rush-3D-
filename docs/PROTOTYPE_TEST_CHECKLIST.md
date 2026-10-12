# River Rush — verification checklist

**Verification levels**
- **Source-level**: file structure, dependencies and formulas inspected. Does not prove compilation.
- **Unity Editor**: needs the new laptop, Unity 6 URP, and the menu command.
- **Device**: needs real Windows PC / Android phone.

## Unity Editor — run from River Rush menu
- [ ] Unity 6 Universal 3D project imports every C# script, shader and inspector component with **zero Console errors**.
- [ ] `River Rush → Create Prototype Scene` saves `Assets/Scenes/RiverRushPrototype.unity`.
- [ ] `River Rush → Validate Prototype Geometry` logs a PASS for its five integrated checks.
- [ ] Regenerate the same seed twice: terrain, river route and sea outlet match.
- [ ] Check downhill route, absence of mesh holes and river water clipping on turns.
- [ ] Verify shader: transparent blue water, flowing white ripples and foam at the bank; ocean opaque.
- [ ] Running player makes visible water disturbances, which propagate and decay without exploding.
- [ ] Pickups trigger exactly once, grow water volume/visual width and add a splash.
- [ ] Floating cube gets physically lifted and pulled downstream.
- [ ] Running/steering/bank contact and camera follow work.
- [ ] Five AI racers spawn, are colored differently, move only after the countdown and steer independently.
- [ ] Player's camera follows the human rather than a randomly selected AI bot.
- [ ] During a race, all racers appear in a correctly ordered leaderboard with distances/progress.
- [ ] Player reaches the ocean and race manager freezes final placement/time.
- [ ] "New Random Race" reloads the scene with a new seed (and different river geometry).
- [ ] "Retry Same Map" reloads the exact previous seed, reproducing branch paths, trees, rocks and water color palette.
- [ ] With Inspector `Randomize Each Race` disabled, pressing New Random Race still changes the seed.
- [ ] HUD displays current seed, useful for bug reports and map replay.
- [ ] Saved scene is included in Build Settings automatically for Windows/Android replay.

## GitHub source-only validation
- [ ] GitHub Actions > Source and Procedural World Tests (No Unity License) completes successfully on `main`.
- [ ] Locally, `python tools/source_audit.py` prints PASS.
- [ ] `dotnet run --project tools/branch-smoke/BranchSmoke.csproj --configuration Release` tests the same C# geometry used by Unity over 2709 configurations.
- [ ] Remember: the .NET step compiles only Unity-independent branch math, **not** the entire Unity scene or shaders.

## Procedural world, forests and tributaries
- [ ] Generator creates 5 water-filled subsidiary channels carved into the terrain.
- [ ] Every branch flows downstream toward +Z and meets the main river centerline.
- [ ] The river mesh never clips into terrain on bends, narrow channels and water junctions.
- [ ] Each of the 5 junctions creates exactly one pickup; pickups are collected once.
- [ ] Picking up tributaries increases water volume, wave impulse and splash audio.
- [ ] Floating rigidbodies respond to water currents on both tributaries and main river.
- [ ] Terrain altitude/slope shows grass, forest floor, exposed rock, snow and coastal sand.
- [ ] Different seeds generate different tree clusters and varied Alpine/valley geography.
- [ ] Same seed yields identical terrain, branch paths, rock and tree placements.
- [ ] Trees/rocks/shrubs never block the main race corridor or subsidiary channel geometry.
- [ ] Forest, detailed forest, boulders and shrubs are combined into <=4 mesh groups.
- [ ] BatterySaver/Auto lower forest density **visually** by disabling extra mesh layers.
- [ ] Test at least 20 seeds in Unity for water clipping, smooth joins and scenery distribution.
- [ ] Profile CPU time of level generation and GPU triangle/draw-call budget on Android.

## Natural audio and water colors
- [ ] One human + five AI bots: racers have distinct blue/turquoise/cyan semi-transparent colors.
- [ ] Repeating a seed reproduces the same water-color palette; a new seed varies hues naturally.
- [ ] Inspector crowd preview >=8 human players enables randomized rainbow racer colors.
- [ ] Toggle crowd preview off: normal blue palettes return; environment stays naturally blue throughout.
- [ ] Confirm transparent droplet shader compiles in Unity 6 URP on Android and Windows.
- [ ] Listen to separate water, wind, leaves/birds loops and check for clipping/seam clicks.
- [ ] River rush grows louder with speed; forest ambience fades toward the sea.
- [ ] Collect tributary: audible one-shot splash. Mute/volume controls and PlayerPrefs persistence work.
- [ ] Test optional licensed recordings as overrides, without destroying imported audio clips.
- [ ] Measure FPS and draw calls with 8, 32 and 100 racers. No benchmark claim until device runs.

## Performance / Android
- [ ] Play on at least one weak and one strong phone; note Android versions and refresh rate.
- [ ] Select 30 / 60 / 90 / 120 / Native and compare *actual* FPS counter against requested cap.
- [ ] For 120Hz display, 90 FPS may map to 60 because Android frame pacing chooses a divisor.
- [ ] Test Auto for 15 minutes, including thermal throttling and graphics-level downshift.
- [ ] Verify quality tiers change render resolution / water detail / shadows without errors.
- [ ] Check GPU overdraw from transparent river and CPU time spent updating wave mesh.
- [ ] Confirm APK touch steering, aspect ratios and resuming after app backgrounding.
- [ ] Record average and 1%-low FPS, temperature trends and memory usage.
- [ ] Use the Benchmark button (or F5 on Windows): after 60 s, confirm average FPS, 1% low, worst frame time and Unity memory appear.
- [ ] Verify the CSV report is saved **locally** and includes seed, platform, resolution, graphics level and FPS cap.
- [ ] Pause/background the app during benchmark: status must be INVALID_PAUSED_OR_STALLED rather than claiming a valid result.
- [ ] Benchmark low/mid/high graphic settings under comparable camera/route conditions; real GPU/thermal profiles still required.

## Windows
- [ ] Test standalone x64 EXE at 1080p and 1440p, on both integrated and dedicated GPU.
- [ ] Test Auto/Native on a 144Hz-or-faster screen where available.
- [ ] A/D and arrow keys steer; F3 toggles counter and F4 toggles graphics UI.
- [ ] Check vsync / frame pacing and window resize.

## Not yet done / not included
- [ ] Unity Editor test has been run
- [ ] Windows EXE has been compiled
- [ ] Android APK has been compiled
- [ ] Proper 3D fluid simulation / branching water graph
- [ ] Advanced/competitive AI and online multiplayer (simple offline AI bots are source-coded)
- [ ] Production-quality art and responsive UI

## Bug report
Unity version; OS/hardware; target FPS versus measured FPS; Console error; reproduction steps; screenshot/video.

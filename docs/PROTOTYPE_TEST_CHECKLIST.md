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
- [ ] Saved scene is included in Build Settings automatically for Windows/Android replay.

## GitHub source-only validation
- [ ] GitHub Actions > Source Audit (No Unity License) completes successfully on `main`.
- [ ] Locally, `python tools/source_audit.py` prints PASS.
- [ ] Remember: this is not a C# compiler or Unity shader validation.

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

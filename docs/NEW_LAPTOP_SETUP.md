# River Rush 3D — New Laptop Quick Start (Unity 6)

The old laptop is not required for any work in this repository. The GitHub code and
documentation have been created remotely. **There is not yet a confirmed working
APK or EXE, and there is not yet a fully initialized Unity project.**

## 1. Install and open

1. Install Unity Hub and log in with a Unity account.
2. Install a stable **Unity 6** editor with **Android Build Support**, **Android SDK & NDK Tools**, **OpenJDK**, and **Windows Build Support** as applicable.
3. Create a new project from **Universal 3D (URP)**. Name it RiverRush3D.
4. Download this repository as ZIP or clone it with Git.
5. Copy the repository's **Assets/Scripts**, **Assets/Shaders**, and **Assets/Editor**
   directories into your new Unity project's `Assets/` directory.
6. Wait for Unity to import C# files and URP shaders. Check the Console for errors.
7. In Project Settings > Player, set **Active Input Handling** to **Both**
   or **Input Manager (Old)** for current keyboard/touch prototype.
8. Set default Android orientation to **Landscape** in Player settings before making APK.

## 2. Create the game level

1. From Unity menu choose **River Rush > Create Prototype Scene**.
2. The command creates height-colored mountains/forests/snowcaps/sandy coast,
   the random-seed main river plus five carved tributaries, four batched low-poly
   forest/rock/shrub layers, animated water surfaces, ocean, player, five AI racers,
   five confluence pickups, wave simulation and a floating crate. It also adds
   nature audio, transparent blue racers, the FPS menu and race leaderboard.
3. Run **River Rush > Validate Prototype Geometry**. It now also checks tributary
   geometry, confluence pickups, biome shader and generated forests. Fix any reported failures.
4. Press **Play**. Wait for the 3-second countdown, then steer using A/D or arrow
   keys. Test camera, waves, floating crate, pickups, bots, rank and ocean finish.
5. Click **NEW RANDOM RACE** to reload with a fresh procedural seed.
6. Test 30/60/90/120/Native frame caps and the graphics quality settings.

## 3. Build Windows EXE

1. Select Windows x64 as active target in **File > Build Profiles / Build Settings**.
2. Choose **River Rush > Build > Windows x64 EXE**.
3. After a successful build, output is `Builds/Windows/RiverRush3D.exe`
   inside the Unity project. Test on the target Windows machine.

## 4. Build Android APK

1. Switch active target to Android (requires Android Build Support and tools).
2. Set Player Settings as needed for your device, including orientation and package name.
3. Choose **River Rush > Build > Android APK**.
4. After a successful build, output is `Builds/Android/RiverRush3D.apk`.
5. Install APK on a device and test **actual FPS, touch input, graphics, startup and thermals**.
   An APK is not guaranteed to run well solely because it builds.

## 5. GitHub validation available without Unity

On GitHub select **Actions > Source and Procedural World Tests (No Unity License)**.
The workflow also starts on pushes to `main` and pull requests. It runs
`python tools/source_audit.py` plus a real .NET 8 compile/run of the same
`RiverBranchMath.cs` that Unity uses. The C# smoke test covers **2709 seeded world
configurations / 13545 tributaries / 243810 points** for repeatability, directional
monotonicity and precise main-river confluence. This **does not** compile the full
Unity project or validate rendered meshes and shaders.

## Outstanding launch blockers

- Unity Editor compile / shader import test.
- Fix any compiler errors (record exact messages).
- Device FPS/performance and 15-minute temperature tests.
- Art/audio/menus, deeper AI balancing and user experience polish.
- Branching river-network generation, real-time networking and monetization are not built.

## Important

Always commit Unity project code changes to Git. Do not commit `Library/`,
`Temp/`, `Builds/`, personal tokens or signing keystores.

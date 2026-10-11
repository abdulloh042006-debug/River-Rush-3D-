# Prototype validation checklist (Unity 6, URP)

The checklist is **not evidence of a passing build** until run inside Unity on the new laptop.

## Import and rendering
- [ ] Create a Unity 6 Universal 3D project; copy `Assets/` folders into it.
- [ ] All C# scripts compile without Console errors.
- [ ] Custom URP shader imports without pink materials or shader errors.
- [ ] River Rush → Create Prototype Scene generates and saves a scene.
- [ ] Press Play: terrain, water ribbon, player sphere and follow camera all appear.
- [ ] Water ripples move and pale foam appears near the river banks.
- [ ] Same seed creates visually identical terrain after restarting the scene.
- [ ] All water surface points stay above their terrain channel (no excessive clipping).
- [ ] Centerline height never increases toward the sea.

## Gameplay
- [ ] Arrow keys / A-D steer on Windows.
- [ ] Touch steering works on Android.
- [ ] Player remains in the river's lateral steering corridor.
- [ ] Pickups are collected once and visibly increase the player's width.
- [ ] Camera follows without jitter.
- [ ] No repeated exceptions in Console.

## Builds
- [ ] Windows x64 build launches and gameplay matches Editor.
- [ ] Android APK installs on a real device and input works.
- [ ] Measure FPS, memory and overheating on Android midrange hardware.
- [ ] Inspect performance of the transparent river shader.
- [ ] Validate screens at 16:9, 19.5:9 and a tablet aspect ratio.

## Known gaps, not yet passing features
- [ ] Real flowing-water physics
- [ ] Multiple forks and joining channels
- [ ] Ocean mesh, finish trigger, and victory screen
- [ ] AI opponents
- [ ] Multiplayer
- [ ] Final art, audio and UI

## Bug report format
Unity version; device/OS; Console error text; reproduction steps; screenshot/video; expected and actual results.

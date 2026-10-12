# River Rush 3D — 32 GB Laptop Onboarding

**32 GB RAM is plenty for starting a Unity 6 URP project**, but it does not establish the GPU performance. A laptop with 32 GB RAM and integrated graphics may render much more slowly than a laptop with 16 GB RAM and an RTX GPU.

## Before installing Unity

In Windows, open **Task Manager → Performance** and inspect:
- **CPU:** the exact processor name (e.g. Ryzen 7 7840HS or Intel Core i7-13620H).
- **GPU 0 / GPU 1:** every listed GPU name (integrated + dedicated, if present).
- **Memory:** 32 GB capacity, current usage, free/available RAM.
- **Disk:** whether the main drive is an SSD and has around 50 GB spare for Unity, the Android SDK and the project.

If comfortable running a read-only local script, clone/download this repository and run in a Windows PowerShell window opened at its root:

```powershell
.\tools\check-laptop.ps1
```

The command **only reads** CPU, RAM, GPU, Windows and drive free-space information; it does not install anything or upload data. Use `-SaveReport` to write `RiverRush_Laptop_Report.json` locally if you want to review or share the hardware summary. Review any report yourself before sharing.

## Our GPU-aware graphics choices

| Machine | Automatic starting level | Reason |
| --- | --- | --- |
| 8 GB RAM, any GPU | **Balanced** | Save memory while importing assets |
| 32 GB RAM with Iris Xe / Vega / Radeon 780M integrated GPU | **Balanced** | Shared RAM does not substitute for a dedicated graphics chip |
| 32 GB RAM with a 4 GB discrete GPU | **High** | Test and downshift if 1% low FPS is poor |
| 32 GB RAM with RTX / RX dedicated GPU and >=7 GB reported graphics memory | **Ultra candidate** | Confirm real FPS and thermals before committing |
| 32 GB RAM, GPU unknown | **Balanced** | Prefer a reliable initial launch to overestimating the hardware |

In Unity choose **River Rush → Diagnostics → Check Laptop and URP** after importing scripts. The Console report identifies the hardware, active URP pipeline and five project shaders. This is a readiness check, **not a successful compile guarantee**.

## Recommended first test

1. Install Unity Hub and **Unity 6 Universal 3D / URP** plus Windows modules. Android can be added later through Hub.
2. Make a new Universal 3D project and copy this repository's `Assets/Scripts`, `Assets/Shaders` and `Assets/Editor` folders into it (the repo currently contains source, not a complete Unity project).
3. After script compilation, run **River Rush → Diagnostics → Check Laptop and URP**, then **River Rush → Create Prototype Scene**.
4. Run **River Rush → Validate Prototype Geometry**; do not skip Console errors.
5. Start with **Balanced / 60 FPS**, even on 32 GB, until the scene is visually correct. If the actual GPU is clearly dedicated and powerful, compare High and Ultra.
6. Press **F5** or click **Benchmark**; run the built-in 60-second local FPS capture under comparable conditions.
7. Test a different seed, **Retry Same Map**, forest/river regeneration and Windows EXE. Install Android Build Support and try an APK once Windows is stable.

## Metrics to collect on laptop

Measure actual mean and 1%-low FPS, frame-time spikes, Unity CPU/GPU profiler data, total RAM used, and whether the laptop throttles after 10–15 minutes. High preset with stable 60 FPS is preferable to Ultra with severe frame drops.

**No Unity Editor build, APK/EXE compilation or hardware performance result has been verified in this repository yet.** The no-Unity GitHub workflow compiles only the deterministic river and quality-policy C# modules.

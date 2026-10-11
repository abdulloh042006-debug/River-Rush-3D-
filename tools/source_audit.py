#!/usr/bin/env python3
"""License-free static sanity checks for River Rush Unity source.

This DOES NOT compile C# or prove that Unity shaders work. Run Unity's own
validator and real platform builds when Unity 6 is available.
"""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "terrain": "Assets/Scripts/World/ProceduralTerrain.cs",
    "route": "Assets/Scripts/World/RiverRoute.cs",
    "bootstrap": "Assets/Scripts/World/RiverRaceBootstrap.cs",
    "flow": "Assets/Scripts/World/RiverFlowField.cs",
    "waves": "Assets/Scripts/World/WaterWaveSimulator.cs",
    "surface": "Assets/Scripts/World/RiverWaterSurface.cs",
    "ocean": "Assets/Scripts/World/OceanWaterPhysics.cs",
    "runner": "Assets/Scripts/Gameplay/RiverRunner.cs",
    "growth": "Assets/Scripts/Gameplay/RiverGrowth.cs",
    "pickups": "Assets/Scripts/Gameplay/TributaryPickup.cs",
    "floating": "Assets/Scripts/Gameplay/WaterFloatBody.cs",
    "bot": "Assets/Scripts/Gameplay/RiverBotAI.cs",
    "race": "Assets/Scripts/Gameplay/RiverRaceManager.cs",
    "camera": "Assets/Scripts/Gameplay/RiverFollowCamera.cs",
    "graphics": "Assets/Scripts/Graphics/AdaptiveQualityController.cs",
    "builder": "Assets/Editor/RiverRushSceneBuilder.cs",
    "validator": "Assets/Editor/RiverRushEditorValidator.cs",
    "river_shader": "Assets/Shaders/StylizedRiverURP.shader",
    "ocean_shader": "Assets/Shaders/StylizedOceanURP.shader",
}


def check_delimiters(source: str) -> list[str]:
    pairs = {")": "(", "]": "[", "}": "{"}
    stack: list[tuple[str, int]] = []
    errors: list[str] = []
    state = "code"
    i = 0
    while i < len(source):
        c = source[i]
        nxt = source[i + 1] if i + 1 < len(source) else ""
        if state == "line":
            if c == "\n":
                state = "code"
        elif state == "block":
            if c == "*" and nxt == "/":
                state = "code"
                i += 1
        elif state in {"string", "char"}:
            if c == "\\":
                i += 1
            elif c == ('"' if state == "string" else "'"):
                state = "code"
        elif c == "/" and nxt == "/":
            state = "line"
            i += 1
        elif c == "/" and nxt == "*":
            state = "block"
            i += 1
        elif c == '"':
            state = "string"
        elif c == "'":
            state = "char"
        elif c in "([{":
            stack.append((c, i))
        elif c in ")]}":
            if not stack or stack[-1][0] != pairs[c]:
                errors.append(f"Unexpected {c!r} at offset {i}")
            else:
                stack.pop()
        i += 1
    if stack:
        errors.append(f"Unclosed brackets: {len(stack)}")
    if state not in {"code", "line"}:
        errors.append(f"Unterminated {state}")
    return errors


def main() -> int:
    errors: list[str] = []
    code: dict[str, str] = {}
    line_total = 0
    for key, path in FILES.items():
        file_path = ROOT / path
        if not file_path.is_file():
            errors.append(f"Missing {path}")
            continue
        body = file_path.read_text(encoding="utf-8")
        code[key] = body
        line_total += len(body.splitlines())
        errors.extend(f"{path}: {err}" for err in check_delimiters(body))

    checks = {
        "random-seed terrain": ("terrain", "randomizeEachRace"),
        "race bootstrap": ("bootstrap", "route.Rebuild()"),
        "distance-based route": ("route", "TotalLength"),
        "hydraulic velocity": ("flow", "TrySample("),
        "wave propagation": ("waves", "FixedUpdate()"),
        "visible wave displacement": ("surface", "waves.GetHeight(meters)"),
        "river and ocean buoyancy": ("floating", "ocean.TrySample("),
        "human and bot control": ("runner", "SetBotSteer("),
        "AI steering": ("bot", "DecideLane()"),
        "leaderboard": ("race", "SortStandings()"),
        "chase camera": ("camera", "IsHumanControlled"),
        "adaptive FPS": ("graphics", "TargetFPS()"),
        "high refresh Native mode": ("graphics", "FrameCap.Native"),
        "scene includes bot AI": ("builder", "AddComponent<RiverBotAI>()"),
        "scene includes race manager": ("builder", "AddComponent<RiverRaceManager>()"),
        "scene added to build": ("builder", "EditorBuildSettings.scenes"),
        "Unity editor validator": ("validator", "Validate()"),
        "foam shader": ("river_shader", "_FoamWidth"),
        "ocean shader": ("ocean_shader", "_OceanColor"),
    }
    for label, (file_key, marker) in checks.items():
        if marker not in code.get(file_key, ""):
            errors.append(f"Broken source dependency: {label} ({marker})")

    print(f"Checked {len(code)}/{len(FILES)} Unity files, {line_total} lines")
    for error in errors:
        print(f"FAIL: {error}")
    if errors:
        return 1
    print(f"PASS: static structure and {len(checks)} source dependency checks")
    print("NOTE: Unity compilation and actual device FPS are NOT verified by this check.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

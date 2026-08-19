# VOID HUNTER

A twin-stick alien wave shooter for Windows. Hold the rift. Burn the swarm.

## Play

Double-click `Play.bat`, or run:

`publish\win-x64\VoidHunter.exe`

High scores are saved in `%APPDATA%\VoidHunter\save.json`.

There are two worlds, **10 levels each**.

Choose your campaign from the main menu:

1. **Rift Campaign** — top-down twin-stick, 10 levels.
2. **Abyss World** — optional 3D arena, 10 new alien types and Hydra bosses.

After Rift 10 you can **Enter the Abyss** or **Claim Victory** and stop. Beat Abyss 10 for the full ending.

## Controls

| Action | Input |
|---|---|
| Move | WASD or arrow keys |
| Aim | Mouse |
| Fire | Left mouse or Space |
| Dash | Right mouse or Shift |
| Switch weapons | 1–4 or mouse wheel |
| Pause | Esc or P |
| Fullscreen | F11 |
| Mute | M |

## Arsenal

- **Pulse** — fast cyan bolts. Extra barrel at higher levels.
- **Spread** — wide violet fan.
- **Rail** — piercing gold lance.
- **Nova** — slow orbs that detonate.

Weapon crates upgrade the equipped gun, then unlock the next. Hull orbs repair. Hex tokens raise a shield. Gold cores start Overdrive.

Every fifth Rift level a Leviathan arrives. In the Abyss, Hydras take that slot. Pause with Esc for Resume, Restart Level, Main Menu, or Quit Game.

## Build

Requires the .NET 10 SDK.

```
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -o publish\win-x64
```

`--smoke` runs a short autoplay session and writes a screenshot, then exits.

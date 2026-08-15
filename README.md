# VOID HUNTER

A twin-stick alien wave shooter for Windows. Hold the rift. Burn the swarm.

## Play

Double-click `Play.bat`, or run:

`publish\win-x64\VoidHunter.exe`

High scores are saved in `%APPDATA%\VoidHunter\save.json`.

There are **10 levels**. Clear a level to see your result, then choose **Next Level**. Beat level 10 to win. If your hull reaches zero you lose and can retry that level or quit. Quit is available from the main menu, pause menu, and result screens.

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

Every fifth level a Leviathan arrives. Pause with Esc for Resume, Restart Level, Main Menu, or Quit Game.

## Build

Requires the .NET 10 SDK.

```
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true -o publish\win-x64
```

`--smoke` runs a short autoplay session and writes a screenshot, then exits.

# Bloodborne PC

Bloodborne on Windows in one package: launcher and engine. You only need your own game dump.

**No game files are included.** Bring a decrypted Bloodborne dump (CUSA03173 or another
region) with update 1.09.

## Install

1. Download `BloodbornePC-<version>-win64.zip` from
   [Releases](https://github.com/Awakenchan/BloodBorneButPc/releases) and extract it anywhere.
2. Run `Bloodborne Launcher.exe` and choose your game folder (the one with `eboot.bin`).
3. Press **Play**.

The launcher updates itself from the Releases page.

## Features

- **Play** – game folder check (eboot.bin, version 1.09), saves folder, one-click start.
- **Display** – in-game menu language (English / Russian), game language, resolution,
  fullscreen, widescreen (21:9, 32:9), 30 / 60 / 90 / 120 / 144 FPS or uncapped.
- **Graphics** – FSR 4, FSR 3, DLSS, TAA, sharpening, picture and camera tuning, game effects.
- **Controls** – keyboard and mouse for all 29 actions (one key and one mouse input each),
  mouse camera, mouse pointer in the game's menus, sensitivity and invert.
  - **Profiles**: save layouts, copy them as JSON to any paste site, import from a link or file.
- **Trophies** – all 40, completion rate, in-game unlock notification with sound.
- **Mods & patches**, **Online** (shadNet), **Game output** log.

### Control profile format

```json
{
  "format": "bbport-controls",
  "version": 2,
  "name": "Mouse focus",
  "author": "you",
  "description": "Attacks on the mouse",
  "bindings": { "attack": "Mouse1", "strong_attack": "Mouse2", "lock_on": "Mouse3", "roll": "Space" }
}
```

Actions: `move_forward move_back move_left move_right walk roll l3 attack strong_attack
strong_modifier transform firearm lock_on blood_vial use_item interact switch_item switch_right
switch_left dpad_up gestures personal_effects menu confirm back look_up look_down look_left
look_right`. Keys use SDL names (`Space`, `Left Shift`, `Return`, `F`); mouse inputs are
`Mouse1`–`Mouse5`, `WheelUp`, `WheelDown`. Each action takes one key and one mouse input.

## Building the launcher

.NET 8 SDK on Windows:

```
dotnet build launcher/windows/BloodborneLauncher.sln -c Release
```

`launcher/windows/LauncherBuilder` builds `Build Launcher.exe`, which rebuilds the launcher when
its sources change (`--force` always). Put a folder path in `launcher/windows/install.txt` to
install into a package folder instead of `LauncherApp/`.

## Engine

The engine (`out/`, `scripts/`) is [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc)
at tag `v0.4.0-darkizuku-preview.1`, which builds on
[deadinside28/bloodborne_pc](https://github.com/deadinside28/bloodborne_pc) (bbport) and
[shadPS4](https://github.com/shadps4-emu/shadPS4). Its source and licenses are there and in the
package's `licenses/` folder.

Trophy list from [pyL1nx/bloodborne_pc](https://github.com/pyL1nx/bloodborne_pc).
UI: [WPF-UI](https://github.com/lepoco/wpfui).

## License

The launcher is GPL-2.0-or-later (see [LICENSE](LICENSE)). The engine is GPL-3.0-or-later.

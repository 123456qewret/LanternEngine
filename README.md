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
- **Display** – in-game menu language (English, Spanish, Portuguese, French, German, Italian,
  Polish, Turkish, Russian), game language, resolution,
  fullscreen, widescreen (21:9, 32:9), 30 / 60 / 90 / 120 / 144 FPS or uncapped.
- **Graphics** – FSR 4, FSR 3, DLSS, TAA, sharpening, picture and camera tuning, game effects.
  Experimental: VRAM cleanup, texture memory sized to the GPU, precise frame pacing, render thread priority,
  background shader compiling (all on, each can be turned off).
- **Controls** – keyboard and mouse for all 29 actions (one key and one mouse input each), controller
  remapping (click an action, press the button on the pad), swap sticks, invert camera, stick dead zone,
  live controller detection (DualShock 4, DualSense, Xbox, Switch Pro, generic pads),
  mouse camera, mouse pointer in the game's menus, sensitivity and invert.
  - **Profiles**: EldenRingStyle (the default), PC default and Mouse focus built in; save your own, copy them as JSON to any paste site, import from a link or file.
- **Trophies** – all 40, completion rate, in-game unlock notification with sound.
- **Mods & patches**, **Online** (shadNet), **Game output** log (also saved to `logs\last_session.log`).
- **Layout** – Modern (tabs, glass) or Classic (sidebar).

### Control profile format

```json
{
  "format": "bbport-controls",
  "version": 2,
  "name": "Mouse focus",
  "author": "you",
  "description": "Attacks on the mouse",
  "bindings": { "attack": "Mouse1", "strong_attack": "Mouse3", "lock_on": "Mouse2", "roll": "Space" }
}
```

Actions: `move_forward move_back move_left move_right walk roll l3 attack strong_attack
strong_modifier transform firearm lock_on blood_vial use_item interact switch_item switch_right
switch_left dpad_up gestures personal_effects menu confirm back look_up look_down look_left
look_right`. Keys use SDL names (`Space`, `Left Shift`, `Return`, `F`); mouse inputs are
`Mouse1` (left), `Mouse2` (middle), `Mouse3` (right), `Mouse4`, `Mouse5`, `WheelUp`, `WheelDown`. Each action takes one key and one mouse input.

## Building the launcher

.NET 8 SDK on Windows:

```
dotnet build launcher/windows/BloodborneLauncher.sln -c Release
```

`launcher/windows/LauncherBuilder` builds `Build Launcher.exe`, which rebuilds the launcher when
its sources change (`--force` always). Put a folder path in `launcher/windows/install.txt` to
install into a package folder instead of `LauncherApp/`.

## Engine

Our Windows build of the engine, with our own changes: performance and stutter work, VRAM cleanup,
uncapped FPS, controller support, more menu languages and crash fixes. The patch, the full list and build
steps are in [engine/](engine/).

## Credits

| | |
|---|---|
| Engine base | [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc) (built on [bbport](https://github.com/deadinside28/bloodborne_pc) and [shadPS4](https://github.com/shadps4-emu/shadPS4)) |
| VRAM cleanup | adapted from [IFreemz/shadPS4-Bloodborne-DLSS-FSR](https://github.com/IFreemz/shadPS4-Bloodborne-DLSS-FSR) |
| Crash and controller fixes | adapted from [AwesomeObserver](https://github.com/AwesomeObserver/bloodborne_pc), [Supermedo](https://github.com/Supermedo/bloodborne_pc) and [deadinside28](https://github.com/deadinside28/bloodborne_pc) |
| Controller mappings | [SDL_GameControllerDB](https://github.com/mdqinc/SDL_GameControllerDB) |
| Trophy list | [pyL1nx/bloodborne_pc](https://github.com/pyL1nx/bloodborne_pc) |
| UI library | [WPF-UI](https://github.com/lepoco/wpfui) |

## License

The launcher is GPL-2.0-or-later (see [LICENSE](LICENSE)). The engine is GPL-3.0-or-later; its licenses are in the package's `licenses/` folder.

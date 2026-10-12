# Bloodborne PC

Bloodborne on Windows in one package: our launcher and our own engine. You only need your own game dump.

**Community:** [join the Discord](https://discord.gg/d3Uv3Cfr5x) for help, news, co-op partners and beta feedback.

**No game files are included.** Bring a decrypted Bloodborne dump (CUSA03173 or another
region) with update 1.09.

## Install

1. Download `BloodbornePC-<version>-win64.zip` from
   [Releases](https://github.com/Awakenchan/BloodBorneButPc/releases) and extract it anywhere.
2. Run `Bloodborne Launcher.exe` and choose your game folder (the one with `eboot.bin`).
3. Press **Play**.

The launcher updates itself from the Releases page.

## Features

- **Play**: game folder check, player profiles (each with its own saves and trophies), automatic save backups with restore, session summary.
- **Display**: resolution, fullscreen, widescreen (21:9, 32:9), native HDR, 30 to 144 FPS or uncapped, in-game menu in 9 languages.
- **Graphics**: FSR 4, FSR 3, DLSS and DLAA, TAA, sharpening, occlusion culling, picture and camera tuning, best settings for your PC.
- **Controls**: keyboard and mouse for every action, controller remapping, stick options, live controller detection, shareable profiles.
- **Cheats**: Blood Vials and Quicksilver Bullets (infinite or a set amount), infinite consumables and stamina, stealth, no weapon wear, firearm range, keep echoes on death, echo multiplier. Each can be turned off.
- **Trophies**: all 40, completion rate, in-game notification, reset.
- **More**: mods and patches, online (The Hunter's Dream for messages and bloodstains, shadNet for co-op), game log, crash helper with safe mode, memory check before starting, stable or beta updates.

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

The engine is developed here. It started from [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc)
(built on bbport and shadPS4) and has been reworked for Windows since: about 10,000 lines changed or added
across nearly 100 files.

- **Performance**: occlusion culling with real query results, cached texture, sampler and render target lookups,
  the end of each frame handed to the draw thread, render threads first, uncapped FPS. Up to the 120 FPS cap in
  busy areas on a Ryzen 5 5600 with an RTX 5060.
- **Picture**: native HDR (highlights from the game's own HDR scene), DLSS texture LOD bias, FSR 4 balanced model.
- **Stability**: fixes for silent crashes (0xC0000005), exit 23 asserts, low memory and display loss; crash
  reports and dumps; a GPU hang guard; VRAM cleanup.
- **Sound**: the game's audio threads at their PS4 priority on Windows.
- **Game**: cheats, menu languages, controller support, online with The Hunter's Dream.

The patch against the starting point, every change with its switch, and build steps are in [engine/](engine/).

## Credits

| | |
|---|---|
| Engine starting point | [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc) (built on [bbport](https://github.com/deadinside28/bloodborne_pc) and [shadPS4](https://github.com/shadps4-emu/shadPS4)) |
| VRAM cleanup | adapted from [IFreemz/shadPS4-Bloodborne-DLSS-FSR](https://github.com/IFreemz/shadPS4-Bloodborne-DLSS-FSR) |
| Crash and controller fixes | adapted from [AwesomeObserver](https://github.com/AwesomeObserver/bloodborne_pc), [Supermedo](https://github.com/Supermedo/bloodborne_pc) and [deadinside28](https://github.com/deadinside28/bloodborne_pc) |
| Cheats and occlusion culling research | [droogie/bbhost](https://github.com/droogie/bbhost) |
| Controller mappings | [SDL_GameControllerDB](https://github.com/mdqinc/SDL_GameControllerDB) |
| Trophy list | [pyL1nx/bloodborne_pc](https://github.com/pyL1nx/bloodborne_pc) |
| UI library | [WPF-UI](https://github.com/lepoco/wpfui) |

## License

The launcher is GPL-2.0-or-later (see [LICENSE](LICENSE)). The engine is GPL-3.0-or-later; its licenses are in the package's `licenses/` folder.

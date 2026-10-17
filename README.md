# Bloodborne PC
<a href="https://trendshift.io/repositories/302591?utm_source=trendshift-badge&amp;utm_medium=badge&amp;utm_campaign=badge-trendshift-302591" target="_blank" rel="noopener noreferrer"><img src="https://trendshift.io/api/badge/trendshift/repositories/302591/daily?language=C%23" alt="Awakenchan%2FBloodBorneButPc | Trendshift" width="250" height="55"/></a>

Bloodborne on Windows: a launcher and my engine, built on bbport and shadPS4 and reworked by me for performance.

> **Note:** I'm keeping this going on my own, and updates are slower now because of the hate I've been getting. I'll keep working on it when I can.

**No game files are included.** You need your own decrypted Bloodborne dump (CUSA03173) with update 1.09.

## Install

1. Download `BloodbornePC-<version>-win64.zip` from [Releases](https://github.com/Awakenchan/BloodBorneButPc/releases) and extract it anywhere.
2. Run `Bloodborne Launcher.exe` and pick your game folder (the one with `eboot.bin`).
3. Press **Play**. The launcher updates itself.

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

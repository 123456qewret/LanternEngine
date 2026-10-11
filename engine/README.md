# Engine

Our Windows build of the engine: [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc) at tag
`v0.4.0-darkizuku-preview.1` with the changes in `bbhost-changes.patch` (engine) and
`run-windows-frame-rate.patch` (`scripts/run_windows.py`).

## Changes

| Change | Switch |
|---|---|
| 120 / 144 / uncapped FPS on Windows | `BB_FPS_LIMIT`, `BB_VBLANK_HZ` |
| VRAM cleanup: GPU-drawn textures written back and freed under memory pressure | `BB_IMAGE_WRITEBACK_GC=0` off |
| Texture memory sized to the GPU (80% of its budget on discrete cards) | `BB_GC_PRESSURE_PERCENT=0` off |
| Precise frame pacing | `BB_PRECISE_PACING=0` off |
| Render threads at above-normal priority | `BB_GPU_THREAD_PRIORITY=0` off |
| Shorter spin at the end of the game's sleeps | `BB_SLEEP_SPIN_US=600` off |
| Pipelines created on compile threads (shorter hitches in new areas) | `BB_ASYNC_PIPELINES=0` off |
| Working profile-guided builds with clang (`BB_PGO=generate`, then `use`) | |
| In-game menu in Spanish, Portuguese (Brazil), French, German, Italian, Polish, Turkish | `menu_language` |
| Controller: the pad in use takes over; swap sticks, invert camera, dead zone | `pad_swap_sticks`, `pad_invert_look_x/y`, `pad_deadzone` |
| Controller: Cross/Circle in the name entry, held buttons ignored after menus | |
| No 12-second minimum loading time after death | `BB_RESPAWN_DELAY_FIX=0` off |
| Crash fixes: oversized command data, empty recording chunk; swapchain recreation | |
| DLSS texture LOD bias | `BB_DLSS_MIP_BIAS=0` off |
| Optional FSR 4 balanced model | `BB_FSR4_BALANCED=1` |

Parts are adapted from [IFreemz/shadPS4-Bloodborne-DLSS-FSR](https://github.com/IFreemz/shadPS4-Bloodborne-DLSS-FSR),
[AwesomeObserver/bloodborne_pc](https://github.com/AwesomeObserver/bloodborne_pc),
[Supermedo's Windows port](https://github.com/Supermedo/bloodborne_pc) and
[deadinside28/bloodborne_pc](https://github.com/deadinside28/bloodborne_pc) (all GPL-2.0-or-later); the authors are
credited in the code.

## Build

In the MSYS2 **CLANG64** shell (packages as in the engine's README):

```bash
git clone --branch v0.4.0-darkizuku-preview.1 --recurse-submodules https://github.com/DarkIzuku/bloodborne_pc
cd bloodborne_pc
git apply /path/to/engine/bbhost-changes.patch /path/to/engine/run-windows-frame-rate.patch
cp /path/to/engine/win_compat.h gpu/
CFLAGS="-Wno-error=unused-but-set-global" CXXFLAGS="-include $PWD/gpu/win_compat.h" bash build.sh
```

`win_compat.h` supplies headers the bundled FSR 3 SDK expects transitively, which newer libc++
no longer includes. The result is `out/bb-probe.exe`; the package's `out/` holds it with the
CLANG64 DLLs it loads.

GPL-3.0-or-later, like the engine.

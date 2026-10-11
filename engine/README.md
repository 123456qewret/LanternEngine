# Engine

The package's engine is [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc) at tag
`v0.4.0-darkizuku-preview.1`, built for Windows with these changes:

| Patch | What it does | Switch |
|---|---|---|
| `windows-frame-rate.patch` | Lifts the Windows-only 30 / 60 / 90 limit, so 120, 144 and uncapped work | `BB_FPS_LIMIT`, `BB_VBLANK_HZ` |
| `run-windows-frame-rate.patch` | Lets `scripts/run_windows.py` pass those rates on | |
| `vram-cleanup.patch` | Writes GPU-drawn textures back and frees them under memory pressure, so VRAM no longer fills up over long sessions | `BB_IMAGE_WRITEBACK_GC=0` turns it off |
| `frame-pacing.patch` | High-resolution timer and evenly spaced presents | `BB_PRECISE_PACING=0` turns it off |
| `fsr4-balanced.patch` | Optional FSR 4 balanced model for sharper motion | `BB_FSR4_BALANCED=1` |
| `menu-languages.patch` | In-game menu in Spanish, Portuguese (Brazil), French, German, Italian, Polish and Turkish | `menu_language` in bbport.ini |

`vram-cleanup.patch` adapts work from
[IFreemz/shadPS4-Bloodborne-DLSS-FSR](https://github.com/IFreemz/shadPS4-Bloodborne-DLSS-FSR)
(GPL-2.0-or-later); the authors are credited in the code.

## Build

In the MSYS2 **CLANG64** shell (packages as in the engine's README):

```bash
git clone --branch v0.4.0-darkizuku-preview.1 --recurse-submodules https://github.com/DarkIzuku/bloodborne_pc
cd bloodborne_pc
git apply /path/to/engine/*.patch
cp /path/to/win_compat.h gpu/
CFLAGS="-Wno-error=unused-but-set-global" CXXFLAGS="-include $PWD/gpu/win_compat.h" bash build.sh
```

`win_compat.h` supplies headers the bundled FSR 3 SDK expects transitively, which newer libc++
no longer includes. The result is `out/bb-probe.exe`; the package's `out/` holds it with the
CLANG64 DLLs it loads.

GPL-3.0-or-later, like the engine.

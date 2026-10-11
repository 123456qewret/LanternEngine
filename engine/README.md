# Engine

The package's engine is [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc) at tag
`v0.4.0-darkizuku-preview.1`, built for Windows with two changes:

- `windows-frame-rate.patch` – lifts the Windows-only 30 / 60 / 90 limit in
  `gpu/shim/core/emulator_settings.h`, so 120, 144 and uncapped work as they do on Linux.
- `run-windows-frame-rate.patch` – lets `scripts/run_windows.py` pass those rates on (the
  uncapped patch with the sprint fix, plus the frame limiter).

## Build

In the MSYS2 **CLANG64** shell (packages as in the engine's README):

```bash
git clone --branch v0.4.0-darkizuku-preview.1 --recurse-submodules https://github.com/DarkIzuku/bloodborne_pc
cd bloodborne_pc
git apply /path/to/windows-frame-rate.patch /path/to/run-windows-frame-rate.patch
cp /path/to/win_compat.h gpu/
CFLAGS="-Wno-error=unused-but-set-global" CXXFLAGS="-include $PWD/gpu/win_compat.h" bash build.sh
```

`win_compat.h` supplies headers the bundled FSR 3 SDK expects transitively, which newer libc++
no longer includes. The result is `out/bb-probe.exe`; the package's `out/` holds it with the
CLANG64 DLLs it loads.

GPL-3.0-or-later, like the engine.

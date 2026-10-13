# Engine

The engine as developed here. Our work is `bbhost-changes.patch` (engine and scripts), applied to its starting point, [DarkIzuku/bloodborne_pc](https://github.com/DarkIzuku/bloodborne_pc)
at tag `v0.4.0-darkizuku-preview.1`.

## Changes

| Change | Switch |
|---|---|
| 120 / 144 / uncapped FPS on Windows | `BB_FPS_LIMIT`, `BB_VBLANK_HZ` |
| VRAM cleanup: GPU-drawn textures written back and freed under memory pressure | `BB_IMAGE_WRITEBACK_GC=0` off |
| GPU hang guard: textures in use are never freed, deleted ones kept a little longer | `BB_GC_RECENT_FRAMES=0`, `BB_IMAGE_FREE_DELAY_FRAMES=0` off |
| Texture memory sized to the GPU (80% of its budget on discrete cards) | `BB_GC_PRESSURE_PERCENT=0` off |
| Precise frame pacing | `BB_PRECISE_PACING=0` off |
| Render threads at above-normal priority | `BB_GPU_THREAD_PRIORITY=0` off |
| Shorter spin at the end of the game's sleeps | `BB_SLEEP_SPIN_US=600` off |
| Audio threads (FMOD, PS4 priority 256-300) above the game's other threads on Windows | `BB_AUDIO_THREAD_PRIORITY=0` off |
| Audio output follows the device's real rate (proportional correction, queue capped), so sound can't drift behind | |
| Audio device thread watched (writes, rate, longest gap in the audio statistics) and raised when Windows leaves it at normal priority; DirectSound output with a larger buffer as an option | `BB_AUDIO_STATS`, `SDL_AUDIO_DRIVER=directsound`, `SDL_AUDIO_DEVICE_SAMPLE_FRAMES` |
| Pipelines created on compile threads (shorter hitches in new areas) | `BB_ASYNC_PIPELINES=0` off |
| Occlusion culling: real occlusion query results, so the game skips hidden objects; a block reads hidden only after three empty results (no flicker) | `BB_OCCLUSION=0` off |
| Cached texture, sampler and render target lookups kept across image (un)registration | `BB_MEMO_REVALIDATE=0`, `BB_SAMPLER_MEMO=0` off |
| End of each submission handed to the draw recording thread instead of draining it | `BB_SUBMIT_END_TASKS=0` off |
| Draws whose shader is still compiling are skipped for a moment instead of freezing the game; pipelines bound once compiled | `BB_SKIP_UNCOMPILED=0`, `BB_LATE_PIPELINE_BIND=0`, `BB_PIPELINE_TAKE=0` off |
| Game threads no longer wait behind texture work on write faults | `BB_FAULT_SKIP_TEXTURES=0`, `BB_DRAIN_SLEEP=0` off |
| Working profile-guided builds with clang (`BB_PGO=generate`, then `use`) | |
| In-game menu in Spanish, Portuguese (Brazil), French, German, Italian, Polish, Turkish | `menu_language` |
| Controller: the pad in use takes over; swap sticks, invert camera, dead zone | `pad_swap_sticks`, `pad_invert_look_x/y`, `pad_deadzone` |
| Controller: Cross/Circle in the name entry, held buttons ignored after menus | |
| No 12-second minimum loading time after death | `BB_RESPAWN_DELAY_FIX=0` off |
| Crash fixes: oversized command data, empty recording chunk, out-of-order flip IRQ, SnormNz packed formats, failed allocations in audio decoding and the save list; swapchain recreation | |
| Crash reports for host faults on game threads (Windows ended those without a trace); RAM in the log once a minute | |
| Write faults at the end of a GPU mapping handled (they ended the game, 0xC0000005); unclaimed faults on guest pages get access back; crash dump next to the log; display loss and unknown formats no longer exit | |
| Picture tuning live: the picture hooks are always installed and the switch gates them per call; wider ranges, typed values | `graphics_controls` |
| HDR native mode: the game's UI at its own brightness and colours (sRGB to DCI-P3) | `hdr_ui_brightness`, `hdr_ui_color` |
| Trophies a save already earned: the 19 boss trophies whose defeat flags are set count once a character is in the world ("earned in this save") | |
| Chromatic aberration, depth of field, motion blur and the game's own anti-aliasing off by default | `effect_*` |
| Online: characters given no id (0x8000000000000000) ask again; summon sign timeout (droogie bbhost 566184a) | `BB_SIGN_TIMEOUT` (`online_sign_timeout`) |
| The mouse cursor hidden 2 s after the mouse last moved (it was always shown on Windows unless the mouse camera was on) | `hide_cursor` |
| Online: the client reports what it is after signing in (shadNet 0.0.34 signs out clients that report nothing); a rejection is logged and signs out at once | `BB_CLIENT_VERSION` |
| Launch: the boot image (prepare, link_libc, link_modules, content profile) is reused while its inputs are unchanged, about 10 s a launch | `out/prepared.json` |
| Loading profile: the game's main loop, render and resource-loading threads and the engine's GPU command and recording threads sampled every 2 ms, their hottest game and engine code in the log every 5 s | `BB_SAMPLE_THREADS=load` |
| Patches check the executable first: a built-in patch whose write sites differ from the standard 1.09 (CRC-32s in `patches/Bloodborne-1.09-sites.json`) is left out and logged instead of writing into other code | |
| A patch that enlarges the game's heaps (resolution patches above 1080p from the patches folder) gets the 9152 MiB pool; a full pool is logged and explained in the crash report | `BB_DMEM_MB` |
| Online: a network drop of the shadNet connection keeps the game signed in for 90 s while it reconnects (it returned to the title menu) | |
| Online co-op: shadNet server framing for signaling, UPnP port mapping (Supermedo 004d2c37), P2P port 3658 by default, play logs to the game server | `SHADPS4_P2P_PORT` |
| In-game PC Colour page (brightness, contrast, saturation, hue from the game's own tone map) and a fog slider on PC Graphics, live; the same in the launcher's Picture tuning | `graphics_brightness`, `graphics_contrast`, `graphics_hue`, `graphics_fog` |
| HDR output: HDR10 or scRGB swapchain with paper white, peak and highlight boost; native mode takes highlight brightness from the game's scene (a gain map), expand mode from the SDR picture | `hdr`, `hdr_mode`, `hdr_paper_white`, `hdr_peak`, `hdr_boost` |
| DLSS texture LOD bias | `BB_DLSS_MIP_BIAS=0` off |
| Optional FSR 4 balanced model | `BB_FSR4_BALANCED=1` |
| Cheats: Blood Vials and Quicksilver Bullets (infinite or a set amount), consumables, stamina, stealth, no weapon wear, firearm range, keep echoes, echo multiplier, add echoes/insight; live and revertible (in-game menu) | `cheat_*` |
| Texture refresh: an image the GPU rewrites every frame (the game's copy of the depth buffer) keeps the hashes of its guest memory while nothing wrote that memory, instead of hashing 9 MB a frame on the draw recording thread; large images are hashed on the copy threads | `BB_KEEP_MIP_HASHES=0`, `BB_PARALLEL_IMAGE_HASH=0` off |
| Texture set memo for shader stages without prepared draw data as well | `BB_LOCAL_TEXTURE_SETS=0` off |
| Effects at reduced presets: passes at the output size that read a reduced-resolution image through a point sampler read the full-size image instead, which is upscaled with linear filtering (blocky effects and moon edge with DLSS/FSR) | `BB_PROXY_POINT_NATIVE=0`, `BB_SCENE_LINEAR_RESOLVE=0` off |
| Keyboard and mouse button prompts: the game's 20 key-guide icons and the HUD's buttons drawn as the bound keys when they are uploaded, the game's own icons again with a controller | `button_prompts` (`auto`, `keyboard`, `controller`) |
| A patch that changes the game's region (Unlock Game Region) is left out while online is on, unless kept: the game has no server for that region | `BB_REGION_PATCH_ONLINE=keep` |
| Output resolution 7680x2160 (32:9), and `auto`: the display's own size (read from Windows at start), the camera and UI widened to fill it | `output_res`, `BB_DISPLAY_SIZE` |
| A third-party resolution patch (any patch writing the game's resolution and UI layout sites) is left out while the output resolution isn't 1920x1080, unless kept: applied after the engine's own, it kept its shape on screen (black bars) and stopped the widescreen hooks | `BB_RESOLUTION_PATCH=keep` |

Parts are adapted from [IFreemz/shadPS4-Bloodborne-DLSS-FSR](https://github.com/IFreemz/shadPS4-Bloodborne-DLSS-FSR),
[AwesomeObserver/bloodborne_pc](https://github.com/AwesomeObserver/bloodborne_pc),
[Supermedo's Windows port](https://github.com/Supermedo/bloodborne_pc) and
[deadinside28/bloodborne_pc](https://github.com/deadinside28/bloodborne_pc) (all GPL-2.0-or-later) and [droogie/bbhost](https://github.com/droogie/bbhost) (GPL-3.0); the authors are
credited in the code.

## Build

In the MSYS2 **CLANG64** shell (packages as in the engine's README):

```bash
git clone --branch v0.4.0-darkizuku-preview.1 --recurse-submodules https://github.com/DarkIzuku/bloodborne_pc
cd bloodborne_pc
git apply /path/to/engine/bbhost-changes.patch
cp /path/to/engine/win_compat.h gpu/
CFLAGS="-Wno-error=unused-but-set-global" CXXFLAGS="-include $PWD/gpu/win_compat.h" bash build.sh
```

`win_compat.h` supplies headers the bundled FSR 3 SDK expects transitively, which newer libc++
no longer includes. The result is `out/bb-probe.exe`; the package's `out/` holds it with the
CLANG64 DLLs it loads.

GPL-3.0-or-later, like the engine.

using System.IO;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// The environment run_windows.py gets from the launcher. Most settings travel in bbport.ini
    /// (graphics, controls, online); these are the ones the engine takes from the environment.
    /// </summary>
    public static class GameEnvironment
    {
        public static string GameDir(LauncherSettings s) => Paths.Expand(s.Str("game_dir"));

        /// <summary>The saves folder the player chose (or the package's user folder): the Main profile.</summary>
        public static string SavesRoot(LauncherSettings s) =>
            s.Str("user_dir") is { Length: > 0 } dir ? Paths.Expand(dir) : Path.Combine(Paths.DataDir, "user");

        /// <summary>Where the game keeps saves and trophies: the current player profile's folder.</summary>
        public static string UserDir(LauncherSettings s) =>
            PlayerProfiles.Folder(s, PlayerProfiles.Current(s));

        public static string ModsDir(LauncherSettings s) =>
            s.Str("mods_dir") is { Length: > 0 } dir ? Paths.Expand(dir) : Path.Combine(Paths.DataDir, "mods");

        public static string PatchesDir(LauncherSettings s) =>
            s.Str("patches_dir") is { Length: > 0 } dir ? Paths.Expand(dir) : Path.Combine(Paths.DataDir, "patches");

        /// <summary>30, 60 and 90 are the engine's fixed presets; 120 and uncapped use the uncapped
        /// patch (scripts\run_windows.py turns them into BB_FPS=uncap with a limit).</summary>
        public static string Fps(LauncherSettings s) => s.Str("fps_mode") is "30" or "90" or "120" or "144" or "uncap" ? s.Str("fps_mode") : "60";

        public static Dictionary<string, string> Build(LauncherSettings s)
        {
            var fps = Fps(s);
            var env = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // The package's engine is already built: run_windows.py must not try MSYS2.
                ["BB_PREBUILT"] = "1",
                ["BB_DATA_DIR"] = Paths.DataDir,
                ["BB_CONFIG"] = Paths.IniPath,
                ["BB_GAME_DIR"] = GameDir(s),
                ["BB_USER_DIR"] = UserDir(s),
                ["BB_MODS_DIR"] = ModsDir(s),
                ["BB_MODS_CONFIG"] = Path.Combine(Paths.DataDir, "mods.json"),
                ["BB_MODS_ENABLED"] = s.Bool("mods_enabled") ? "1" : "0",
                ["BB_PATCHES_DIR"] = PatchesDir(s),
                ["BB_PATCHES_CONFIG"] = Path.Combine(Paths.DataDir, "patches.json"),
                ["BB_LANGUAGE"] = s.Str("language"),
                ["BB_FPS"] = fps,
                ["BB_FPS_LIMIT"] = fps == "uncap" ? "0" : fps,
                ["BB_VBLANK_HZ"] = fps switch { "90" => "90", "30" or "60" => "60", _ => "0" },
                ["PYTHONUNBUFFERED"] = "1",
            };
            if (s.Str("present_mode").Length > 0) env["BB_PRESENT_MODE"] = s.Str("present_mode");
            if (s.Bool("frame_stats")) env["BB_FRAME_STATS"] = "1";
            if (s.Bool("fsr_profile")) env["BB_FSR4_PROFILE"] = "1";
            if (s.Bool("audio_stats")) env["BB_AUDIO_STATS"] = "1";
            if (s.Bool("fsr4_balanced")) env["BB_FSR4_BALANCED"] = "1";
            // Experimental engine features: on by default, each can be turned off.
            env["BB_PRECISE_PACING"] = s.Bool("exp_frame_pacing") ? "1" : "0";
            env["BB_IMAGE_WRITEBACK_GC"] = s.Bool("exp_vram_cleanup") ? "1" : "0";
            // Gamepads (SDL3 reads its hints from the environment): DualShock 3 through SDL's own
            // driver (with DsHidMini or Sony's sixaxis driver installed), and the community
            // mapping database for third-party pads when the package ships it.
            foreach (var (key, value) in SdlHints())
                env[key] = value;

            // Off: the engine's earlier behaviour; on: its default (left unset).
            if (!s.Bool("exp_vram_budget")) { env["BB_GC_PRESSURE_PERCENT"] = "0"; env["BB_GC_PRESSURE_SECONDS"] = "0"; }
            if (!s.Bool("exp_thread_priority")) env["BB_GPU_THREAD_PRIORITY"] = "0";
            if (!s.Bool("exp_sleep_spin")) env["BB_SLEEP_SPIN_US"] = "600";
            if (!s.Bool("exp_async_pipelines")) env["BB_ASYNC_PIPELINES"] = "0";
            if (!s.Bool("exp_occlusion")) env["BB_OCCLUSION"] = "0";
            if (s.Bool("section_stats")) env["BB_SECTIONS"] = "1";
            if (s.Bool("safe_mode_next"))
            {
                // Safe mode (after a crash, one run): every experimental engine option and the mods off.
                env["BB_PRECISE_PACING"] = "0";
                env["BB_IMAGE_WRITEBACK_GC"] = "0";
                env["BB_GC_PRESSURE_PERCENT"] = "0";
                env["BB_GC_PRESSURE_SECONDS"] = "0";
                env["BB_GPU_THREAD_PRIORITY"] = "0";
                env["BB_SLEEP_SPIN_US"] = "600";
                env["BB_ASYNC_PIPELINES"] = "0";
                env["BB_OCCLUSION"] = "0";
                env["BB_MODS_ENABLED"] = "0";
            }
            // The account password never goes to bbport.ini; the engine takes it from here.
            if (s.Str("online_password").Length > 0) env["BB_SHADNET_PASSWORD"] = s.Str("online_password");

            foreach (var item in s.Str("extra_env").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var at = item.IndexOf('=');
                if (at > 0)
                    env[item[..at]] = item[(at + 1)..];
            }
            return env;
        }

        /// <summary>SDL3 hints for controllers, given to the game and used by the launcher's own SDL (SdlPad).</summary>
        public static IEnumerable<(string Key, string Value)> SdlHints()
        {
            yield return ("SDL_JOYSTICK_HIDAPI_PS3", "1");
            yield return ("SDL_JOYSTICK_HIDAPI_PS3_SIXAXIS_DRIVER", "1");
            var mappings = Path.Combine(Paths.PortDir, "gamecontrollerdb.txt");
            if (File.Exists(mappings)) yield return ("SDL_GAMECONTROLLERCONFIG_FILE", mappings);
        }
    }
}

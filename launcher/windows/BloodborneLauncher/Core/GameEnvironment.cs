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

        public static string UserDir(LauncherSettings s) =>
            s.Str("user_dir") is { Length: > 0 } dir ? Paths.Expand(dir) : Path.Combine(Paths.DataDir, "user");

        public static string ModsDir(LauncherSettings s) =>
            s.Str("mods_dir") is { Length: > 0 } dir ? Paths.Expand(dir) : Path.Combine(Paths.DataDir, "mods");

        public static string PatchesDir(LauncherSettings s) =>
            s.Str("patches_dir") is { Length: > 0 } dir ? Paths.Expand(dir) : Path.Combine(Paths.DataDir, "patches");

        /// <summary>The engine runs 30, 60 or 90 FPS.</summary>
        public static string Fps(LauncherSettings s) => s.Str("fps_mode") is "30" or "90" ? s.Str("fps_mode") : "60";

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
                ["BB_FPS_LIMIT"] = fps,
                ["BB_VBLANK_HZ"] = fps == "90" ? "90" : "60",
                ["PYTHONUNBUFFERED"] = "1",
            };
            if (s.Str("present_mode").Length > 0) env["BB_PRESENT_MODE"] = s.Str("present_mode");
            if (s.Bool("frame_stats")) env["BB_FRAME_STATS"] = "1";
            if (s.Bool("fsr_profile")) env["BB_FSR4_PROFILE"] = "1";
            if (s.Bool("audio_stats")) env["BB_AUDIO_STATS"] = "1";
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
    }
}

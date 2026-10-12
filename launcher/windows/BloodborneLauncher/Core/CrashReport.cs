using System.IO;
using System.Text;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// What a bug report needs after a crash: versions, the PC, the settings that matter and the
    /// end of the session log. The user's own folder is replaced by %USERPROFILE%.
    /// </summary>
    public static class CrashReport
    {
        private static readonly string[] Experimental =
            { "exp_vram_cleanup", "exp_frame_pacing", "exp_vram_budget", "exp_thread_priority", "exp_sleep_spin", "exp_async_pipelines" };

        /// <summary>What stopped the game, from the end of the session log (exit code 23 is the
        /// engine stopping itself, for several reasons).</summary>
        public static string Explain(int code)
        {
            var tail = Tail(GameRunner.SessionLog, 120).ToList();
            bool Has(string text) => tail.Any(l => l.Contains(text, StringComparison.OrdinalIgnoreCase));
            if (Has("Out of order flip IRQ"))
                return "A frame reached the screen out of order (a timing race in the engine; Precise frame pacing makes it likelier).";
            if (Has("DEVICE_LOST") || Has("device lost"))
                return "The graphics driver stopped responding: the GPU hung or lost the game's memory.";
            var assertion = tail.FindIndex(l => l.Contains("Assertion Failed", StringComparison.OrdinalIgnoreCase));
            if (assertion >= 0)
            {
                var detail = assertion + 1 < tail.Count && !tail[assertion + 1].StartsWith("STOP:") ? tail[assertion + 1].Trim() : "";
                return "The engine stopped on an internal check" + (detail.Length > 0 ? $": {detail}." : ".");
            }
            return code == 23
                ? "The engine stopped itself on an error (exit code 23); the report has the details."
                : $"The game closed unexpectedly (exit code {code}).";
        }

        public static string Build(LauncherSettings s, IniFile ini, int code, int logLines = 150)
        {
            var hw = Hardware.Detect();
            var text = new StringBuilder();
            text.AppendLine($"Bloodborne Launcher {Updater.CurrentLabel}, engine {Updater.LocalEngine() ?? "unknown"}");
            text.AppendLine($"Exit code {code}: {Explain(code)}");
            text.AppendLine($"{hw.Summary}, {hw.RamBytes >> 30} GB RAM, Windows {Environment.OSVersion.Version}");
            text.AppendLine($"Upscaler {ini.Get("upscaler")}, preset {ini.Get("preset")}, output {ini.Get("output_res")}, " +
                            $"FPS {s.Str("fps_mode")}, live resolution {ini.Get("live_resolution")}, mods {(s.Bool("mods_enabled") ? "on" : "off")}");
            text.AppendLine("Experimental: " + string.Join(", ", Experimental.Select(k => $"{k[4..].Replace('_', ' ')} {(s.Bool(k) ? "on" : "off")}")));
            text.AppendLine();
            text.AppendLine($"Last {logLines} lines of logs\\last_session.log:");
            text.AppendLine("```");
            foreach (var line in Tail(GameRunner.SessionLog, logLines))
                text.AppendLine(line);
            text.AppendLine("```");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return home.Length > 3 ? text.ToString().Replace(home, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase) : text.ToString();
        }

        private static IEnumerable<string> Tail(string path, int count)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                var lines = new Queue<string>();
                while (reader.ReadLine() is { } line)
                {
                    lines.Enqueue(line);
                    if (lines.Count > count) lines.Dequeue();
                }
                return lines;
            }
            catch (Exception e)
            {
                return new[] { "(the session log could not be read: " + e.Message + ")" };
            }
        }
    }
}

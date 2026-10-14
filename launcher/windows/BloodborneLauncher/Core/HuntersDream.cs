using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// The Hunter's Dream co-op pool (online_pool=huntersdream), with bbhost's players, through
    /// bbbridge by MaskZeus (GPL-3.0-or-later, the package's bbbridge folder): scripts/online.py and
    /// run_windows.py start it with the game. Here: its Discord sign-in and the accounts it holds,
    /// in network\bbbridge\ beside bbport.ini (updates keep that folder).
    /// </summary>
    public static class HuntersDream
    {
        public static string Folder => Path.Combine(Paths.DataDir, "network", "bbbridge");
        public static string StateFile => Path.Combine(Folder, "bbbridge_state.json");
        public static string Cli => Path.Combine(Paths.PortDir, "bbbridge", "bbbridge-cli.exe");

        /// <summary>bbbridge's settings file, pointing at its account file here (as online.py writes it).</summary>
        public static string ConfigFile()
        {
            Directory.CreateDirectory(Folder);
            var path = Path.Combine(Folder, "bbbridge_config.json");
            JsonObject config;
            try { config = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject(); }
            catch (Exception) { config = new JsonObject(); }
            var state = Path.GetFullPath(StateFile);
            if (config["state_file"]?.GetValue<string>() != state)
            {
                config["state_file"] = state;
                File.WriteAllText(path, config.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
            }
            return path;
        }

        /// <summary>The accounts signed in on this PC (their online names).</summary>
        public static List<string> Accounts()
        {
            try
            {
                if (JsonNode.Parse(File.ReadAllText(StateFile))?["accounts"] is JsonObject accounts)
                    return accounts.Select(a => a.Key).ToList();
            }
            catch (Exception) { }
            return new List<string>();
        }

        /// <summary>Discord sign-in: bbbridge-cli shows a code for thehuntersdream.com and waits
        /// until it is approved there. <paramref name="code"/> gets (page, code) once it is known.
        /// Returns the account's name, or throws with bbbridge's reason.</summary>
        public static async Task<string> SignInAsync(Action<string, string> code, CancellationToken cancel)
        {
            if (!File.Exists(Cli)) throw new InvalidOperationException($"{Cli} is missing: reinstall the package.");
            var start = new ProcessStartInfo(Cli)
            {
                WorkingDirectory = Folder,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add("--config");
            start.ArgumentList.Add(ConfigFile());
            start.ArgumentList.Add("--link-discord");
            using var process = Process.Start(start) ?? throw new InvalidOperationException("bbbridge did not start");
            using var stop = cancel.Register(() => { try { process.Kill(); } catch (Exception) { } });
            string? result = null;
            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                // "Open <page> and enter the code <code>", then "signed in as <name>" or "failed: <why>".
                const string open = "Open ", enter = " and enter the code ";
                var at = line.IndexOf(enter, StringComparison.Ordinal);
                if (line.StartsWith(open, StringComparison.Ordinal) && at > 0)
                    code(line[open.Length..at], line[(at + enter.Length)..].Trim());
                else
                    result = line.Trim();
            }
            await process.WaitForExitAsync();
            cancel.ThrowIfCancellationRequested();
            const string signed = "signed in as ";
            if (result is { } r && r.StartsWith(signed, StringComparison.Ordinal))
                return r[signed.Length..];
            throw new InvalidOperationException(result is { } f && f.StartsWith("failed: ") ? f["failed: ".Length..] : "the sign-in did not finish");
        }
    }
}

using System.IO;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// Bloodborne saves from another build - shadPS4, another port's folder, a copy on a USB stick:
    /// the game's SPRJ0005 folder (userdata0000.., the save files), copied in as the current
    /// profile's. Every build keeps them the same way (savedata\&lt;user&gt;\&lt;game id&gt;\SPRJ0005), so
    /// the files themselves need no conversion. The current saves are backed up first
    /// (SaveBackups), so Restore brings them back.
    /// </summary>
    public static class SaveImport
    {
        public const string SaveDir = "SPRJ0005";

        public sealed record Source(string Dir, DateTime Saved, string From);

        /// <summary>The folder the engine keeps this game's saves under: param.sfo's
        /// INSTALL_DIR_SAVEDATA (the Game of the Year edition saves under the base game's id),
        /// else its TITLE_ID (bb-probe, src/probe.c); a profile's existing one when there is no
        /// game folder.</summary>
        public static string GameId(LauncherSettings s)
        {
            var sfo = GameCheck.ReadSfo(Path.Combine(GameEnvironment.GameDir(s), "sce_sys", "param.sfo"));
            if (sfo.TryGetValue("INSTALL_DIR_SAVEDATA", out var id) && id.Length > 0) return id;
            if (sfo.TryGetValue("TITLE_ID", out id) && id.Length > 0) return id;
            var user = Path.Combine(SaveBackups.SaveData(s), "1");
            return Directory.Exists(user)
                ? Directory.GetDirectories(user).Select(Path.GetFileName).FirstOrDefault(d => Directory.Exists(Path.Combine(user, d!, SaveDir))) ?? "CUSA00207"
                : "CUSA00207";
        }

        /// <summary>Where the current profile's saves go.</summary>
        public static string Target(LauncherSettings s) => Path.Combine(SaveBackups.SaveData(s), "1", GameId(s), SaveDir);

        public static bool HasSaves(string dir) =>
            Directory.Exists(dir) && Directory.EnumerateFiles(dir, "userdata*").Any();

        /// <summary>Saves in the places other builds keep them (shadPS4's user folder), newest first.</summary>
        public static List<Source> Known(LauncherSettings s)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var roots = new[]
            {
                (Path.Combine(appData, "shadPS4"), "shadPS4"),
                (Path.Combine(documents, "shadPS4"), "shadPS4"),
            };
            var ours = Path.GetFullPath(Target(s));
            return roots.Where(r => Directory.Exists(r.Item1))
                .SelectMany(r => Find(r.Item1, r.Item2, 5))
                .Where(f => !string.Equals(Path.GetFullPath(f.Dir), ours, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Saved).ToList();
        }

        /// <summary>Save folders under `root` (it, or a few folders down), newest first.</summary>
        public static List<Source> Find(string root, string from, int depth = 7)
        {
            var found = new List<Source>();
            void Walk(string dir, int left)
            {
                // SPRJ0005 itself, an export named after it (Apollo Save Tool: CUSA00207_SPRJ0005),
                // or any folder with the game's system save file.
                var name = Path.GetFileName(dir);
                if (name.EndsWith(SaveDir, StringComparison.OrdinalIgnoreCase) || File.Exists(Path.Combine(dir, "userdata0010")))
                {
                    if (HasSaves(dir))
                    {
                        found.Add(new Source(dir, Directory.GetFiles(dir, "userdata*").Max(File.GetLastWriteTime), from));
                        return;
                    }
                }
                if (left == 0) return;
                string[] children;
                try { children = Directory.GetDirectories(dir); }
                catch { return; }
                foreach (var child in children)
                {
                    // Game files and backups are not where saves live, and a dump has thousands of folders.
                    var childName = Path.GetFileName(child);
                    if (childName.Equals("dvdroot_ps4", StringComparison.OrdinalIgnoreCase) ||
                        childName.Equals("launcher-backups", StringComparison.OrdinalIgnoreCase) ||
                        childName.Equals("save-backups", StringComparison.OrdinalIgnoreCase))
                        continue;
                    Walk(child, left - 1);
                }
            }
            Walk(root, depth);
            return found.OrderByDescending(f => f.Saved).ToList();
        }

        /// <summary>Whether `root` (or a folder a few levels under it) holds a PS4 save as the console
        /// copies it to USB: sdimg_SPRJ0005, an image encrypted with that console's keys, which no
        /// PC tool can open; it has to be exported decrypted on the PS4 (Apollo Save Tool) or by
        /// Save Wizard.</summary>
        public static bool HasEncryptedPs4Save(string root, int depth = 7)
        {
            bool Walk(string dir, int left)
            {
                try
                {
                    if (Directory.EnumerateFiles(dir, "sdimg_*").Any()) return true;
                    return left > 0 && Directory.GetDirectories(dir).Any(child => Walk(child, left - 1));
                }
                catch { return false; }
            }
            return Walk(root, depth);
        }

        /// <summary>Copies a save folder's files in as the current profile's saves, after backing up
        /// what is there.</summary>
        public static void Import(LauncherSettings s, string from)
        {
            if (!HasSaves(from)) throw new IOException("That folder has no Bloodborne save files (userdata).");
            SaveBackups.Create(s, beforeRestore: true);
            var target = Target(s);
            // Copied next to it first, so a failed copy leaves the current saves in place. Only the
            // save files: shadPS4's sce_sys folder inside is its own bookkeeping.
            var staging = target + ".importing";
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
            Directory.CreateDirectory(staging);
            foreach (var file in Directory.GetFiles(from))
                File.Copy(file, Path.Combine(staging, Path.GetFileName(file)));
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
            Directory.Move(staging, target);
        }
    }
}

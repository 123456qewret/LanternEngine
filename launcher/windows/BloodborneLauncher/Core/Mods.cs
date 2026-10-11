using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace BloodborneLauncher.Core
{
    /// <summary>Mod folders and their load order (scripts/mods.py; profile in mods.json).</summary>
    public static class Mods
    {
        private static readonly HashSet<string> GameFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            "action", "adhoc", "chr", "event", "facegen", "font", "map", "menu", "movie", "msg", "mtd",
            "obj", "other", "param", "paramdef", "parts", "remo", "script", "sfx", "shader", "sound",
        };

        private static readonly string[] Serials =
        {
            "CUSA03173", "CUSA00900", "CUSA00207", "CUSA00208", "CUSA00299", "CUSA01363",
            "CUSA03179", "CUSA03014", "CUSA03023",
        };

        private static readonly string[] LooseFileTypes = { ".txt", ".md", ".jpg", ".png", ".ini" };

        public static string ProfilePath => Path.Combine(Paths.DataDir, "mods.json");

        /// <summary>Folders of `root` with an accepted layout, alphabetically.</summary>
        public static List<string> Discover(string root)
        {
            if (!Directory.Exists(root))
                return new List<string>();
            return Directory.GetDirectories(root)
                .Where(HasContent)
                .Select(Path.GetFileName)
                .OfType<string>()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool HasContent(string folder)
        {
            try
            {
                return ContentRoot(folder);
            }
            catch
            {
                return false;
            }
        }

        private static string Child(string folder, string name) =>
            Directory.Exists(folder)
                ? Directory.EnumerateFileSystemEntries(folder)
                      .FirstOrDefault(e => string.Equals(Path.GetFileName(e), name, StringComparison.OrdinalIgnoreCase))
                  ?? Path.Combine(folder, name)
                : Path.Combine(folder, name);

        private static bool ContentRoot(string folder)
        {
            if (!Directory.Exists(folder))
                return false;
            foreach (var wrapper in new[] { "", "app0" }.Concat(Serials))
            {
                var baseDir = wrapper.Length > 0 ? Child(folder, wrapper) : folder;
                if (Directory.Exists(Child(baseDir, "dvdroot_ps4")))
                    return true;
            }

            var entries = Directory.EnumerateFileSystemEntries(folder)
                .Where(e => !Path.GetFileName(e).StartsWith('.')).ToList();
            var folders = entries.Where(Directory.Exists).ToList();
            if (folders.Count > 0 && folders.All(f => GameFolders.Contains(Path.GetFileName(f))))
                return true;
            if (folders.Count == 1 && !entries.Any(e => File.Exists(e) &&
                    !LooseFileTypes.Contains(Path.GetExtension(e), StringComparer.OrdinalIgnoreCase)))
                return ContentRoot(folders[0]);
            return false;
        }

        public sealed record Entry(string Name, bool Enabled);

        /// <summary>The mods in load order: the saved order first, new folders after, enabled.</summary>
        public static List<Entry> Load(string root)
        {
            var available = Discover(root);
            var order = new List<string>();
            var disabled = new HashSet<string>();
            try
            {
                if (File.Exists(ProfilePath) && JsonNode.Parse(File.ReadAllText(ProfilePath)) is JsonObject profile)
                {
                    order = profile["order"]?.AsArray().Select(n => n?.GetValue<string>() ?? "").ToList() ?? order;
                    disabled = profile["disabled"]?.AsArray().Select(n => n?.GetValue<string>() ?? "").ToHashSet() ?? disabled;
                }
            }
            catch
            {
                // A broken profile: everything enabled, alphabetical.
            }

            return order.Concat(available).Distinct().Where(available.Contains)
                .Select(n => new Entry(n, !disabled.Contains(n))).ToList();
        }

        public static void Save(IEnumerable<Entry> mods)
        {
            var list = mods.ToList();
            var profile = new JsonObject
            {
                ["order"] = new JsonArray(list.Select(m => (JsonNode?)m.Name).ToArray()),
                ["disabled"] = new JsonArray(list.Where(m => !m.Enabled).Select(m => (JsonNode?)m.Name).ToArray()),
            };
            Directory.CreateDirectory(Paths.DataDir);
            File.WriteAllText(ProfilePath, profile.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
    }

    /// <summary>Third-party shadPS4 XML patches for 01.09 (scripts/patches.py; profile in patches.json).</summary>
    public static class Patches
    {
        public sealed record Patch(string Key, string Name, string File, string Author, string Note, bool DefaultOn);

        public static string ProfilePath => Path.Combine(Paths.DataDir, "patches.json");

        public static List<Patch> Discover(string directory)
        {
            var found = new List<Patch>();
            if (!Directory.Exists(directory))
                return found;

            var builtIn = Path.GetFullPath(Path.Combine(Paths.PortDir, "patches", "Bloodborne.xml"));
            foreach (var path in Directory.GetFiles(directory, "*.xml").OrderBy(p => p, StringComparer.Ordinal))
            {
                if (string.Equals(Path.GetFullPath(path), builtIn, StringComparison.OrdinalIgnoreCase))
                    continue;
                XElement root;
                try
                {
                    root = XDocument.Load(path).Root!;
                }
                catch
                {
                    continue;
                }

                var ids = root.Descendants("ID").Select(e => e.Value.Trim()).Where(v => v.Length > 0).ToHashSet();
                if (ids.Count > 0 && !ids.Overlaps(GameCheck.SupportedTitles))
                    continue;

                foreach (var meta in root.Descendants("Metadata"))
                {
                    if ((string?)meta.Attribute("AppVer") != "01.09" ||
                        ((string?)meta.Attribute("AppElf") ?? "eboot.bin") != "eboot.bin")
                        continue;
                    var name = (string?)meta.Attribute("Name") ?? "";
                    var file = Path.GetFileName(path);
                    found.Add(new Patch($"{file}/{name}", name.Length > 0 ? name : $"{file}/{name}", file,
                        (string?)meta.Attribute("Author") ?? "",
                        ((string?)meta.Attribute("Note") ?? "").Replace("\\n", "\n"),
                        string.Equals((string?)meta.Attribute("isEnabled"), "true", StringComparison.OrdinalIgnoreCase)));
                }
            }
            return found;
        }

        public static (HashSet<string> Enabled, HashSet<string> Disabled) LoadProfile()
        {
            try
            {
                if (File.Exists(ProfilePath) && JsonNode.Parse(File.ReadAllText(ProfilePath)) is JsonObject profile)
                    return (Set(profile["enabled"]), Set(profile["disabled"]));
            }
            catch
            {
                // Broken: every patch at its file's default.
            }
            return (new HashSet<string>(), new HashSet<string>());

            static HashSet<string> Set(JsonNode? node) =>
                node?.AsArray().Select(n => n?.GetValue<string>() ?? "").ToHashSet() ?? new HashSet<string>();
        }

        public static bool IsOn(Patch patch, (HashSet<string> Enabled, HashSet<string> Disabled) profile) =>
            profile.Enabled.Contains(patch.Key) || (patch.DefaultOn && !profile.Disabled.Contains(patch.Key));

        /// <summary>Records the switches that differ from each file's isEnabled; patches of other
        /// folders keep their saved choice.</summary>
        public static void Save(IEnumerable<(Patch Patch, bool On)> shown)
        {
            var list = shown.ToList();
            var keys = list.Select(p => p.Patch.Key).ToHashSet();
            var (enabled, disabled) = LoadProfile();
            enabled.RemoveWhere(keys.Contains);
            disabled.RemoveWhere(keys.Contains);
            foreach (var (patch, on) in list)
            {
                if (on == patch.DefaultOn) continue;
                (on ? enabled : disabled).Add(patch.Key);
            }

            var profile = new JsonObject
            {
                ["enabled"] = new JsonArray(enabled.OrderBy(k => k, StringComparer.Ordinal).Select(k => (JsonNode?)k).ToArray()),
                ["disabled"] = new JsonArray(disabled.OrderBy(k => k, StringComparer.Ordinal).Select(k => (JsonNode?)k).ToArray()),
            };
            Directory.CreateDirectory(Paths.DataDir);
            File.WriteAllText(ProfilePath, profile.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        }
    }
}

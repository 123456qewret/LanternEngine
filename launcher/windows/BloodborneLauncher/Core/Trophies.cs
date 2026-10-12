using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BloodborneLauncher.Core
{
    public enum Grade { Platinum, Gold, Silver, Bronze }

    public sealed record Trophy(int Id, string Name, string Description, Grade Grade, bool Dlc)
    {
        /// <summary>PlayStation trophy points, which weight the completion rate.</summary>
        public int Points => Grade switch { Grade.Platinum => 180, Grade.Gold => 90, Grade.Silver => 30, _ => 15 };
    }

    /// <summary>
    /// Bloodborne's 40 trophies (the PS4 ids of CUSA03173) and the unlocks the game made.
    ///
    /// A runtime with trophy support (github.com/pyL1nx/bloodborne_pc) handles
    /// sceNpTrophyUnlockTrophy by appending "id unix-time" to &lt;saves&gt;\trophies.log and printing
    /// "Runtime: trophy &lt;id&gt; unlocked". The tracker reads both (the output line also from
    /// last_run.log, for games started without the launcher) and records what it finds in the same
    /// file, the same way that launcher does, so both agree. The platinum is the console's job: it is
    /// awarded here once every base-game trophy is unlocked (The Old Hunters does not count).
    /// </summary>
    public sealed class TrophyTracker
    {
        public const string LogName = "trophies.log";
        public const int PlatinumId = 0;

        public static readonly Trophy[] All =
        {
            new(0, "Bloodborne", "All trophies acquired. Hats off!", Grade.Platinum, false),
            new(1, "Yharnam Sunrise", "You lived through the hunt, and saw another day.", Grade.Gold, false),
            new(2, "Honoring Wishes", "Captivated by the moon presence, you pledge to watch over the hunter's dream.", Grade.Gold, false),
            new(3, "Childhood's Beginning", "You became an infant Great One, lifting humanity into its next childhood.", Grade.Gold, false),
            new(4, "Yharnam, Pthumerian Queen", "Defeat Yharnam, Blood Queen of the Old Labyrinth.", Grade.Gold, false),
            new(5, "Hunter's Essence", "Acquire all hunter weapons.", Grade.Gold, false),
            new(6, "Hunter's Craft", "Acquire all special hunter tools.", Grade.Gold, false),
            new(7, "Weapon Master", "Acquire a weapon of the highest level.", Grade.Silver, false),
            new(8, "Blood Gem Master", "Acquire an extremely precious blood gem.", Grade.Silver, false),
            new(9, "Rune Master", "Acquire an extremely precious Caryll Rune.", Grade.Silver, false),
            new(10, "Cainhurst", "Gain entry to Cainhurst, the lost and ruined castle.", Grade.Silver, false),
            new(11, "The Choir", "Gain entry to the realm of the Choir, the high stratum of the Healing Church.", Grade.Silver, false),
            new(12, "The Source of the Dream", "Discover the abandoned old workshop, the source of the hunter's dream.", Grade.Silver, false),
            new(13, "Nightmare Lecture Building", "Gain entry into the Byrgenwerth lecture building, that drifts within the realm of nightmare.", Grade.Silver, false),
            new(14, "Father Gascoigne", "Defeat the beast that once was Father Gascoigne.", Grade.Bronze, false),
            new(15, "Vicar Amelia", "Defeat the beast that once was Vicar Amelia.", Grade.Bronze, false),
            new(16, "Shadow of Yharnam", "Defeat the Shadow of Yharnam.", Grade.Bronze, false),
            new(17, "Rom, the Vacuous Spider", "Defeat Great One: Rom, the Vacuous Spider.", Grade.Bronze, false),
            new(18, "The One Reborn", "Defeat the One Reborn.", Grade.Bronze, false),
            new(19, "Micolash, Host of the Nightmare", "Defeat Micolash, Host of the Nightmare.", Grade.Bronze, false),
            new(20, "Mergo's Wet Nurse", "Defeat Great One: Mergo's Wet Nurse.", Grade.Bronze, false),
            new(21, "Cleric Beast", "Defeat Cleric Beast.", Grade.Bronze, false),
            new(22, "Blood-starved Beast", "Defeat Blood-starved Beast.", Grade.Bronze, false),
            new(23, "The Witch of Hemwick", "Defeat the Witch of Hemwick.", Grade.Bronze, false),
            new(24, "Darkbeast Paarl", "Defeat Darkbeast Paarl.", Grade.Bronze, false),
            new(25, "Amygdala", "Defeat Great One: Amygdala.", Grade.Bronze, false),
            new(26, "Martyr Logarius", "Defeat Martyr Logarius.", Grade.Bronze, false),
            new(27, "Celestial Emissary", "Defeat Great One: Celestial Emissary.", Grade.Bronze, false),
            new(28, "Ebrietas, Daughter of the Cosmos", "Defeat Great One: Ebrietas, Daughter of the Cosmos.", Grade.Bronze, false),
            new(29, "Blood Gem Contact", "Acquire a blood gem that imbues hunter weapons with special strength.", Grade.Bronze, false),
            new(30, "Rune Contact", "Acquire a Caryll Rune that endows hunters with special strength.", Grade.Bronze, false),
            new(31, "Chalice of Pthumeru", "Acquire the Chalice of Pthumeru that seals the catacombs that form a web deep below Yharnam.", Grade.Bronze, false),
            new(32, "Chalice of Ailing Loran", "Acquire the Chalice of Ailing Loran that seals the tragic land lost to the sands.", Grade.Bronze, false),
            new(33, "Chalice of Isz", "Acquire the Great Chalice of Isz that seals the home of the cosmic kin.", Grade.Bronze, false),
            new(34, "Old Hunter's Essence", "Acquire all old hunter weapons.", Grade.Gold, true),
            new(35, "Orphan of Kos", "Defeat Great One: Orphan of Kos.", Grade.Silver, true),
            new(36, "Ludwig, the Holy Blade", "Defeat the beast that was once Ludwig, the Holy Blade.", Grade.Bronze, true),
            new(37, "Lady Maria of the Astral Clocktower", "Defeat Lady Maria of the Astral Clocktower.", Grade.Bronze, true),
            new(38, "Living Failures", "Defeat the failed attempts to become Great Ones.", Grade.Bronze, true),
            new(39, "Laurence, the First Vicar", "Defeat the beast that was once Laurence, the First Vicar.", Grade.Bronze, true),
        };

        public static readonly Dictionary<int, Trophy> ById = All.ToDictionary(t => t.Id);
        private static readonly int[] PlatinumNeeds = All.Where(t => !t.Dlc && t.Id != PlatinumId).Select(t => t.Id).ToArray();
        private static readonly Regex UnlockLine = new(@"Runtime: trophy (\d+) unlocked", RegexOptions.Compiled);
        // Trophies a save already had (the engine's trophy_sync.cpp: bosses it had defeated), as an
        // imported save reports them: recorded without a notification each.
        private static readonly Regex SaveLine = new(@"Runtime: trophy (\d+) earned in this save", RegexOptions.Compiled);

        private string _userDir = "";
        private (long, DateTime)? _logStamp, _runStamp;
        private long _runOffset;
        private byte[] _runTail = Array.Empty<byte>();

        /// <summary>Trophy id → unlock time (unix seconds).</summary>
        public Dictionary<int, long> Unlocked { get; } = new();

        public string LogPath => Path.Combine(_userDir, LogName);
        private string RunLogPath => Path.Combine(_userDir, "last_run.log");

        /// <summary>Switches to another saves folder; what is already there is read without announcing it.</summary>
        public void SetUserDir(string userDir)
        {
            if (string.Equals(_userDir, userDir, StringComparison.OrdinalIgnoreCase)) return;
            _userDir = userDir;
            Unlocked.Clear();
            _logStamp = _runStamp = null;
            _runOffset = 0;
            _runTail = Array.Empty<byte>();
            Poll(readRunLog: true);
        }

        /// <summary>
        /// Starts trophy progress over: trophies.log is kept as a dated backup and emptied, and what
        /// last_run.log already holds is treated as read, so old unlocks don't come back. Returns the
        /// backup's path, or null when there was nothing to reset.
        /// </summary>
        public string? Reset()
        {
            string? backup = null;
            if (File.Exists(LogPath))
            {
                backup = Path.Combine(_userDir, $"trophies.{DateTime.Now:yyyyMMdd-HHmmss}.log.bak");
                File.Move(LogPath, backup);
            }
            Unlocked.Clear();
            _logStamp = Stamp(LogPath);
            try
            {
                var run = File.Exists(RunLogPath) ? File.ReadAllBytes(RunLogPath) : Array.Empty<byte>();
                _runOffset = run.Length;
                _runTail = run[Math.Max(0, run.Length - 64)..];
                _runStamp = Stamp(RunLogPath);
            }
            catch
            {
                // Unreadable run log: it is read again from the start, as on any other start.
            }
            return backup;
        }

        // ---------------------------------------------------------------- progress

        public int Count => Unlocked.Keys.Count(ById.ContainsKey);
        public int TotalPoints => All.Sum(t => t.Points);
        public int EarnedPoints => All.Where(t => Unlocked.ContainsKey(t.Id)).Sum(t => t.Points);

        /// <summary>Completion as PlayStation shows it: earned trophy points over all points.</summary>
        public double Completion => (double)EarnedPoints / TotalPoints;

        public (int Earned, int Total) Progress(bool dlc) =>
            (All.Count(t => t.Dlc == dlc && Unlocked.ContainsKey(t.Id)), All.Count(t => t.Dlc == dlc));

        // ---------------------------------------------------------------- reading

        /// <summary>New unlocks in trophies.log (and last_run.log), oldest first.</summary>
        public List<int> Poll(bool readRunLog)
        {
            var found = RefreshLog();
            if (readRunLog)
                found.AddRange(ReadRunLog());
            found.AddRange(AwardPlatinum());
            return found;
        }

        /// <summary>A line of the running game's output.</summary>
        public List<int> Observe(string line)
        {
            var ids = UnlockLine.Matches(line).Select(m => int.Parse(m.Groups[1].Value)).ToList();
            return ids.Count == 0 ? new List<int>() : Record(ids, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        /// <summary>A line of the running game's output: trophies the loaded save had already earned.</summary>
        public List<int> ObserveSave(string line)
        {
            var ids = SaveLine.Matches(line).Select(m => int.Parse(m.Groups[1].Value)).ToList();
            return ids.Count == 0 ? new List<int>() : Record(ids, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        private static (long, DateTime)? Stamp(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists ? (info.Length, info.LastWriteTimeUtc) : null;
            }
            catch
            {
                return null;
            }
        }

        private List<int> RefreshLog()
        {
            var found = new List<int>();
            var stamp = Stamp(LogPath);
            if (stamp == _logStamp) return found;
            _logStamp = stamp;

            var read = new Dictionary<int, long>();
            try
            {
                using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                while (reader.ReadLine() is { } line)
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[0], out var id) && long.TryParse(parts[1], out var when))
                        if (!read.TryGetValue(id, out var seen) || when < seen)
                            read[id] = when;
                }
            }
            catch
            {
                return found;
            }

            foreach (var (id, when) in read.OrderBy(p => p.Value))
            {
                if (!Unlocked.ContainsKey(id)) found.Add(id);
                if (!Unlocked.TryGetValue(id, out var seen) || when < seen) Unlocked[id] = when;
            }
            return found;
        }

        private List<int> ReadRunLog()
        {
            var stamp = Stamp(RunLogPath);
            if (stamp == null || stamp == _runStamp) return new List<int>();
            _runStamp = stamp;
            try
            {
                using var stream = new FileStream(RunLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                // A new run rewrites the file: then the bytes before the offset differ.
                if (_runOffset > 0 && _runOffset <= stream.Length)
                {
                    stream.Seek(_runOffset - _runTail.Length, SeekOrigin.Begin);
                    var check = new byte[_runTail.Length];
                    stream.ReadExactly(check);
                    if (!check.AsSpan().SequenceEqual(_runTail)) _runOffset = 0;
                }
                else if (_runOffset > stream.Length)
                    _runOffset = 0;

                stream.Seek(_runOffset, SeekOrigin.Begin);
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                var chunk = memory.ToArray();
                // Only whole lines: the rest is read again once the game has written it.
                var end = Array.LastIndexOf(chunk, (byte)'\n') + 1;
                if (end == 0) return new List<int>();
                _runOffset += end;
                _runTail = chunk[Math.Max(0, end - 64)..end];

                var text = Encoding.UTF8.GetString(chunk, 0, end);
                var when = new DateTimeOffset(stamp.Value.Item2).ToUnixTimeSeconds();
                var found = new List<int>();
                // Trophies a save already had: recorded without a notification each; only a
                // platinum they complete is announced.
                var saved = SaveLine.Matches(text).Select(m => int.Parse(m.Groups[1].Value)).ToList();
                if (saved.Count > 0) found.AddRange(Record(saved, when).Where(id => id == PlatinumId));
                var ids = UnlockLine.Matches(text).Select(m => int.Parse(m.Groups[1].Value)).ToList();
                if (ids.Count > 0) found.AddRange(Record(ids, when));
                return found;
            }
            catch
            {
                return new List<int>();
            }
        }

        /// <summary>Unlocks seen in output: added to trophies.log unless already known.</summary>
        private List<int> Record(IEnumerable<int> ids, long when)
        {
            var found = RefreshLog();
            foreach (var id in ids)
            {
                if (Unlocked.ContainsKey(id)) continue;
                Unlocked[id] = when;
                Append(id, when);
                found.Add(id);
            }
            found.AddRange(AwardPlatinum());
            return found;
        }

        private List<int> AwardPlatinum()
        {
            if (Unlocked.ContainsKey(PlatinumId) || !PlatinumNeeds.All(Unlocked.ContainsKey))
                return new List<int>();
            var when = PlatinumNeeds.Max(id => Unlocked[id]);
            Unlocked[PlatinumId] = when;
            Append(PlatinumId, when);
            return new List<int> { PlatinumId };
        }

        private void Append(int id, long when)
        {
            try
            {
                Directory.CreateDirectory(_userDir);
                File.AppendAllText(LogPath, $"{id} {when}\n");
                _logStamp = Stamp(LogPath);
            }
            catch
            {
                // Read-only saves folder: the unlock still shows for this session.
            }
        }

        /// <summary>Whether the port's bb-probe.exe reports unlocks at all (its log line is in the exe).</summary>
        public static bool RuntimeReportsTrophies(string probe)
        {
            try
            {
                var needle = Encoding.ASCII.GetBytes("trophy %d unlocked");
                var bytes = File.ReadAllBytes(probe);
                return bytes.AsSpan().IndexOf(needle) >= 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

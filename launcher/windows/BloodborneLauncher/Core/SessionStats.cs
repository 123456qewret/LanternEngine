using System.Globalization;
using System.Text.RegularExpressions;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// One play session from the engine's output: how long it ran, the frame rate (from the
    /// "Frame stats:" lines, printed with Frame statistics on) and the stutters ("Stall:" lines).
    /// Playtime adds up per player profile in the settings (playtime.&lt;profile&gt;, seconds).
    /// </summary>
    public sealed class SessionStats
    {
        private static readonly Regex FrameStats = new(@"^Frame stats: ([\d.]+) FPS, worst frame ([\d.]+) ms", RegexOptions.Compiled);
        private static readonly Regex Stall = new(@"^Stall: ([\d.]+) ms frame", RegexOptions.Compiled);

        private readonly List<double> _fps = new();

        public DateTime Started { get; } = DateTime.Now;
        public TimeSpan Length { get; private set; }
        public double WorstFrameMs { get; private set; }
        public int Stutters { get; private set; }
        public double AverageFps => _fps.Count > 0 ? _fps.Average() : 0;
        public double LowestFps => _fps.Count > 0 ? _fps.Min() : 0;
        public bool HasFrameStats => _fps.Count > 0;

        public void Observe(string line)
        {
            if (line.StartsWith("Frame stats: ", StringComparison.Ordinal) && FrameStats.Match(line) is { Success: true } m)
            {
                _fps.Add(Parse(m.Groups[1].Value));
                WorstFrameMs = Math.Max(WorstFrameMs, Parse(m.Groups[2].Value));
            }
            else if (line.StartsWith("Stall: ", StringComparison.Ordinal) && Stall.Match(line) is { Success: true } s)
            {
                Stutters++;
                WorstFrameMs = Math.Max(WorstFrameMs, Parse(s.Groups[1].Value));
            }
        }

        public void Finish() => Length = DateTime.Now - Started;

        /// <summary>"1 h 12 min · 112 FPS average (lowest 98) · worst frame 107 ms · 3 stutters".</summary>
        public string Summary =>
            Duration(Length) + (HasFrameStats
                ? $" · {AverageFps:0} FPS average (lowest {LowestFps:0}) · worst frame {WorstFrameMs:0} ms · " +
                  (Stutters == 1 ? "1 stutter" : $"{Stutters} stutters")
                : " · turn on Frame statistics (More › Advanced) for FPS and stutters");

        public static string Duration(TimeSpan time) =>
            time.TotalHours >= 1 ? $"{(int)time.TotalHours} h {time.Minutes} min" :
            time.TotalMinutes >= 1 ? $"{(int)time.TotalMinutes} min" : $"{Math.Max(0, (int)time.TotalSeconds)} s";

        public static TimeSpan Playtime(LauncherSettings s, string profile) =>
            TimeSpan.FromSeconds(Math.Max(0, s.Int(Key(profile))));

        public static void AddPlaytime(LauncherSettings s, string profile, TimeSpan time) =>
            s.Set(Key(profile), (int)Math.Min(int.MaxValue, Playtime(s, profile).TotalSeconds + time.TotalSeconds));

        private static string Key(string profile) => "playtime." + profile.ToLowerInvariant();

        private static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    }
}

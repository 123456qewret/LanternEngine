namespace BloodborneLauncher.Core
{
    /// <summary>A drop-down entry. Note is shown under the row while this entry is selected.</summary>
    public sealed record Choice(string Label, string Value, string? Note = null);

    public sealed record Effect(string Key, string Title, bool Default, string? Hint = null);

    /// <summary>Every list the launcher offers, in English, with the values bbport expects.</summary>
    public static class Choices
    {
        public static readonly Choice[] Upscalers =
        {
            new("FSR 4", "fsr4"),
            new("FSR 4.1.1", "fsr411"),
            new("FSR 3", "fsr3"),
            new("DLSS (NVIDIA RTX)", "dlss"),
            new("TAA (native anti-aliasing)", "taa"),
            new("Off", "off"),
        };

        public static readonly Choice[] Presets =
        {
            new("Native AA", "0"),
            new("Quality (x1.5)", "1"),
            new("Balanced (x1.7)", "2"),
            new("Performance (x2)", "3"),
            new("Ultra Performance (x3)", "4"),
        };

        public static readonly Choice[] OutputResolutions =
        {
            new("1280 × 720 (Steam Deck)", "1280x720"),
            new("1920 × 1080", "1920x1080"),
            new("2560 × 1440", "2560x1440"),
            new("3840 × 2160", "3840x2160"),
        };

        public static readonly Effect[] Effects =
        {
            new("effect_chromatic_aberration", "Chromatic aberration", true),
            new("effect_dof", "Depth of field", true),
            new("effect_motion_blur", "Motion blur", true),
            new("effect_ssao", "Ambient occlusion (SSAO)", true),
            new("effect_game_aa", "Game's own anti-aliasing", true),
            new("effect_dynamic_shadows", "Shadows from dynamic lights", true),
            new("effect_ssr", "Screen-space reflections", false, "Not in the original game"),
            new("skip_intro", "Skip intro videos", false),
            new("debug_camera", "Free camera", false, "Toggle with Cross + L3 / Space + Z"),
            new("debug_menu", "Debug menu", false,
                "Left touchpad / Tab. Needs the adhoc folder from Nexus mod #253 (adhoc/font) in the game's dvdroot_ps4 or as a mod; without it the patch is not applied"),
        };

        public static readonly Choice[] ModelLod =
        {
            new("As in the game", "0"),
            new("Highest (-2)", "-2"),
            new("Lower (1)", "1"),
            new("Lowest (2)", "2"),
        };

        public static readonly Choice[] FpsModes =
        {
            new("60 FPS", "60"),
            new("90 FPS", "90", "Includes the sprint fix for high frame rates"),
            new("120 FPS", "120", "The uncapped patch with a 120 FPS limit"),
            new("144 FPS", "144", "The uncapped patch with a 144 FPS limit"),
            new("Uncapped", "uncap", "No limit: as fast as your PC runs it. Use VSync (FIFO) to keep it to your monitor"),
            new("30 FPS (as on PS4)", "30"),
        };

        /// <summary>The port's in-game settings menu (Insert / L3+R3).</summary>
        public static readonly Choice[] MenuLanguages =
        {
            new("English", "en"),
            new("Russian", "ru"),
        };

        public static readonly Choice[] PresentModes =
        {
            new("Mailbox", "Mailbox", "Low latency without tearing"),
            new("FIFO (VSync)", "Fifo", "Classic vertical sync"),
            new("FIFO Relaxed", "FifoRelaxed", "VSync that tears instead of stuttering when a frame is late"),
            new("Immediate", "Immediate", "Lowest latency; may tear"),
        };

        public static readonly Choice[] DrawPipe =
        {
            new("Auto", "", "On with 8 or more CPU threads"),
            new("On", "1"),
            new("Off", "0", "More stable, but slower"),
        };

        public static readonly Choice[] GameLanguages =
        {
            new("English", "1"),
            new("Russian", "8"),
            new("Japanese", "0"),
            new("French", "2"),
            new("Spanish", "3"),
            new("German", "4"),
            new("Italian", "5"),
        };

        public static readonly Choice[] LiveResolution =
        {
            new("Auto (by GPU)", "auto"),
            new("Off (faster)", "0"),
            new("On", "1"),
        };

        public static readonly Choice[] Readbacks =
        {
            new("Relaxed", "", "Default"),
            new("Off", "0"),
            new("Precise", "2"),
        };

        public static readonly Choice[] LaunchMethods =
        {
            new("Bloodborne PC engine", "windows", "Prepares mods and patches, then starts the engine (out\\bb-probe.exe)"),
            new("Custom command", "custom", "Runs a program of your choice with the settings as environment variables"),
        };

        public static int IndexOf(IReadOnlyList<Choice> choices, string value)
        {
            for (var i = 0; i < choices.Count; i++)
                if (choices[i].Value == value)
                    return i;
            return 0;
        }
    }
}

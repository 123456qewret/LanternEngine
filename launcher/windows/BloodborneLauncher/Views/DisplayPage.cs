using BloodborneLauncher.Core;
using Wpf.Ui.Controls;

namespace BloodborneLauncher.Views
{
    /// <summary>Screen, frame rate and the language of the port's in-game menu.</summary>
    public sealed class DisplayPage : LauncherPage
    {
        public DisplayPage() : base("Display", "Resolution, window mode, frame rate and menu language. Applied when the game starts.")
        {
            Group("Language");
            ComboRow(SymbolRegular.LocalLanguage24, "In-game menu language", "The port's settings menu (Insert or L3 + R3)",
                Choices.MenuLanguages, () => Ini.Get("menu_language", "en"), v => Ini.Set("menu_language", v));
            SettingCombo(SymbolRegular.Translate24, "Game language", "The game's own text and voices", Choices.GameLanguages, "language");

            Group("Screen");
            IniCombo(SymbolRegular.ResizeLarge24, "Output resolution",
                "The upscaler renders below this and fills it in", Choices.OutputResolutions, "output_res");
            IniCombo(SymbolRegular.ArrowSync24, "Live resolution change",
                "Change the resolution in the game menu without restarting. Off is faster; turn on only if you switch resolutions in game", Choices.LiveResolution, "live_resolution");
            IniToggle(SymbolRegular.FullScreenMaximize24, "Fullscreen", "Borderless, so trophy notifications show on top", "fullscreen");
            ToggleRow(SymbolRegular.RatioOneToOne24, "Widescreen", "21:9 and 32:9 outputs (3440 × 1440, 5120 × 1440) with the right field of view",
                () => Ini.Get("widescreen", "1") != "0", v => Ini.Set("widescreen", v ? "1" : "0"));
            SettingCombo(SymbolRegular.Image24, "Presentation mode", null, Choices.PresentModes, "present_mode");

            Group("HDR");
            var hdrHint = WindowsHdr.AnyOn()
                ? "The game's picture on an HDR screen, with brighter highlights. Applied when the game starts"
                : WindowsHdr.AnySupported()
                    ? "Turn on Use HDR in Windows' display settings first (Settings › System › Display)"
                    : "This display shows no HDR";
            IniToggle(SymbolRegular.Hdr24, "HDR output", hdrHint, "hdr");
            ComboRow(SymbolRegular.BrightnessHigh24, "Paper white", "How bright the game's white is. Match it to Windows' SDR content brightness",
                Choices.HdrPaperWhite, () => Ini.Get("hdr_paper_white", "200"), v => Ini.Set("hdr_paper_white", v));
            ComboRow(SymbolRegular.WeatherSunny24, "Peak brightness", "Your display's peak brightness; highlights stop there",
                Choices.HdrPeak, () => Ini.Get("hdr_peak", "1000"), v => Ini.Set("hdr_peak", v));
            ComboRow(SymbolRegular.Sparkle24, "Highlight boost", "Off keeps the game's own look; higher makes lamps, fire and the sky brighter",
                Choices.HdrBoost, () => Ini.Get("hdr_boost", "0.25"), v => Ini.Set("hdr_boost", v));

            Group("Frame rate");
            SettingCombo(SymbolRegular.Gauge24, "Frame rate", "30, 60 and 90 are fixed presets; 120 and Uncapped use the uncapped patch with the sprint fix", Choices.FpsModes, "fps_mode");
        }
    }
}

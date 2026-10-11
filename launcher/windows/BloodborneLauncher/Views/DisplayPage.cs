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
            IniCombo(SymbolRegular.ArrowSync24, "Change resolution in game",
                "Without restarting, but slower on older GPUs", Choices.LiveResolution, "live_resolution");
            IniToggle(SymbolRegular.FullScreenMaximize24, "Fullscreen", "Borderless, so trophy notifications show on top", "fullscreen");
            ToggleRow(SymbolRegular.RatioOneToOne24, "Widescreen", "21:9 and 32:9 outputs (3440 × 1440, 5120 × 1440) with the right field of view",
                () => Ini.Get("widescreen", "1") != "0", v => Ini.Set("widescreen", v ? "1" : "0"));
            SettingCombo(SymbolRegular.Image24, "Presentation mode", null, Choices.PresentModes, "present_mode");

            Group("Frame rate");
            SettingCombo(SymbolRegular.Gauge24, "Frame rate", "30, 60 and 90 are fixed presets; 120 and Uncapped use the uncapped patch with the sprint fix", Choices.FpsModes, "fps_mode");
        }
    }
}

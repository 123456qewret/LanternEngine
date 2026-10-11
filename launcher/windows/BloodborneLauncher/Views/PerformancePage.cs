using BloodborneLauncher.Core;
using Wpf.Ui.Controls;

namespace BloodborneLauncher.Views
{
    public sealed class PerformancePage : LauncherPage
    {
        public PerformancePage() : base("Performance", "How the port schedules GPU work and moves data. Applied when the game starts.")
        {
            Group("GPU work");
            SettingCombo(SymbolRegular.Flow20, "Two-stage GPU pipeline", "20–30% faster; turn off if the game is unstable",
                Choices.DrawPipe, "draw_pipe");
            SettingCombo(SymbolRegular.ArrowSwap24, "CPU reads of GPU data", null, Choices.Readbacks, "readbacks");
        }
    }

    public sealed class DeveloperPage : LauncherPage
    {
        public DeveloperPage() : base("Developer", "Diagnostics for tracking down stutters and crashes.")
        {
            Group("Logs");
            SettingToggle(SymbolRegular.TextBulletListSquare24, "Frame statistics in the log", "BB_FRAME_STATS", "frame_stats");
            SettingToggle(SymbolRegular.DeveloperBoard24, "GPU profile in the log", "BB_GPU_PROFILE", "gpu_profile");

            Group("Checks");
            SettingToggle(SymbolRegular.CheckboxChecked24, "Vulkan validation layers", "Much slower; needs the Vulkan SDK", "vk_validation");

            Group("Environment");
            TextRow(SymbolRegular.Code24, "Extra variables", "NAME=value pairs separated by spaces, passed to the game", "BB_EXAMPLE=1 OTHER=2",
                () => S.Str("extra_env"), v => S.Set("extra_env", v.Trim()), width: 380);
        }
    }
}

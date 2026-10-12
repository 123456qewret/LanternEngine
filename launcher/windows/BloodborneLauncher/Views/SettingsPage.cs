using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Build;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using TextBlock = System.Windows.Controls.TextBlock;

namespace BloodborneLauncher.Views
{
    /// <summary>The launcher itself: where the port is, how the game is started, and rebuilding.</summary>
    public sealed class SettingsPage : LauncherPage
    {
        private readonly TextBlock _portHint, _buildHint;
        private readonly StackPanel _customRows = new();
        private readonly TextBlock _updateHint;
        private readonly Wpf.Ui.Controls.Button _updateNow;

        public SettingsPage() : base("Settings", "The Bloodborne PC engine, how the game is started, and keeping this launcher up to date.")
        {
            Group("Appearance");
            FrameworkElement layout = null!;
            var reverting = false;
            layout = ComboRow(SymbolRegular.PanelLeft24, "Layout", "Modern: tabs and compact rows. Classic: the sidebar and one card per setting",
                new[] { new Choice("Modern", "modern"), new Choice("Classic", "classic") },
                () => S.Str("ui_layout") == "classic" ? "classic" : "modern", v => S.Set("ui_layout", v), changed: () =>
                {
                    if (reverting || RestartForLayout()) return;
                    // Cancelled: put back the layout that is showing, so the next start doesn't switch either.
                    reverting = true;
                    var shown = SettingRow.Classic ? "classic" : "modern";
                    S.Set("ui_layout", shown);
                    Ws.SaveNow();
                    var index = shown == "classic" ? 1 : 0;
                    if (layout is Segmented segmented) segmented.Select(index, notify: false);
                    else if (layout is ComboBox box) box.SelectedIndex = index;
                    reverting = false;
                });

            Group("Bloodborne PC");
            Add(Card(SymbolRegular.FolderLink24, "Engine folder", "", Row(
                SmallButton("Browse", SymbolRegular.FolderOpen24, (_, _) => ChoosePort()),
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(Paths.PortDir)),
                IconButton(SymbolRegular.ArrowUndo24, "Detect automatically", (_, _) =>
                {
                    S.Set("port_dir", "");
                    Ws.ResolvePort();
                    Ws.SaveSoon();
                    Refresh();
                })), out _portHint));

            Group("Launch");
            SettingCombo(SymbolRegular.Rocket24, "Start the game with", null, Choices.LaunchMethods, "launch_method", changed: () => { ShowMethod(); Ws.SaveSoon(); });

            TextRow(SymbolRegular.Code24, "Command", "A program to run instead of Bloodborne.exe --run, with the settings as environment variables",
                @"C:\path\to\game.exe", () => S.Str("custom_command"), v => S.Set("custom_command", v), _customRows, 380);
            TextRow(SymbolRegular.TextDescription24, "Arguments", null, "Optional",
                () => S.Str("custom_args"), v => S.Set("custom_args", v), _customRows, 380);
            Body.Children.Add(_customRows);

            Group("Performance");
            SettingCombo(SymbolRegular.Flow20, "Two-stage GPU pipeline", "20–30% faster; turn off if the game is unstable",
                Choices.DrawPipe, "draw_pipe");
            SettingCombo(SymbolRegular.ArrowSwap24, "CPU reads of GPU data", "Relaxed is the default", Choices.Readbacks, "readbacks");

            Group("Diagnostics");
            SettingToggle(SymbolRegular.TextBulletListSquare24, "Frame statistics in the log", "BB_FRAME_STATS", "frame_stats");
            SettingToggle(SymbolRegular.DeveloperBoard24, "FSR 4 profile in the log", "BB_FSR4_PROFILE", "fsr_profile");
            SettingToggle(SymbolRegular.Speaker224, "Audio statistics in the log", "BB_AUDIO_STATS", "audio_stats");
            SettingToggle(SymbolRegular.Timer24, "Render stage timings in the log", "BB_SECTIONS: where each frame's CPU time goes (with frame statistics)", "section_stats");
            TextRow(SymbolRegular.Code24, "Extra variables", "NAME=value pairs separated by spaces, passed to the game", "BB_EXAMPLE=1",
                () => S.Str("extra_env"), v => S.Set("extra_env", v.Trim()), width: 300);

            Group("Files");
            Add(Card(SymbolRegular.DocumentSettings20, "Launcher settings", Paths.ConfigFile,
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(Paths.ConfigDir)), out _));
            Add(Card(SymbolRegular.DocumentSettings20, "Port settings (bbport.ini)", Paths.IniPath,
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(Path.GetDirectoryName(Paths.IniPath)!)), out _));

            Group("Updates");
            _updateNow = SmallButton("Update now", SymbolRegular.ArrowDownload24, async (_, _) => await MainWindow.Instance!.InstallUpdateAsync(),
                ControlAppearance.Primary);
            Add(Card(SymbolRegular.ArrowDownload24, "Launcher updates", "", Row(
                SmallButton("Check now", SymbolRegular.ArrowClockwise24, async (_, _) => await Ws.CheckForUpdatesAsync(quiet: false)),
                _updateNow,
                IconButton(SymbolRegular.Open24, "Open the releases page", (_, _) =>
                    Process.Start(new ProcessStartInfo(Updater.ReleasesPage) { UseShellExecute = true }))), out _updateHint));
            ComboRow(SymbolRegular.Branch24, "Update channel",
                "Stable: releases that have been tested. Beta: every change as soon as it is done, before it is tested",
                new[] { new Choice("Stable", "stable"), new Choice("Beta", "beta") },
                () => Updater.BetaChannel(S) ? "beta" : "stable",
                v =>
                {
                    if (v == (Updater.BetaChannel(S) ? "beta" : "stable")) return;
                    S.Set("update_channel", v);
                    Ws.SaveSoon();
                    Ws.Log(v == "beta" ? "Update channel: beta." : "Update channel: stable.", LogLevel.Step);
                    _ = Ws.CheckForUpdatesAsync(quiet: false);
                });
            SettingToggle(SymbolRegular.Alert24, "Check for updates at start", $"Looks at github.com/{Updater.Repository}/releases", "check_updates");
            Ws.UpdateStateChanged += UpdateUpdates;
            UpdateUpdates();

            // Only a copy built from the source checkout can rebuild itself.
            var build = Card(SymbolRegular.ArrowSync24, "Build", "", Row(
                SmallButton("Rebuild", SymbolRegular.ArrowClockwise24, (_, _) => Rebuild(force: false)),
                SmallButton("Force rebuild", SymbolRegular.Wrench24, (_, _) => Rebuild(force: true))), out _buildHint);
            if (Paths.SourceRoot != null)
            {
                Group("Build from source");
                Add(build);
            }

            Ws.PortChanged += Refresh;
        }

        public override void OnShown()
        {
            Refresh();
            ShowMethod();
        }

        private void Refresh()
        {
            SetHint(_portHint, Paths.IsPortDir(Paths.PortDir)
                ? Paths.PortDir + (Paths.DetectPortDir() == Paths.PortDir ? "  ·  the package this launcher is in" : "") + "\nThe engine that runs your game: out\\bb-probe.exe, its scripts and Python. Your game files stay where they are"
                : "Not found. It is normally the folder this launcher is in (with bin\\bb-probe.exe and run.py)");
            SetHint(_buildHint, BuildStatus());
        }

        private void UpdateUpdates()
        {
            var update = Ws.Update;
            var latest = Ws.Latest;
            _updateNow.Visibility = update != null ? Visibility.Visible : Visibility.Collapsed;
            SetHint(_updateHint,
                update != null ? $"{update.Name} is available (you have {Updater.CurrentLabel})" +
                                 (update.IsNewerThan(Updater.Current, Updater.CurrentBeta) ? "" : ": back to stable")
                : Ws.CheckedAt == null ? $"You have {Updater.CurrentLabel}"
                : latest == null ? $"You have {Updater.CurrentLabel}; no release is published yet"
                : $"You have {Updater.CurrentLabel}, the latest ({latest.Tag})");
        }

        /// <summary>Restarts into the chosen layout; false if the player cancelled.</summary>
        private static bool RestartForLayout()
        {
            if (!Prompt.Confirm("Switch layout", "The launcher restarts to change its layout.", "Restart now")) return false;
            Ws.SaveNow();
            if (Environment.ProcessPath is { } self)
                Process.Start(new ProcessStartInfo(self) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(self)! });
            Application.Current.Shutdown();
            return true;
        }

        private void ShowMethod() =>
            _customRows.Visibility = GameRunner.Custom(S) ? Visibility.Visible : Visibility.Collapsed;

        private void ChoosePort()
        {
            var path = PickFolder("Engine folder (with run.py and bin\\bb-probe.exe)", Paths.PortDir);
            if (path == null) return;
            if (!Paths.IsPortDir(path))
            {
                System.Windows.MessageBox.Show(Application.Current.MainWindow!, "That folder has no run.py with Bloodborne.exe or bin\\bb-probe.exe.",
                    "Engine folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            S.Set("port_dir", path);
            Ws.ResolvePort();
            Ws.SaveSoon();
            Refresh();
        }

        // ---------------------------------------------------------------- rebuild

        private static string? BuilderPath()
        {
            var beside = Path.Combine(AppContext.BaseDirectory, SourceStamp.BuilderExe);
            if (File.Exists(beside)) return beside;
            if (Paths.SourceRoot is { } root)
            {
                var installed = Path.Combine(SourceStamp.AppDirOf(root), SourceStamp.BuilderExe);
                if (File.Exists(installed)) return installed;
            }
            return null;
        }

        private static string BuildStatus()
        {
            var version = typeof(SettingsPage).Assembly.GetName().Version;
            var root = Paths.SourceRoot;
            if (root == null)
                return $"Version {version?.ToString(3)}. The sources (launcher/windows) are not next to this copy, so it cannot rebuild itself.";

            var stamp = SourceStamp.Read(SourceStamp.AppDirOf(root));
            var current = SourceStamp.Compute(root, SourceStamp.LauncherProject);
            var state = stamp.Launcher == current
                ? "Up to date with the sources"
                : "The sources changed since this build: rebuild to pick the changes up";
            var built = stamp.Built == default ? "" : $" · built {stamp.Built.ToLocalTime():yyyy-MM-dd HH:mm}";
            return $"Version {version?.ToString(3)}{built}. {state}.";
        }

        private void Rebuild(bool force)
        {
            var builder = BuilderPath();
            if (builder == null)
            {
                System.Windows.MessageBox.Show(Application.Current.MainWindow!,
                    $"{SourceStamp.BuilderExe} was not found next to the launcher.", "Rebuild", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Ws.Game.Running)
            {
                System.Windows.MessageBox.Show(Application.Current.MainWindow!, "Stop the game before rebuilding the launcher.",
                    "Rebuild", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Ws.SaveNow();
            var args = $"--wait-pid {Environment.ProcessId}" + (force ? " --force" : "");
            Process.Start(new ProcessStartInfo(builder, args) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(builder)! });
            Application.Current.Shutdown();
        }
    }
}

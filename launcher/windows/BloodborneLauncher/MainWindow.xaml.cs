using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BloodborneLauncher.Core;
using BloodborneLauncher.Views;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace BloodborneLauncher
{
    public partial class MainWindow
    {
        private readonly ObservableCollection<LogItem> _log = new();
        private readonly Dictionary<Type, LauncherPage> _pages = new();
        private bool _logExpanded;
        private bool _closing;

        public static MainWindow? Instance => Application.Current.MainWindow as MainWindow;

        public MainWindow()
        {
            InitializeComponent();

            LogList.ItemsSource = _log;
            var ws = Workspace.Current;
            ws.Logged += OnLogged;
            ws.Saved += () => SavedText.Text = $"Saved {DateTime.Now:HH:mm:ss}";
            ws.GameStateChanged += UpdatePlayButton;
            ws.Initialise();

            var version = typeof(MainWindow).Assembly.GetName().Version;
            VersionText.Text = version == null ? "" : $"v{version.Major}.{version.Minor}.{version.Build}";

            ws.UpdateStateChanged += () =>
            {
                TitleUpdate.Visibility = ws.Update != null ? Visibility.Visible : Visibility.Collapsed;
                if (ws.Update != null) TitleUpdateText.Text = $"Update {ws.Update.Tag}";
            };

            Loaded += async (_, _) =>
            {
                Updater.CleanUp();
                if (ws.Settings.Bool("check_updates"))
                    _ = ws.CheckForUpdatesAsync(quiet: true);
                await Task.Yield();
                PlayItem.IsChecked = true;
                UpdatePlayButton();
                // --test-trophy: a sample notification, to check how it looks over the game.
                if (Environment.GetCommandLineArgs().Contains("--test-trophy"))
                    TrophyToast.Announce(TrophyTracker.ById[14]);
                ws.Log(Paths.IsPortDir(Paths.PortDir)
                        ? $"Bloodborne Launcher ready. Engine: {Paths.PortDir}"
                        : "Bloodborne Launcher ready. Choose the engine folder in Settings.",
                    Paths.IsPortDir(Paths.PortDir) ? LogLevel.Good : LogLevel.Warn);
            };
            Closing += OnClosing;
        }

        /// <summary>A wider resize border than FluentWindow's 4px, and the top strip handed back
        /// to Windows as a real caption.</summary>
        protected override void SetWindowChrome()
        {
            base.SetWindowChrome();
            if (System.Windows.Shell.WindowChrome.GetWindowChrome(this) is { } chrome)
            {
                if (ResizeMode != ResizeMode.NoResize)
                    chrome.ResizeBorderThickness = new Thickness(8);
                chrome.CaptionHeight = 34;
            }
        }

        private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Workspace.Current.SaveNow();
            if (_closing || !Workspace.Current.Game.Running)
                return;

            var answer = System.Windows.MessageBox.Show(this,
                "The game is still running. Stop it and close the launcher?",
                "Bloodborne Launcher", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (answer == MessageBoxResult.Yes)
            {
                e.Cancel = true;
                _closing = true;
                await Workspace.Current.StopGameAsync();
                Close();
            }
        }

        // ---------------------------------------------------------------- window buttons

        private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();
        private void MinimizeWindow_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        // ---------------------------------------------------------------- navigation

        private void SidebarItem_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.RadioButton { Tag: Type pageType } item)
                return;

            if (!_pages.TryGetValue(pageType, out var page))
                _pages[pageType] = page = (LauncherPage)Activator.CreateInstance(pageType)!;
            PageFrame.Navigate(page);
            page.OnShown();

            var name = (item.Content as Panel)?.Children.OfType<System.Windows.Controls.TextBlock>().FirstOrDefault()?.Text;
            TitlePage.Text = string.IsNullOrEmpty(name) ? "" : $" — {name}";
        }

        public void Show(string page)
        {
            var item = page switch
            {
                "output" => OutputItem,
                "online" => OnlineItem,
                "controls" => ControlsItem,
                _ => PlayItem,
            };
            if (item.IsChecked == true)
                SidebarItem_Checked(item, new RoutedEventArgs());
            item.IsChecked = true;
        }

        private void PageFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            while (PageFrame.CanGoBack)
                PageFrame.RemoveBackEntry();
        }

        // ---------------------------------------------------------------- play

        private async void TitlePlay_Click(object sender, RoutedEventArgs e) => await PlayOrStopAsync();

        private async void TitleUpdate_Click(object sender, RoutedEventArgs e) => await InstallUpdateAsync();

        /// <summary>Asks, downloads the newer launcher, swaps it in and restarts.</summary>
        public async Task InstallUpdateAsync()
        {
            var ws = Workspace.Current;
            if (ws.Update is not { } release) return;
            var notes = release.Notes.Trim();
            if (notes.Length > 1200) notes = notes[..1200] + "…";
            var body = $"You have v{Updater.Current}. The launcher downloads {release.Tag}, replaces itself and restarts; " +
                       "your settings, profiles and trophies stay." +
                       (ws.Game.Running ? "\n\nThe game keeps running, but key remapping and trophy notifications pause until the launcher is back." : "") +
                       (notes.Length > 0 ? "\n\nWhat's new:\n" + notes : "");
            if (!Prompt.Confirm($"Update to {release.Name}?", body, "Update now"))
                return;

            TitleUpdate.IsEnabled = false;
            var progress = new Progress<double>(p => TitleUpdateText.Text = $"Downloading {p:P0}");
            try
            {
                ws.SaveNow();
                await Updater.InstallAsync(release, progress);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                TitleUpdate.IsEnabled = true;
                TitleUpdateText.Text = $"Update {release.Tag}";
                ws.Log("Update failed: " + ex.GetBaseException().Message, LogLevel.Bad);
                Prompt.Tell("Update failed", ex.GetBaseException().Message + $"\n\nYou can download it by hand from {Updater.ReleasesPage}");
            }
        }

        /// <summary>The one place the game is started from, so the title bar and the Play page agree.</summary>
        public async Task PlayOrStopAsync()
        {
            var ws = Workspace.Current;
            if (ws.Game.Running)
            {
                await ws.StopGameAsync();
                return;
            }

            var s = ws.Settings;
            var ini = ws.Ini;
            if (ini.Get("online_enabled") == "1")
            {
                // The engine refuses these at start; say so here instead.
                var problem = ini.Get("online_host").Trim().Length == 0 ? "Enter the server before playing online."
                    : ini.Get("online_username").Contains('@') ? "The account name is the name registered on the server, not an email address."
                    : ini.Get("online_username").Trim().Length == 0 ? "Enter the account name before playing online."
                    : s.Str("online_password").Length == 0 ? "Enter the account password before playing online."
                    : null;
                if (problem != null)
                {
                    Prompt.Tell("Online", problem + "\n\nOr turn \"Play online\" off to play offline.");
                    Show("online");
                    return;
                }
            }

            // No port folder yet: look for it again, then let the player point at it right here.
            if (!GameRunner.Custom(s) && !Paths.IsPortDir(Paths.PortDir))
            {
                ws.ResolvePort();
                if (!Paths.IsPortDir(Paths.PortDir))
                {
                    if (!Prompt.Confirm("Where is the engine?",
                            "The launcher needs the Bloodborne PC engine (the folder with run.py and bin\\bb-probe.exe). " +
                            "It is normally the folder the launcher came in.", "Choose folder"))
                        return;
                    var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Port folder (with Bloodborne.exe and run.py)" };
                    if (dialog.ShowDialog(this) != true) return;
                    if (!Paths.IsPortDir(dialog.FolderName))
                    {
                        Prompt.Tell("Not the port folder", "That folder has no run.py with Bloodborne.exe or bin\\bb-probe.exe.");
                        return;
                    }
                    s.Set("port_dir", dialog.FolderName);
                    ws.ResolvePort();
                    ws.SaveNow();
                }
            }

            var error = ws.StartGame();
            if (error != null)
            {
                System.Windows.MessageBox.Show(this, error, "Could not start the game", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (s.Bool("switch_to_output"))
                Show("output");
        }

        private void UpdatePlayButton()
        {
            var running = Workspace.Current.Game.Running;
            TitlePlayText.Text = running ? "Stop" : "Play";
            TitlePlayIcon.Symbol = running ? SymbolRegular.Stop24 : SymbolRegular.Play24;
            TitlePlayIcon.Foreground = (Brush)FindResource(running ? "DangerTextBrush" : "AccentTextFillColorPrimaryBrush");
            TitlePlay.ToolTip = running ? "Stop the game" : "Start the game";
            foreach (var page in _pages.Values)
                (page as PlayPage)?.UpdateRunning();
        }

        // ---------------------------------------------------------------- log

        private void OnLogged(string message, LogLevel level)
        {
            _log.Add(new LogItem { Time = DateTime.Now.ToString("HH:mm:ss"), Message = message, Color = BrushFor(level) });
            LogCount.Text = $"{_log.Count} events";
            LogScroller.ScrollToEnd();

            StatusText.Text = message;
            StatusText.ToolTip = message;
            StatusDot.Fill = level == LogLevel.Info ? (Brush)FindResource("StatusDotIdleBrush") : BrushFor(level);
        }

        private Brush BrushFor(LogLevel level) => (Brush)FindResource(level switch
        {
            LogLevel.Good => "GoodTextBrush",
            LogLevel.Bad => "DangerTextBrush",
            LogLevel.Warn => "WarningTextBrush",
            LogLevel.Step => "AccentTextFillColorPrimaryBrush",
            _ => "TextFillColorSecondaryBrush",
        });

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            _log.Clear();
            LogCount.Text = "";
            StatusText.Text = "Ready";
            StatusText.ToolTip = null;
            StatusDot.Fill = (Brush)FindResource("StatusDotIdleBrush");
        }

        private void ToggleLog_Click(object sender, RoutedEventArgs e) => ToggleLog();
        private void StatusBar_Click(object sender, MouseButtonEventArgs e) => ToggleLog();

        private void ToggleLog()
        {
            _logExpanded = !_logExpanded;
            LogPanel.Visibility = _logExpanded ? Visibility.Visible : Visibility.Collapsed;
            if (_logExpanded)
                LogScroller.ScrollToEnd();
        }
    }

    public class LogItem
    {
        public string Time { get; set; } = "";
        public string Message { get; set; } = "";
        public Brush Color { get; set; } = Brushes.White;
    }
}

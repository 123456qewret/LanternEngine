using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;

namespace BloodborneLauncher.Views
{
    /// <summary>Home: the game folder and its check, saves, language, a summary and the Play button.</summary>
    public sealed class PlayPage : LauncherPage
    {
        private readonly InfoBar _problem;
        private readonly SettingRow _engineCard;
        private readonly TextBlock _gameHint, _userHint, _statGame, _statUpscaler, _statResolution, _statFps, _statVia;
        private readonly SymbolIcon _gameIcon;
        private readonly Button _play, _resetUser;
        private readonly TextBlock _playStatus;
        private readonly DispatcherTimer _clock;

        public PlayPage() : base("Play", "Check the game folder, choose where saves go, and start Bloodborne.")
        {
            // ==================== Summary + Play ====================
            // A glass pane in the Modern layout, a card in Classic.
            var summary = new Border
            {
                Margin = new Thickness(0, 14, 0, 0),
                Padding = new Thickness(18, 16, 18, 16),
                CornerRadius = new CornerRadius(SettingRow.Classic ? 8 : 16),
                BorderThickness = new Thickness(1),
            };
            summary.SetResourceReference(Border.BackgroundProperty, "GlassPaneBrush");
            summary.SetResourceReference(Border.BorderBrushProperty, "GlassPaneStrokeBrush");
            var stack = new StackPanel();
            var stats = new UniformGrid { Rows = 1 };
            stats.Children.Add(Stat("Game", out _statGame));
            stats.Children.Add(Stat("Upscaler", out _statUpscaler));
            stats.Children.Add(Stat("Output", out _statResolution));
            stats.Children.Add(Stat("Frame rate", out _statFps));
            stats.Children.Add(Stat("Trophies", out _statVia));
            stack.Children.Add(stats);

            var actions = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition());
            _play = new Button
            {
                Appearance = ControlAppearance.Primary,
                Content = "Play",
                Icon = Icon(SymbolRegular.Play24),
                Padding = new Thickness(26, 10, 26, 10),
                FontWeight = FontWeights.SemiBold,
            };
            _play.Click += async (_, _) => await MainWindow.Instance!.PlayOrStopAsync();
            actions.Children.Add(_play);
            _playStatus = Text("", "Muted");
            _playStatus.Margin = new Thickness(16, 0, 0, 0);
            _playStatus.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(_playStatus, 1);
            actions.Children.Add(_playStatus);
            stack.Children.Add(actions);
            summary.Child = stack;
            Body.Children.Add(summary);

            _problem = new InfoBar { IsClosable = false, IsOpen = false, Margin = new Thickness(0, 12, 0, 0) };
            Body.Children.Add(_problem);

            // A package from before this launcher's engine (it updated itself, the engine did not).
            _engineCard = Card(SymbolRegular.ArrowDownload24, "Engine update available",
                "This package has an older engine than this launcher needs (it unlocks 120 FPS and uncapped). " +
                "Updating downloads the latest package and keeps your settings, saves and mods.",
                SmallButton("Update engine", SymbolRegular.ArrowDownload24, async (_, _) => await MainWindow.Instance!.UpdateEngineAsync(),
                    ControlAppearance.Primary), out _);
            _engineCard.Margin = new Thickness(0, 12, 0, 0);
            Body.Children.Add(_engineCard);

            // ==================== Game ====================
            Group("Game");
            var gameCard = Card(SymbolRegular.Folder24, "Game folder", "", Row(
                SmallButton("Browse", SymbolRegular.FolderOpen24, (_, _) => ChooseGame()),
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(GameEnvironment.GameDir(S)))), out _gameHint);
            _gameIcon = gameCard.Icon;
            gameCard.ShowIcon = true;
            Add(gameCard);

            _resetUser = IconButton(SymbolRegular.ArrowUndo24, "Back to the default folder", (_, _) =>
            {
                S.Set("user_dir", "");
                Ws.SaveSoon();
                Refresh();
            });
            Add(Card(SymbolRegular.Save24, "Saves folder", "", Row(
                SmallButton("Browse", SymbolRegular.FolderOpen24, (_, _) => ChooseUser()),
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(GameEnvironment.UserDir(S))),
                _resetUser), out _userHint));

            ComboRow(SymbolRegular.LocalLanguage24, "In-game menu language", "The port's settings menu (Insert or L3 + R3)",
                Choices.MenuLanguages, () => Ini.Get("menu_language", "en"), v => Ini.Set("menu_language", v));

            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clock.Tick += (_, _) => UpdateRunning();

            Ws.PortChanged += Refresh;
            Ws.Saved += RefreshSummary;
            Ws.TrophiesChanged += RefreshSummary;
        }

        private static StackPanel Stat(string label, out TextBlock value)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            panel.Children.Add(Text(label, "StatLabel"));
            value = Text("—", "StatValue");
            panel.Children.Add(value);
            return panel;
        }

        public override void OnShown() => Refresh();

        private void Refresh()
        {
            var gameDir = GameEnvironment.GameDir(S);
            var status = GameCheck.Inspect(gameDir);
            SetHint(_gameHint, string.IsNullOrEmpty(gameDir) ? "Not chosen" : gameDir);
            _gameIcon.Symbol = status.Health == Health.Good ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.Warning24;
            _gameIcon.SetResourceReference(ForegroundProperty, status.Health switch
            {
                Health.Good => "GoodTextBrush",
                Health.Warning => "WarningTextBrush",
                _ => "DangerTextBrush",
            });
            _gameIcon.ToolTip = status.Summary;

            _problem.IsOpen = status.Health != Health.Good;
            _problem.Severity = status.Health == Health.Bad ? InfoBarSeverity.Error : InfoBarSeverity.Warning;
            _problem.Title = status.Health == Health.Bad ? "The game folder needs attention" : "Check the game";
            _problem.Message = status.Summary;

            var custom = S.Str("user_dir").Length > 0;
            var userDir = GameEnvironment.UserDir(S);
            var saves = Path.Combine(userDir, "savedata");
            var found = Directory.Exists(saves) && Directory.EnumerateFileSystemEntries(saves).Any();
            SetHint(_userHint, (custom ? userDir : $"Default: {userDir}") +
                               (found ? "  ·  saves found" : "  ·  no saves yet; the game creates them here"));
            _resetUser.IsEnabled = custom;

            _engineCard.Visibility = Updater.EngineOutdated ? Visibility.Visible : Visibility.Collapsed;
            _statGame.Text = status.Health == Health.Bad ? "Not found" : $"{status.TitleId} {status.Version}".Trim();
            RefreshSummary();
            UpdateRunning();
        }

        private void RefreshSummary()
        {
            _statUpscaler.Text = Label(Choices.Upscalers, Ini.Get("upscaler"));
            _statResolution.Text = Ini.Get("output_res").Replace("x", " × ");
            _statFps.Text = Label(Choices.FpsModes, GameEnvironment.Fps(S));
            _statVia.Text = $"{Ws.Trophies.Count} / {TrophyTracker.All.Length} · {Ws.Trophies.Completion * 100:0}%";
        }

        private static string Label(IReadOnlyList<Choice> choices, string value) =>
            choices.FirstOrDefault(c => c.Value == value)?.Label.Split(" (")[0] ?? value;

        public void UpdateRunning()
        {
            var game = Ws.Game;
            _play.Content = game.Running ? "Stop" : "Play";
            _play.Icon = Icon(game.Running ? SymbolRegular.Stop24 : SymbolRegular.Play24);
            _play.Appearance = game.Running ? ControlAppearance.Danger : ControlAppearance.Primary;

            if (game.Running)
            {
                var up = DateTime.Now - game.StartedAt;
                _playStatus.Text = $"Running for {(int)up.TotalMinutes}:{up.Seconds:00}  ·  output on the Game output page";
                _clock.Start();
            }
            else
            {
                _clock.Stop();
                _playStatus.Text = GameCheck.Inspect(GameEnvironment.GameDir(S)).Health == Health.Bad
                    ? "Choose the game folder first"
                    : "Settings are saved as you change them";
            }
        }

        private void ChooseGame()
        {
            var path = PickFolder("Game folder (the one with eboot.bin)", GameEnvironment.GameDir(S));
            if (path == null) return;
            S.Set("game_dir", path);
            if (!Paths.IsPortDir(Paths.PortDir))
                Ws.ResolvePort(); // the port may sit beside the new game folder
            Ws.SaveSoon();
            Ws.Log("Game folder: " + path, LogLevel.Step);
            Refresh();
        }

        private void ChooseUser()
        {
            var path = PickFolder("Saves folder", GameEnvironment.UserDir(S));
            if (path == null) return;
            S.Set("user_dir", path);
            Ws.SaveSoon();
            Refresh();
        }
    }
}

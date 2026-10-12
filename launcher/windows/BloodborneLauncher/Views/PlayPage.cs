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
        private readonly Button _profileButton;
        private readonly TextBlock _profileHint;
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

            // Player profiles: each its own saves and trophies (a button with a menu, not a drop-down,
            // so a click can't pick an entry by itself).
            _profileButton = SmallButton("Main", SymbolRegular.Person24, (_, _) => ShowProfileMenu());
            Add(Card(SymbolRegular.People24, "Player profile",
                "Each profile has its own save files and trophies, so several people can play their own runs on this PC", Row(
                _profileButton,
                SmallButton("New…", SymbolRegular.PersonAdd24, (_, _) => NewProfile()),
                IconButton(SymbolRegular.Rename24, "Rename this profile", (_, _) => RenameProfile()),
                IconButton(SymbolRegular.PersonDelete24, "Remove this profile from the list (its saves stay on disk)", (_, _) => RemoveProfile())),
                out _profileHint));

            _resetUser = IconButton(SymbolRegular.ArrowUndo24, "Back to the default folder", (_, _) =>
            {
                S.Set("user_dir", "");
                Ws.SaveSoon();
                Refresh();
            });
            Add(Card(SymbolRegular.Save24, "Saves folder", "", Row(
                SmallButton("Browse", SymbolRegular.FolderOpen24, (_, _) => ChooseUser()),
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(GameEnvironment.SavesRoot(S))),
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
            var root = GameEnvironment.SavesRoot(S);
            SetHint(_userHint, (custom ? root : $"Default: {root}") + "  ·  every profile's saves live here");
            _resetUser.IsEnabled = custom;
            RefreshProfile();

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

        // ---------------------------------------------------------------- player profiles

        private void RefreshProfile()
        {
            var profile = PlayerProfiles.Current(S);
            _profileButton.Content = profile;
            var dir = GameEnvironment.UserDir(S);
            var saves = Path.Combine(dir, "savedata");
            var found = Directory.Exists(saves) && Directory.EnumerateFileSystemEntries(saves).Any();
            SetHint(_profileHint, dir + (found ? "  ·  saves found" : "  ·  no saves yet; the game creates them here") +
                                  $"  ·  {Ws.Trophies.Count} / {TrophyTracker.All.Length} trophies");
        }

        private void ShowProfileMenu()
        {
            var menu = new ContextMenu { PlacementTarget = _profileButton, Placement = PlacementMode.Bottom };
            var current = PlayerProfiles.Current(S);
            foreach (var profile in PlayerProfiles.All(S))
            {
                var name = profile;
                var item = new System.Windows.Controls.MenuItem { Header = name, IsCheckable = true, IsChecked = name.Equals(current, StringComparison.OrdinalIgnoreCase) };
                item.Click += (_, _) => SwitchProfile(name);
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private bool GameBlocksProfiles()
        {
            if (!Ws.Game.Running) return false;
            Prompt.Tell("Player profile", "Close the game before changing profiles: it is using this profile's saves.");
            return true;
        }

        private void SwitchProfile(string name)
        {
            if (name.Equals(PlayerProfiles.Current(S), StringComparison.OrdinalIgnoreCase) || GameBlocksProfiles()) return;
            S.Set("player_profile", PlayerProfiles.IsMain(name) ? "" : name);
            Directory.CreateDirectory(GameEnvironment.UserDir(S));
            Ws.Trophies.SetUserDir(GameEnvironment.UserDir(S));
            Ws.SaveSoon();
            Ws.Log($"Player profile: {name}.", LogLevel.Step);
            Refresh();
        }

        private void NewProfile()
        {
            if (GameBlocksProfiles()) return;
            var values = Prompt.Ask("New player profile", "Its own save files and trophies. The game starts a new character the first time.",
                new Prompt.Field("Name", "", "e.g. Alex"));
            if (values == null) return;
            var name = PlayerProfiles.Validate(S, values[0], out var error);
            if (name == null) { Prompt.Tell("New player profile", error); return; }
            PlayerProfiles.Add(S, name);
            SwitchProfile(name);
        }

        private void RenameProfile()
        {
            var current = PlayerProfiles.Current(S);
            if (PlayerProfiles.IsMain(current)) { Prompt.Tell("Rename profile", "Main is the original saves folder and keeps its name."); return; }
            if (GameBlocksProfiles()) return;
            var values = Prompt.Ask("Rename profile", null, new Prompt.Field("Name", current));
            if (values == null) return;
            var name = PlayerProfiles.Validate(S, values[0], out var error, renaming: current);
            if (name == null) { Prompt.Tell("Rename profile", error); return; }
            try
            {
                PlayerProfiles.Rename(S, current, name);
            }
            catch (Exception e)
            {
                Prompt.Tell("Rename profile", "Could not rename its folder: " + e.Message);
                return;
            }
            Ws.Trophies.SetUserDir(GameEnvironment.UserDir(S));
            Ws.SaveSoon();
            Refresh();
        }

        private void RemoveProfile()
        {
            var current = PlayerProfiles.Current(S);
            if (PlayerProfiles.IsMain(current)) { Prompt.Tell("Remove profile", "Main is the original saves folder and can't be removed."); return; }
            if (GameBlocksProfiles()) return;
            if (!Prompt.Confirm("Remove profile",
                    $"{current} is taken off the list and the launcher switches to Main. Its saves and trophies are not deleted: " +
                    $"they stay in {GameEnvironment.UserDir(S)}, and adding a profile with the same name brings them back.", "Remove"))
                return;
            PlayerProfiles.Remove(S, current);
            S.Set("player_profile", "");
            Ws.Trophies.SetUserDir(GameEnvironment.UserDir(S));
            Ws.SaveSoon();
            Refresh();
        }

        private void ChooseUser()
        {
            if (GameBlocksProfiles()) return;
            var path = PickFolder("Saves folder", GameEnvironment.SavesRoot(S));
            if (path == null) return;
            S.Set("user_dir", path);
            Ws.SaveSoon();
            Refresh();
        }
    }
}

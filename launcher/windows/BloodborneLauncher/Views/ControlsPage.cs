using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// The engine's PC controls: mouse camera and menu pointer, profiles, and the 29 actions, each
    /// bound to one key and one mouse input (bbport.ini bind.*, the same records the in-game
    /// System › Key Bindings menu edits). Clicking an action listens for the next key, mouse button
    /// or wheel step: a key replaces the key, a mouse input the mouse input. × removes one,
    /// right-click unbinds the action. Inputs used twice, and the port's Insert key, are flagged.
    /// </summary>
    public sealed class ControlsPage : LauncherPage
    {
        private readonly StackPanel _table = new();
        private readonly InfoBar _conflicts;
        private readonly TextBox _search;
        private readonly TextBlock _padStatus;

        // Capture overlay
        private readonly Grid _overlay;
        private readonly TextBlock _captureTitle, _captureFeedback;
        private ControlDef? _target;

        // Profiles
        private readonly ComboBox _profileBox;
        private readonly TextBlock _profileHint;
        private readonly Button _deleteProfile;
        private List<ControlProfiles.Profile> _profiles = new();
        private bool _fillingProfiles;

        public ControlsPage() : base("Controls",
            "Keyboard and mouse for every action, the mouse camera, and shareable profiles. Gamepads work as on PS4.")
        {
            // ==================== PC controls & mouse ====================
            Group("Keyboard & mouse");
            var pc = ToggleRow(SymbolRegular.Keyboard24, "PC controls", "The keyboard and mouse layout below; off keeps the gamepad-style keys",
                () => Ini.Get("pc_controls", "1") != "0", v => Ini.Set("pc_controls", v ? "1" : "0"));
            var mouse = new StackPanel();
            ToggleRow(SymbolRegular.CursorClick24, "Mouse camera", "The mouse turns the game's own camera by raw counts, at any frame rate",
                () => Ini.Get("mouse_camera", "1") != "0", v => Ini.Set("mouse_camera", v ? "1" : "0"), mouse);
            ToggleRow(SymbolRegular.CursorHover24, "Mouse in menus", "The mouse becomes a pointer in the game's menus: hover and click items",
                () => Ini.Get("mouse_menu", "1") != "0", v => Ini.Set("mouse_menu", v ? "1" : "0"), mouse);
            SliderRow(SymbolRegular.Gauge24, "Mouse sensitivity", "5 is the default", 0, 10, 1,
                () => ParseDouble(Ini.Get("mouse_sensitivity", "5"), 5), v => Ini.Set("mouse_sensitivity", ((int)v).ToString()), "0", mouse);
            IniToggle(SymbolRegular.ArrowSwap24, "Invert horizontal", null, "mouse_invert_x", mouse);
            IniToggle(SymbolRegular.ArrowSortDown24, "Invert vertical", null, "mouse_invert_y", mouse);
            IniToggle(SymbolRegular.ArrowRotateClockwise24, "Camera auto-rotation", "Let the camera turn by itself with movement, as with a gamepad",
                "mouse_auto_rotation", mouse);
            Body.Children.Add(mouse);
            void Enable() => mouse.IsEnabled = pc.IsChecked == true;
            pc.Click += (_, _) => { Enable(); Rebuild(); };
            Enable();

            // ==================== Profiles ====================
            Group("Profiles");
            _profileBox = new ComboBox { Width = 260, VerticalAlignment = VerticalAlignment.Center };
            _profileBox.SelectionChanged += (_, _) => ProfileChosen();
            _deleteProfile = IconButton(SymbolRegular.Delete24, "Delete this profile", (_, _) => DeleteProfile());
            Add(Card(SymbolRegular.PeopleCommunity24, "Profile", "", Row(_profileBox, _deleteProfile), out _profileHint));
            Add(Card(SymbolRegular.Share24, "Save and share", "Profiles are small JSON files. Copy one and paste it on any paste site " +
                "(Pastebin, GitHub Gist, Rentry, hastebin…) to share it; others import it from the link.", null, out _));
            var share = new WrapPanel { Margin = new Thickness(46, -4, 0, 12) };
            share.Children.Add(SmallButton("Save as…", SymbolRegular.Save24, (_, _) => SaveProfile()));
            share.Children.Add(SmallButton("Copy JSON", SymbolRegular.Copy24, (_, _) => CopyProfile()));
            share.Children.Add(SmallButton("Export file…", SymbolRegular.ArrowExportUp24, (_, _) => ExportProfile()));
            share.Children.Add(SmallButton("Import file…", SymbolRegular.DocumentArrowDown20, (_, _) => ImportFile()));
            share.Children.Add(SmallButton("Import from link…", SymbolRegular.Link24, async (_, _) => await ImportLinkAsync()));
            foreach (FrameworkElement b in share.Children) b.Margin = new Thickness(0, 4, 6, 0);
            Body.Children.Add(share);

            Group("Gamepad");
            Add(Card(SymbolRegular.XboxController24, "Controller", "", null, out _padStatus));

            // ==================== Bindings ====================
            var header = new Grid { Margin = new Thickness(2, 24, 0, 8) };
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var title = Text("Actions", "GroupHeader");
            title.Margin = new Thickness(0);
            title.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(title);
            _search = new TextBox { PlaceholderText = "Filter actions or keys", PlaceholderEnabled = true, Width = 210, Icon = Icon(SymbolRegular.Search24) };
            _search.TextChanged += (_, _) => Rebuild();
            var tools = Row(_search, SmallButton("Reset all", SymbolRegular.ArrowUndo24, (_, _) =>
            {
                Bindings.ResetAll(Ini);
                Ws.Log("Key bindings reset to the defaults.", LogLevel.Step);
                Changed();
            }));
            Grid.SetColumn(tools, 1);
            header.Children.Add(tools);
            Body.Children.Add(header);

            var help = Text("Click an action and press a key, click a mouse button or turn the wheel. Each action takes one key and one " +
                            "mouse input. × removes one, right-click unbinds the action.", "Faint");
            help.Margin = new Thickness(2, 0, 0, 8);
            Body.Children.Add(help);

            _conflicts = new InfoBar { IsClosable = false, IsOpen = false, Severity = InfoBarSeverity.Warning, Margin = new Thickness(0, 0, 0, 8) };
            Body.Children.Add(_conflicts);
            Body.Children.Add(new Card { Content = _table, Padding = new Thickness(12, 8, 12, 10) });

            // ==================== Capture overlay ====================
            _captureTitle = Text("", "PageTitle");
            var captureHelp = Text("Press a key, or click / scroll inside the box. Esc cancels.", "Muted");
            captureHelp.Margin = new Thickness(0, 4, 0, 12);
            _captureFeedback = Text("", "Muted");
            _captureFeedback.SetResourceReference(TextBlock.ForegroundProperty, "WarningTextBrush");

            var padText = Text("Click or scroll here", "Faint");
            padText.HorizontalAlignment = HorizontalAlignment.Center;
            padText.VerticalAlignment = VerticalAlignment.Center;
            var capturePad = new Border
            {
                Height = 110,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                Child = padText,
                Tag = "capture-wheel",
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10),
            };
            capturePad.SetResourceReference(Border.BackgroundProperty, "PanelDeepBrush");
            capturePad.SetResourceReference(Border.BorderBrushProperty, "AccentTextFillColorPrimaryBrush");
            capturePad.PreviewMouseDown += (_, e) =>
            {
                e.Handled = true;
                if (InputMap.MouseToken(e.ChangedButton) is { } name) Commit(name);
            };
            capturePad.PreviewMouseWheel += (_, e) =>
            {
                e.Handled = true;
                Commit(e.Delta > 0 ? "WheelUp" : "WheelDown");
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            buttons.Children.Add(SmallButton("Unbind", SymbolRegular.Dismiss24, (_, _) => Commit(null)));
            buttons.Children.Add(SmallButton("Cancel", SymbolRegular.ArrowExit20, (_, _) => EndCapture()));

            var dialog = new StackPanel();
            dialog.Children.Add(_captureTitle);
            dialog.Children.Add(captureHelp);
            dialog.Children.Add(capturePad);
            dialog.Children.Add(_captureFeedback);
            dialog.Children.Add(buttons);
            var dialogCard = new Border
            {
                Width = 500,
                Padding = new Thickness(22, 18, 22, 16),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = dialog,
            };
            dialogCard.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            dialogCard.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");

            _overlay = new Grid { Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Color.FromArgb(0xB0, 0x10, 0x10, 0x12)), Focusable = true };
            _overlay.Children.Add(dialogCard);
            _overlay.MouseDown += (_, e) => { if (e.OriginalSource == _overlay) EndCapture(); };

            var root = new Grid();
            Content = null;
            root.Children.Add(Scroller);
            root.Children.Add(_overlay);
            Content = root;
            PreviewKeyDown += Page_PreviewKeyDown;

            var status = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            status.Tick += (_, _) => { if (IsVisible) UpdatePadStatus(); };
            status.Start();
            Ws.PortChanged += () => { Rebuild(); FillProfiles(); };
            Rebuild();
            FillProfiles();
        }

        public override void OnShown()
        {
            UpdatePadStatus();
            Rebuild();
            FillProfiles();
        }

        private void UpdatePadStatus()
        {
            var slots = XInput.Connected();
            SetHint(_padStatus, slots.Count == 0
                ? "No XInput controller detected right now. PlayStation pads work in the game directly; they show here through Steam Input or DS4Windows."
                : $"Connected: XInput {(slots.Count == 1 ? "slot" : "slots")} {string.Join(", ", slots)}. The game uses the first one.");
        }

        // ---------------------------------------------------------------- the table

        private Dictionary<string, List<ControlDef>> Usage()
        {
            var usage = new Dictionary<string, List<ControlDef>>(StringComparer.OrdinalIgnoreCase);
            foreach (var control in InputMap.Controls)
                foreach (var name in Bindings.Get(Ini, control))
                {
                    if (!usage.TryGetValue(name, out var list)) usage[name] = list = new List<ControlDef>();
                    list.Add(control);
                }
            return usage;
        }

        private void Rebuild()
        {
            _table.Children.Clear();
            _table.IsEnabled = Ini.Get("pc_controls", "1") != "0";
            var usage = Usage();
            var filter = _search.Text.Trim();

            _table.Children.Add(TableRow(Header("Action"), Header("Keyboard & mouse"), Header("Controller"), new FrameworkElement(), header: true));

            foreach (var group in InputMap.Controls.GroupBy(c => c.Group))
            {
                var rows = group.Where(c => Matches(c, filter)).ToList();
                if (rows.Count == 0) continue;

                var groupLabel = Text(group.Key.ToUpperInvariant(), "StatLabel");
                groupLabel.FontWeight = FontWeights.SemiBold;
                groupLabel.Margin = new Thickness(4, 14, 0, 4);
                _table.Children.Add(groupLabel);

                foreach (var control in rows)
                {
                    var name = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    name.Children.Add(new TextBlock { Text = control.Label, FontSize = 13.5 });
                    if (control.Hint.Length > 0) name.Children.Add(Text(control.Hint, "Faint"));

                    var c = control;
                    var reset = new Button
                    {
                        Icon = Icon(SymbolRegular.ArrowUndo24),
                        Appearance = ControlAppearance.Transparent,
                        Padding = new Thickness(6),
                        ToolTip = $"Back to the default ({Show(control.Default)})",
                        VerticalAlignment = VerticalAlignment.Center,
                        Visibility = Bindings.IsDefault(Ini, control) ? Visibility.Hidden : Visibility.Visible,
                    };
                    reset.Click += (_, _) => Store(c, InputMap.Split(c.Default));

                    var pad = Text(control.Pad ?? "—", "Faint");
                    pad.VerticalAlignment = VerticalAlignment.Center;
                    _table.Children.Add(TableRow(name, Cell(control, usage), pad, reset));
                }
            }

            var shared = usage.Where(u => u.Value.Count > 1).Select(u => $"{InputMap.Display(u.Key)} ({string.Join(", ", u.Value.Select(c => c.Label))})").ToList();
            var reserved = usage.Keys.Where(InputMap.Reserved.ContainsKey).Select(k => InputMap.Reserved[k]).ToList();
            var lines = new List<string>();
            if (shared.Count > 0) lines.Add("Used by more than one action: " + string.Join("; ", shared));
            if (reserved.Count > 0) lines.Add("Also used by the port: " + string.Join("; ", reserved));
            _conflicts.IsOpen = lines.Count > 0;
            _conflicts.Title = shared.Count > 0 ? "Shared inputs" : "Reserved keys";
            _conflicts.Message = string.Join("\n", lines);
        }

        private static string Show(string value) =>
            InputMap.Split(value) is { Count: > 0 } names ? string.Join(", ", names.Select(InputMap.Display)) : "not bound";

        private bool Matches(ControlDef control, string filter)
        {
            if (filter.Length == 0) return true;
            bool Has(string s) => s.Contains(filter, StringComparison.OrdinalIgnoreCase);
            return Has(control.Label) || Has(control.Hint) || (control.Pad != null && Has(control.Pad)) ||
                   Bindings.Get(Ini, control).Any(n => Has(n) || Has(InputMap.Display(n)));
        }

        private static TextBlock Header(string text)
        {
            var block = Text(text, "StatLabel");
            block.FontWeight = FontWeights.SemiBold;
            return block;
        }

        private static Grid TableRow(FrameworkElement name, FrameworkElement keys, FrameworkElement pad, FrameworkElement action, bool header = false)
        {
            var grid = new Grid { Margin = new Thickness(0, header ? 4 : 1, 0, header ? 2 : 1), MinHeight = header ? 0 : 40 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            name.Margin = new Thickness(4, 0, 8, 0);
            Grid.SetColumn(keys, 1);
            Grid.SetColumn(pad, 2);
            Grid.SetColumn(action, 3);
            keys.Margin = new Thickness(header ? 10 : 4, 0, 4, 0);
            pad.Margin = new Thickness(10, 0, 0, 0);
            grid.Children.Add(name);
            grid.Children.Add(keys);
            grid.Children.Add(pad);
            grid.Children.Add(action);
            return grid;
        }

        /// <summary>One action's inputs: chips, and the whole cell as a capture button.</summary>
        private FrameworkElement Cell(ControlDef control, Dictionary<string, List<ControlDef>> usage)
        {
            var names = Bindings.Get(Ini, control);
            var chips = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };

            foreach (var name in names)
            {
                var others = usage.TryGetValue(name, out var list) ? list.Where(c => c != control).ToList() : new List<ControlDef>();
                var reserved = InputMap.Reserved.TryGetValue(name, out var why) ? why : null;

                var label = new TextBlock { Text = InputMap.Display(name), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
                label.SetResourceReference(TextBlock.FontFamilyProperty, "MonoFont");
                var icon = new SymbolIcon
                {
                    Symbol = InputMap.IsMouse(name) ? SymbolRegular.CursorClick24 : SymbolRegular.Keyboard24,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                icon.SetResourceReference(ForegroundProperty, "TextFillColorTertiaryBrush");
                var remove = new System.Windows.Controls.Button
                {
                    Content = new SymbolIcon { Symbol = SymbolRegular.Dismiss16, FontSize = 10 },
                    Margin = new Thickness(6, 0, -3, 0),
                    Padding = new Thickness(2),
                    ToolTip = "Remove",
                };
                remove.SetResourceReference(StyleProperty, "BareButton");
                var n = name;
                remove.Click += (_, e) =>
                {
                    e.Handled = true;
                    Store(control, Bindings.Get(Ini, control).Where(b => !b.Equals(n, StringComparison.OrdinalIgnoreCase)).ToList());
                };
                var chip = new Border { Child = Row(icon, label, remove) };
                chip.SetResourceReference(StyleProperty, "Chip");
                if (others.Count > 0 || reserved != null)
                {
                    chip.SetResourceReference(Border.BorderBrushProperty, "WarningTextBrush");
                    label.SetResourceReference(TextBlock.ForegroundProperty, "WarningTextBrush");
                    chip.ToolTip = string.Join("\n", new[]
                    {
                        others.Count > 0 ? "Also bound to: " + string.Join(", ", others.Select(c => c.Label)) : null,
                        reserved,
                    }.Where(t => t != null));
                }
                chips.Children.Add(chip);
            }

            if (names.Count == 0)
            {
                var none = Text("Not bound", "Faint");
                none.FontStyle = FontStyles.Italic;
                none.Margin = new Thickness(2, 0, 8, 0);
                none.VerticalAlignment = VerticalAlignment.Center;
                chips.Children.Add(none);
            }

            if (!Bindings.IsDefault(Ini, control))
            {
                var dot = new System.Windows.Shapes.Ellipse { Width = 5, Height = 5, Margin = new Thickness(6, 0, 0, 0), ToolTip = "Changed from the default" };
                dot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "AccentTextFillColorPrimaryBrush");
                chips.Children.Add(dot);
            }

            var cell = new System.Windows.Controls.Button
            {
                Content = chips,
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ToolTip = $"Click to set a key or mouse input for {control.Label}. Right-click to unbind.",
            };
            cell.SetResourceReference(StyleProperty, "BareButton");
            cell.Click += (_, _) => BeginCapture(control);
            cell.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;
                Store(control, new List<string>());
            };
            return cell;
        }

        // ---------------------------------------------------------------- storing

        private void Store(ControlDef control, List<string> names)
        {
            Bindings.Set(Ini, control, names);
            Changed();
        }

        private void Changed()
        {
            Ws.SaveSoon();
            Rebuild();
            FillProfiles();
        }

        // ---------------------------------------------------------------- capture

        private void BeginCapture(ControlDef control)
        {
            _target = control;
            _captureTitle.Text = $"Bind {control.Label}";
            _captureFeedback.Text = "";
            _overlay.Visibility = Visibility.Visible;
            _overlay.Focus();
            Keyboard.Focus(_overlay);
        }

        private void EndCapture()
        {
            _overlay.Visibility = Visibility.Collapsed;
            _target = null;
        }

        /// <summary>name: the captured input (it replaces the one of its kind), or null to unbind.</summary>
        private void Commit(string? name)
        {
            if (_target == null) return;
            var control = _target;
            EndCapture();
            if (name == null)
                Bindings.Set(Ini, control, new List<string>());
            else
                Bindings.Assign(Ini, control, name, keepOtherKind: true);
            Changed();
            Ws.Log($"{control.Label}: {Show(InputMap.Join(Bindings.Get(Ini, control)))}", LogLevel.Step);
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_target == null) return;
            e.Handled = true;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                EndCapture();
                return;
            }
            var name = InputMap.SdlKeyName(key);
            if (name == null)
            {
                _captureFeedback.Text = $"{key} cannot be bound; try another key.";
                return;
            }
            Commit(name);
        }

        // ---------------------------------------------------------------- profiles

        private static bool SameBindings(Dictionary<string, string> a, Dictionary<string, string> b) =>
            InputMap.Controls.All(c =>
            {
                var x = InputMap.Split(a.TryGetValue(c.Name, out var va) ? va : c.Default);
                var y = InputMap.Split(b.TryGetValue(c.Name, out var vb) ? vb : c.Default);
                return x.Count == y.Count && x.Zip(y).All(p => p.First.Equals(p.Second, StringComparison.OrdinalIgnoreCase));
            });

        private ControlProfiles.Profile? Matching()
        {
            var current = ControlProfiles.Current(Ini);
            var chosen = S.Str("controls_profile");
            return _profiles.FirstOrDefault(p => p.Name == chosen && SameBindings(p.Bindings, current))
                   ?? _profiles.FirstOrDefault(p => SameBindings(p.Bindings, current));
        }

        private void FillProfiles()
        {
            _fillingProfiles = true;
            _profiles = ControlProfiles.All();
            _profileBox.Items.Clear();
            var match = Matching();
            if (match == null)
                _profileBox.Items.Add(new ComboBoxItem { Content = "Custom (not saved)", Tag = null });
            foreach (var p in _profiles)
                _profileBox.Items.Add(new ComboBoxItem { Content = p.BuiltIn ? $"{p.Name} (built in)" : p.Name, Tag = p });
            _profileBox.SelectedItem = _profileBox.Items.OfType<ComboBoxItem>().First(i => ReferenceEquals(i.Tag, match));
            SetHint(_profileHint, match == null
                ? "Your current bindings are not saved in a profile. Use Save as… to keep them"
                : string.Join("  ·  ", new[] { match.Description, match.Author.Length > 0 ? "by " + match.Author : "" }.Where(t => t.Length > 0)));
            _deleteProfile.IsEnabled = match is { BuiltIn: false };
            _fillingProfiles = false;
        }

        private void ProfileChosen()
        {
            if (_fillingProfiles || _profileBox.SelectedItem is not ComboBoxItem { Tag: ControlProfiles.Profile profile }) return;
            if (Matching() == null && !Prompt.Confirm("Replace your bindings?",
                    $"Your current bindings are not saved in a profile. Using \"{profile.Name}\" replaces them.", "Use profile"))
            {
                FillProfiles();
                return;
            }
            ControlProfiles.Apply(S, Ini, profile);
            Ws.Log($"Control profile: {profile.Name}", LogLevel.Step);
            Changed();
        }

        private ControlProfiles.Profile CurrentAsProfile()
        {
            var match = Matching();
            return new ControlProfiles.Profile(
                match?.Name ?? (S.Str("controls_profile") is { Length: > 0 } n ? n : "My controls"),
                match?.Author ?? S.Str("profile_author"),
                match?.Description ?? "",
                ControlProfiles.Current(Ini));
        }

        private void SaveProfile()
        {
            var current = CurrentAsProfile();
            var values = Prompt.Ask("Save profile", "Saves your current bindings so you can switch back to them or share them.",
                new Prompt.Field("Name", current.BuiltIn ? "" : current.Name, "My controls"),
                new Prompt.Field("Author", S.Str("profile_author"), "Optional"),
                new Prompt.Field("Description", current.Description, "What the layout is good for (optional)", Multiline: true));
            if (values == null) return;

            var name = values[0].Trim();
            if (name.Length == 0) name = "My controls";
            if (ControlProfiles.BuiltIns.Any(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Prompt.Tell("Choose another name", $"\"{name}\" is a built-in profile.");
                return;
            }
            if (_profiles.Any(p => !p.BuiltIn && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) &&
                !Prompt.Confirm("Replace the profile?", $"A profile called \"{name}\" already exists.", "Replace"))
                return;

            S.Set("profile_author", values[1].Trim());
            var profile = new ControlProfiles.Profile(name, values[1].Trim(), values[2].Trim(), ControlProfiles.Current(Ini));
            try
            {
                ControlProfiles.Save(profile);
                S.Set("controls_profile", name);
                Ws.SaveSoon();
                Ws.Log($"Saved control profile \"{name}\".", LogLevel.Good);
            }
            catch (Exception e)
            {
                Ws.Log("Could not save the profile: " + e.Message, LogLevel.Bad);
            }
            FillProfiles();
        }

        private void DeleteProfile()
        {
            if (_profileBox.SelectedItem is not ComboBoxItem { Tag: ControlProfiles.Profile { BuiltIn: false } profile }) return;
            if (!Prompt.Confirm("Delete the profile?", $"\"{profile.Name}\" is removed from this PC. Your current bindings stay as they are.", "Delete"))
                return;
            try
            {
                ControlProfiles.Delete(profile);
                Ws.Log($"Deleted control profile \"{profile.Name}\".", LogLevel.Step);
            }
            catch (Exception e)
            {
                Ws.Log("Could not delete the profile: " + e.Message, LogLevel.Bad);
            }
            FillProfiles();
        }

        private void CopyProfile()
        {
            Clipboard.SetText(ControlProfiles.ToJson(CurrentAsProfile()));
            Ws.Log("Profile JSON copied. Paste it on any paste site and share the link.", LogLevel.Good);
        }

        private void ExportProfile()
        {
            var profile = CurrentAsProfile();
            var dialog = new Microsoft.Win32.SaveFileDialog { Title = "Export control profile", FileName = profile.Name + ".json", Filter = "Control profile|*.json" };
            if (dialog.ShowDialog() != true) return;
            try
            {
                System.IO.File.WriteAllText(dialog.FileName, ControlProfiles.ToJson(profile));
                Ws.Log("Profile exported: " + dialog.FileName, LogLevel.Good);
            }
            catch (Exception e)
            {
                Ws.Log("Could not export the profile: " + e.Message, LogLevel.Bad);
            }
        }

        private void ImportFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Import control profile", Filter = "Control profile|*.json|All files|*.*" };
            if (dialog.ShowDialog() != true) return;
            try
            {
                Offer(ControlProfiles.Parse(System.IO.File.ReadAllText(dialog.FileName)));
            }
            catch (Exception e)
            {
                Prompt.Tell("Could not import", e.Message);
            }
        }

        private async Task ImportLinkAsync()
        {
            var values = Prompt.Ask("Import from a link",
                "Paste a link to a shared profile: a Pastebin, GitHub Gist or file, Rentry, hastebin, paste.rs, pastes.dev or dpaste link, " +
                "or any link that shows the raw JSON.",
                new Prompt.Field("Link", "", "https://pastebin.com/…"));
            if (values == null || values[0].Trim().Length == 0) return;
            try
            {
                Ws.Log("Downloading the profile…", LogLevel.Step);
                Offer(await ControlProfiles.DownloadAsync(values[0]));
            }
            catch (Exception e)
            {
                Prompt.Tell("Could not import", e is FormatException ? e.Message : "The link could not be read: " + e.GetBaseException().Message);
            }
        }

        /// <summary>Shows what an imported profile changes and, if accepted, saves and uses it.</summary>
        private void Offer(ControlProfiles.Parsed parsed)
        {
            var profile = parsed.Profile;
            var current = ControlProfiles.Current(Ini);
            var changes = InputMap.Controls
                .Where(c => !SameBindings(new() { [c.Name] = profile.Bindings.GetValueOrDefault(c.Name, c.Default) }, new() { [c.Name] = current[c.Name] }))
                .Select(c => $"  {c.Label}: {Show(profile.Bindings.GetValueOrDefault(c.Name, c.Default))}")
                .ToList();

            var text = new List<string>();
            if (profile.Author.Length > 0) text.Add("by " + profile.Author);
            if (profile.Description.Length > 0) text.Add(profile.Description);
            text.Add(changes.Count == 0
                ? "It is the same as your current bindings."
                : $"Changes {changes.Count} of {InputMap.Controls.Length} actions:\n" + string.Join("\n", changes.Take(12)) + (changes.Count > 12 ? "\n  …" : ""));
            if (parsed.Notes.Count > 0)
                text.Add("Notes:\n  " + string.Join("\n  ", parsed.Notes.Take(6)));

            if (!Prompt.Confirm($"Import \"{profile.Name}\"?", string.Join("\n\n", text), "Save and use"))
                return;

            var name = profile.Name;
            for (var i = 2; _profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)); i++)
                name = $"{profile.Name} ({i})";
            profile = profile with { Name = name };
            try
            {
                ControlProfiles.Save(profile);
            }
            catch (Exception e)
            {
                Ws.Log("Could not save the imported profile: " + e.Message, LogLevel.Bad);
            }
            ControlProfiles.Apply(S, Ini, profile);
            Ws.Log($"Imported control profile \"{name}\".", LogLevel.Good);
            Changed();
        }
    }
}

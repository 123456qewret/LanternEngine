using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace BloodborneLauncher.Views
{
    /// <summary>Mod folders with their load order, and third-party eboot patches.</summary>
    public sealed class ModsPage : LauncherPage
    {
        private readonly StackPanel _modList = new();
        private readonly StackPanel _patchList = new();
        private readonly TextBlock _modsHint, _patchesHint;
        private List<Mods.Entry> _mods = new();
        private List<(Patches.Patch Patch, bool On)> _patches = new();

        public ModsPage() : base("Mods & patches",
            "Extract each mod into its own folder (with dvdroot_ps4, or straight chr/, parts/ and so on). Applied when the game starts.")
        {
            Group("Mods");
            SettingToggle(SymbolRegular.PuzzlePiece24, "Load mods", "When two mods replace the same file, the one lower in the list wins",
                "mods_enabled");
            Add(Card(SymbolRegular.Folder24, "Mods folder", "", Row(
                SmallButton("Browse", SymbolRegular.FolderOpen24, (_, _) =>
                {
                    if (PickFolder("Mods folder", GameEnvironment.ModsDir(S)) is { } path)
                    {
                        S.Set("mods_dir", path);
                        Ws.SaveSoon();
                        RefreshMods();
                    }
                }),
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(GameEnvironment.ModsDir(S))),
                IconButton(SymbolRegular.ArrowClockwise24, "Refresh the list", (_, _) => RefreshMods())), out _modsHint));
            Body.Children.Add(_modList);

            Group("Third-party patches");
            Body.Children.Add(Text("shadPS4-format XML patches for version 01.09 from the patches folder.", "Faint"));
            Add(Card(SymbolRegular.Folder24, "Patches folder", "", Row(
                SmallButton("Browse", SymbolRegular.FolderOpen24, (_, _) =>
                {
                    if (PickFolder("Patches folder", GameEnvironment.PatchesDir(S)) is { } path)
                    {
                        S.Set("patches_dir", path);
                        Ws.SaveSoon();
                        RefreshPatches();
                    }
                }),
                IconButton(SymbolRegular.Open24, "Open in Explorer", (_, _) => Paths.OpenFolder(GameEnvironment.PatchesDir(S))),
                IconButton(SymbolRegular.ArrowClockwise24, "Refresh the list", (_, _) => RefreshPatches())), out _patchesHint));
            Body.Children.Add(_patchList);


            Ws.PortChanged += OnShown;
        }


        public override void OnShown()
        {
            RefreshMods();
            RefreshPatches();
        }

        private void RefreshMods()
        {
            var dir = GameEnvironment.ModsDir(S);
            _mods = Mods.Load(dir);
            SetHint(_modsHint, _mods.Count == 0 ? $"{dir} — no mods" : $"{dir} — {_mods.Count} mod{(_mods.Count == 1 ? "" : "s")}");
            DrawMods();
        }

        private void DrawMods()
        {
            _modList.Children.Clear();
            for (var i = 0; i < _mods.Count; i++)
            {
                var index = i;
                var mod = _mods[i];
                var toggle = new ToggleSwitch { IsChecked = mod.Enabled, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
                toggle.Click += (_, _) =>
                {
                    _mods[index] = mod with { Enabled = toggle.IsChecked == true };
                    SaveMods();
                };
                var up = IconButton(SymbolRegular.ArrowUp24, "Load earlier", (_, _) => Move(index, -1));
                var down = IconButton(SymbolRegular.ArrowDown24, "Load later", (_, _) => Move(index, 1));
                up.IsEnabled = i > 0;
                down.IsEnabled = i < _mods.Count - 1;

                var card = Card(SymbolRegular.Box24, mod.Name, $"Load order {i + 1}", Row(up, down, toggle), out _);
                card.Margin = new Thickness(24, 0, 0, 4);
                _modList.Children.Add(card);
            }
        }

        private void Move(int index, int direction)
        {
            var target = index + direction;
            if (target < 0 || target >= _mods.Count) return;
            (_mods[index], _mods[target]) = (_mods[target], _mods[index]);
            SaveMods();
            DrawMods();
        }

        private void SaveMods()
        {
            try
            {
                Mods.Save(_mods);
            }
            catch (Exception e)
            {
                Ws.Log("Could not save mods.json: " + e.Message, LogLevel.Bad);
            }
        }

        private void RefreshPatches()
        {
            var dir = GameEnvironment.PatchesDir(S);
            var profile = Patches.LoadProfile();
            _patches = Patches.Discover(dir).Select(p => (p, Patches.IsOn(p, profile))).ToList();
            SetHint(_patchesHint, _patches.Count == 0 ? $"{dir} — no patches" : dir);

            _patchList.Children.Clear();
            for (var i = 0; i < _patches.Count; i++)
            {
                var index = i;
                var (patch, on) = _patches[i];
                var toggle = new ToggleSwitch { IsChecked = on, VerticalAlignment = VerticalAlignment.Center };
                toggle.Click += (_, _) =>
                {
                    _patches[index] = (patch, toggle.IsChecked == true);
                    try
                    {
                        Patches.Save(_patches);
                    }
                    catch (Exception e)
                    {
                        Ws.Log("Could not save patches.json: " + e.Message, LogLevel.Bad);
                    }
                };
                var hint = patch.File + (patch.Author.Length > 0 ? $" · by {patch.Author}" : "");
                var card = Card(SymbolRegular.DocumentText24, patch.Name, hint, toggle, out _);
                card.Margin = new Thickness(24, 0, 0, 4);
                if (patch.Note.Length > 0)
                    card.ToolTip = patch.Note;
                _patchList.Children.Add(card);
            }
        }
    }
}

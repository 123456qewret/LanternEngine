using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// Cheats the engine applies live (bbport.ini cheat_*: off, infinite or an amount kept topped up).
    /// Turning one off restores the game values it changed; the items you have stay.
    /// </summary>
    public sealed class CheatsPage : LauncherPage
    {
        private static readonly Choice[] Modes =
        {
            new("Off", "off"),
            new("Infinite", "infinite", "Using one never lowers the count"),
            new("Custom amount", "custom", "Refilled to the amount below whenever it drops"),
        };

        /// <summary>Engine builds from before the cheats module: they ignore the cheat_* keys.</summary>
        private static readonly string[] EnginesWithoutCheats =
            { "bbhost-0.4.0-fps1", "bbhost-0.4.0-fps2", "bbhost-0.4.0-fps3", "bbhost-0.4.0-fps4" };

        private readonly InfoBar _engine;

        public CheatsPage() : base("Cheats",
            "Applied live while you play; also in the in-game menu (Insert or L3 + R3). Turning a cheat off puts the game's values back. Items you already have stay.")
        {
            _engine = new InfoBar
            {
                IsClosable = false,
                Severity = InfoBarSeverity.Warning,
                Margin = new Thickness(0, 14, 0, 0),
                Title = "Not in this engine yet",
                Message = "Your engine doesn't apply cheats yet: they arrive with the next engine update. What you pick here is saved and starts working then.",
            };
            Body.Children.Add(_engine);

            Group("Items");
            Cheat(SymbolRegular.Drop24, "Blood Vials", "cheat_vials", "cheat_vials_amount");
            Cheat(SymbolRegular.Flash24, "Quicksilver Bullets", "cheat_bullets", "cheat_bullets_amount");
        }

        /// <summary>A mode row and its amount slider for one cheat key.</summary>
        private void Cheat(SymbolRegular icon, string title, string key, string amountKey)
        {
            Slider amount = null!;
            string Mode() => Ini.Get(key, "off") switch
            {
                "infinite" => "infinite",
                var v when int.TryParse(v, out var n) && n > 0 => "custom",
                _ => "off",
            };
            int Amount() => Math.Clamp(int.TryParse(Ini.Get(key, ""), out var n) && n > 0 ? n : S.Int(amountKey), 1, 99);

            ComboRow(icon, title, null, Modes, Mode, v =>
            {
                Ini.Set(key, v == "custom" ? Amount().ToString(CultureInfo.InvariantCulture) : v);
                ShowAmount(amount, v == "custom");
                Ws.Log($"{title}: {(v == "custom" ? $"kept at {Amount()}" : v)}.", LogLevel.Step);
            });
            amount = SliderRow(SymbolRegular.NumberSymbol24, "Amount", $"How many {title} to keep", 1, 99, 1, () => Amount(), v =>
            {
                S.Set(amountKey, (int)v);
                if (Mode() == "custom") Ini.Set(key, ((int)v).ToString(CultureInfo.InvariantCulture));
            }, "0");
            ShowAmount(amount, Mode() == "custom");
        }

        /// <summary>The amount row only shows for Custom amount.</summary>
        private static void ShowAmount(Slider amount, bool show)
        {
            DependencyObject? at = amount;
            while (at != null && at is not SettingRow) at = LogicalTreeHelper.GetParent(at);
            ((UIElement?)at ?? amount).Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>The package's engine applies cheats (newer than the builds listed above).</summary>
        public static bool Supported => Updater.LocalEngine() is { } engine && !EnginesWithoutCheats.Contains(engine);

        public override void OnShown() => _engine.IsOpen = !Supported;
    }
}

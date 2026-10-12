using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using PasswordBox = Wpf.Ui.Controls.PasswordBox;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// Online play. Messages, bloodstains and ghosts come from the game server (The Hunter's Dream
    /// by default); sign-in, bells and summons go through a shadNet server. The engine reads these
    /// from bbport.ini (online_*, scripts/online.py); only the password stays out of it and is
    /// passed when the game starts.
    /// </summary>
    public sealed class OnlinePage : LauncherPage
    {
        private readonly ToggleSwitch _online;
        private readonly StackPanel _results = new();
        private readonly Button _test;

        public OnlinePage() : base("Online",
            "Messages, bloodstains and ghosts come from the game server; sign-in, bells and summons go through a shadNet server. " +
            "Everyone needs game version 1.09 and the same servers.")
        {
            // Earlier versions defaulted the game server to the shadNet Web API, which has none.
            if (Ini.Get("online_game_api").Length > 0 && Online.GameServer(Ini.Get("online_game_api")) == LauncherSettings.OnlineCommunity)
                Ini.Set("online_game_api", "");

            Group("Play");
            _online = ToggleRow(SymbolRegular.Globe24, "Play online", "Off: the game stays offline",
                () => Ini.Get("online_enabled") == "1", v =>
                {
                    Ini.Set("online_enabled", v ? "1" : "0");
                    // The public servers unless others are entered.
                    if (v && Ini.Get("online_host").Trim().Length == 0)
                    {
                        Ini.Set("online_host", "srv.shadps4.net");
                        Ini.Set("online_port", "31313");
                    }
                });

            Group("Game server (messages, bloodstains, ghosts)");
            TextRow(SymbolRegular.Globe24, "Game server", "Empty: The Hunter's Dream. Only the name is needed; the game picks the port",
                "thehuntersdream.com", () => Ini.Get("online_game_api"),
                v => Ini.Set("online_game_api", v.Trim().Length == 0 ? "" : Online.GameServer(v)));

            Group("Co-op server (sign-in, bells and summons)");
            Body.Children.Add(Text("A shadNet server: srv.shadps4.net, port 31313, is shadPS4's public one. For a private server, use the " +
                                   "address, port and web address its owner gives you.", "Faint"));
            IniText(SymbolRegular.Server24, "Server", "Host name only, without a port", "srv.shadps4.net", "online_host");
            IniText(SymbolRegular.PlugConnected24, "Port", "TCP port of the server (31313 for the public one)", "31313", "online_port", 120);
            IniText(SymbolRegular.Link24, "Web API", "Empty: the server with port 31315", "http://srv.shadps4.net:31315", "online_webapi");

            Group("Account");
            Body.Children.Add(Text("Your account name on that server (up to 16 characters), not an email address. The password is " +
                                   "kept with the launcher's settings on this PC, not in bbport.ini.", "Faint"));
            IniText(SymbolRegular.Person24, "Account name", null, "Account name", "online_username");
            var password = new PasswordBox
            {
                Width = 300,
                PlaceholderText = "Password",
                PlaceholderEnabled = true,
                Password = S.Str("online_password"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            password.PasswordChanged += (_, _) =>
            {
                S.Set("online_password", password.Password);
                Ws.SaveSoon();
            };
            Add(Card(SymbolRegular.Password24, "Password", null, password, out _));

            Group("Connection");
            ToggleRow(SymbolRegular.Router24, "UPnP", "Open the port on the router automatically; turn off with a VPN",
                () => Ini.Get("online_upnp") == "1", v => Ini.Set("online_upnp", v ? "1" : "0"));
            IniText(SymbolRegular.Router24, "Local P2P port", "Empty picks one automatically", "Automatic", "online_p2p_port", 120);
            _test = SmallButton("Test", SymbolRegular.PlayCircle24, async (_, _) => await TestAsync());
            Add(Card(SymbolRegular.Pulse24, "Test the connection", "Checks the game server, the co-op server and its Web API", _test, out _));
            Body.Children.Add(_results);
        }

        private void IniText(SymbolRegular icon, string title, string? hint, string placeholder, string key, double width = 300) =>
            TextRow(icon, title, hint, placeholder, () => Ini.Get(key), v => Ini.Set(key, v.Trim()), width: width);

        public override void OnShown() => _online.IsChecked = Ini.Get("online_enabled") == "1";

        private async Task TestAsync()
        {
            var host = Ini.Get("online_host").Trim();
            var port = Ini.Get("online_port", "31313").Trim();
            if (host.Length == 0)
            {
                Prompt.Tell("No co-op server", "Enter the co-op server first (srv.shadps4.net for the public one).");
                return;
            }
            var webapi = Ini.Get("online_webapi").Trim() is { Length: > 0 } w ? w : $"http://{host}:31315";

            _results.Children.Clear();
            _test.IsEnabled = false;
            _test.Content = "Testing…";
            try
            {
                foreach (var r in await Online.CheckAsync(Online.GameServer(Ini.Get("online_game_api")), $"{host}:{port}", webapi))
                {
                    var card = Card(r.Ok ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24,
                        $"{r.Name}: {r.Address}", r.Ok ? "Reachable" : $"Not reachable ({r.Detail})", null, out _);
                    card.ShowIcon = true;
                    card.Icon.SetResourceReference(ForegroundProperty, r.Ok ? "GoodTextBrush" : "DangerTextBrush");
                    SetHint(card.Hint, r.Ok ? "Reachable" : $"Not reachable ({r.Detail})");
                    card.Margin = new Thickness(24, 0, 0, 4);
                    _results.Children.Add(card);
                    Ws.Log($"{r.Name} {r.Address}: {(r.Ok ? "reachable" : "not reachable " + r.Detail)}", r.Ok ? LogLevel.Good : LogLevel.Warn);
                }
            }
            finally
            {
                _test.IsEnabled = true;
                _test.Content = "Test";
            }
        }
    }
}

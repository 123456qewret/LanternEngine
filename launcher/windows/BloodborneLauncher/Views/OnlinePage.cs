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

        /// <summary>Where shadNet accounts are made (the public server's).</summary>
        private const string ShadnetSignUp = "https://shadnet.shadps4.net";

        public OnlinePage() : base("Online",
            "1. Turn on Play online. 2. Choose a co-op pool: The Hunter's Dream (sign in with Discord, play with bbhost players) " +
            "or a shadNet server (its account below). 3. In the game, choose online at the title screen: The Hunter's Dream's " +
            "welcome message means you are online. Everyone needs game version 1.09.")
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

            Group("Co-op pool");
            ComboRow(SymbolRegular.PeopleTeam24, "Play with", null, Choices.CoopPools,
                () => Ini.Get("online_pool", "shadnet") == "huntersdream" ? "huntersdream" : "shadnet",
                v => Ini.Set("online_pool", v));
            _thdButton = SmallButton("Sign in with Discord", SymbolRegular.Person24, async (_, _) => await SignInAsync());
            Add(Card(SymbolRegular.PersonAccounts24, "The Hunter's Dream account", null, _thdButton, out _thdHint));
            ShowAccount();

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
            Body.Children.Add(Text("A shadNet account (shadPS4's co-op server), not a Hunter's Dream one: The Hunter's Dream " +
                                   "needs no account here, its messages and bloodstains come on their own (its accounts are for the " +
                                   "bbhost launcher). Enter the account name you registered (up to 16 characters), not an email " +
                                   "address. The password is kept encrypted for your Windows user, not in bbport.ini.", "Faint"));
            Add(Card(SymbolRegular.PersonAdd24, "No shadNet account yet?", "Register one on shadPS4's site, then enter it below",
                SmallButton("Create account", SymbolRegular.Open24, (_, _) =>
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ShadnetSignUp) { UseShellExecute = true })),
                out _));
            IniText(SymbolRegular.Person24, "Account name", null, "Account name", "online_username");
            var password = new PasswordBox
            {
                Width = 300,
                PlaceholderText = "Password",
                PlaceholderEnabled = true,
                Password = S.OnlinePassword,
                VerticalAlignment = VerticalAlignment.Center,
            };
            password.PasswordChanged += (_, _) =>
            {
                S.OnlinePassword = password.Password;
                Ws.SaveSoon();
            };
            Add(Card(SymbolRegular.Password24, "Password", null, password, out _));

            Group("Connection");
            ToggleRow(SymbolRegular.Router24, "UPnP", "Open the port on the router automatically; turn off with a VPN",
                () => Ini.Get("online_upnp") == "1", v => Ini.Set("online_upnp", v ? "1" : "0"));
            IniText(SymbolRegular.Router24, "Local P2P port", "Empty: 3658, the PS4's (UPnP opens it on the router)", "3658", "online_p2p_port", 120);
            ComboRow(SymbolRegular.Timer24, "Summon sign timeout", "How long a sign you touched waits for its host before giving up",
                Choices.SignTimeout, () => Ini.Get("online_sign_timeout", "30"), v => Ini.Set("online_sign_timeout", v));
            _test = SmallButton("Test", SymbolRegular.PlayCircle24, async (_, _) => await TestAsync());
            Add(Card(SymbolRegular.Pulse24, "Test the connection", "Checks the game server, the co-op server and its Web API", _test, out _));
            Body.Children.Add(_results);
        }

        private readonly Button _thdButton;
        private readonly System.Windows.Controls.TextBlock _thdHint;
        private CancellationTokenSource? _signIn;

        private void ShowAccount()
        {
            var accounts = HuntersDream.Accounts();
            var name = accounts.Contains(Ini.Get("online_thd_account")) ? Ini.Get("online_thd_account") : accounts.FirstOrDefault();
            SetHint(_thdHint, name is null
                ? "Not signed in. Your Discord account becomes your online name; no password is stored"
                : $"Signed in as {name}");
        }

        /// <summary>bbbridge's Discord sign-in: the code goes to the clipboard and the page opens;
        /// pressing the button again while it waits cancels.</summary>
        private async Task SignInAsync()
        {
            if (_signIn is { } running)
            {
                running.Cancel();
                return;
            }
            _signIn = new CancellationTokenSource();
            _thdButton.Content = "Cancel";
            try
            {
                var name = await HuntersDream.SignInAsync((page, code) => Dispatcher.Invoke(() =>
                {
                    try { Clipboard.SetText(code); } catch (Exception) { }
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(page) { UseShellExecute = true });
                    SetHint(_thdHint, $"Enter the code {code} on the page that opened (it is on the clipboard), then approve");
                }), _signIn.Token);
                Ini.Set("online_thd_account", name);
                Ini.Set("online_pool", "huntersdream");
                Ws.Log($"The Hunter's Dream: signed in as {name}", LogLevel.Good);
                ShowAccount();
            }
            catch (OperationCanceledException)
            {
                ShowAccount();
            }
            catch (Exception e)
            {
                ShowAccount();
                Prompt.Tell("Sign-in failed", e.Message);
            }
            finally
            {
                _signIn.Dispose();
                _signIn = null;
                _thdButton.Content = "Sign in with Discord";
            }
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
                var results = await Online.CheckAsync(Online.GameServer(Ini.Get("online_game_api")), $"{host}:{port}", webapi);
                var p2p = int.TryParse(Ini.Get("online_p2p_port").Trim(), out var pinned) && pinned is > 0 and < 65536 ? pinned : 3658;
                // Not while the game plays: the ping carries the account name and could point the
                // server at the launcher's port instead of the game's.
                if (!Ws.Game.Running && await Online.CheckUdpAsync(host, int.TryParse(port, out var tcp) ? tcp : 31313,
                        Ini.Get("online_username").Trim(), p2p) is { } udp)
                    results.Add(udp);
                else
                    Ws.Log("Co-op UDP: no answer outside the game (the server answers signed-in players); summons are the real test", LogLevel.Info);
                foreach (var r in results)
                {
                    var said = r.Ok ? (r.Detail.Length > 0 ? $"Reachable: {r.Detail}" : "Reachable") : $"Not reachable ({r.Detail})";
                    var card = Card(r.Ok ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24,
                        $"{r.Name}: {r.Address}", said, null, out _);
                    card.ShowIcon = true;
                    card.Icon.SetResourceReference(ForegroundProperty, r.Ok ? "GoodTextBrush" : "DangerTextBrush");
                    SetHint(card.Hint, said);
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

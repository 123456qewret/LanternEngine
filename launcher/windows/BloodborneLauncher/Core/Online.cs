using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace BloodborneLauncher.Core
{
    /// <summary>The online connection test: the game server (messages, bloodstains, ghosts), the
    /// shadNet server's TCP port (sign-in, co-op) and its Web API.</summary>
    public static class Online
    {
        public sealed record Result(string Name, string Address, bool Ok, string Detail);

        /// <summary>The game server as scripts/online.py uses it: empty is The Hunter's Dream, a
        /// bare name gets https://, and the old default (the public shadNet Web API, which has no
        /// game server) becomes The Hunter's Dream.</summary>
        public static string GameServer(string text)
        {
            text = text.Trim().TrimEnd('/');
            if (text.Length == 0) return LauncherSettings.OnlineCommunity;
            if (!text.Contains("://")) text = "https://" + text;
            return Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Host == "srv.shadps4.net"
                ? LauncherSettings.OnlineCommunity
                : text;
        }

        public static async Task<List<Result>> CheckAsync(string gameServer, string server, string webapi)
        {
            var results = new List<Result> { await CheckGameServerAsync(gameServer) };
            try
            {
                var at = server.LastIndexOf(':');
                var host = at > 0 ? server[..at] : server;
                var port = at > 0 && int.TryParse(server[(at + 1)..], out var p) ? p : 31313;
                using var tcp = new TcpClient();
                using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await tcp.ConnectAsync(host, port, cancel.Token);
                results.Add(new Result("Co-op server", server, true, ""));
            }
            catch (Exception e)
            {
                results.Add(new Result("Co-op server", server, false, e.GetBaseException().Message));
            }

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var body = await http.GetStringAsync(webapi.TrimEnd('/') + "/status");
                var ok = body.Replace(" ", "").Contains("\"ok\":true");
                results.Add(new Result("Web API", webapi, ok, ok ? "" : "not a shadNet server"));
            }
            catch (Exception e)
            {
                results.Add(new Result("Web API", webapi, false, e.GetBaseException().Message));
            }
            return results;
        }

        /// <summary>The game's first request: its server information (ss.info, base64 XML that
        /// lists the game APIs). Without a port, the game's own (20443) is used.</summary>
        private static async Task<Result> CheckGameServerAsync(string gameServer)
        {
            try
            {
                var uri = new UriBuilder(gameServer);
                if (new Uri(gameServer).IsDefaultPort) uri.Port = 20443;
                uri.Path = "/bb-eu/ss.info";
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(7) };
                var body = await http.GetStringAsync(uri.Uri);
                var xml = Encoding.UTF8.GetString(Convert.FromBase64String(body.Trim()));
                var ok = xml.Contains("<api_", StringComparison.Ordinal);
                return new Result("Game server", gameServer, ok, ok ? "" : "not a Bloodborne game server");
            }
            catch (FormatException)
            {
                return new Result("Game server", gameServer, false, "not a Bloodborne game server");
            }
            catch (Exception e)
            {
                return new Result("Game server", gameServer, false, e.GetBaseException().Message);
            }
        }
    }
}

using System.Net.Http;
using System.Net.Sockets;

namespace BloodborneLauncher.Core
{
    /// <summary>The online connection test: the shadNet server's TCP port and its Web API.</summary>
    public static class Online
    {
        public sealed record Result(string Name, string Address, bool Ok, string Detail);

        public static async Task<List<Result>> CheckAsync(string server, string webapi)
        {
            var results = new List<Result>();
            try
            {
                var at = server.LastIndexOf(':');
                var host = at > 0 ? server[..at] : server;
                var port = at > 0 && int.TryParse(server[(at + 1)..], out var p) ? p : 31313;
                using var tcp = new TcpClient();
                using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await tcp.ConnectAsync(host, port, cancel.Token);
                results.Add(new Result("Server", server, true, ""));
            }
            catch (Exception e)
            {
                results.Add(new Result("Server", server, false, e.GetBaseException().Message));
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
    }
}

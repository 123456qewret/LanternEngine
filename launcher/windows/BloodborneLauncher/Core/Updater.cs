using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// Updates from the GitHub releases of the launcher's repository. The latest release's tag
    /// (v1.2.3) is compared with this build's version; its asset "Bloodborne Launcher.exe" (or a
    /// .zip holding it) replaces this exe. A running exe cannot be overwritten but can be renamed,
    /// so the old one is moved aside, the new one put in its place and started, and the old one is
    /// removed on the next start.
    /// </summary>
    public static class Updater
    {
        public const string Repository = "Awakenchan/BloodBorneButPc";
        public static string ReleasesPage => $"https://github.com/{Repository}/releases";
        private const string ExeName = "Bloodborne Launcher.exe";

        public sealed record Release(Version Version, string Tag, string Name, string Notes, string Page, string? ExeUrl, string? ZipUrl, long Size);

        public static Version Current
        {
            get
            {
                var v = typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0);
                return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
            }
        }

        /// <summary>The latest published release, or null when there is none (or no network).</summary>
        public static async Task<Release?> LatestAsync()
        {
            using var http = Client(TimeSpan.FromSeconds(10));
            using var response = await http.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!TryVersion(tag, out var version))
                return null;

            string? exe = null, zip = null;
            long size = 0;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                var url = asset.GetProperty("browser_download_url").GetString();
                // GitHub stores "Bloodborne Launcher.exe" as "Bloodborne.Launcher.exe".
                if (name.Replace(".", " ").Replace(" exe", ".exe").Equals(ExeName, StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("BloodborneLauncher.exe", StringComparison.OrdinalIgnoreCase))
                {
                    exe = url;
                    size = asset.GetProperty("size").GetInt64();
                }
                else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && zip == null)
                {
                    zip = url;
                    if (exe == null) size = asset.GetProperty("size").GetInt64();
                }
            }
            return new Release(version, tag,
                root.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } title ? title : tag,
                root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "",
                root.GetProperty("html_url").GetString() ?? ReleasesPage, exe, zip, size);
        }

        public static bool TryVersion(string tag, out Version version)
        {
            var text = tag.Trim().TrimStart('v', 'V');
            var cut = text.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut > 0) text = text[..cut];
            if (Version.TryParse(text.Contains('.') ? text : text + ".0", out var v))
            {
                version = new Version(v.Major, v.Minor, Math.Max(0, v.Build));
                return true;
            }
            version = new Version(0, 0, 0);
            return false;
        }

        /// <summary>Downloads the release, swaps the exe and starts the new one; the caller then exits.</summary>
        public static async Task InstallAsync(Release release, IProgress<double>? progress = null)
        {
            var self = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot tell where this launcher is.");
            var folder = Path.GetDirectoryName(self)!;
            var work = Path.Combine(Path.GetTempPath(), "bbport-launcher-update");
            if (Directory.Exists(work)) Directory.Delete(work, recursive: true);
            Directory.CreateDirectory(work);

            var url = release.ExeUrl ?? release.ZipUrl ?? throw new InvalidOperationException("This release has no launcher download attached.");
            var download = Path.Combine(work, release.ExeUrl != null ? ExeName : "update.zip");
            using (var http = Client(TimeSpan.FromMinutes(10)))
            using (var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? release.Size;
                await using var input = await response.Content.ReadAsStreamAsync();
                await using var output = File.Create(download);
                var buffer = new byte[81920];
                long done = 0;
                int read;
                while ((read = await input.ReadAsync(buffer)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read));
                    done += read;
                    if (total > 0) progress?.Report((double)done / total);
                }
            }

            var newExe = download;
            if (release.ExeUrl == null)
            {
                var unpacked = Path.Combine(work, "unpacked");
                ZipFile.ExtractToDirectory(download, unpacked);
                newExe = Directory.GetFiles(unpacked, ExeName, SearchOption.AllDirectories).FirstOrDefault()
                         ?? throw new InvalidOperationException($"The release's .zip has no {ExeName}.");
            }
            if (new FileInfo(newExe).Length < 100_000)
                throw new InvalidOperationException("The download is too small to be the launcher.");

            var aside = self + ".old";
            if (File.Exists(aside)) File.Delete(aside);
            File.Move(self, aside);
            try
            {
                File.Copy(newExe, self);
            }
            catch
            {
                File.Move(aside, self); // put the working one back
                throw;
            }
            Process.Start(new ProcessStartInfo(self) { UseShellExecute = true, WorkingDirectory = folder });
        }

        /// <summary>Removes the exe an update moved aside (it was still running then).</summary>
        public static void CleanUp()
        {
            try
            {
                if (Environment.ProcessPath is { } self && File.Exists(self + ".old"))
                    File.Delete(self + ".old");
            }
            catch
            {
                // Still in use for a moment; removed next time.
            }
        }

        private static HttpClient Client(TimeSpan timeout)
        {
            var http = new HttpClient { Timeout = timeout };
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"BloodborneLauncher/{Current}");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return http;
        }
    }
}

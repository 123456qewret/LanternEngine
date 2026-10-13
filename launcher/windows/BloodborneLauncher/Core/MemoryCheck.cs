using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// Whether Windows has room for the game before it starts. The engine reserves the game's
    /// whole memory pool at start (about 6 GB; about 10 GB with Live resolution off above 1080p)
    /// and Windows counts it against RAM plus page file, used or not. With the page file off or
    /// fixed small and other programs open, that limit runs out during play and the engine
    /// crashes (0xC0000005). A page file Windows manages grows instead.
    /// </summary>
    public static class MemoryCheck
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatus
        {
            public uint Length, Load;
            public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtended;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

        /// <summary>MiB the game needs: the pool (direct memory plus 1 GiB of flexible memory)
        /// and about 3 GiB for the engine, the graphics driver and the shader caches.</summary>
        public static long NeededMiB(IniFile ini)
        {
            var dmem = 5056L;
            var (w, h) = Choices.OutputSize(ini.Get("output_res", "1920x1080"));
            if (ini.Get("live_resolution") == "0" && (long)w * h > 1920 * 1080)
                dmem = 9152;
            return dmem + 1024 + 3072;
        }

        /// <summary>What to tell the player when the game may not fit, else null.</summary>
        public static string? Problem(IniFile ini)
        {
            var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
            if (!GlobalMemoryStatusEx(ref status)) return null;
            var (managed, maxPageFileMiB) = PageFile();
            if (managed) return null;
            long ram = (long)(status.TotalPhys >> 20), limit = (long)(status.TotalPageFile >> 20);
            long inUse = limit - (long)(status.AvailPageFile >> 20);
            // The page file may be larger now than its configured size (set, not yet restarted).
            long pageFile = Math.Max(maxPageFileMiB, limit - ram);
            long room = ram + pageFile - inUse, need = NeededMiB(ini);
            if (room >= need) return null;
            var file = pageFile < 256 ? "the page file is off" : $"the page file is fixed at {pageFile / 1024.0:0.#} GB";
            return $"The game needs about {need / 1024.0:0.#} GB of memory (RAM plus page file). With the programs open now, " +
                   $"about {Math.Max(0, room) / 1024.0:0.#} GB is left, because {file}. Windows can run out during play, and the game then crashes.\n\n" +
                   "To fix it, set the page file to System managed size: Windows Settings › System › About › Advanced system settings › " +
                   "Performance Settings › Advanced › Virtual memory › Change. Closing other programs also helps.";
        }

        /// <summary>Whether Windows manages the page file size, else the configured maximum in MiB
        /// (0: off). HKLM\...\Memory Management\PagingFiles: "?:\pagefile.sys" (automatic),
        /// "C:\pagefile.sys 0 0" (system managed), "C:\pagefile.sys 4096 8192" (fixed).</summary>
        private static (bool Managed, long MaxMiB) PageFile()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
                if (key?.GetValue("PagingFiles") is not string[] files) return (true, 0);
                long max = 0;
                foreach (var entry in files.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    var parts = entry.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts[0].StartsWith("?:", StringComparison.Ordinal)) return (true, 0);
                    if (parts.Length < 3) return (true, 0);
                    if (!long.TryParse(parts[2], out var size)) return (true, 0);
                    if (size == 0) return (true, 0);
                    max += size;
                }
                return (false, max);
            }
            catch
            {
                return (true, 0); // unknown: no warning
            }
        }
    }
}

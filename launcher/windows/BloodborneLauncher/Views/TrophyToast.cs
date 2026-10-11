using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BloodborneLauncher.Core;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// The trophy notification shown over the game: a small borderless window that never takes
    /// focus and lets every click through, placed in a corner of the monitor the game is on. It
    /// shows over windowed and borderless fullscreen games (SDL's fullscreen is borderless);
    /// unlocks arriving together are shown one after another.
    /// </summary>
    public sealed class TrophyToast : Window
    {
        private static readonly Queue<Trophy> Pending = new();
        private static bool _showing;

        public static Color GradeColor(Grade grade) => grade switch
        {
            Grade.Platinum => Color.FromRgb(0xB9, 0xD3, 0xEE),
            Grade.Gold => Color.FromRgb(0xE3, 0xB3, 0x41),
            Grade.Silver => Color.FromRgb(0xC4, 0xC8, 0xD0),
            _ => Color.FromRgb(0xC8, 0x84, 0x4B),
        };

        /// <summary>Queues a notification (and its sound) for an unlocked trophy.</summary>
        public static void Announce(Trophy trophy)
        {
            Pending.Enqueue(trophy);
            if (!_showing) Next();
        }

        private static void Next()
        {
            if (Pending.Count == 0)
            {
                _showing = false;
                return;
            }
            _showing = true;
            var trophy = Pending.Dequeue();
            var s = Workspace.Current.Settings;
            if (s.Bool("trophy_sound"))
                Chime.Play(s.Int("trophy_volume"), s.Str("trophy_sound_file"));
            new TrophyToast(trophy, s.Str("trophy_position")).Show();
        }

        private readonly string _corner;
        private readonly TranslateTransform _slide = new();

        private TrophyToast(Trophy trophy, string corner)
        {
            _corner = corner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowActivated = false;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            Focusable = false;
            IsHitTestVisible = false;

            var color = GradeColor(trophy.Grade);
            var badge = new Border
            {
                Width = 52,
                Height = 52,
                CornerRadius = new CornerRadius(26),
                Background = new RadialGradientBrush(Color.FromArgb(0x55, color.R, color.G, color.B), Color.FromArgb(0x10, color.R, color.G, color.B)),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(1.5),
                Child = new SymbolIcon { Symbol = SymbolRegular.Trophy24, Filled = true, FontSize = 26, Foreground = new SolidColorBrush(color) },
                VerticalAlignment = VerticalAlignment.Center,
            };

            var label = new TextBlock
            {
                Text = $"{trophy.Grade.ToString().ToUpperInvariant()} TROPHY UNLOCKED",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(color),
            };
            var name = new TextBlock
            {
                Text = trophy.Name,
                FontSize = 15.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF2)),
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            var description = new TextBlock
            {
                Text = trophy.Description,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xA8, 0xAD)),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300,
                Margin = new Thickness(0, 3, 0, 0),
            };
            var text = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(label);
            text.Children.Add(name);
            text.Children.Add(description);

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(badge);
            row.Children.Add(text);

            Content = new Border
            {
                Margin = new Thickness(18),
                Padding = new Thickness(16, 14, 22, 14),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(0xF0, 0x1C, 0x1C, 0x1F)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x90, color.R, color.G, color.B)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.55 },
                Child = row,
                MinWidth = 340,
                RenderTransform = _slide,
            };
            Opacity = 0;

            SourceInitialized += (_, _) =>
            {
                // Click-through, never activated, no Alt+Tab entry.
                var handle = new WindowInteropHelper(this).Handle;
                SetWindowLong(handle, GWL_EXSTYLE, GetWindowLong(handle, GWL_EXSTYLE) |
                    WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED);
            };
            Loaded += (_, _) =>
            {
                Place();
                Animate();
            };
        }

        /// <summary>The work area of the monitor the game is on (the primary one without a game).</summary>
        private Rect Area()
        {
            var window = IntPtr.Zero;
            foreach (var probe in Process.GetProcessesByName("bb-probe"))
            {
                if (probe.MainWindowHandle != IntPtr.Zero) window = probe.MainWindowHandle;
                probe.Dispose();
            }
            var monitor = window != IntPtr.Zero
                ? MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST)
                : MonitorFromPoint(new Point32(), MONITOR_DEFAULTTOPRIMARY);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return SystemParameters.WorkArea;

            // Device pixels → this window's units.
            var source = PresentationSource.FromVisual(this);
            var scaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1;
            var scaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1;
            var r = info.Monitor; // the whole monitor: a fullscreen game covers the taskbar too
            return new Rect(r.Left * scaleX, r.Top * scaleY, (r.Right - r.Left) * scaleX, (r.Bottom - r.Top) * scaleY);
        }

        private void Place()
        {
            var area = Area();
            var right = _corner.EndsWith("right");
            var bottom = _corner.StartsWith("bottom");
            Left = right ? area.Right - ActualWidth - 12 : area.Left + 12;
            Top = bottom ? area.Bottom - ActualHeight - 12 : area.Top + 12;
        }

        private void Animate()
        {
            var from = _corner.EndsWith("right") ? 60 : -60;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)));
            _slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(from, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });

            var hold = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5.5) };
            hold.Tick += (_, _) =>
            {
                hold.Stop();
                var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(380));
                fade.Completed += (_, _) =>
                {
                    Close();
                    Next();
                };
                BeginAnimation(OpacityProperty, fade);
            };
            hold.Start();
        }

        // ---------------------------------------------------------------- Win32

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_LAYERED = 0x80000, WS_EX_NOACTIVATE = 0x8000000;
        private const uint MONITOR_DEFAULTTOPRIMARY = 1, MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct Point32 { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect32 { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public Rect32 Monitor, Work;
            public uint Flags;
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr window, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr window, int index, int value);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(Point32 point, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    }
}

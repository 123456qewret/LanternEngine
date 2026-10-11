using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace BloodborneLauncher.Views
{
    /// <summary>A small modal dialog in the launcher's style: a title, text, optional fields, and two buttons.</summary>
    public sealed class Prompt : Window
    {
        public sealed record Field(string Label, string Value = "", string Placeholder = "", bool Multiline = false);

        private readonly List<TextBox> _boxes = new();

        private Prompt(string title, string? message, IReadOnlyList<Field> fields, string ok, bool showCancel)
        {
            Owner = Application.Current.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.Height;
            Width = 480;

            var stack = new StackPanel();
            stack.Children.Add(LauncherPage.Text(title, "PageTitle"));
            if (!string.IsNullOrEmpty(message))
            {
                var text = LauncherPage.Text(message, "Muted");
                text.Margin = new Thickness(0, 6, 0, 4);
                stack.Children.Add(text);
            }
            foreach (var field in fields)
            {
                var label = LauncherPage.Text(field.Label, "StatLabel");
                label.Margin = new Thickness(0, 12, 0, 4);
                stack.Children.Add(label);
                var box = new TextBox
                {
                    Text = field.Value,
                    PlaceholderText = field.Placeholder,
                    PlaceholderEnabled = true,
                    AcceptsReturn = field.Multiline,
                    TextWrapping = field.Multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    MinHeight = field.Multiline ? 70 : 0,
                };
                _boxes.Add(box);
                stack.Children.Add(box);
            }

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            if (showCancel)
            {
                var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0), IsCancel = true };
                cancel.Click += (_, _) => DialogResult = false;
                buttons.Children.Add(cancel);
            }
            var accept = new Button { Content = ok, Appearance = ControlAppearance.Primary, IsDefault = true };
            accept.Click += (_, _) => DialogResult = true;
            buttons.Children.Add(accept);
            stack.Children.Add(buttons);

            var card = new Border
            {
                Padding = new Thickness(22, 18, 22, 18),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Child = stack,
            };
            card.SetResourceReference(Border.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            card.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            card.MouseLeftButtonDown += (_, e) => { if (e.OriginalSource is Border or StackPanel) DragMove(); };
            Content = card;
            SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");

            Loaded += (_, _) =>
            {
                if (_boxes.Count > 0)
                {
                    _boxes[0].Focus();
                    _boxes[0].SelectAll();
                }
            };
            PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) DialogResult = false; };
        }

        /// <summary>The fields' values, or null if cancelled.</summary>
        public static string[]? Ask(string title, string? message, params Field[] fields)
        {
            var dialog = new Prompt(title, message, fields, "OK", showCancel: true);
            return dialog.ShowDialog() == true ? dialog._boxes.Select(b => b.Text).ToArray() : null;
        }

        public static bool Confirm(string title, string message, string ok) =>
            new Prompt(title, message, Array.Empty<Field>(), ok, showCancel: true).ShowDialog() == true;

        public static void Tell(string title, string message) =>
            new Prompt(title, message, Array.Empty<Field>(), "OK", showCancel: false).ShowDialog();
    }
}

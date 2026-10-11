using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// Makes the mouse wheel scroll the page. A card's ComboBox or text box would otherwise swallow
    /// the wheel even when it has nothing to scroll; this takes it in the tunnelling pass unless the
    /// control under the pointer really can scroll further that way.
    /// </summary>
    public static class PageScroll
    {
        public static void Attach(ScrollViewer page)
        {
            page.PreviewMouseWheel += (_, e) =>
            {
                if (e.Handled || Owned(e.OriginalSource as DependencyObject, page, e.Delta))
                    return;
                page.ScrollToVerticalOffset(page.VerticalOffset - e.Delta);
                e.Handled = true;
            };
        }

        private static bool Owned(DependencyObject? source, ScrollViewer page, int delta)
        {
            while (source != null && !ReferenceEquals(source, page))
            {
                if (source is ScrollViewer inner && inner.ScrollableHeight > 0)
                {
                    var room = delta > 0 ? inner.VerticalOffset > 0 : inner.VerticalOffset < inner.ScrollableHeight;
                    if (room)
                        return true;
                }
                if (source is ComboBox || source is Slider)
                    return true;
                // The binding capture pad takes the wheel itself.
                if (source is FrameworkElement { Tag: "capture-wheel" })
                    return true;

                source = source is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }
            return false;
        }
    }
}

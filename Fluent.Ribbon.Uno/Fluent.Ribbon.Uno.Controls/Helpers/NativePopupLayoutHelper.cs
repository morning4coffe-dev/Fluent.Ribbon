#if WINDOWS
namespace Fluent;

using Windows.Foundation;

internal static class NativePopupLayoutHelper
{
    internal static void Position(
        Popup popup, FrameworkElement content, FrameworkElement anchor, bool submenu)
    {
        if (anchor.XamlRoot is not { } root)
        {
            return;
        }

        var viewport = root.Size;
        Size size;
        if (content.IsLoaded && content.ActualWidth > 0 && content.ActualHeight > 0)
        {
            size = new Size(content.ActualWidth, content.ActualHeight);
        }
        else
        {
            content.Measure(new Size(
                Math.Max(0, viewport.Width - 2 * PopupResizeHelper.ViewportMargin),
                Math.Max(0, viewport.Height - 2 * PopupResizeHelper.ViewportMargin)));
            size = content.DesiredSize;
        }
        var bounds = anchor.TransformToVisual(root.Content)
            .TransformBounds(new Rect(0, 0, anchor.ActualWidth, anchor.ActualHeight));
        Point position;
        if (submenu)
        {
            position = PopupResizeHelper.PlaceSubmenu(
                bounds, size, viewport, anchor.FlowDirection == FlowDirection.RightToLeft);
        }
        else
        {
            var x = anchor.FlowDirection == FlowDirection.RightToLeft ? bounds.Right - size.Width : bounds.Left;
            var y = bounds.Bottom;
            if (y + size.Height > viewport.Height - PopupResizeHelper.ViewportMargin
                && bounds.Top >= size.Height + PopupResizeHelper.ViewportMargin)
            {
                y = bounds.Top - size.Height;
            }
            position = new Point(
                Math.Clamp(x, 0, Math.Max(0, viewport.Width - size.Width)),
                Math.Clamp(y, 0, Math.Max(0, viewport.Height - size.Height)));
        }

        // A template Popup is positioned relative to its actual template origin,
        // not necessarily the active QAT anchor or the source's last location.
        if (VisualTreeHelper.GetParent(popup) is not null)
        {
            var origin = popup.TransformToVisual(root.Content).TransformPoint(new Point());
            position = new Point(position.X - origin.X, position.Y - origin.Y);
        }
        if (Math.Abs(popup.HorizontalOffset - position.X) > 0.01)
        {
            popup.HorizontalOffset = position.X;
        }
        if (Math.Abs(popup.VerticalOffset - position.Y) > 0.01)
        {
            popup.VerticalOffset = position.Y;
        }
    }
}
#endif

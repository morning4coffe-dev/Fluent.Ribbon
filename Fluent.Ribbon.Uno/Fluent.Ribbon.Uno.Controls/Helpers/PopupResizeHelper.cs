namespace Fluent;

using System.Runtime.CompilerServices;
using Windows.Foundation;

internal static class PopupResizeHelper
{
    internal const double ViewportMargin = 16;
    private static readonly ConditionalWeakTable<ResizeableContentControl, RequestedSize> RequestedSizes = new();

    internal static double NormalizeMaximum(double value) =>
        double.IsNaN(value) || value < 0 ? double.PositiveInfinity : value;

    internal static void Apply(
        ResizeableContentControl content,
        FrameworkElement owner,
        ContextMenuResizeMode mode,
        double minimumWidth,
        double minimumHeight,
        double maximumHeight,
        double initialHeight = double.NaN,
        bool resetHeight = false)
    {
        var requested = RequestedSizes.GetValue(content, static element => new RequestedSize(element));
        if (resetHeight)
        {
            requested.Height = initialHeight;
        }

        var viewport = owner.XamlRoot?.Size ?? new Size(double.PositiveInfinity, double.PositiveInfinity);
        var maxWidth = Math.Max(0, viewport.Width - (2 * ViewportMargin));
        var maxHeight = Math.Min(
            NormalizeMaximum(maximumHeight),
            Math.Max(0, viewport.Height - (2 * ViewportMargin)));
        content.FlowDirection = owner.FlowDirection;
        content.RequestedTheme = owner.ActualTheme;
        content.IsEnabled = owner is not Control { IsEnabled: false };
        for (DependencyObject? current = owner; current is FrameworkElement element;
             current = VisualTreeHelper.GetParent(current))
        {
            if (element.Resources.TryGetValue("RibbonCompactTargetSize", out _))
            {
                Fluent.Helpers.TouchTargetGeometry.PropagateCompactTargetSize(owner, content);
                break;
            }
        }
        content.ResizeMode = mode;
        var wasApplying = requested.Applying;
        requested.Applying = true;
        try
        {
            content.MinWidth = Math.Min(Math.Max(0, minimumWidth), maxWidth);
            content.MinHeight = Math.Min(Math.Max(0, minimumHeight), maxHeight);
            content.MaxWidth = maxWidth;
            content.MaxHeight = maxHeight;
            content.Width = Constrain(requested.Width, content.MinWidth, maxWidth);
            content.Height = Constrain(requested.Height, content.MinHeight, maxHeight);
        }
        finally
        {
            requested.Applying = wasApplying;
        }
    }

    internal static void RecordResize(ResizeableContentControl content, bool resizeWidth)
    {
        if (RequestedSizes.TryGetValue(content, out var requested))
        {
            if (resizeWidth)
            {
                requested.Width = content.Width;
            }

            // A user resize to the already-clamped value is still a new request.
            requested.Height = content.Height;
        }
    }

    private static double Constrain(double requested, double minimum, double maximum) =>
        double.IsNaN(requested)
            ? double.NaN
            : ResizeableContentControl.ClampResizeDimension(requested, minimum, maximum);

    private sealed class RequestedSize
    {
        internal double Width;
        internal double Height;
        internal bool Applying;

        internal RequestedSize(ResizeableContentControl content)
        {
            Width = content.Width;
            Height = content.Height;
            content.RegisterPropertyChangedCallback(FrameworkElement.WidthProperty, (sender, _) =>
            {
                if (!Applying)
                {
                    Width = ((FrameworkElement)sender).Width;
                }
            });
            content.RegisterPropertyChangedCallback(FrameworkElement.HeightProperty, (sender, _) =>
            {
                if (!Applying)
                {
                    Height = ((FrameworkElement)sender).Height;
                }
            });
        }
    }

    internal static bool IsResizeInteraction(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ResizeHandle
                || current is ResizeableContentControl { IsMouseOverResizeThumbs: true })
            {
                return true;
            }
        }

        return false;
    }

    internal static Point PlaceSubmenu(Rect owner, Size popup, Size viewport, bool rightToLeft)
    {
        var x = rightToLeft ? owner.Left - popup.Width : owner.Right;
        if (x < 0 || x + popup.Width > viewport.Width)
        {
            x = rightToLeft ? owner.Right : owner.Left - popup.Width;
        }

        return new Point(
            Math.Clamp(x, 0, Math.Max(0, viewport.Width - popup.Width)),
            Math.Clamp(owner.Top, 0, Math.Max(0, viewport.Height - popup.Height)));
    }
}

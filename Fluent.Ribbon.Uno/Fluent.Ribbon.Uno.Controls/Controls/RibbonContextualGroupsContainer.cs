namespace Fluent;

using Windows.Foundation;

/// <summary>
/// A panel that arranges <see cref="RibbonContextualTabGroup"/> headers across the ribbon
/// title bar, positioning each colored group header directly above its associated contextual
/// tabs (matching the WPF Fluent.Ribbon behaviour). The panel spans the full width of the
/// title bar and places each group by transforming its first/last visible tab's position into
/// the panel's own coordinate space.
/// </summary>
public partial class RibbonContextualGroupsContainer : Panel
{
    // Tracks the last arranged X for each group so LayoutUpdated can detect when the
    // underlying tabs have moved (resize / show / hide) and trigger a re-arrange.
    private readonly Dictionary<RibbonContextualTabGroup, double> _lastStartX = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonContextualGroupsContainer"/> class.
    /// </summary>
    public RibbonContextualGroupsContainer()
    {
        LayoutUpdated += OnLayoutUpdated;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
        var measureHeight = height <= 0 ? double.PositiveInfinity : height;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, measureHeight));
        }

        // The panel is a full-width overlay; it fills the space it's given and positions
        // children absolutely, so it doesn't need to claim a specific desired width.
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var rect = ComputeRect(child, finalSize);
            child.Arrange(rect);

            if (child is RibbonContextualTabGroup group)
            {
                _lastStartX[group] = rect.Width > 0 ? rect.X : double.NaN;
            }
        }

        return finalSize;
    }

    // Re-arrange when the tabs a group tracks have moved. ArrangeOverride records the applied
    // X per group; here we recompute the target X and invalidate only when it drifts, so the
    // layout converges and then stops (no feedback loop).
    private void OnLayoutUpdated(object? sender, object e)
    {
        if (Children.Count == 0)
        {
            return;
        }

        var size = new Size(ActualWidth, ActualHeight);

        foreach (var child in Children)
        {
            if (child is not RibbonContextualTabGroup group)
            {
                continue;
            }

            var rect = ComputeRect(group, size);
            var current = rect.Width > 0 ? rect.X : double.NaN;
            var applied = _lastStartX.TryGetValue(group, out var value) ? value : double.NaN;

            if (!NearlyEqual(applied, current))
            {
                InvalidateArrange();
                break;
            }
        }
    }

    // Computes the header rectangle for a group by aligning it above its contextual tabs.
    private Rect ComputeRect(UIElement child, Size finalSize)
    {
        if (child is not RibbonContextualTabGroup group
            || group.InnerVisibility != Visibility.Visible)
        {
            return default;
        }

        var first = group.FirstVisibleItem;
        var last = group.LastVisibleItem;

        if (first is null || last is null)
        {
            return default;
        }

        double startX;
        double endX;

        try
        {
            startX = first.TransformToVisual(this).TransformPoint(default).X;
            endX = last.TransformToVisual(this).TransformPoint(new Point(last.ActualWidth, 0)).X;
        }
        catch
        {
            // TransformToVisual can throw if an element is momentarily detached during layout.
            return default;
        }

        if (double.IsNaN(startX) || double.IsNaN(endX) || double.IsInfinity(startX) || double.IsInfinity(endX))
        {
            return default;
        }

        startX = Math.Max(0, startX);
        endX = Math.Min(finalSize.Width, endX);
        var width = Math.Max(0, endX - startX);

        if (width <= 0)
        {
            return default;
        }

        return new Rect(startX, 0, width, finalSize.Height);
    }

    private static bool NearlyEqual(double a, double b)
    {
        if (double.IsNaN(a) && double.IsNaN(b))
        {
            return true;
        }

        if (double.IsNaN(a) || double.IsNaN(b))
        {
            return false;
        }

        return Math.Abs(a - b) < 0.5;
    }
}

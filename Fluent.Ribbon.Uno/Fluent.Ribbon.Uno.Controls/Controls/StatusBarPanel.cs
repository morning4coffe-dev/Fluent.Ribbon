namespace Fluent;

/// <summary>
/// A panel for the <see cref="RibbonStatusBar"/> that arranges children from left to right,
/// with right-aligned items placed at the end.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
public partial class StatusBarPanel : Panel
{
    /// <inheritdoc/>
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        double maxHeight = 0;
        double totalWidth = 0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            totalWidth += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        var width = double.IsPositiveInfinity(availableSize.Width)
            ? totalWidth
            : availableSize.Width;

        return new Windows.Foundation.Size(width, maxHeight);
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        // Separate left-aligned and right-aligned children
        var leftChildren = new List<UIElement>();
        var rightChildren = new List<UIElement>();

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (child is FrameworkElement fe && fe.HorizontalAlignment == HorizontalAlignment.Right)
            {
                rightChildren.Add(child);
            }
            else
            {
                leftChildren.Add(child);
            }
        }

        // Arrange left-aligned children
        double leftOffset = 0;

        foreach (var child in leftChildren)
        {
            var width = child.DesiredSize.Width;
            child.Arrange(new Windows.Foundation.Rect(leftOffset, 0, width, finalSize.Height));
            leftOffset += width;
        }

        // Arrange right-aligned children (from right edge)
        double rightOffset = finalSize.Width;

        for (var i = rightChildren.Count - 1; i >= 0; i--)
        {
            var child = rightChildren[i];
            var width = child.DesiredSize.Width;
            rightOffset -= width;
            child.Arrange(new Windows.Foundation.Rect(rightOffset, 0, width, finalSize.Height));
        }

        return finalSize;
    }
}

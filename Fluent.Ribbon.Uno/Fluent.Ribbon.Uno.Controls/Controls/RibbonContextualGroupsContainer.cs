namespace Fluent;

/// <summary>
/// A panel that arranges <see cref="RibbonContextualTabGroup"/> controls,
/// sizing each group to match its associated visible tab items' width.
/// </summary>
public partial class RibbonContextualGroupsContainer : Panel
{
    private readonly List<Windows.Foundation.Size> _sizes = new();

    /// <inheritdoc />
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        var allGroupsWidth = 0.0;
        _sizes.Clear();

        var availableHeight = double.IsPositiveInfinity(availableSize.Height) ? 0 : availableSize.Height;

        foreach (var child in Children)
        {
            if (child is not RibbonContextualTabGroup contextualGroup)
            {
                child.Measure(new Windows.Foundation.Size(0, availableHeight));
                _sizes.Add(new Windows.Foundation.Size(0, availableHeight));
                continue;
            }

            // Calculate total width of visible tab items in this group
            var tabsWidth = 0.0;
            var visibleItems = contextualGroup.Items
                .Where(item => item.Visibility == Visibility.Visible && item.DesiredSize.Width > 0)
                .ToList();

            foreach (var item in visibleItems)
            {
                tabsWidth += item.DesiredSize.Width;
            }

            var finalWidth = tabsWidth;
            allGroupsWidth += finalWidth;

            if (allGroupsWidth > availableSize.Width)
            {
                finalWidth -= allGroupsWidth - availableSize.Width;
                allGroupsWidth = availableSize.Width;
            }

            finalWidth = Math.Max(0, finalWidth);
            contextualGroup.Measure(new Windows.Foundation.Size(finalWidth, availableHeight));
            _sizes.Add(new Windows.Foundation.Size(finalWidth, availableHeight));
        }

        return new Windows.Foundation.Size(allGroupsWidth, availableHeight);
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        double x = 0;

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var size = i < _sizes.Count ? _sizes[i] : new Windows.Foundation.Size(0, finalSize.Height);

            child.Arrange(new Windows.Foundation.Rect(x, 0, size.Width, Math.Max(finalSize.Height, size.Height)));
            x += size.Width;
        }

        return finalSize;
    }
}

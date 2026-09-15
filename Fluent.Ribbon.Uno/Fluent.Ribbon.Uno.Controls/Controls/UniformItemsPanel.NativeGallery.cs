#if WINDOWS
namespace Fluent;

using Windows.Foundation;

public partial class UniformItemsPanel
{
    private RibbonGallery? nativeGallery;
    private Canvas? nativeHeaderHost;
    private readonly Dictionary<string, TextBlock> nativeHeaders = new(StringComparer.Ordinal);
    private double nativeCellWidth;
    private double nativeCellHeight;

    internal void ConfigureNativeGallery(RibbonGallery? gallery, Canvas? headerHost)
    {
        if (!ReferenceEquals(headerHost, nativeHeaderHost))
        {
            foreach (var header in nativeHeaders.Values)
            {
                nativeHeaderHost?.Children.Remove(header);
            }
            nativeHeaders.Clear();
        }

        nativeGallery = gallery;
        nativeHeaderHost = headerHost;
        var groups = gallery is not null && (gallery.IsGrouped || !string.IsNullOrEmpty(gallery.GroupBy))
            ? gallery.Items.Select(gallery.GetNativeGalleryGroup)
                .Where(group => !string.IsNullOrEmpty(group)).Distinct(StringComparer.Ordinal).ToArray()
            : [];
        foreach (var group in nativeHeaders.Keys.Except(groups, StringComparer.Ordinal).ToArray())
        {
            nativeHeaderHost?.Children.Remove(nativeHeaders[group]);
            nativeHeaders.Remove(group);
        }

        if (nativeHeaderHost is not null)
        {
            foreach (var group in groups)
            {
                if (nativeHeaders.ContainsKey(group))
                {
                    continue;
                }

                var header = new TextBlock
                {
                    Text = group,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 11,
                    Margin = new Thickness(4, 8, 4, 4),
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["RibbonSecondaryTextBrush"],
                };
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(header, group);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetHeadingLevel(
                    header, Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level3);
                nativeHeaders.Add(group, header);
                nativeHeaderHost.Children.Add(header);
            }
        }

        InvalidateMeasure();
    }

    private IEnumerable<IGrouping<string, UIElement>> GetNativeVisibleGroups()
    {
        var grouped = nativeGallery!.IsGrouped || !string.IsNullOrEmpty(nativeGallery.GroupBy);
        return Children.Where(child => child.Visibility != Visibility.Collapsed)
            .GroupBy(child => grouped ? nativeGallery.GetNativeGalleryGroup(child) : string.Empty, StringComparer.Ordinal)
            .OrderBy(group => string.IsNullOrEmpty(group.Key) ? 1 : 0);
    }

    private Size MeasureNativeGallery(Size availableSize)
    {
        var width = ItemWidth > 0 ? ItemWidth : 0;
        var height = ItemHeight > 0 ? ItemHeight : 0;
        var constraint = new Size(width > 0 ? width : double.PositiveInfinity,
            height > 0 ? height : double.PositiveInfinity);
        nativeCellWidth = width;
        nativeCellHeight = height;
        foreach (var child in Children)
        {
            var item = nativeGallery!.GetNativeGalleryItem(child);
            if (!ReferenceEquals(item, child))
            {
                child.Visibility = item.Visibility;
            }
            child.Measure(constraint);
            if (child.Visibility != Visibility.Collapsed)
            {
                if (width == 0)
                {
                    nativeCellWidth = Math.Max(nativeCellWidth, child.DesiredSize.Width);
                }
                if (height == 0)
                {
                    nativeCellHeight = Math.Max(nativeCellHeight, child.DesiredSize.Height);
                }
            }
        }

        var groups = GetNativeVisibleGroups().ToArray();
        var visibleGroups = groups.Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var (group, header) in nativeHeaders)
        {
            header.Visibility = visibleGroups.Contains(group) ? Visibility.Visible : Visibility.Collapsed;
            header.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        }

        double desiredWidth = 0, desiredHeight = 0;
        foreach (var group in groups)
        {
            if (nativeHeaders.TryGetValue(group.Key, out var header))
            {
                desiredWidth = Math.Max(desiredWidth, header.DesiredSize.Width);
                desiredHeight += header.DesiredSize.Height;
            }

            var count = group.Count();
            var columns = ComputeColumns(availableSize.Width, nativeCellWidth, count);
            desiredWidth = Math.Max(desiredWidth, columns * nativeCellWidth);
            desiredHeight += Math.Ceiling((double)count / columns) * nativeCellHeight;
        }

        return new Size(desiredWidth, desiredHeight);
    }

    private Size ArrangeNativeGallery(Size finalSize)
    {
        double y = 0;
        foreach (var group in GetNativeVisibleGroups())
        {
            if (nativeHeaders.TryGetValue(group.Key, out var header))
            {
                // The overlay owns headings, never item containers. Its origin matches ItemsPresenter.
                Canvas.SetLeft(header, 0);
                Canvas.SetTop(header, y);
                var width = Math.Max(0, finalSize.Width - header.Margin.Left - header.Margin.Right);
                if (header.Width != width)
                {
                    header.Width = width;
                }
                y += header.DesiredSize.Height;
            }

            var items = group.ToArray();
            var columns = ComputeColumns(finalSize.Width, nativeCellWidth, items.Length);
            for (var index = 0; index < items.Length; index++)
            {
                items[index].Arrange(new Rect(
                    index % columns * nativeCellWidth,
                    y + index / columns * nativeCellHeight,
                    nativeCellWidth, nativeCellHeight));
            }
            y += Math.Ceiling((double)items.Length / columns) * nativeCellHeight;
        }

        foreach (var child in Children.Where(child => child.Visibility == Visibility.Collapsed))
        {
            child.Arrange(new Rect());
        }

        return finalSize;
    }
}
#endif

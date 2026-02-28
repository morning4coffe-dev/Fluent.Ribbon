using Windows.Foundation;

namespace Fluent;

/// <summary>
/// Internal placeholder element used in <see cref="GalleryPanel"/> to represent
/// an item that hasn't been realized yet. Used for virtualization scenarios.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. In WPF this inherits ContentElement;
/// in Uno/WinUI we use a lightweight FrameworkElement instead.
/// </remarks>
internal partial class GalleryItemPlaceholder : FrameworkElement
{
    /// <summary>
    /// Gets or sets the target item this placeholder represents.
    /// </summary>
    public object? Target { get; set; }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        return new Windows.Foundation.Size(0, 0);
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        return new Windows.Foundation.Size(0, 0);
    }
}

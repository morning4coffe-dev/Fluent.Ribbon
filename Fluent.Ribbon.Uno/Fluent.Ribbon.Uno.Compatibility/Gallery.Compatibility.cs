namespace Fluent;

public partial class Gallery
{
    /// <summary>
    /// WPF read-only-property key substitute. WinUI exposes the dependency property directly.
    /// </summary>
    public static readonly DependencyProperty IsLastItemPropertyKey =
        IsLastItemProperty;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride()
    {
        return new GalleryItem();
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is GalleryItem or RibbonGalleryItem;
    }
}

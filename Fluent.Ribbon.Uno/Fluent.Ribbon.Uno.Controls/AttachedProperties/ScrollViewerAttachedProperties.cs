namespace Fluent;

/// <summary>
/// Attached properties for <see cref="ScrollViewer"/>.
/// </summary>
public partial class ScrollViewerAttachedProperties : DependencyObject
{
    /// <summary>
    /// Identifies the style used for scroll bars inside a scroll viewer.
    /// </summary>
    public static readonly DependencyProperty ScrollBarStyleProperty =
        DependencyProperty.RegisterAttached(
            "ScrollBarStyle",
            typeof(Style),
            typeof(ScrollViewerAttachedProperties),
            new PropertyMetadata(null));

    /// <summary>
    /// Sets the scroll-bar style.
    /// </summary>
    public static void SetScrollBarStyle(DependencyObject element, Style? value)
    {
        element.SetValue(ScrollBarStyleProperty, value);
    }

    /// <summary>
    /// Gets the scroll-bar style.
    /// </summary>
    public static Style? GetScrollBarStyle(DependencyObject element)
    {
        return (Style?)element.GetValue(ScrollBarStyleProperty);
    }
}

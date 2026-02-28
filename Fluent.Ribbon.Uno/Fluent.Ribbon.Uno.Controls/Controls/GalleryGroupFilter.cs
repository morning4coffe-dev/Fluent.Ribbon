namespace Fluent;

/// <summary>
/// Represents a filter for gallery groups.
/// Used to select which groups of items are visible in a gallery.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public partial class GalleryGroupFilter : DependencyObject
{
    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(GalleryGroupFilter),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the display title for this filter.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="Groups"/> dependency property.</summary>
    public static readonly DependencyProperty GroupsProperty =
        DependencyProperty.Register(
            nameof(Groups),
            typeof(string),
            typeof(GalleryGroupFilter),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets a comma-separated list of group names that should be visible when this filter is active.
    /// </summary>
    public string Groups
    {
        get => (string)GetValue(GroupsProperty);
        set => SetValue(GroupsProperty, value);
    }

    /// <summary>
    /// Gets the group names as an array.
    /// </summary>
    /// <returns>Array of group name strings.</returns>
    public string[] GetGroupNames()
    {
        return (Groups ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();
    }
}

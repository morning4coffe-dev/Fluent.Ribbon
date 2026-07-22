namespace Fluent;

/// <summary>
/// Interface for controls that support toggle button behavior.
/// </summary>
public interface IToggleButton
{
    /// <summary>
    /// Gets or sets the name of the group that the toggle button belongs to.
    /// Use the GroupName property to specify a grouping of toggle buttons to
    /// create a mutually exclusive set of controls.
    /// </summary>
    string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the toggle button is checked.
    /// </summary>
    bool? IsChecked { get; set; }

    /// <summary>
    /// Gets whether the toggle button is loaded.
    /// </summary>
    bool IsLoaded => this is FrameworkElement element && element.IsLoaded;

    /// <summary>
    /// Gets the visual parent of the toggle button.
    /// </summary>
    DependencyObject Parent =>
        this is DependencyObject dependencyObject
            ? VisualTreeHelper.GetParent(dependencyObject)
            : null!;
}

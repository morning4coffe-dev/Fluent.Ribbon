namespace Fluent;

/// <summary>
/// WPF-compatible shared-size metadata for <see cref="RibbonGroupBoxWrapPanel"/>.
/// </summary>
public partial class RibbonGroupBoxWrapPanel
{
    /// <summary>Identifies the shared-size group-name attached property.</summary>
    public static readonly DependencyProperty SharedSizeGroupNameProperty =
        DependencyProperty.RegisterAttached(
            "SharedSizeGroupName",
            typeof(string),
            typeof(RibbonGroupBoxWrapPanel),
            new PropertyMetadata(null));

    /// <summary>Gets the shared-size group name.</summary>
    public static string? GetSharedSizeGroupName(DependencyObject element) =>
        (string?)element.GetValue(SharedSizeGroupNameProperty);

    /// <summary>Sets the shared-size group name.</summary>
    public static void SetSharedSizeGroupName(DependencyObject element, string? value) =>
        element.SetValue(SharedSizeGroupNameProperty, value);

    /// <summary>Identifies the exclude-from-shared-size attached property.</summary>
    public static readonly DependencyProperty ExcludeFromSharedSizeProperty =
        DependencyProperty.RegisterAttached(
            "ExcludeFromSharedSize",
            typeof(bool),
            typeof(RibbonGroupBoxWrapPanel),
            new PropertyMetadata(false));

    /// <summary>Gets whether an element is excluded from shared sizing.</summary>
    public static bool GetExcludeFromSharedSize(DependencyObject element) =>
        (bool)element.GetValue(ExcludeFromSharedSizeProperty);

    /// <summary>Sets whether an element is excluded from shared sizing.</summary>
    public static void SetExcludeFromSharedSize(DependencyObject element, bool value) =>
        element.SetValue(ExcludeFromSharedSizeProperty, value);
}

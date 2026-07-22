namespace Fluent.StyleSelectors;

/// <summary>
/// Selects the application-menu item-container style.
/// </summary>
public class ApplicationMenuItemContainerStyleSelector : StyleSelector
{
    /// <summary>
    /// Gets the shared selector instance.
    /// </summary>
    public static ApplicationMenuItemContainerStyleSelector Instance { get; } = new();

    /// <summary>Selects a style using the WPF-compatible public method.</summary>
    public new virtual Style? SelectStyle(object item, DependencyObject container)
    {
        return SelectStyleCore(item, container);
    }

    /// <inheritdoc />
    protected override Style? SelectStyleCore(object item, DependencyObject container)
    {
        return item is RibbonMenuItem
            ? FindStyle(container, "Fluent.Ribbon.Styles.ApplicationMenu.MenuItem")
            : base.SelectStyleCore(item, container);
    }

    internal static Style? FindStyle(DependencyObject container, string key)
    {
        if (container is FrameworkElement element
            && element.Resources.TryGetValue(key, out var localStyle)
            && localStyle is Style style)
        {
            return style;
        }

        return Application.Current?.Resources.TryGetValue(key, out var applicationStyle) == true
            ? applicationStyle as Style
            : null;
    }
}

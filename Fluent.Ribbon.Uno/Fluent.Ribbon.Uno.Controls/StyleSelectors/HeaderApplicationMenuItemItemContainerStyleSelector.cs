namespace Fluent.StyleSelectors;

/// <summary>
/// Selects second-level application-menu styles.
/// </summary>
public class HeaderApplicationMenuItemItemContainerStyleSelector : StyleSelector
{
    /// <summary>
    /// Gets the shared selector instance.
    /// </summary>
    public static HeaderApplicationMenuItemItemContainerStyleSelector Instance { get; } = new();

    /// <summary>Selects a style using the WPF-compatible public method.</summary>
    public new virtual Style? SelectStyle(object item, DependencyObject container)
    {
        return SelectStyleCore(item, container);
    }

    /// <inheritdoc />
    protected override Style? SelectStyleCore(object item, DependencyObject container)
    {
        return item is RibbonMenuItem
            ? ApplicationMenuItemContainerStyleSelector.FindStyle(
                container,
                "Fluent.Ribbon.Styles.ApplicationMenu.MenuItemSecondLevel")
            : base.SelectStyleCore(item, container);
    }
}

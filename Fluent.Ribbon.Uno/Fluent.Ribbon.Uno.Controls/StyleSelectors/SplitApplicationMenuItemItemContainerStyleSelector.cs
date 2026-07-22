namespace Fluent.StyleSelectors;

/// <summary>
/// Selects second-level styles for split application-menu items.
/// </summary>
public class SplitApplicationMenuItemItemContainerStyleSelector : StyleSelector
{
    /// <summary>
    /// Gets the shared selector instance.
    /// </summary>
    public static SplitApplicationMenuItemItemContainerStyleSelector Instance { get; } = new();

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

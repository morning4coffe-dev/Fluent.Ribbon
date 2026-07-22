namespace Fluent.StyleSelectors;

/// <summary>
/// Selects item-container styles for Backstage tabs.
/// </summary>
public class BackstageTabControlItemContainerStyleSelector : StyleSelector
{
    /// <summary>
    /// Gets the shared selector instance.
    /// </summary>
    public static BackstageTabControlItemContainerStyleSelector Instance { get; } = new();

    /// <summary>Selects a style using the WPF-compatible public method.</summary>
    public new virtual Style? SelectStyle(object item, DependencyObject container)
    {
        return SelectStyleCore(item, container);
    }

    /// <inheritdoc />
    protected override Style? SelectStyleCore(object item, DependencyObject container)
    {
        return item switch
        {
            RibbonButton => ApplicationMenuItemContainerStyleSelector.FindStyle(
                container,
                "Fluent.Ribbon.Styles.BackstageTabControl.Button"),
            SeparatorTabItem => ApplicationMenuItemContainerStyleSelector.FindStyle(
                container,
                "Fluent.Ribbon.Styles.BackstageTabControl.SeparatorTabItem"),
            _ => base.SelectStyleCore(item, container),
        };
    }
}

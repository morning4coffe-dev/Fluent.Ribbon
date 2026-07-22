namespace Fluent.TemplateSelectors;

/// <summary>
/// Selects one-line or two-line Ribbon group header templates.
/// </summary>
public class RibbonGroupBoxHeaderTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets the shared selector instance.
    /// </summary>
    public static readonly RibbonGroupBoxHeaderTemplateSelector Instance = new();

    /// <summary>Selects a template using the WPF-compatible public entry point.</summary>
    public new virtual DataTemplate? SelectTemplate(object item, DependencyObject container)
        => SelectTemplateCore(item, container);

    /// <inheritdoc />
    protected override DataTemplate? SelectTemplateCore(
        object item,
        DependencyObject container)
    {
        var key = RibbonGroupBox.GetIsCollapsedHeaderContentPresenter(container)
            ? "Fluent.Ribbon.DataTemplates.RibbonGroupBox.TwoLineHeader"
            : "Fluent.Ribbon.DataTemplates.RibbonGroupBox.OneLineHeader";

        if (container is FrameworkElement element
            && element.Resources.TryGetValue(key, out var localTemplate)
            && localTemplate is DataTemplate template)
        {
            return template;
        }

        return Application.Current?.Resources.TryGetValue(key, out var applicationTemplate) == true
            ? applicationTemplate as DataTemplate
            : base.SelectTemplateCore(item, container);
    }
}

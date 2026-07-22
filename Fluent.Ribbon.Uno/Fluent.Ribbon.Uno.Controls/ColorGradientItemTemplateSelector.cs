namespace Fluent;

/// <summary>
/// Selects top, center, or bottom gradient color templates.
/// </summary>
public class ColorGradientItemTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Selects a template for the supplied gradient item and container.
    /// </summary>
    public new virtual DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        SelectTemplateCore(item, container);

    /// <inheritdoc />
    protected override DataTemplate? SelectTemplateCore(
        object item,
        DependencyObject container)
    {
        if (item is null)
        {
            return null;
        }

        var itemsControl = FindAncestor<ItemsControl>(container);
        var colorGallery = FindAncestor<ColorGallery>(container);
        if (itemsControl is null || colorGallery is null)
        {
            return null;
        }

        var index = itemsControl.Items.IndexOf(item);
        var key = index < colorGallery.Columns
            ? "Fluent.Ribbon.DataTemplates.GradientColorTopData"
            : index >= itemsControl.Items.Count - colorGallery.Columns
                ? "Fluent.Ribbon.DataTemplates.GradientColorBottomData"
                : "Fluent.Ribbon.DataTemplates.GradientColorCenterData";

        if (itemsControl.Resources.TryGetValue(key, out var localTemplate)
            && localTemplate is DataTemplate template)
        {
            return template;
        }

        return Application.Current?.Resources.TryGetValue(key, out var applicationTemplate) == true
            ? applicationTemplate as DataTemplate
            : null;
    }

    private static T? FindAncestor<T>(DependencyObject element)
        where T : class
    {
        var current = VisualTreeHelper.GetParent(element);
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}

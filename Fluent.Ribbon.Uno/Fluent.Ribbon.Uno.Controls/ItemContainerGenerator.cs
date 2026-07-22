namespace Fluent;

/// <summary>
/// Portable item-container lookup used by gallery compatibility APIs.
/// </summary>
public class ItemContainerGenerator
{
    /// <summary>Returns the data item represented by a realized container.</summary>
    public virtual object? ItemFromContainerOrContainerContent(object? container)
    {
        return container is FrameworkElement { DataContext: not null } element
            ? element.DataContext
            : container;
    }
}

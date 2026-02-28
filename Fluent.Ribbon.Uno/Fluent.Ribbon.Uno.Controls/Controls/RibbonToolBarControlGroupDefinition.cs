namespace Fluent;

/// <summary>
/// Defines a group of control definitions in a <see cref="RibbonToolBarLayoutDefinition"/>.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public partial class RibbonToolBarControlGroupDefinition : DependencyObject
{
    /// <summary>
    /// Gets the collection of control definitions in this group.
    /// </summary>
    public ObservableCollection<RibbonToolBarControlDefinition> Children { get; } = new();

    /// <summary>
    /// Occurs when the <see cref="Children"/> collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? ChildrenChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToolBarControlGroupDefinition"/> class.
    /// </summary>
    public RibbonToolBarControlGroupDefinition()
    {
        Children.CollectionChanged += (s, e) => ChildrenChanged?.Invoke(s, e);
    }
}

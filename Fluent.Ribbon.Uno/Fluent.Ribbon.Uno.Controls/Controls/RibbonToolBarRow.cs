namespace Fluent;

/// <summary>
/// A row in a <see cref="RibbonToolBarLayoutDefinition"/>.
/// Contains a mix of <see cref="RibbonToolBarControlDefinition"/> and
/// <see cref="RibbonToolBarControlGroupDefinition"/> objects.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public partial class RibbonToolBarRow : DependencyObject
{
    /// <summary>
    /// Gets the collection of children (control definitions and group definitions).
    /// </summary>
    public ObservableCollection<DependencyObject> Children { get; } = new();
}

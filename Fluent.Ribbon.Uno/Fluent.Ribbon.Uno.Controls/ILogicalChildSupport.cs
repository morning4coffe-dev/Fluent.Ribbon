namespace Fluent;

/// <summary>
/// Interface for controls that support logical child management.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// In WPF, controls override LogicalChildren; in Uno this is typically handled differently.
/// </remarks>
public interface ILogicalChildSupport
{
    /// <summary>
    /// Adds a logical child.
    /// </summary>
    void AddLogicalChild(object child);

    /// <summary>
    /// Removes a logical child.
    /// </summary>
    void RemoveLogicalChild(object child);
}

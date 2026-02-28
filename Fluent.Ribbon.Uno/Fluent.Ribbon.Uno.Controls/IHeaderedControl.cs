namespace Fluent;

/// <summary>
/// Defines a control that provides a header.
/// </summary>
public interface IHeaderedControl
{
    /// <summary>
    /// Gets or sets the header content.
    /// </summary>
    object? Header { get; set; }
}

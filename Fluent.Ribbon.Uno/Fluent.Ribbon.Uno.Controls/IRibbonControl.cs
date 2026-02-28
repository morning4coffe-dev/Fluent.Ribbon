namespace Fluent;

/// <summary>
/// Base interface for Fluent ribbon controls.
/// </summary>
public interface IRibbonControl : IHeaderedControl, IKeyTipedControl
{
    /// <summary>
    /// Gets or sets the size of the control.
    /// </summary>
    RibbonControlSize Size { get; set; }

    /// <summary>
    /// Gets or sets the icon for the element.
    /// </summary>
    object? Icon { get; set; }
}

namespace Fluent;

/// <summary>
/// Base interface for Fluent ribbon controls.
/// </summary>
public interface IRibbonControl : IHeaderedControl, IKeyTipedControl, ILogicalChildSupport
{
    /// <summary>
    /// Gets or sets the size of the control.
    /// </summary>
    RibbonControlSize Size { get; set; }

    /// <summary>
    /// Gets or sets the size definition for the control.
    /// </summary>
    RibbonControlSizeDefinition SizeDefinition
    {
        get => new(Size, Size, Size);
        set => Size = value.Large;
    }

    /// <summary>
    /// Gets or sets the icon for the element.
    /// </summary>
    object? Icon { get; set; }
}

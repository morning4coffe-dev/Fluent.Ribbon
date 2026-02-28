namespace Fluent;

/// <summary>
/// Defines an interface for ribbon controls that can be scaled.
/// </summary>
public interface IScalableRibbonControl
{
    /// <summary>
    /// Gets or sets the size of the control.
    /// </summary>
    RibbonControlSize Size { get; set; }

    /// <summary>
    /// Scales the control to the specified size definition.
    /// </summary>
    /// <param name="size">The size to scale to.</param>
    void ScaleTo(RibbonControlSize size);
}

namespace Fluent;

/// <summary>
/// Defines an interface for ribbon controls that can be scaled.
/// </summary>
public interface IScalableRibbonControl
{
    /// <summary>
    /// Resets the control to its largest scale.
    /// </summary>
    void ResetScale()
    {
        ScaleTo(RibbonControlSize.Large);
    }

    /// <summary>
    /// Enlarges the control by one logical size.
    /// </summary>
    void Enlarge()
    {
        ScaleTo(Size switch
        {
            RibbonControlSize.Small => RibbonControlSize.Medium,
            RibbonControlSize.Medium => RibbonControlSize.Large,
            _ => RibbonControlSize.Large,
        });
    }

    /// <summary>
    /// Reduces the control by one logical size.
    /// </summary>
    void Reduce()
    {
        ScaleTo(Size switch
        {
            RibbonControlSize.Large => RibbonControlSize.Medium,
            RibbonControlSize.Medium => RibbonControlSize.Small,
            _ => RibbonControlSize.Small,
        });
    }

    /// <summary>
    /// Occurs when the control is scaled.
    /// </summary>
    event EventHandler Scaled
    {
        add
        {
        }

        remove
        {
        }
    }

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

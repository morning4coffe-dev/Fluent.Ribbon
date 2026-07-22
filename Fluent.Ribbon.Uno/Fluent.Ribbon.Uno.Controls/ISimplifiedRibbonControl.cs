namespace Fluent;

/// <summary>
/// Base interface for controls that support simplified ribbon mode.
/// </summary>
public interface ISimplifiedRibbonControl : ISimplifiedStateControl
{
    /// <summary>
    /// Gets or sets the size definition used in simplified mode.
    /// </summary>
    RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => new(RibbonControlSize.Large, RibbonControlSize.Medium, RibbonControlSize.Small);
        set
        {
        }
    }

    /// <summary>
    /// Gets or sets whether the ribbon is in simplified mode.
    /// </summary>
    bool IsSimplified { get; }
}

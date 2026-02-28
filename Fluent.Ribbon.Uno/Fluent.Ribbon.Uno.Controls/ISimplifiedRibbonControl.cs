namespace Fluent;

/// <summary>
/// Base interface for controls that support simplified ribbon mode.
/// </summary>
public interface ISimplifiedRibbonControl : ISimplifiedStateControl
{
    /// <summary>
    /// Gets or sets whether the ribbon is in simplified mode.
    /// </summary>
    bool IsSimplified { get; }
}

namespace Fluent;

/// <summary>
/// Base interface for controls requiring simplified state.
/// </summary>
public interface ISimplifiedStateControl
{
    /// <summary>
    /// Updates the simplified state.
    /// </summary>
    /// <param name="isSimplified">Whether the control should be in simplified mode.</param>
    void UpdateSimplifiedState(bool isSimplified);
}

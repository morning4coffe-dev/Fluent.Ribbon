namespace Fluent.Extensibility;

/// <summary>
/// Allows a control to contribute custom KeyTip metadata.
/// </summary>
public interface IKeyTipInformationProvider
{
    /// <summary>
    /// Gets the KeyTip metadata belonging to the current control.
    /// </summary>
    IEnumerable<KeyTipInformation> GetKeyTipInformations(bool hide);
}

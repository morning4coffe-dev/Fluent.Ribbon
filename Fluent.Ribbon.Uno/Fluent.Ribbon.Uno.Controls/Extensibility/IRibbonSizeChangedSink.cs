namespace Fluent.Extensibility;

/// <summary>
/// Receives notifications when a ribbon control changes logical size.
/// </summary>
public interface IRibbonSizeChangedSink
{
    /// <summary>
    /// Called after the logical size changes.
    /// </summary>
    void OnSizePropertyChanged(RibbonControlSize previous, RibbonControlSize current);
}

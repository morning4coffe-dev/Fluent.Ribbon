namespace Fluent;

/// <summary>
/// Interface for ribbon windows that expose the title bar.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// In WPF this is used by RibbonWindow; in Uno platform this can be implemented
/// by any container hosting a ribbon.
/// </remarks>
public interface IRibbonWindow
{
    /// <summary>
    /// Gets the <see cref="RibbonTitleBar"/> instance from this window.
    /// </summary>
    RibbonTitleBar? TitleBar { get; }
}

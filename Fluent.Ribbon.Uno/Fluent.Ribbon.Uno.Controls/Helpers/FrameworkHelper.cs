namespace Fluent.Helpers;

/// <summary>
/// Provides framework version and environment information.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
public static class FrameworkHelper
{
    /// <summary>
    /// Gets a value indicating whether the application is running in design mode.
    /// </summary>
    public static bool IsInDesignMode
    {
        get
        {
#if HAS_UNO
            return false;
#else
            return Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#endif
        }
    }
}

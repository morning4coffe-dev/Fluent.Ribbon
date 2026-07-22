namespace Fluent.Modern.Media;

using Microsoft.UI.Composition.SystemBackdrops;

/// <summary>
/// <para><b>Modern extension</b> — describes the opt-in backdrop material to apply to a ribbon host window.</para>
/// </summary>
[ModernExtension]
public enum RibbonBackdropKind
{
    /// <summary>No system backdrop.</summary>
    None,

    /// <summary>Windows Mica base material.</summary>
    Mica,

    /// <summary>Windows Mica alternate material.</summary>
    MicaAlt,

    /// <summary>Desktop acrylic material.</summary>
    Acrylic,
}

/// <summary>
/// <para><b>Modern extension</b> — applies WinUI system backdrops without throwing on unsupported platforms.</para>
/// </summary>
[ModernExtension]
public static class RibbonBackdrop
{
    #region Methods

    /// <summary>
    /// Attempts to apply the requested system backdrop to a window.
    /// </summary>
    /// <param name="window">The app window to update.</param>
    /// <param name="kind">The backdrop material to apply.</param>
    /// <returns><c>true</c> when the backdrop request was applied; otherwise <c>false</c>.</returns>
    public static bool TryApply(Window window, RibbonBackdropKind kind)
    {
        try
        {
            if (window is null)
            {
                return false;
            }

            window.SystemBackdrop = kind switch
            {
                RibbonBackdropKind.None => null,
                RibbonBackdropKind.Mica => new MicaBackdrop { Kind = MicaKind.Base },
                RibbonBackdropKind.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                RibbonBackdropKind.Acrylic => new DesktopAcrylicBackdrop(),
                _ => null,
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

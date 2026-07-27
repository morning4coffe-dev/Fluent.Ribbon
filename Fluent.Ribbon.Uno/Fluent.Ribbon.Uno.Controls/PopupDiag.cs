namespace Fluent;

using System.IO;

// TEMPORARY DIAGNOSTICS — remove after the pointer/click root-cause determination.
// Gated behind the SHOWCASE_POPUPLOG environment variable so it has zero effect on normal runs.
internal static class PopupDiag
{
    private static readonly string? LogPath = Environment.GetEnvironmentVariable("SHOWCASE_POPUPLOG");

    internal static bool Enabled => !string.IsNullOrEmpty(LogPath);

    internal static void Log(string message)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            File.AppendAllText(LogPath!, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostics must never affect behaviour.
        }
    }
}

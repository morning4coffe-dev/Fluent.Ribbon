namespace Fluent;

/// <summary>
/// State storage implementation that persists ribbon state (minimized, QAT position, simplified)
/// to local application settings.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. WPF version uses IsolatedStorage;
/// Uno/WinUI uses ApplicationData.Current.LocalSettings.
/// </remarks>
public class RibbonStateStorage : IRibbonStateStorage
{
    private const string StorageKey = "FluentRibbon";
    private const string IsMinimizedKey = "IsMinimized";
    private const string ShowQATBelowKey = "ShowQuickAccessToolBarBelowRibbon";
    private const string IsSimplifiedKey = "IsSimplified";

    // Temporary in-memory snapshot
    private bool _tempIsMinimized;
    private bool _tempShowQATBelow;
    private bool _tempIsSimplified;

    /// <inheritdoc/>
    public bool IsMinimized { get; set; }

    /// <inheritdoc/>
    public bool ShowQuickAccessToolBarBelowRibbon { get; set; }

    /// <inheritdoc/>
    public bool IsSimplified { get; set; }

    /// <inheritdoc/>
    public void Save()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var container = settings.CreateContainer(StorageKey, Windows.Storage.ApplicationDataCreateDisposition.Always);

            container.Values[IsMinimizedKey] = IsMinimized;
            container.Values[ShowQATBelowKey] = ShowQuickAccessToolBarBelowRibbon;
            container.Values[IsSimplifiedKey] = IsSimplified;
        }
        catch
        {
            // Silently fail if storage is unavailable (e.g., some Uno targets)
        }
    }

    /// <inheritdoc/>
    public void Load()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (settings.Containers.ContainsKey(StorageKey))
            {
                var container = settings.Containers[StorageKey];

                if (container.Values.TryGetValue(IsMinimizedKey, out var isMin) && isMin is bool b1)
                {
                    IsMinimized = b1;
                }

                if (container.Values.TryGetValue(ShowQATBelowKey, out var showBelow) && showBelow is bool b2)
                {
                    ShowQuickAccessToolBarBelowRibbon = b2;
                }

                if (container.Values.TryGetValue(IsSimplifiedKey, out var isSimp) && isSimp is bool b3)
                {
                    IsSimplified = b3;
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }

    /// <inheritdoc/>
    public void SaveTemporary()
    {
        _tempIsMinimized = IsMinimized;
        _tempShowQATBelow = ShowQuickAccessToolBarBelowRibbon;
        _tempIsSimplified = IsSimplified;
    }

    /// <inheritdoc/>
    public void LoadTemporary()
    {
        IsMinimized = _tempIsMinimized;
        ShowQuickAccessToolBarBelowRibbon = _tempShowQATBelow;
        IsSimplified = _tempIsSimplified;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        IsMinimized = false;
        ShowQuickAccessToolBarBelowRibbon = false;
        IsSimplified = false;

        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.DeleteContainer(StorageKey);
        }
        catch
        {
            // Silently fail
        }
    }
}

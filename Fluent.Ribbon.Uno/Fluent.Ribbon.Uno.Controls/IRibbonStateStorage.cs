namespace Fluent;

/// <summary>
/// Interface for ribbon state storage.
/// Allows persisting and restoring ribbon state (minimized, QAT position, etc.)
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public interface IRibbonStateStorage
{
    /// <summary>
    /// Gets or sets whether the ribbon is minimized.
    /// </summary>
    bool IsMinimized { get; set; }

    /// <summary>
    /// Gets or sets whether the Quick Access Toolbar is shown below the ribbon.
    /// </summary>
    bool ShowQuickAccessToolBarBelowRibbon { get; set; }

    /// <summary>
    /// Gets or sets whether the simplified ribbon layout is active.
    /// </summary>
    bool IsSimplified { get; set; }

    /// <summary>
    /// Saves the current state.
    /// </summary>
    void Save();

    /// <summary>
    /// Loads the previously saved state.
    /// </summary>
    void Load();

    /// <summary>
    /// Saves a temporary copy of the state (in memory).
    /// </summary>
    void SaveTemporary();

    /// <summary>
    /// Loads the temporary copy of the state.
    /// </summary>
    void LoadTemporary();

    /// <summary>
    /// Resets all stored state.
    /// </summary>
    void Reset();
}

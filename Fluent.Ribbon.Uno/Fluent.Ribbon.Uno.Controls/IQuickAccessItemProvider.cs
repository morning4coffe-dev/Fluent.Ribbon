namespace Fluent;

/// <summary>
/// Implemented by ribbon controls that can add a compact copy of themselves to the
/// <see cref="QuickAccessToolBar"/>.
/// </summary>
public interface IQuickAccessItemProvider
{
    /// <summary>
    /// Gets or sets whether this control may be added to the Quick Access Toolbar.
    /// </summary>
    bool CanAddToQuickAccessToolBar { get; set; }

    /// <summary>
    /// Creates a compact element that mirrors this control's action, suitable for
    /// hosting in the Quick Access Toolbar. Returns <see langword="null"/> when the
    /// control cannot provide a quick access item.
    /// </summary>
    FrameworkElement? CreateQuickAccessItem();
}

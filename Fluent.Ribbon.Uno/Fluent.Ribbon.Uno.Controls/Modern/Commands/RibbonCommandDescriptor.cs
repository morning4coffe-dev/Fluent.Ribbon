namespace Fluent.Modern.Commands;

using Fluent;

/// <summary>
/// <para><b>Modern extension</b> — describes a ribbon command discoverable by Tell Me search.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonCommandDescriptor
{
    private readonly Ribbon _ribbon;

    internal RibbonCommandDescriptor(
        Ribbon ribbon,
        string displayName,
        string tabHeader,
        string groupHeader,
        string? keyTip,
        object? icon,
        string? screenTipText,
        DependencyObject sourceControl,
        RibbonTabItem owningTab)
    {
        _ribbon = ribbon;
        DisplayName = displayName;
        TabHeader = tabHeader;
        GroupHeader = groupHeader;
        KeyTip = keyTip;
        Icon = icon;
        ScreenTipText = screenTipText;
        SourceControl = sourceControl;
        OwningTab = owningTab;
    }

    #region Properties

    /// <summary>
    /// Gets the command display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the owning tab header text.
    /// </summary>
    public string TabHeader { get; }

    /// <summary>
    /// Gets the owning group header text.
    /// </summary>
    public string GroupHeader { get; }

    /// <summary>
    /// Gets the command key tip, if any.
    /// </summary>
    public string? KeyTip { get; }

    /// <summary>
    /// Gets an optional command icon reference.
    /// </summary>
    public object? Icon { get; }

    /// <summary>
    /// Gets the command screen tip text, if any.
    /// </summary>
    public string? ScreenTipText { get; }

    /// <summary>
    /// Gets the source control represented by this command.
    /// </summary>
    public DependencyObject SourceControl { get; }

    /// <summary>
    /// Gets the owning ribbon tab.
    /// </summary>
    public RibbonTabItem OwningTab { get; }

    /// <summary>
    /// Gets whether the command and its owning tab are currently visible and enabled.
    /// </summary>
    public bool IsAvailable =>
        OwningTab.Visibility == Visibility.Visible
        && SourceControl is UIElement { Visibility: Visibility.Visible }
        && (SourceControl is not Control control || control.IsEnabled);

    /// <summary>
    /// Gets the location text shown alongside the command.
    /// </summary>
    public string Location => string.IsNullOrWhiteSpace(GroupHeader) ? TabHeader : $"{TabHeader} › {GroupHeader}";

    #endregion

    #region Methods

    /// <summary>
    /// Selects the owning tab in the ribbon.
    /// </summary>
    public void Navigate()
    {
        _ribbon.SelectedTab = OwningTab;
        var index = _ribbon.Tabs.IndexOf(OwningTab);
        if (index >= 0)
        {
            _ribbon.SelectedTabIndex = index;
        }
    }

    /// <summary>
    /// Invokes the source control through the shared ribbon invoker.
    /// </summary>
    /// <returns><c>true</c> when the source control handled the invocation; otherwise <c>false</c>.</returns>
    public bool Invoke()
    {
        return IsAvailable && RibbonInvoker.Invoke(SourceControl);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DisplayName;
    }

    #endregion
}

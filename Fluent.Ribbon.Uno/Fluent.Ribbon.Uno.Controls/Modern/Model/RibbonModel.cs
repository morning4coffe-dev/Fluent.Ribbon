namespace Fluent.Modern.Model;

/// <summary>
/// <para><b>Modern extension</b> — describes a data-driven ribbon surface.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonModel
{
    /// <summary>
    /// Gets the tabs in the ribbon.
    /// </summary>
    public IList<RibbonTabModel> Tabs { get; } = new ObservableCollection<RibbonTabModel>();
}

/// <summary>
/// <para><b>Modern extension</b> — describes a ribbon tab.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonTabModel
{
    /// <summary>
    /// Gets or sets the tab header.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets the groups in the tab.
    /// </summary>
    public IList<RibbonGroupModel> Groups { get; } = new ObservableCollection<RibbonGroupModel>();
}

/// <summary>
/// <para><b>Modern extension</b> — describes a ribbon group.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonGroupModel
{
    /// <summary>
    /// Gets or sets the group header.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets the items in the group.
    /// </summary>
    public IList<RibbonItemModel> Items { get; } = new ObservableCollection<RibbonItemModel>();
}

/// <summary>
/// <para><b>Modern extension</b> — base model for data-driven ribbon items.</para>
/// </summary>
[ModernExtension]
public abstract class RibbonItemModel
{
    /// <summary>
    /// Gets or sets the item header.
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Segoe Fluent Icons/MDL2 glyph.
    /// </summary>
    public string? IconGlyph { get; set; }

    /// <summary>
    /// Gets or sets the modern WinUI icon source.
    /// </summary>
    public IconSource? IconSource { get; set; }

    /// <summary>
    /// Gets or sets the item size.
    /// </summary>
    public RibbonControlSize Size { get; set; } = RibbonControlSize.Large;

    /// <summary>
    /// Gets or sets the keyboard gesture, such as Ctrl+S.
    /// </summary>
    public string? Gesture { get; set; }

    /// <summary>
    /// Gets or sets the command to execute.
    /// </summary>
    public ICommand? Command { get; set; }

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    public object? CommandParameter { get; set; }

    /// <summary>
    /// Gets or sets the screen tip title.
    /// </summary>
    public string? ScreenTipTitle { get; set; }

    /// <summary>
    /// Gets or sets the screen tip text.
    /// </summary>
    public string? ScreenTipText { get; set; }
}

/// <summary>
/// <para><b>Modern extension</b> — describes a ribbon command button.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonButtonModel : RibbonItemModel
{
}

/// <summary>
/// <para><b>Modern extension</b> — describes a ribbon toggle button.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonToggleButtonModel : RibbonItemModel
{
    /// <summary>
    /// Gets or sets whether the toggle is checked.
    /// </summary>
    public bool IsChecked { get; set; }
}

/// <summary>
/// <para><b>Modern extension</b> — describes a ribbon separator.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonSeparatorModel : RibbonItemModel
{
}

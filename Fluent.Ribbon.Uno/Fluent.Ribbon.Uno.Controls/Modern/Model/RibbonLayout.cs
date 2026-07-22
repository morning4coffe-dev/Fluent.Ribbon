namespace Fluent.Modern.Model;

/// <summary>
/// <para><b>Modern extension</b> — describes persisted ribbon tab and Quick Access Toolbar layout.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonLayout
{
    /// <summary>
    /// Gets or sets the layout schema version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the persisted tab layouts.
    /// </summary>
    public IList<RibbonTabLayout> Tabs { get; set; } = new List<RibbonTabLayout>();

    /// <summary>
    /// Gets or sets the ordered Quick Access Toolbar item keys.
    /// </summary>
    public IList<string> QuickAccessItemKeys { get; set; } = new List<string>();
}

/// <summary>
/// <para><b>Modern extension</b> — describes the persisted layout for a single ribbon tab.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonTabLayout
{
    /// <summary>
    /// Gets or sets the stable tab key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the tab is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the tab order.
    /// </summary>
    public int Order { get; set; }
}

#pragma warning disable 8618

namespace Fluent.Localization;

using System.ComponentModel;
using System.Reflection;
using Fluent.Localization.Languages;

/// <summary>
/// Base class for ribbon UI localizations.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public abstract class RibbonLocalizationBase : INotifyPropertyChanged, IEquatable<RibbonLocalizationBase>
{
    /// <summary>
    /// Creates a new instance and initializes <see cref="CultureName"/> and <see cref="DisplayName"/>
    /// from <see cref="RibbonLocalizationAttribute"/>.
    /// </summary>
    protected RibbonLocalizationBase()
    {
        CultureName = GetType().GetCustomAttribute<RibbonLocalizationAttribute>()?.CultureName;
        DisplayName = GetType().GetCustomAttribute<RibbonLocalizationAttribute>()?.DisplayName;
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    protected RibbonLocalizationBase(string cultureName, string displayName)
    {
        CultureName = cultureName;
        DisplayName = displayName;
    }

    /// <summary>Gets or sets the culture name.</summary>
    public string? CultureName { get; }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; }

    /// <summary>Fallback instance for localization (English).</summary>
    public static readonly RibbonLocalizationBase FallbackLocalization = new English();

    /// <summary>Gets text for "Automatic".</summary>
    public abstract string Automatic { get; }

    /// <summary>Gets the Uid of the backstage back button.</summary>
    public abstract string BackstageBackButtonUid { get; }

    /// <summary>Gets KeyTip of backstage button.</summary>
    public abstract string BackstageButtonKeyTip { get; }

    /// <summary>Gets text of backstage button.</summary>
    public abstract string BackstageButtonText { get; }

    /// <summary>Gets "Customize Status Bar" text.</summary>
    public abstract string CustomizeStatusBar { get; }

    /// <summary>Gets text for "More colors...".</summary>
    public abstract string MoreColors { get; }

    /// <summary>Gets text for "No color".</summary>
    public abstract string NoColor { get; }

    /// <summary>Quick Access ToolBar DropDown Button ToolTip.</summary>
    public abstract string QuickAccessToolBarDropDownButtonTooltip { get; }

    /// <summary>Quick Access ToolBar Menu Header.</summary>
    public abstract string QuickAccessToolBarMenuHeader { get; }

    /// <summary>Quick Access ToolBar Menu Show Above.</summary>
    public abstract string QuickAccessToolBarMenuShowAbove { get; }

    /// <summary>Quick Access ToolBar Menu Show Below.</summary>
    public abstract string QuickAccessToolBarMenuShowBelow { get; }

    /// <summary>Quick Access ToolBar More Controls Button ToolTip.</summary>
    public abstract string QuickAccessToolBarMoreControlsButtonTooltip { get; }

    /// <summary>Ribbon Context Menu Add Gallery.</summary>
    public abstract string RibbonContextMenuAddGallery { get; }

    /// <summary>Ribbon Context Menu Add Group.</summary>
    public abstract string RibbonContextMenuAddGroup { get; }

    /// <summary>Ribbon Context Menu Add Item.</summary>
    public abstract string RibbonContextMenuAddItem { get; }

    /// <summary>Ribbon Context Menu Add Menu.</summary>
    public abstract string RibbonContextMenuAddMenu { get; }

    /// <summary>Ribbon Context Menu Customize Quick Access Toolbar.</summary>
    public abstract string RibbonContextMenuCustomizeQuickAccessToolBar { get; }

    /// <summary>Ribbon Context Menu Customize the ribbon.</summary>
    public abstract string RibbonContextMenuCustomizeRibbon { get; }

    /// <summary>Ribbon Context Menu Minimize the ribbon.</summary>
    public abstract string RibbonContextMenuMinimizeRibbon { get; }

    /// <summary>Ribbon Context Menu Remove Item.</summary>
    public abstract string RibbonContextMenuRemoveItem { get; }

    /// <summary>Ribbon Context Menu Show Above.</summary>
    public abstract string RibbonContextMenuShowAbove { get; }

    /// <summary>Ribbon Context Menu Show Below.</summary>
    public abstract string RibbonContextMenuShowBelow { get; }

    /// <summary>Show Ribbon.</summary>
    public virtual string ShowRibbon { get; }

    /// <summary>Expand Ribbon.</summary>
    public virtual string ExpandRibbon { get; }

    /// <summary>Minimize Ribbon.</summary>
    public virtual string MinimizeRibbon { get; }

    /// <summary>Ribbon Layout.</summary>
    public virtual string RibbonLayout { get; }

    /// <summary>Use classic Ribbon.</summary>
    public abstract string UseClassicRibbon { get; }

    /// <summary>Use simplified Ribbon.</summary>
    public abstract string UseSimplifiedRibbon { get; }

    /// <summary>DisplayOptions Button ScreenTip Title.</summary>
    public virtual string DisplayOptionsButtonScreenTipTitle { get; }

    /// <summary>DisplayOptions Button ScreenTip Text.</summary>
    public virtual string DisplayOptionsButtonScreenTipText { get; }

    /// <summary>ScreenTip's disable reason header.</summary>
    public abstract string ScreenTipDisableReasonHeader { get; }

    /// <summary>ScreenTip's F1 label header.</summary>
    public abstract string ScreenTipF1LabelHeader { get; }

#pragma warning disable 67
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore 67

    /// <inheritdoc />
    public bool Equals(RibbonLocalizationBase? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return CultureName == other.CultureName;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RibbonLocalizationBase localizationBase && Equals(localizationBase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return CultureName?.GetHashCode(StringComparison.Ordinal) ?? 0;
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(RibbonLocalizationBase? left, RibbonLocalizationBase? right)
    {
        return Equals(left, right);
    }

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(RibbonLocalizationBase? left, RibbonLocalizationBase? right)
    {
        return !Equals(left, right);
    }
}

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

    /// <summary>Gets the heading for theme colors.</summary>
    public virtual string ThemeColors => FallbackLocalization.ThemeColors;

    /// <summary>Gets the heading for standard colors.</summary>
    public virtual string StandardColors => FallbackLocalization.StandardColors;

    /// <summary>Gets the heading for recent colors.</summary>
    public virtual string RecentColors => FallbackLocalization.RecentColors;

    /// <summary>Gets the gallery filter label.</summary>
    public virtual string GalleryFilter => FallbackLocalization.GalleryFilter;

    /// <summary>Gets the format used to describe an opaque RGB color.</summary>
    public virtual string ColorDescriptionFormat => FallbackLocalization.ColorDescriptionFormat;

    /// <summary>Gets the format used to describe a color with transparency.</summary>
    public virtual string ColorDescriptionWithAlphaFormat => FallbackLocalization.ColorDescriptionWithAlphaFormat;

    /// <summary>Gets the affirmative label for the custom-color dialog.</summary>
    public virtual string ColorPickerSelect => FallbackLocalization.ColorPickerSelect;

    /// <summary>Gets the cancel label for the custom-color dialog.</summary>
    public virtual string ColorPickerCancel => FallbackLocalization.ColorPickerCancel;

    /// <summary>Gets the automation selection status for the active color.</summary>
    public virtual string SelectedColor => FallbackLocalization.SelectedColor;

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

    /// <summary>Gets the accessible name for scrolling ribbon content to the left.</summary>
    public virtual string ScrollRibbonLeft => FallbackLocalization.ScrollRibbonLeft;

    /// <summary>Gets the accessible name for scrolling ribbon content to the right.</summary>
    public virtual string ScrollRibbonRight => FallbackLocalization.ScrollRibbonRight;

    /// <summary>Gets the accessible name for scrolling an inline gallery upward.</summary>
    public virtual string ScrollGalleryUp => FallbackLocalization.ScrollGalleryUp;

    /// <summary>Gets the accessible name for scrolling an inline gallery downward.</summary>
    public virtual string ScrollGalleryDown => FallbackLocalization.ScrollGalleryDown;

    /// <summary>Gets the accessible name for opening a collapsed inline gallery.</summary>
    public virtual string OpenGallery => FallbackLocalization.OpenGallery;

    /// <summary>Gets the accessible name for opening additional inline-gallery options.</summary>
    public virtual string OpenGalleryOptions => FallbackLocalization.OpenGalleryOptions;

    /// <summary>Gets the accessible name for a group dialog launcher without a header.</summary>
    public virtual string OpenGroupDialog => FallbackLocalization.OpenGroupDialog;

    /// <summary>Gets the format used for a group dialog launcher with a header.</summary>
    public virtual string OpenGroupDialogFormat => FallbackLocalization.OpenGroupDialogFormat;

    /// <summary>Gets the default accessible name for a ribbon menu.</summary>
    public virtual string RibbonMenuName => FallbackLocalization.RibbonMenuName;

    /// <summary>Gets the default accessible name for a ribbon toolbar.</summary>
    public virtual string RibbonToolBarName => FallbackLocalization.RibbonToolBarName;

    /// <summary>Gets the default accessible name for a ribbon group.</summary>
    public virtual string RibbonGroupName => FallbackLocalization.RibbonGroupName;

    /// <summary>Gets the default accessible name for a gallery.</summary>
    public virtual string GalleryName => FallbackLocalization.GalleryName;

    /// <summary>Gets the default accessible name for the Quick Access Toolbar.</summary>
    public virtual string QuickAccessToolBarName => FallbackLocalization.QuickAccessToolBarName;

    /// <summary>Gets the format for an unnamed Quick Access Toolbar command.</summary>
    public virtual string QuickAccessToolBarItemFormat => FallbackLocalization.QuickAccessToolBarItemFormat;

    /// <summary>Gets the default accessible name for the ribbon status bar.</summary>
    public virtual string StatusBarName => FallbackLocalization.StatusBarName;

    /// <summary>Gets the format for an unnamed status-bar item.</summary>
    public virtual string StatusBarItemFormat => FallbackLocalization.StatusBarItemFormat;

    /// <summary>Gets the default accessible name for an application menu.</summary>
    public virtual string ApplicationMenuName => FallbackLocalization.ApplicationMenuName;

    /// <summary>Gets the default accessible name for the ribbon.</summary>
    public virtual string RibbonName => FallbackLocalization.RibbonName;

    /// <summary>Gets the localized UI Automation control type for a ribbon.</summary>
    public virtual string RibbonControlType => FallbackLocalization.RibbonControlType;

    /// <summary>Gets the localized UI Automation control type for a drop-down button.</summary>
    public virtual string DropDownButtonControlType => FallbackLocalization.DropDownButtonControlType;

    /// <summary>Gets the default accessible name for ribbon search.</summary>
    public virtual string RibbonSearchName => FallbackLocalization.RibbonSearchName;

    /// <summary>Gets the default ribbon search placeholder.</summary>
    public virtual string RibbonSearchPlaceholder => FallbackLocalization.RibbonSearchPlaceholder;

    /// <summary>Gets the default accessible name for a ribbon notification.</summary>
    public virtual string RibbonNotificationName => FallbackLocalization.RibbonNotificationName;

    /// <summary>Gets the accessible name for the vertical resize handle.</summary>
    public virtual string ResizeVerticalHandleName => FallbackLocalization.ResizeVerticalHandleName;

    /// <summary>Gets keyboard help for the vertical resize handle.</summary>
    public virtual string ResizeVerticalHandleHelpText => FallbackLocalization.ResizeVerticalHandleHelpText;

    /// <summary>Gets the accessible name for the two-dimensional resize handle.</summary>
    public virtual string ResizeBothHandleName => FallbackLocalization.ResizeBothHandleName;

    /// <summary>Gets keyboard help for the two-dimensional resize handle.</summary>
    public virtual string ResizeBothHandleHelpText => FallbackLocalization.ResizeBothHandleHelpText;

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

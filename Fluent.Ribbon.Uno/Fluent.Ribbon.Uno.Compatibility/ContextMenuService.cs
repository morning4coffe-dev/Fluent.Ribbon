using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Fluent;

/// <summary>
/// Maps Fluent context-menu compatibility APIs to WinUI context flyouts.
/// </summary>
/// <remarks>
/// WinUI does not support WPF dependency-property metadata overrides or custom
/// context-menu routed events. <see cref="Attach"/> is therefore declarative;
/// <see cref="Coerce"/> performs the instance-level mapping when requested.
/// </remarks>
public static class ContextMenuService
{
    /// <summary>Aliases WinUI's context-flyout dependency property.</summary>
    public static DependencyProperty ContextMenuProperty => FrameworkElement.ContextFlyoutProperty;

    /// <summary>
    /// Stores the WPF-compatible show-on-disabled preference.
    /// </summary>
    /// <remarks>Actual disabled-input behavior remains platform-defined.</remarks>
    public static readonly DependencyProperty ShowOnDisabledProperty =
        DependencyProperty.RegisterAttached(
            "ShowOnDisabled",
            typeof(bool),
            typeof(ContextMenuService),
            new PropertyMetadata(true));

    /// <summary>
    /// Registers compatibility intent for a control type.
    /// </summary>
    /// <remarks>WinUI does not permit WPF-style metadata overrides by owner type.</remarks>
    public static void Attach(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
    }

    /// <summary>Gets the compatibility context menu assigned to an element.</summary>
    public static ContextMenu? GetContextMenu(DependencyObject element)
    {
        return element is FrameworkElement frameworkElement
            ? frameworkElement.ContextFlyout as ContextMenu
            : null;
    }

    /// <summary>Assigns a compatibility context menu to an element.</summary>
    public static void SetContextMenu(DependencyObject element, ContextMenu? value)
    {
        if (element is FrameworkElement frameworkElement)
        {
            frameworkElement.ContextFlyout = value;
        }
    }

    /// <summary>Gets whether a context flyout is requested for disabled controls.</summary>
    public static bool GetShowOnDisabled(DependencyObject element)
    {
        return (bool)element.GetValue(ShowOnDisabledProperty);
    }

    /// <summary>Sets whether a context flyout is requested for disabled controls.</summary>
    public static void SetShowOnDisabled(DependencyObject element, bool value)
    {
        element.SetValue(ShowOnDisabledProperty, value);
    }

    /// <summary>
    /// Returns an existing flyout or creates the portable quick-access flyout.
    /// </summary>
    public static object? CoerceContextMenu(DependencyObject? element, object? baseValue)
    {
        if (baseValue is ContextMenu { IsQuickAccessCompatibilityMenu: true })
        {
            baseValue = null;
        }
        else if (baseValue is not null)
        {
            return baseValue;
        }

        if (element is not FrameworkElement frameworkElement
            || element is not IQuickAccessItemProvider provider
            || provider.CanAddToQuickAccessToolBar is false)
        {
            return null;
        }

        var ribbon = RibbonControl.GetParentRibbon(frameworkElement);
        if (ribbon is null)
        {
            return null;
        }

        var contextMenu = new ContextMenu
        {
            IsQuickAccessCompatibilityMenu = true
        };
        var localization = RibbonLocalization.Current.Localization;
        var menuItem = new MenuFlyoutItem();

        if (ribbon.IsInQuickAccessToolBar(provider))
        {
            menuItem.Text = localization.RibbonContextMenuRemoveItem;
            menuItem.Click += (_, _) => ribbon.RemoveFromQuickAccessToolBar(provider);
        }
        else
        {
            menuItem.Text = localization.RibbonContextMenuAddItem;
            menuItem.Click += (_, _) => ribbon.AddToQuickAccessToolBar(provider);
        }

        contextMenu.Items.Add(menuItem);
        return contextMenu;
    }

    /// <summary>Re-evaluates the compatibility context flyout for an element.</summary>
    public static void Coerce(DependencyObject? element)
    {
        if (element is not FrameworkElement frameworkElement)
        {
            return;
        }

        var coerced = CoerceContextMenu(element, frameworkElement.ContextFlyout);
        frameworkElement.ContextFlyout = coerced as FlyoutBase;
    }
}

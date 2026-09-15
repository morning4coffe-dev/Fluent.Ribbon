using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

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

        if (element is not FrameworkElement frameworkElement)
        {
            return null;
        }

        var ribbon = RibbonControl.GetParentRibbon(frameworkElement);
        if (ribbon is null || !CanUseDefaultMenu())
        {
            return null;
        }

        var contextMenu = new ContextMenu
        {
            IsQuickAccessCompatibilityMenu = true
        };
        var permissionCallbacks = new List<(DependencyProperty Property, long Token)>();
        var menuItem = new MenuFlyoutItem();
        ConfigureItem();
        if (contextMenu.Items.Count == 0)
        {
            return null;
        }

        contextMenu.Opening += (_, _) =>
        {
            ConfigureItem();
            if (contextMenu.Items.Count > 0 && permissionCallbacks.Count == 0)
            {
                foreach (var property in new[]
                         {
                             Ribbon.IsDefaultContextMenuEnabledProperty,
                             Ribbon.CanCustomizeQuickAccessToolBarItemsProperty,
                             Ribbon.IsQuickAccessToolBarVisibleProperty,
                             Control.IsEnabledProperty,
                         })
                {
                    permissionCallbacks.Add((property,
                        ribbon.RegisterPropertyChangedCallback(property, (_, _) => contextMenu.Hide())));
                }
            }
        };
        contextMenu.Opened += (_, _) =>
        {
            if (!CanUseDefaultMenu() || contextMenu.Items.Count == 0)
            {
                contextMenu.Hide();
            }
        };
        contextMenu.Closed += (_, _) =>
        {
            foreach (var (property, token) in permissionCallbacks)
            {
                ribbon.UnregisterPropertyChangedCallback(property, token);
            }
            permissionCallbacks.Clear();
        };

        return contextMenu;

        void ConfigureItem()
        {
            contextMenu.Items.Clear();
            ICommand action = ribbon.IsInQuickAccessToolBar(frameworkElement)
                ? Ribbon.RemoveFromQuickAccessCommand
                : Ribbon.AddToQuickAccessCommand;
            if (!CanUseDefaultMenu() || !action.CanExecute(frameworkElement))
            {
                return;
            }

            var localization = RibbonLocalization.Current.Localization;
            menuItem.Text = ribbon.IsInQuickAccessToolBar(frameworkElement)
                ? localization.RibbonContextMenuRemoveItem
                : localization.RibbonContextMenuAddItem;
            var command = new XamlUICommand { Label = menuItem.Text };
            command.CanExecuteRequested += (_, args) =>
                args.CanExecute = CanUseDefaultMenu() && action.CanExecute(frameworkElement);
            command.ExecuteRequested += (_, _) =>
            {
                if (CanUseDefaultMenu() && action.CanExecute(frameworkElement))
                {
                    action.Execute(frameworkElement);
                }
            };
            menuItem.Command = command;
            contextMenu.Items.Add(menuItem);
        }

        bool CanUseDefaultMenu()
        {
            if (ribbon is not
                {
                    IsDefaultContextMenuEnabled: true, IsEnabled: true,
                    CanCustomizeQuickAccessToolBarItems: true, IsQuickAccessToolBarVisible: true,
                }
                || !ReferenceEquals(RibbonControl.GetParentRibbon(frameworkElement), ribbon))
            {
                return false;
            }

            for (DependencyObject? current = frameworkElement; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is FrameworkElement owner
                    && owner.ContextFlyout is not ContextMenu { IsQuickAccessCompatibilityMenu: true }
                    && (owner.ContextFlyout is not null
                        || owner.ReadLocalValue(FrameworkElement.ContextFlyoutProperty) != DependencyProperty.UnsetValue
                        || owner.GetBindingExpression(FrameworkElement.ContextFlyoutProperty) is not null))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>Re-evaluates the compatibility context flyout for an element.</summary>
    public static void Coerce(DependencyObject? element)
    {
        if (element is not FrameworkElement frameworkElement)
        {
            return;
        }

        var current = frameworkElement.ContextFlyout;
        var coerced = CoerceContextMenu(element, current);
        if (ReferenceEquals(current, coerced))
        {
            return;
        }

        if (coerced is null && current is ContextMenu { IsQuickAccessCompatibilityMenu: true })
        {
            frameworkElement.ClearValue(FrameworkElement.ContextFlyoutProperty);
        }
        else
        {
            frameworkElement.ContextFlyout = coerced as FlyoutBase;
        }
    }
}

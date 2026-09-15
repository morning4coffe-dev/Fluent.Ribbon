using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Runtime.CompilerServices;

namespace Fluent;

/// <summary>
/// Helper that wires up the "Add to / Remove from Quick Access Toolbar" context menu
/// for ribbon controls implementing <see cref="IQuickAccessItemProvider"/>.
/// </summary>
internal static class QuickAccessHelper
{
    private static readonly ConditionalWeakTable<UIElement, QuickAccessItemAssociation> QuickAccessItems = new();
    private static readonly ConditionalWeakTable<Ribbon, DefaultContextMenu> DefaultContextMenus = new();

    internal static void AssociateQuickAccessItem(
        Ribbon ribbon,
        IQuickAccessItemProvider provider,
        FrameworkElement item)
    {
        if (QuickAccessItems.TryGetValue(item, out var association)
            && association.Owner.TryGetTarget(out var owner))
        {
            if (ReferenceEquals(owner, ribbon)
                && EqualityComparer<IQuickAccessItemProvider>.Default.Equals(association.Provider, provider))
            {
                return;
            }

            throw new InvalidOperationException(
                "A quick access copy cannot be shared by different ribbons or providers.");
        }

        QuickAccessItems.Remove(item);
        QuickAccessItems.Add(item, new QuickAccessItemAssociation(ribbon, provider));
        item.RightTapped -= OnRightTapped;
        item.RightTapped += OnRightTapped;
    }

    internal static bool TryGetQuickAccessProvider(
        Ribbon ribbon,
        object? item,
        out IQuickAccessItemProvider provider)
    {
        if (item is UIElement element
            && QuickAccessItems.TryGetValue(element, out var association)
            && association.Owner.TryGetTarget(out var owner)
            && ReferenceEquals(owner, ribbon))
        {
            provider = association.Provider;
            return true;
        }

        provider = null!;
        return false;
    }

    internal static object? ClonePresentationValue(object? value)
    {
        return QuickAccessPresentationValue.Create(value);
    }

    internal static void SynchronizePresentationValue(
        DependencyObject source,
        DependencyProperty sourceProperty,
        DependencyObject target,
        DependencyProperty targetProperty)
    {
        CopyPresentationValue(source, sourceProperty, target, targetProperty);

        var weakTarget = new WeakReference<DependencyObject>(target);
        long token = 0;
        token = source.RegisterPropertyChangedCallback(
            sourceProperty,
            (sender, changedProperty) =>
            {
                if (weakTarget.TryGetTarget(out var liveTarget))
                {
                    CopyPresentationValue(
                        sender,
                        changedProperty,
                        liveTarget,
                        targetProperty);
                }
                else
                {
                    sender.UnregisterPropertyChangedCallback(changedProperty, token);
                }
            });
    }

    private static void CopyPresentationValue(
        DependencyObject source,
        DependencyProperty sourceProperty,
        DependencyObject target,
        DependencyProperty targetProperty)
    {
        var value = ClonePresentationValue(source.GetValue(sourceProperty));
        if (Equals(target.GetValue(targetProperty), value) is false)
        {
            target.SetValue(targetProperty, value);
        }
    }

    /// <summary>
    /// Walks up the visual tree from <paramref name="element"/> to find the owning <see cref="Ribbon"/>.
    /// </summary>
    public static Ribbon? FindOwningRibbon(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is Ribbon ribbon)
            {
                return ribbon;
            }

            if (element is UIElement item
                && QuickAccessItems.TryGetValue(item, out var association)
                && association.Owner.TryGetTarget(out var owner)
                && owner.OwnsQuickAccessItem(item))
            {
                return owner;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    /// <summary>
    /// Attaches the quick-access context menu behaviour to a control. Safe to call
    /// multiple times; the handler is only attached once.
    /// </summary>
    public static void AttachContextMenu(FrameworkElement control)
    {
        if (control is not IQuickAccessItemProvider and not Ribbon and not QuickAccessToolBar)
        {
            return;
        }

        control.RightTapped -= OnRightTapped;
        control.RightTapped += OnRightTapped;
    }

    private static void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.Handled || sender is not FrameworkElement element)
        {
            return;
        }

        e.Handled = OpenContextMenu(element, e.OriginalSource as DependencyObject, e.GetPosition(element)) is not null;
    }

    internal static MenuFlyout? OpenContextMenu(
        FrameworkElement element,
        DependencyObject? originalSource,
        Windows.Foundation.Point position)
    {
        var ribbon = FindOwningRibbon(element);
        if (ribbon is null || !CanUseDefaultContextMenu(ribbon, element, originalSource))
        {
            return null;
        }

        var localization = RibbonLocalization.Current.Localization;
        var flyout = new MenuFlyout();
        var isPresent = ribbon.IsInQuickAccessToolBar(element);
        if (ribbon.CanCustomizeQuickAccessItem(element, add: !isPresent))
        {
            AddCommand(
                isPresent ? localization.RibbonContextMenuRemoveItem : localization.RibbonContextMenuAddItem,
                isPresent ? Ribbon.RemoveFromQuickAccessCommand : Ribbon.AddToQuickAccessCommand,
                element);
        }

        if (ribbon.IsQuickAccessToolBarVisible
            && (ribbon.CanCustomizeQuickAccessToolBar || ribbon.CanQuickAccessLocationChanging))
        {
            if (flyout.Items.Count > 0)
            {
                flyout.Items.Add(new MenuFlyoutSeparator());
            }

            if (ribbon.CanCustomizeQuickAccessToolBar)
            {
                AddCommand(localization.RibbonContextMenuCustomizeQuickAccessToolBar,
                    Ribbon.CustomizeQuickAccessToolbarCommand, ribbon);
            }

            if (ribbon.CanQuickAccessLocationChanging)
            {
                AddCommand(
                    ribbon.ShowQuickAccessToolBarAboveRibbon
                        ? localization.RibbonContextMenuShowBelow
                        : localization.RibbonContextMenuShowAbove,
                    ribbon.ShowQuickAccessToolBarAboveRibbon
                        ? Ribbon.ShowQuickAccessBelowCommand
                        : Ribbon.ShowQuickAccessAboveCommand,
                    ribbon);
            }
        }

        if (flyout.Items.Count == 0)
        {
            return null;
        }

        CloseDefaultContextMenu(ribbon);
        var state = DefaultContextMenus.GetValue(ribbon, static _ => new DefaultContextMenu());
        state.Flyout = new WeakReference<MenuFlyout>(flyout);
        FlyoutShowHelper.ShowDeferred(flyout, element, position,
            () => state.Flyout?.TryGetTarget(out var current) == true
                  && ReferenceEquals(current, flyout)
                  && CanUseDefaultContextMenu(ribbon, element, originalSource));
        return flyout;

        void AddCommand(string label, ICommand command, object parameter)
        {
            flyout.Items.Add(new MenuFlyoutItem
            {
                Text = label,
                Command = Ribbon.CreateGuardedMenuCommand(
                    label,
                    () => CanUseDefaultContextMenu(ribbon, element, originalSource) && command.CanExecute(parameter),
                    () => Internal.CommandHelper.Execute(command, parameter)),
            });
        }
    }

    private static bool CanUseDefaultContextMenu(
        Ribbon ribbon,
        FrameworkElement element,
        DependencyObject? originalSource)
    {
        if (!ribbon.IsDefaultContextMenuEnabled || !ribbon.IsEnabled || !ribbon.IsQuickAccessToolBarVisible
            || !element.IsLoaded || element.XamlRoot is null
            || !ReferenceEquals(FindOwningRibbon(element), ribbon)
            || !FocusRoutingHelper.IsEffectivelyEnabled(element)
            || !FocusRoutingHelper.IsEffectivelyVisible(element)
            || (originalSource is not null && !FocusRoutingHelper.IsEffectivelyEnabled(originalSource)))
        {
            return false;
        }

        for (var current = originalSource ?? element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement owner
                && (owner.ContextFlyout is not null
                    || owner.ReadLocalValue(FrameworkElement.ContextFlyoutProperty) != DependencyProperty.UnsetValue
                    || owner.GetBindingExpression(FrameworkElement.ContextFlyoutProperty) is not null))
            {
                return false;
            }
        }

        return true;
    }

    internal static void CloseDefaultContextMenu(Ribbon ribbon)
    {
        if (DefaultContextMenus.TryGetValue(ribbon, out var state))
        {
            var reference = state.Flyout;
            state.Flyout = null;
            if (reference?.TryGetTarget(out var flyout) == true)
            {
                flyout.Hide();
            }
        }
    }

    private sealed class DefaultContextMenu
    {
        internal WeakReference<MenuFlyout>? Flyout { get; set; }
    }

    // Retaining the association with the copy lets customization hide/reinsert it without
    // creating another copy. The ribbon remains weak, and the table does not root removed copies.
    private sealed class QuickAccessItemAssociation(Ribbon owner, IQuickAccessItemProvider provider)
    {
        public WeakReference<Ribbon> Owner { get; } = new(owner);
        public IQuickAccessItemProvider Provider { get; } = provider;
    }
}

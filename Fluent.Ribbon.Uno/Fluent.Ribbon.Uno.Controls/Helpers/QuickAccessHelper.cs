using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Fluent;

/// <summary>
/// Helper that wires up the "Add to / Remove from Quick Access Toolbar" context menu
/// for ribbon controls implementing <see cref="IQuickAccessItemProvider"/>.
/// </summary>
internal static class QuickAccessHelper
{
    internal static object? ClonePresentationValue(object? value)
    {
        return value is UIElement element
            ? Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(element)
              ?? element.GetType().Name
            : value;
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
        if (control is not IQuickAccessItemProvider)
        {
            return;
        }

        control.RightTapped -= OnRightTapped;
        control.RightTapped += OnRightTapped;
    }

    private static void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element is not IQuickAccessItemProvider provider)
        {
            return;
        }

        if (!provider.CanAddToQuickAccessToolBar)
        {
            return;
        }

        var ribbon = FindOwningRibbon(element);
        if (ribbon is null)
        {
            return;
        }

        var localization = RibbonLocalization.Current.Localization;
        var flyout = new MenuFlyout();

        if (ribbon.IsInQuickAccessToolBar(provider))
        {
            var remove = new MenuFlyoutItem { Text = localization.RibbonContextMenuRemoveItem };
            remove.Click += (_, _) => ribbon.RemoveFromQuickAccessToolBar(provider);
            flyout.Items.Add(remove);
        }
        else
        {
            var add = new MenuFlyoutItem { Text = localization.RibbonContextMenuAddItem };
            add.Click += (_, _) => ribbon.AddToQuickAccessToolBar(provider);
            flyout.Items.Add(add);
        }

        FlyoutShowHelper.ShowDeferred(flyout, element, e.GetPosition(element));
        e.Handled = true;
    }
}

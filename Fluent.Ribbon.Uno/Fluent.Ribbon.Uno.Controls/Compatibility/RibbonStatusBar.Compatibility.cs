namespace Fluent;

using System.Collections.Specialized;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// WPF-compatible collection and customization behavior for <see cref="RibbonStatusBar"/>.
/// </summary>
public partial class RibbonStatusBar
{
    private readonly Flyout _customizationFlyout = new()
    {
        Placement = FlyoutPlacementMode.Top,
    };

    private readonly StackPanel _customizationPanel = new()
    {
        MinWidth = 220,
        Spacing = 2,
    };

    private readonly Dictionary<StatusBarItem, long> _statusItemTokens = new();
    private readonly Dictionary<StatusBarItem, StatusBarMenuItem> _statusMenuItems = new();

    private void InitializeCompatibility()
    {
        _customizationFlyout.Content = _customizationPanel;
        _customizationFlyout.Opening += OnCustomizationFlyoutOpening;
        ContextFlyout ??= _customizationFlyout;
        ReconcileStatusItems();
    }

    private void OnCustomizationFlyoutOpening(object? sender, object args)
        => RebuildCustomizationMenu();

    /// <summary>
    /// Creates a portable default item container.
    /// </summary>
    protected virtual DependencyObject GetContainerForItemOverride()
        => new StatusBarItem();

    /// <summary>
    /// Determines whether an item already represents its own portable container.
    /// </summary>
    protected virtual bool IsItemItsOwnContainerOverride(object item)
        => item is StatusBarItem or RibbonSeparator;

    /// <summary>
    /// Handles changes to the status-bar item collection.
    /// </summary>
    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs args)
    {
        ReconcileStatusItems();
        UpdateSeparatorsVisibility(Items);
        RebuildCustomizationMenu();
    }

    private void OnRightItemsChangedCompatibility(NotifyCollectionChangedEventArgs args)
    {
        ReconcileStatusItems();
        UpdateSeparatorsVisibility(RightItems);
        RebuildCustomizationMenu();
    }

    private void ReconcileStatusItems()
    {
        var currentItems = Items
            .Concat(RightItems)
            .OfType<StatusBarItem>()
            .ToHashSet();

        foreach (var removed in _statusItemTokens.Keys.Except(currentItems).ToArray())
        {
            removed.UnregisterPropertyChangedCallback(
                StatusBarItem.IsCheckedProperty,
                _statusItemTokens[removed]);
            _statusItemTokens.Remove(removed);

            if (_statusMenuItems.Remove(removed, out var menuItem))
            {
                menuItem.StatusBarItem = null;
            }
        }

        foreach (var added in currentItems.Except(_statusItemTokens.Keys))
        {
            _statusItemTokens[added] = added.RegisterPropertyChangedCallback(
                StatusBarItem.IsCheckedProperty,
                OnStatusItemCheckedChanged);
        }
    }

    private void OnStatusItemCheckedChanged(
        DependencyObject sender,
        DependencyProperty property)
    {
        UpdateSeparatorsVisibility(Items);
        UpdateSeparatorsVisibility(RightItems);
        RebuildCustomizationMenu();
    }

    private void RebuildCustomizationMenu()
    {
        _customizationPanel.Children.Clear();

        var heading = new TextBlock
        {
            Text = RibbonLocalization.Current.Localization.CustomizeStatusBar,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Margin = new Thickness(12, 8, 12, 4),
        };
        AutomationProperties.SetHeadingLevel(heading, AutomationHeadingLevel.Level2);
        _customizationPanel.Children.Add(heading);

        AddCustomizationItems(Items);

        if (Items.Count > 0 && RightItems.Count > 0)
        {
            _customizationPanel.Children.Add(
                new RibbonSeparator { Orientation = Orientation.Horizontal });
        }

        AddCustomizationItems(RightItems);
    }

    private void AddCustomizationItems(IEnumerable<UIElement> items)
    {
        var itemIndex = 0;
        foreach (var item in items)
        {
            if (item is not StatusBarItem statusItem)
            {
                continue;
            }

            if (!_statusMenuItems.TryGetValue(statusItem, out var menuItem))
            {
                menuItem = new StatusBarMenuItem(statusItem);
                _statusMenuItems[statusItem] = menuItem;
            }

            AutomationProperties.SetAutomationId(
                menuItem,
                $"StatusBarCustomizationItem{_customizationPanel.Children.Count}");
            AutomationProperties.SetName(
                menuItem,
                Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(
                    statusItem.Title ?? statusItem.Content) is { Length: > 0 } itemName
                    ? itemName
                    : string.Format(
                        RibbonLocalization.Current.Localization.StatusBarItemFormat,
                        ++itemIndex));
            _customizationPanel.Children.Add(menuItem);
        }
    }

    private static void UpdateSeparatorsVisibility(
        IEnumerable<UIElement> items)
    {
        var isFirstVisible = true;
        var previousWasSeparator = false;
        RibbonSeparator? pendingSeparator = null;

        foreach (var item in items)
        {
            if (item is RibbonSeparator separator)
            {
                pendingSeparator = separator;
                separator.Visibility =
                    isFirstVisible || previousWasSeparator
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                previousWasSeparator = true;
                continue;
            }

            if (item.Visibility != Visibility.Visible)
            {
                continue;
            }

            isFirstVisible = false;
            previousWasSeparator = false;
            pendingSeparator = null;
        }

        if (pendingSeparator is not null)
        {
            pendingSeparator.Visibility = Visibility.Collapsed;
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonStatusBarAutomationPeer(this);
}

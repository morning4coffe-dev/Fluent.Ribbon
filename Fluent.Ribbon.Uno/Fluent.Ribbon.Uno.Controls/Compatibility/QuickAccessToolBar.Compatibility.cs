namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

/// <summary>
/// WPF-compatible API and portable behavior for <see cref="QuickAccessToolBar"/>.
/// </summary>
public partial class QuickAccessToolBar : ILogicalChildSupport
{
    private Fluent.Collections.ItemCollectionWithLogicalTreeSupport<QuickAccessMenuItem>? _quickAccessItems;
    private readonly Dictionary<QuickAccessMenuItem, long> _quickAccessItemTokens = new();
    private readonly Dictionary<QuickAccessMenuItem, long> _quickAccessTargetTokens = new();
    private readonly Dictionary<QuickAccessMenuItem, IQuickAccessItemProvider>
        _quickAccessSynchronizedProviders = new();
    private readonly HashSet<FrameworkElement> _trackedItems = new();
    private readonly HashSet<FrameworkElement> _overflowedItems = new();
    private bool _overflowUpdatePending;

    internal WinUIButton? MenuButtonForAutomation => _menuButton;

    internal WinUIButton? OverflowButtonForAutomation => _overflowButton;

    /// <summary>
    /// Occurs when items are added to or removed from the toolbar.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? ItemsChanged;

    /// <summary>
    /// Gets the items displayed in the customization menu.
    /// </summary>
    public Fluent.Collections.ItemCollectionWithLogicalTreeSupport<QuickAccessMenuItem> QuickAccessItems
    {
        get
        {
            if (_quickAccessItems is null)
            {
                _quickAccessItems =
                    new Fluent.Collections.ItemCollectionWithLogicalTreeSupport<QuickAccessMenuItem>(this);
                _quickAccessItems.CollectionChanged += OnQuickAccessItemsCollectionChanged;
            }

            return _quickAccessItems;
        }
    }

    /// <summary>
    /// Identifies the <see cref="UpdateKeyTipsAction"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty UpdateKeyTipsActionProperty =
        DependencyProperty.Register(
            nameof(UpdateKeyTipsAction),
            typeof(Action<QuickAccessToolBar>),
            typeof(QuickAccessToolBar),
            new PropertyMetadata(null, OnUpdateKeyTipsActionChanged));

    /// <summary>
    /// Gets or sets a custom action that assigns key tips to toolbar items.
    /// </summary>
    public Action<QuickAccessToolBar>? UpdateKeyTipsAction
    {
        get => (Action<QuickAccessToolBar>?)GetValue(UpdateKeyTipsActionProperty);
        set => SetValue(UpdateKeyTipsActionProperty, value);
    }

    private void InitializeCompatibility()
    {
        Loaded += OnCompatibilityLoaded;
    }

    private void OnCompatibilityLoaded(object sender, RoutedEventArgs args)
    {
        SynchronizeTrackedItems();
        SynchronizeQuickAccessMenuItems();
        UpdateKeyTips();
        Refresh();
        DispatcherQueue.TryEnqueue(
            () =>
            {
                SynchronizeQuickAccessMenuItems();
                UpdateKeyTips();
                Refresh();
            });
    }

    private void OnCompatibilityItemsChanged(NotifyCollectionChangedEventArgs args)
    {
        SynchronizeTrackedItems();
        UpdateKeyTips();
        Refresh();
        ItemsChanged?.Invoke(this, args);
    }

    private void SynchronizeTrackedItems()
    {
        var currentItems = Items.OfType<FrameworkElement>().ToHashSet();

        foreach (var removed in _trackedItems.Except(currentItems).ToArray())
        {
            removed.SizeChanged -= OnCompatibilityChildSizeChanged;
            if (_overflowedItems.Remove(removed))
            {
                removed.Visibility = Visibility.Visible;
            }

            _trackedItems.Remove(removed);
        }

        foreach (var added in currentItems.Except(_trackedItems))
        {
            added.Margin = new Thickness(1, 0, 1, 0);
            added.SizeChanged += OnCompatibilityChildSizeChanged;
            _trackedItems.Add(added);
        }
    }

    private void OnCompatibilityChildSizeChanged(object sender, SizeChangedEventArgs args)
        => Refresh();

    private static void OnUpdateKeyTipsActionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
        => ((QuickAccessToolBar)sender).UpdateKeyTips();

    private void UpdateKeyTips()
    {
        if (UpdateKeyTipsAction is not null)
        {
            UpdateKeyTipsAction(this);
            return;
        }

        for (var index = 0; index < Items.Count; index++)
        {
            var keys = index switch
            {
                < 9 => (index + 1).ToString(CultureInfo.InvariantCulture),
                < 18 => $"0{18 - index}",
                < 44 => $"0{(char)('A' + index - 18)}",
                _ => null,
            };

            KeyTip.SetKeys(Items[index], keys);
        }
    }

    private void OnQuickAccessItemsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null)
        {
            foreach (var item in args.OldItems.OfType<QuickAccessMenuItem>())
            {
                RemoveSynchronizedQuickAccessMenuItem(item);

                if (_quickAccessItemTokens.Remove(item, out var token))
                {
                    item.UnregisterPropertyChangedCallback(
                        QuickAccessMenuItem.IsCheckedProperty,
                        token);
                }

                if (_quickAccessTargetTokens.Remove(item, out var targetToken))
                {
                    item.UnregisterPropertyChangedCallback(
                        QuickAccessMenuItem.TargetProperty,
                        targetToken);
                }
            }
        }

        if (args.NewItems is not null)
        {
            foreach (var item in args.NewItems.OfType<QuickAccessMenuItem>())
            {
                _quickAccessItemTokens[item] = item.RegisterPropertyChangedCallback(
                    QuickAccessMenuItem.IsCheckedProperty,
                    OnQuickAccessItemCheckedChanged);
                _quickAccessTargetTokens[item] = item.RegisterPropertyChangedCallback(
                    QuickAccessMenuItem.TargetProperty,
                    OnQuickAccessItemTargetChanged);
                SynchronizeQuickAccessMenuItem(item, initialize: true);
            }
        }

        if (args.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var item in _quickAccessSynchronizedProviders.Keys.ToArray())
            {
                RemoveSynchronizedQuickAccessMenuItem(item);
            }

            foreach (var (item, token) in _quickAccessItemTokens)
            {
                item.UnregisterPropertyChangedCallback(
                    QuickAccessMenuItem.IsCheckedProperty,
                    token);
            }

            _quickAccessItemTokens.Clear();

            foreach (var (item, token) in _quickAccessTargetTokens)
            {
                item.UnregisterPropertyChangedCallback(
                    QuickAccessMenuItem.TargetProperty,
                    token);
            }

            _quickAccessTargetTokens.Clear();
        }
    }

    private void OnQuickAccessItemCheckedChanged(
        DependencyObject sender,
        DependencyProperty property)
    {
        if (sender is QuickAccessMenuItem item)
        {
            SynchronizeQuickAccessMenuItem(item, initialize: false);
        }
    }

    private void OnQuickAccessItemTargetChanged(
        DependencyObject sender,
        DependencyProperty property)
    {
        if (sender is QuickAccessMenuItem item)
        {
            SynchronizeQuickAccessMenuItem(item, initialize: true);
        }
    }

    private void SynchronizeQuickAccessMenuItems()
    {
        foreach (var item in QuickAccessItems)
        {
            SynchronizeQuickAccessMenuItem(item, initialize: true);
        }
    }

    private void SynchronizeQuickAccessMenuItem(
        QuickAccessMenuItem item,
        bool initialize)
    {
        if (QuickAccessHelper.FindOwningRibbon(this) is not { } ribbon)
        {
            return;
        }

        var provider = item.Target as IQuickAccessItemProvider;
        if (_quickAccessSynchronizedProviders.TryGetValue(item, out var previousProvider)
            && !ReferenceEquals(previousProvider, provider))
        {
            ribbon.RemoveFromQuickAccessToolBar(previousProvider);
            _quickAccessSynchronizedProviders.Remove(item);
        }

        if (provider is null)
        {
            return;
        }

        if (initialize && ribbon.IsInQuickAccessToolBar(provider))
        {
            item.IsChecked = true;
            _quickAccessSynchronizedProviders[item] = provider;
            return;
        }

        if (item.IsChecked)
        {
            ribbon.AddToQuickAccessToolBar(provider);
            _quickAccessSynchronizedProviders[item] = provider;
        }
        else
        {
            ribbon.RemoveFromQuickAccessToolBar(provider);
            _quickAccessSynchronizedProviders.Remove(item);
        }
    }

    private void RemoveSynchronizedQuickAccessMenuItem(QuickAccessMenuItem item)
    {
        if (!_quickAccessSynchronizedProviders.Remove(item, out var provider))
        {
            return;
        }

        if (QuickAccessHelper.FindOwningRibbon(this) is not { } ribbon
            || ribbon.QuickAccessItems.Contains(item))
        {
            return;
        }

        ribbon.RemoveFromQuickAccessToolBar(provider);
    }

    private void AddQuickAccessCustomizationItems(StackPanel panel)
    {
        for (var index = 0; index < QuickAccessItems.Count; index++)
        {
            var item = QuickAccessItems[index];
            if (VisualTreeHelper.GetParent(item) is Panel currentParent)
            {
                currentParent.Children.Remove(item);
            }

            AutomationProperties.SetAutomationId(
                item,
                $"QuickAccessCustomizationItem{index}");
            AutomationProperties.SetName(
                item,
                Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(item.Header)
                is { Length: > 0 } header
                    ? header
                    : string.Format(
                        RibbonLocalization.Current.Localization.QuickAccessToolBarItemFormat,
                        index + 1));
            panel.Children.Add(item);
        }
    }

    /// <summary>
    /// Invalidates measurement and recalculates overflow placement.
    /// </summary>
    public void Refresh()
    {
        InvalidateMeasure();
        ScheduleOverflowUpdate(ActualWidth);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var result = base.MeasureOverride(availableSize);
        ScheduleOverflowUpdate(availableSize.Width);
        return result;
    }

    private void ScheduleOverflowUpdate(double availableWidth)
    {
        if (_overflowUpdatePending
            || availableWidth <= 0
            || double.IsInfinity(availableWidth))
        {
            return;
        }

        _overflowUpdatePending = true;
        DispatcherQueue.TryEnqueue(() =>
        {
            _overflowUpdatePending = false;
            UpdateOverflow(availableWidth);
        });
    }

    /// <summary>
    /// Gets the logical children represented by the portable WinUI collections.
    /// </summary>
    protected IEnumerator LogicalChildren => EnumerateLogicalChildren().GetEnumerator();

    private IEnumerable<object> EnumerateLogicalChildren()
    {
        foreach (var item in Items)
        {
            yield return item;
        }

        foreach (var item in QuickAccessItems)
        {
            yield return item;
        }
    }

    void ILogicalChildSupport.AddLogicalChild(object child)
    {
        // WinUI logical ownership follows the visual/template parent.
    }

    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
        // WinUI logical ownership follows the visual/template parent.
    }
}

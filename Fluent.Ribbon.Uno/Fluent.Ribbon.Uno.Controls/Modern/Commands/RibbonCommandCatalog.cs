namespace Fluent.Modern.Commands;

using System.Collections.Specialized;
using System.Reflection;
using Fluent;
using Windows.Foundation.Collections;

/// <summary>
/// <para><b>Modern extension</b> — builds and searches a logical catalog of ribbon commands.</para>
/// </summary>
[ModernExtension]
public sealed class RibbonCommandCatalog : IDisposable
{
    private readonly Ribbon _ribbon;
    private readonly List<RibbonCommandDescriptor> _commands = new();
    private readonly HashSet<INotifyCollectionChanged> _subscriptions = new();
    private readonly HashSet<IObservableVector<object>> _vectorSubscriptions = new();
    private bool _isDisposed;
    private bool _isRebuildPending;

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonCommandCatalog"/> class.
    /// </summary>
    /// <param name="ribbon">The ribbon whose logical command model should be indexed.</param>
    public RibbonCommandCatalog(Ribbon ribbon)
    {
        _ribbon = ribbon ?? throw new ArgumentNullException(nameof(ribbon));
        Rebuild();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the discovered command descriptors.
    /// </summary>
    public IReadOnlyList<RibbonCommandDescriptor> Commands => _commands;

    /// <summary>
    /// Occurs after collection changes have been coalesced into a rebuilt catalog.
    /// </summary>
    public event EventHandler? Changed;

    #endregion

    #region Methods

    /// <summary>
    /// Rebuilds the command catalog from the ribbon logical Tabs/Groups/Items model.
    /// </summary>
    public void Rebuild()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _isRebuildPending = false;
        RefreshSubscriptions();
        _commands.Clear();

        foreach (var tab in _ribbon.Tabs)
        {
            var tabHeader = GetText(tab.Header);
            foreach (var group in tab.Groups)
            {
                var groupHeader = GetText(group.Header);
                foreach (var item in group.Items)
                {
                    AddItem(item, tab, tabHeader, groupHeader);
                }
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Searches the command catalog with a simple ranked case-insensitive match.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="maxResults">The maximum number of results to return.</param>
    /// <returns>The ranked matching commands.</returns>
    public IReadOnlyList<RibbonCommandDescriptor> Search(string? query, int maxResults)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        FlushPendingRebuild();

        if (string.IsNullOrWhiteSpace(query) || maxResults <= 0)
        {
            return Array.Empty<RibbonCommandDescriptor>();
        }

        var normalizedQuery = query.Trim();
        return _commands
            .Select((descriptor, index) => new { Descriptor = descriptor, Index = index, Rank = GetRank(descriptor, normalizedQuery) })
            .Where(item => item.Descriptor.IsAvailable && item.Rank < int.MaxValue)
            .OrderBy(item => item.Rank)
            .ThenBy(item => item.Index)
            .Take(maxResults)
            .Select(item => item.Descriptor)
            .ToArray();
    }

    /// <summary>
    /// Unsubscribes from all ribbon collection notifications.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _isRebuildPending = false;
        foreach (var subscription in _subscriptions)
        {
            subscription.CollectionChanged -= OnCollectionChanged;
        }

        _subscriptions.Clear();
        foreach (var subscription in _vectorSubscriptions)
        {
            subscription.VectorChanged -= OnVectorChanged;
        }

        _vectorSubscriptions.Clear();
    }

    private void AddItem(UIElement item, RibbonTabItem tab, string tabHeader, string groupHeader)
    {
        if (item is DependencyObject source && IsDiscoverableCommand(item))
        {
            var displayName = GetHeaderText(item);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                _commands.Add(new RibbonCommandDescriptor(
                    _ribbon,
                    displayName,
                    tabHeader,
                    groupHeader,
                    GetKeyTip(item),
                    GetIcon(item),
                    GetScreenTipText(item),
                    source,
                    tab));
            }
        }

        foreach (var child in GetChildItems(item))
        {
            AddItem(child, tab, tabHeader, groupHeader);
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        RefreshSubscriptions();
        ScheduleRebuild();
    }

    private void OnVectorChanged(
        IObservableVector<object> sender,
        IVectorChangedEventArgs args)
    {
        if (_isDisposed)
        {
            return;
        }

        RefreshSubscriptions();
        ScheduleRebuild();
    }

    private void RefreshSubscriptions()
    {
        var desired = new HashSet<INotifyCollectionChanged>
        {
            _ribbon.Tabs,
        };
        var desiredVectors = new HashSet<IObservableVector<object>>();

        foreach (var tab in _ribbon.Tabs)
        {
            desired.Add(tab.Groups);
            foreach (var group in tab.Groups)
            {
                desired.Add(group.Items);
                foreach (var item in group.Items)
                {
                    AddNestedSubscriptions(item, desired, desiredVectors);
                }
            }
        }

        foreach (var obsolete in _subscriptions.Where(subscription => !desired.Contains(subscription)).ToArray())
        {
            obsolete.CollectionChanged -= OnCollectionChanged;
            _subscriptions.Remove(obsolete);
        }

        foreach (var collection in desired.Where(collection => !_subscriptions.Contains(collection)))
        {
            collection.CollectionChanged += OnCollectionChanged;
            _subscriptions.Add(collection);
        }

        foreach (var obsolete in _vectorSubscriptions
                     .Where(subscription => !desiredVectors.Contains(subscription))
                     .ToArray())
        {
            obsolete.VectorChanged -= OnVectorChanged;
            _vectorSubscriptions.Remove(obsolete);
        }

        foreach (var collection in desiredVectors
                     .Where(collection => !_vectorSubscriptions.Contains(collection)))
        {
            collection.VectorChanged += OnVectorChanged;
            _vectorSubscriptions.Add(collection);
        }
    }

    private static void AddNestedSubscriptions(
        UIElement item,
        ISet<INotifyCollectionChanged> subscriptions,
        ISet<IObservableVector<object>> vectorSubscriptions)
    {
        switch (item)
        {
            case RibbonDropDownButton dropDownButton:
                vectorSubscriptions.Add(dropDownButton.Items);
                break;
            case RibbonMenuItem menuItem:
                subscriptions.Add(menuItem.Items);
                break;
        }

        foreach (var child in GetChildItems(item))
        {
            AddNestedSubscriptions(child, subscriptions, vectorSubscriptions);
        }
    }

    private void ScheduleRebuild()
    {
        if (_isRebuildPending)
        {
            return;
        }

        _isRebuildPending = true;
        if (_ribbon.DispatcherQueue?.TryEnqueue(FlushPendingRebuild) != true)
        {
            FlushPendingRebuild();
        }
    }

    private void FlushPendingRebuild()
    {
        if (!_isDisposed && _isRebuildPending)
        {
            Rebuild();
        }
    }

    private static IEnumerable<UIElement> GetChildItems(UIElement item)
    {
        return item switch
        {
            RibbonSplitButton splitButton => splitButton.Items.OfType<UIElement>(),
            RibbonDropDownButton dropDownButton =>
                dropDownButton.Items.OfType<UIElement>(),
            RibbonMenuItem menuItem => menuItem.Items,
            Panel panel => panel.Children.OfType<UIElement>(),
            _ => Array.Empty<UIElement>()
        };
    }

    private static bool IsDiscoverableCommand(UIElement item)
    {
        return item is RibbonButton
            or RibbonToggleButton
            or RibbonSplitButton
            or RibbonDropDownButton
            or RibbonCheckBox
            or RibbonRadioButton
            or RibbonMenuItem;
    }

    private static int GetRank(RibbonCommandDescriptor descriptor, string query)
    {
        if (descriptor.DisplayName.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (descriptor.DisplayName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (Contains(descriptor.KeyTip, query))
        {
            return 2;
        }

        if (Contains(descriptor.DisplayName, query))
        {
            return 3;
        }

        if (Contains(descriptor.GroupHeader, query))
        {
            return 4;
        }

        if (Contains(descriptor.TabHeader, query))
        {
            return 5;
        }

        return int.MaxValue;
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string GetHeaderText(object item)
    {
        return item is IHeaderedControl headered
            ? GetText(headered.Header)
            : GetText(GetPropertyValue<object>(item, "Header"));
    }

    private static string? GetKeyTip(object item)
    {
        return item is IKeyTipedControl keyTiped && !string.IsNullOrWhiteSpace(keyTiped.KeyTip)
            ? keyTiped.KeyTip
            : GetStringProperty(item, "KeyTip");
    }

    private static string? GetScreenTipText(object item)
    {
        return GetStringProperty(item, "ScreenTipText") ?? GetStringProperty(item, "Description");
    }

    private static object? GetIcon(object item)
    {
        return GetPropertyValue<object>(item, "CurrentIconSource")
            ?? GetPropertyValue<object>(item, "LargeIconSource")
            ?? GetPropertyValue<object>(item, "SmallIconSource")
            ?? GetPropertyValue<object>(item, "CurrentIcon")
            ?? GetPropertyValue<object>(item, "Icon")
            ?? GetPropertyValue<object>(item, "LargeIcon")
            ?? GetPropertyValue<object>(item, "MediumIcon");
    }

    private static string? GetStringProperty(object item, string propertyName)
    {
        var value = GetPropertyValue<object>(item, propertyName);
        return value is string text && !string.IsNullOrWhiteSpace(text) ? text : null;
    }

    private static T? GetPropertyValue<T>(object source, string propertyName)
    {
        var property = global::Fluent.Modern.ReflectionPropertyHelper.GetReadableProperty(
            source.GetType(),
            propertyName);
        if (property?.GetMethod is null)
        {
            return default;
        }

        return property.GetValue(source) is T value ? value : default;
    }

    private static string GetText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text.Trim(),
            TextBlock textBlock => textBlock.Text.Trim(),
            ContentControl contentControl => GetText(contentControl.Content),
            _ => value.ToString()?.Trim() ?? string.Empty
        };
    }

    #endregion
}

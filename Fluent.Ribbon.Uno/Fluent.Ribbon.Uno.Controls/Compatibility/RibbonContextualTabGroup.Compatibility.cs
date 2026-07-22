namespace Fluent;

/// <summary>
/// WPF-compatible contextual-tab aliases and visibility synchronization.
/// </summary>
public partial class RibbonContextualTabGroup
{
    private readonly Dictionary<RibbonTabItem, Visibility> _authoredTabVisibility = new();
    private readonly Dictionary<RibbonTabItem, long> _tabVisibilityCallbacks = new();
    private bool _isSynchronizingTabVisibility;

    /// <summary>
    /// Identifies the WPF-named pointer-over foreground property.
    /// </summary>
    public static readonly DependencyProperty TabItemMouseOverForegroundProperty =
        DependencyProperty.Register(
            nameof(TabItemMouseOverForeground),
            typeof(Brush),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata(null, OnMouseOverForegroundChanged));

    /// <summary>Gets or sets the pointer-over foreground for contextual tabs.</summary>
    public Brush? TabItemMouseOverForeground
    {
        get => (Brush?)GetValue(TabItemMouseOverForegroundProperty);
        set => SetValue(TabItemMouseOverForegroundProperty, value);
    }

    /// <summary>
    /// Identifies the WPF-named selected pointer-over foreground property.
    /// </summary>
    public static readonly DependencyProperty TabItemSelectedMouseOverForegroundProperty =
        DependencyProperty.Register(
            nameof(TabItemSelectedMouseOverForeground),
            typeof(Brush),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata(null, OnSelectedMouseOverForegroundChanged));

    /// <summary>Gets or sets the selected pointer-over foreground.</summary>
    public Brush? TabItemSelectedMouseOverForeground
    {
        get => (Brush?)GetValue(TabItemSelectedMouseOverForegroundProperty);
        set => SetValue(TabItemSelectedMouseOverForegroundProperty, value);
    }

    private static void OnMouseOverForegroundChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        ((RibbonContextualTabGroup)sender).TabItemPointerOverForeground = (Brush?)args.NewValue;
    }

    private static void OnSelectedMouseOverForegroundChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        ((RibbonContextualTabGroup)sender).TabItemSelectedPointerOverForeground = (Brush?)args.NewValue;
    }

    /// <summary>
    /// Updates computed visibility and marks the first and last visible contextual tabs.
    /// The misspelling is retained for WPF API compatibility.
    /// </summary>
    public void UpdateInnerVisiblityAndGroupBorders()
    {
        UpdateContextualVisibilityAndBorders();
    }

    private void AttachTabItem(RibbonTabItem item)
    {
        if (_tabVisibilityCallbacks.ContainsKey(item))
        {
            return;
        }

        _authoredTabVisibility[item] = item.Visibility;
        _tabVisibilityCallbacks[item] = item.RegisterPropertyChangedCallback(
            UIElement.VisibilityProperty,
            OnTabItemVisibilityChanged);
    }

    private void DetachTabItem(RibbonTabItem item)
    {
        if (_tabVisibilityCallbacks.Remove(item, out var token))
        {
            item.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, token);
        }

        if (_authoredTabVisibility.Remove(item, out var visibility)
            && item.Visibility != visibility)
        {
            _isSynchronizingTabVisibility = true;
            item.Visibility = visibility;
            _isSynchronizingTabVisibility = false;
        }

        item.HasLeftGroupBorder = false;
        item.HasRightGroupBorder = false;
        item.HasSeparator = false;
    }

    private void OnTabItemVisibilityChanged(DependencyObject sender, DependencyProperty property)
    {
        if (_isSynchronizingTabVisibility || sender is not RibbonTabItem tab)
        {
            return;
        }

        _authoredTabVisibility[tab] = tab.Visibility;
        UpdateContextualVisibilityAndBorders();
    }

    private void UpdateContextualVisibilityAndBorders()
    {
        var groupIsVisible = Visibility == Visibility.Visible;

        _isSynchronizingTabVisibility = true;
        try
        {
            foreach (var tab in Items)
            {
                AttachTabItem(tab);

                if (groupIsVisible)
                {
                    var authoredVisibility = _authoredTabVisibility.TryGetValue(tab, out var value)
                        ? value
                        : Visibility.Visible;
                    if (tab.Visibility != authoredVisibility)
                    {
                        tab.Visibility = authoredVisibility;
                    }
                }
                else
                {
                    if (tab.Visibility != Visibility.Collapsed)
                    {
                        _authoredTabVisibility[tab] = tab.Visibility;
                        tab.Visibility = Visibility.Collapsed;
                    }
                }

                tab.HasLeftGroupBorder = false;
                tab.HasRightGroupBorder = false;
                tab.HasSeparator = false;
            }
        }
        finally
        {
            _isSynchronizingTabVisibility = false;
        }

        var visibleTabs = groupIsVisible
            ? Items.Where(tab => tab.Visibility == Visibility.Visible).ToList()
            : [];

        InnerVisibility = visibleTabs.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (visibleTabs.Count > 0)
        {
            visibleTabs[0].HasLeftGroupBorder = true;
            visibleTabs[0].HasSeparator = true;
            visibleTabs[^1].HasRightGroupBorder = true;
        }
    }
}

namespace Fluent;

using System.Collections;
using Windows.System;

/// <summary>
/// WPF-compatible, portable members for <see cref="RibbonTabControl"/>.
/// </summary>
public partial class RibbonTabControl : IDropDownControl, ILogicalChildSupport
{
    /// <summary>The default gap between tab headers and ribbon content.</summary>
    public const double DefaultContentGapHeight = 3;

    /// <summary>The default ribbon content height.</summary>
    public const double DefaultContentHeight = 100;

    /// <summary>Extra popup space reserved for key tips.</summary>
    public const double AdditionalPopupSpaceForKeyTips = 20;

    /// <summary>Grid-length form of <see cref="AdditionalPopupSpaceForKeyTips"/>.</summary>
    public static readonly GridLength AdditionalPopupSpaceForKeyTipsGridLength =
        new(AdditionalPopupSpaceForKeyTips);

    private readonly ObservableCollection<UIElement> _toolBarItems = new();
    private bool _isUpdatingDropDownState;

    /// <summary>Occurs when an open backstage surface should close.</summary>
    public event EventHandler? RequestBackstageClose;

    /// <inheritdoc />
    public event EventHandler? DropDownOpened;

    /// <inheritdoc />
    public event EventHandler? DropDownClosed;

    /// <summary>Identifies the <see cref="Menu"/> dependency property.</summary>
    public static readonly DependencyProperty MenuProperty =
        DependencyProperty.Register(
            nameof(Menu),
            typeof(UIElement),
            typeof(RibbonTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets content displayed before the tab headers.</summary>
    public UIElement? Menu
    {
        get => (UIElement?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Identifies the <see cref="CanMinimize"/> dependency property.</summary>
    public static readonly DependencyProperty CanMinimizeProperty =
        DependencyProperty.Register(
            nameof(CanMinimize),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether the ribbon may be minimized.</summary>
    public bool CanMinimize
    {
        get => (bool)GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(false, OnIsSimplifiedChanged));

    /// <summary>Gets or sets whether simplified ribbon layout is active.</summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    /// <summary>Identifies the <see cref="CanUseSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty CanUseSimplifiedProperty =
        DependencyProperty.Register(
            nameof(CanUseSimplified),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether simplified mode is available.</summary>
    public bool CanUseSimplified
    {
        get => (bool)GetValue(CanUseSimplifiedProperty);
        set => SetValue(CanUseSimplifiedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(false, OnIsDropDownOpenChanged));

    /// <inheritdoc />
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="HighlightSelectedItem"/> dependency property.</summary>
    public static readonly DependencyProperty HighlightSelectedItemProperty =
        DependencyProperty.Register(
            nameof(HighlightSelectedItem),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether the selected tab header remains highlighted.</summary>
    public bool HighlightSelectedItem
    {
        get => (bool)GetValue(HighlightSelectedItemProperty);
        set => SetValue(HighlightSelectedItemProperty, value);
    }

    /// <summary>Identifies the <see cref="ContentGapHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ContentGapHeightProperty =
        DependencyProperty.Register(
            nameof(ContentGapHeight),
            typeof(double),
            typeof(RibbonTabControl),
            new PropertyMetadata(DefaultContentGapHeight));

    /// <summary>Gets or sets the gap between headers and content.</summary>
    public double ContentGapHeight
    {
        get => (double)GetValue(ContentGapHeightProperty);
        set => SetValue(ContentGapHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="AreTabHeadersVisible"/> dependency property.</summary>
    public static readonly DependencyProperty AreTabHeadersVisibleProperty =
        DependencyProperty.Register(
            nameof(AreTabHeadersVisible),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether tab headers are visible.</summary>
    public bool AreTabHeadersVisible
    {
        get => (bool)GetValue(AreTabHeadersVisibleProperty);
        set => SetValue(AreTabHeadersVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="IsToolBarVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsToolBarVisibleProperty =
        DependencyProperty.Register(
            nameof(IsToolBarVisible),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether toolbar content is visible.</summary>
    public bool IsToolBarVisible
    {
        get => (bool)GetValue(IsToolBarVisibleProperty);
        set => SetValue(IsToolBarVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="IsMouseWheelScrollingEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsMouseWheelScrollingEnabledProperty =
        DependencyProperty.Register(
            nameof(IsMouseWheelScrollingEnabled),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether wheel input over the tab strip changes selection.</summary>
    public bool IsMouseWheelScrollingEnabled
    {
        get => (bool)GetValue(IsMouseWheelScrollingEnabledProperty);
        set => SetValue(IsMouseWheelScrollingEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="IsMouseWheelScrollingEnabledEverywhere"/> dependency property.</summary>
    public static readonly DependencyProperty IsMouseWheelScrollingEnabledEverywhereProperty =
        DependencyProperty.Register(
            nameof(IsMouseWheelScrollingEnabledEverywhere),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether wheel input anywhere in the tab control changes selection.</summary>
    public bool IsMouseWheelScrollingEnabledEverywhere
    {
        get => (bool)GetValue(IsMouseWheelScrollingEnabledEverywhereProperty);
        set => SetValue(IsMouseWheelScrollingEnabledEverywhereProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDisplayOptionsButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsDisplayOptionsButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsDisplayOptionsButtonVisible),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether the display-options button is visible.</summary>
    public bool IsDisplayOptionsButtonVisible
    {
        get => (bool)GetValue(IsDisplayOptionsButtonVisibleProperty);
        set => SetValue(IsDisplayOptionsButtonVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedContent"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedContentProperty =
        DependencyProperty.Register(
            nameof(SelectedContent),
            typeof(object),
            typeof(RibbonTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets the content of the selected tab.</summary>
    public object? SelectedContent
    {
        get => GetValue(SelectedContentProperty);
        private set => SetValue(SelectedContentProperty, value);
    }

    /// <inheritdoc />
    public Popup? DropDownPopup => null;

    /// <summary>Gets the panel hosting tab headers when exposed by the active template.</summary>
    public Panel? TabsContainer { get; private set; }

    /// <summary>Gets the presenter hosting selected tab content.</summary>
    public FrameworkElement? SelectedContentPresenter => _contentPresenter;

    /// <inheritdoc />
    public bool IsContextMenuOpened { get; set; }

    /// <summary>Gets toolbar items associated with the ribbon tabs.</summary>
    public ObservableCollection<UIElement> ToolBarItems => _toolBarItems;

    private void InitializeCompatibility()
    {
        OnInitialized(EventArgs.Empty);
        SelectionChanged += OnCompatibilitySelectionChanged;
        PointerWheelChanged += OnCompatibilityPointerWheelChanged;
        Unloaded += OnCompatibilityUnloaded;
    }

    /// <summary>Provides the WPF-compatible initialization hook.</summary>
    protected virtual void OnInitialized(EventArgs e)
    {
    }

    /// <summary>Creates the default tab container.</summary>
    protected virtual DependencyObject GetContainerForItemOverride()
    {
        return new RibbonTabItem();
    }

    /// <summary>Gets whether an item is already its own tab container.</summary>
    protected virtual bool IsItemItsOwnContainerOverride(object item)
    {
        return item is RibbonTabItem;
    }

    /// <summary>Handles tab collection changes.</summary>
    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        HandleTabItemsChanged(e);
    }

    /// <summary>Handles selection changes.</summary>
    protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        UpdateSelectedContent();

        if (SelectedItem is RibbonTabItem selectedTab)
        {
            selectedTab.IsSelected = true;
            if (IsMinimized)
            {
                IsDropDownOpen = true;
            }
        }
        else if (IsDropDownOpen)
        {
            IsDropDownOpen = false;
        }
    }

    /// <summary>Handles preview wheel input.</summary>
    protected virtual void OnPreviewMouseWheel(PointerRoutedEventArgs e)
    {
    }

    /// <summary>Handles wheel input used to cycle tabs.</summary>
    protected virtual void OnMouseWheel(PointerRoutedEventArgs e)
    {
        if (!IsMouseWheelScrollingEnabled && !IsMouseWheelScrollingEnabledEverywhere)
        {
            return;
        }

        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        if (delta != 0 && SelectRelativeTab(delta > 0 ? -1 : 1))
        {
            e.Handled = true;
        }
    }

    private void UpdateCompatibilityTemplateParts()
    {
        TabsContainer = GetTemplateChild("PART_TabsContainer") as Panel;
        UpdateSelectedContent();
    }

    private static void OnIsSimplifiedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not RibbonTabControl tabControl)
        {
            return;
        }

        foreach (var tab in tabControl.TabItems.OfType<RibbonTabItem>())
        {
            tab.UpdateSimplifiedState((bool)args.NewValue);
        }
    }

    private static void OnIsDropDownOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var tabControl = (RibbonTabControl)sender;
        if (tabControl._isUpdatingDropDownState)
        {
            return;
        }

        if ((bool)args.NewValue && !tabControl.IsMinimized)
        {
            tabControl._isUpdatingDropDownState = true;
            tabControl.IsDropDownOpen = false;
            tabControl._isUpdatingDropDownState = false;
            return;
        }

        tabControl.RaiseRequestBackstageClose();
        tabControl.UpdateMinimizedState();

        if ((bool)args.NewValue)
        {
            if (tabControl.SelectedItem is null)
            {
                tabControl.SelectedItem =
                    tabControl.GetFirstVisibleAndEnabledItem()
                    ?? tabControl.GetFirstVisibleItem();
            }

            tabControl.DropDownOpened?.Invoke(tabControl, EventArgs.Empty);
        }
        else
        {
            tabControl.DropDownClosed?.Invoke(tabControl, EventArgs.Empty);
        }
    }

    private void OnMinimizedCompatibilityChanged(bool isMinimized)
    {
        if (!isMinimized && IsDropDownOpen)
        {
            IsDropDownOpen = false;
        }

        if (!isMinimized && SelectedItem is null)
        {
            SelectFirstTab();
        }
    }

    private void OnCompatibilitySelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        OnSelectionChanged(args);
    }

    private void UpdateSelectedContent()
    {
        SelectedContent = (SelectedItem as RibbonTabItem)?.Content;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        base.OnKeyDown(args);
        if (args.Handled)
        {
            return;
        }

        switch (args.Key)
        {
            case VirtualKey.Escape when IsDropDownOpen:
                IsDropDownOpen = false;
                args.Handled = true;
                break;
            case VirtualKey.Home:
                args.Handled = SelectRelativeTab(1, -1);
                break;
            case VirtualKey.End:
                args.Handled = SelectRelativeTab(-1, TabItems.Count);
                break;
            case VirtualKey.Left:
                args.Handled = SelectRelativeTab(-1);
                break;
            case VirtualKey.Right:
                args.Handled = SelectRelativeTab(1);
                break;
        }
    }

    private void OnCompatibilityPointerWheelChanged(object sender, PointerRoutedEventArgs args)
    {
        OnPreviewMouseWheel(args);
        if (!args.Handled)
        {
            OnMouseWheel(args);
        }
    }

    /// <inheritdoc />
    void ILogicalChildSupport.AddLogicalChild(object child)
    {
    }

    /// <inheritdoc />
    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
            if (Menu is not null)
            {
                yield return Menu;
            }

            foreach (var tab in TabItems)
            {
                yield return tab;
            }

            foreach (var item in ToolBarItems)
            {
                yield return item;
            }
        }
    }

    private bool SelectRelativeTab(int direction, int? startIndex = null)
    {
        if (direction == 0 || TabItems.Count == 0)
        {
            return false;
        }

        var index = startIndex ?? SelectedIndex;
        for (var count = 0; count < TabItems.Count; count++)
        {
            index += direction;
            if (index < 0)
            {
                index = TabItems.Count - 1;
            }
            else if (index >= TabItems.Count)
            {
                index = 0;
            }

            if (TabItems[index] is RibbonTabItem { Visibility: Visibility.Visible, IsEnabled: true } tab)
            {
                SelectedItem = tab;
                return true;
            }
        }

        return false;
    }

    private void OnCompatibilityUnloaded(object sender, RoutedEventArgs args)
    {
        IsDropDownOpen = false;
    }

    /// <summary>Selects the first visible tab while the ribbon is expanded.</summary>
    public void SelectFirstTab()
    {
        if (IsMinimized)
        {
            return;
        }

        SelectedItem = GetFirstVisibleAndEnabledItem()
                       ?? (!IsEnabled ? GetFirstVisibleItem() : null);
    }

    /// <summary>Raises <see cref="RequestBackstageClose"/>.</summary>
    public void RaiseRequestBackstageClose()
    {
        RequestBackstageClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Gets the first visible tab.</summary>
    public object? GetFirstVisibleItem() =>
        TabItems.OfType<RibbonTabItem>().FirstOrDefault(tab => tab.Visibility == Visibility.Visible);

    /// <summary>Gets the first visible and enabled tab.</summary>
    public object? GetFirstVisibleAndEnabledItem() =>
        TabItems.OfType<RibbonTabItem>().FirstOrDefault(
            tab => tab.Visibility == Visibility.Visible && tab.IsEnabled);

    internal void NotifyTabItemsChanged(NotifyCollectionChangedEventArgs args)
    {
        OnItemsChanged(args);
    }

    private void HandleTabItemsChanged(NotifyCollectionChangedEventArgs args)
    {
        if (SelectedItem is RibbonTabItem { Visibility: Visibility.Visible })
        {
            return;
        }

        SelectedItem = GetFirstVisibleAndEnabledItem() ?? GetFirstVisibleItem();
        UpdateSelectedContent();
    }
}

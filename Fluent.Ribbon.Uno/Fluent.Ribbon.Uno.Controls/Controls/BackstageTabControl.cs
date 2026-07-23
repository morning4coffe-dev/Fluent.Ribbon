namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

using System.Collections;

/// <summary>
/// Represents the selector used by <see cref="Backstage"/>.
/// </summary>
[TemplatePart(Name = PART_SelectedContentHost, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_ItemsPanelContainer, Type = typeof(UIElement))]
[TemplatePart(Name = PART_BackButton, Type = typeof(UIElement))]
#if WINDOWS
public partial class BackstageTabControl : ListBox, ILogicalChildSupport
#else
public partial class BackstageTabControl : Selector, ILogicalChildSupport
#endif
{
    private const string PART_SelectedContentHost = "PART_SelectedContentHost";
    private const string PART_ItemsPanelContainer = "PART_ItemsPanelContainer";
    private const string PART_BackButton = "PART_BackButton";

    private WinUIButton? backButton;

    internal ContentPresenter? SelectedContentHost { get; private set; }

    internal UIElement? ItemsPanelContainer { get; private set; }

    internal UIElement? BackButton { get; private set; }

    /// <summary>Identifies the <see cref="BackButtonUid"/> dependency property.</summary>
    public static readonly DependencyProperty BackButtonUidProperty =
        DependencyProperty.Register(
            nameof(BackButtonUid),
            typeof(string),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the automation identifier of the back button.</summary>
    public string? BackButtonUid
    {
        get => (string?)GetValue(BackButtonUidProperty);
        set => SetValue(BackButtonUidProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedContent"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedContentProperty =
        DependencyProperty.Register(
            nameof(SelectedContent),
            typeof(object),
            typeof(BackstageTabControl),
            new PropertyMetadata(null, OnSelectedContentChanged));

    /// <summary>Gets the content of the selected tab.</summary>
    public object? SelectedContent
    {
        get => GetValue(SelectedContentProperty);
        internal set => SetValue(SelectedContentProperty, value);
    }

    /// <summary>Identifies the <see cref="ContentStringFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ContentStringFormatProperty =
        DependencyProperty.Register(
            nameof(ContentStringFormat),
            typeof(string),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the content string format.</summary>
    public string? ContentStringFormat
    {
        get => (string?)GetValue(ContentStringFormatProperty);
        set => SetValue(ContentStringFormatProperty, value);
    }

    /// <summary>Identifies the <see cref="ContentTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(
            nameof(ContentTemplate),
            typeof(DataTemplate),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the fallback content template.</summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="ContentTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty ContentTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(ContentTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the fallback content-template selector.</summary>
    public DataTemplateSelector? ContentTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ContentTemplateSelectorProperty);
        set => SetValue(ContentTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedContentStringFormat"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedContentStringFormatProperty =
        DependencyProperty.Register(
            nameof(SelectedContentStringFormat),
            typeof(string),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets the selected content string format.</summary>
    public string? SelectedContentStringFormat
    {
        get => (string?)GetValue(SelectedContentStringFormatProperty);
        internal set => SetValue(SelectedContentStringFormatProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedContentTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedContentTemplateProperty =
        DependencyProperty.Register(
            nameof(SelectedContentTemplate),
            typeof(DataTemplate),
            typeof(BackstageTabControl),
            new PropertyMetadata(null, OnSelectedContentTemplateChanged));

    /// <summary>Gets the selected content template.</summary>
    public DataTemplate? SelectedContentTemplate
    {
        get => (DataTemplate?)GetValue(SelectedContentTemplateProperty);
        internal set => SetValue(SelectedContentTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedContentTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedContentTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(SelectedContentTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(BackstageTabControl),
            new PropertyMetadata(null, OnSelectedContentTemplateChanged));

    /// <summary>Gets the selected content-template selector.</summary>
    public DataTemplateSelector? SelectedContentTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(SelectedContentTemplateSelectorProperty);
        internal set => SetValue(SelectedContentTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemsPanelMinWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsPanelMinWidthProperty =
        DependencyProperty.Register(
            nameof(ItemsPanelMinWidth),
            typeof(double),
            typeof(BackstageTabControl),
            new PropertyMetadata(125D));

    /// <summary>Gets or sets the minimum width of the items panel.</summary>
    public double ItemsPanelMinWidth
    {
        get => (double)GetValue(ItemsPanelMinWidthProperty);
        set => SetValue(ItemsPanelMinWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemsPanelBackground"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsPanelBackgroundProperty =
        DependencyProperty.Register(
            nameof(ItemsPanelBackground),
            typeof(Brush),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the items-panel background.</summary>
    public Brush? ItemsPanelBackground
    {
        get => (Brush?)GetValue(ItemsPanelBackgroundProperty);
        set => SetValue(ItemsPanelBackgroundProperty, value);
    }

    /// <summary>Identifies the <see cref="ParentBackstage"/> dependency property.</summary>
    public static readonly DependencyProperty ParentBackstageProperty =
        DependencyProperty.Register(
            nameof(ParentBackstage),
            typeof(Backstage),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the containing backstage.</summary>
    public Backstage? ParentBackstage
    {
        get => (Backstage?)GetValue(ParentBackstageProperty);
        set => SetValue(ParentBackstageProperty, value);
    }

    /// <summary>Identifies the <see cref="IsBackButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsBackButtonVisible),
            typeof(bool),
            typeof(BackstageTabControl),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether the back button is visible.</summary>
    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    /// <summary>Initializes a new instance of the <see cref="BackstageTabControl"/> class.</summary>
    public BackstageTabControl()
    {
        DefaultStyleKey = typeof(BackstageTabControl);
        Loaded += HandleLoaded;
        Unloaded += HandleUnloaded;
        Items.VectorChanged += HandleItemsVectorChanged;
        SelectionChanged += HandleSelectionChanged;
        OnInitialized(EventArgs.Empty);
    }

    /// <summary>Provides the WPF-compatible initialization hook.</summary>
    protected virtual void OnInitialized(EventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (backButton is not null)
        {
            backButton.Click -= OnBackButtonClick;
        }

        SelectedContentHost = GetTemplateChild(PART_SelectedContentHost) as ContentPresenter;
        ItemsPanelContainer = GetTemplateChild(PART_ItemsPanelContainer) as UIElement;
        BackButton = GetTemplateChild(PART_BackButton) as UIElement;
        backButton = BackButton as WinUIButton;

        if (backButton is not null)
        {
            backButton.Click += OnBackButtonClick;
            if (string.IsNullOrWhiteSpace(BackButtonUid) is false)
            {
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                    backButton,
                    BackButtonUid);
            }
        }

        UpdateSelectedContent();
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride()
    {
        return new BackstageTabItem();
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is BackstageTabItem
            or BackstageButton
            or SeparatorTabItem;
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(
        DependencyObject element,
        object item)
    {
#if WINDOWS
        // WinUI's ListBox implementation assumes every container is a ListBoxItem and
        // casts inside its base implementation. Backstage uses its own container types.
        if (!ReferenceEquals(element, item)
            && element is BackstageTabItem tabItem)
        {
            tabItem.Content = item;
        }
#else
        base.PrepareContainerForItemOverride(element, item);
#endif
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(
        DependencyObject element,
        object item)
    {
#if WINDOWS
        if (!ReferenceEquals(element, item)
            && element is BackstageTabItem tabItem
            && ReferenceEquals(tabItem.Content, item))
        {
            tabItem.Content = null;
        }
#else
        base.ClearContainerForItemOverride(element, item);
#endif
    }

    /// <inheritdoc />
    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        if (SelectedIndex < 0)
        {
            if (SelectPreselectedTab() is false)
            {
                SelectFirstAvailableTab();
            }
        }
        else
        {
            UpdateSelectedContent();
        }
    }

    /// <inheritdoc />
    protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        foreach (var item in Items)
        {
            if (GetTabItem(item) is { } tabItem)
            {
                tabItem.IsSelected = ReferenceEquals(item, SelectedItem)
                                     || ReferenceEquals(tabItem, SelectedItem);
            }
        }

        UpdateSelectedContent();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (!e.Handled && XamlRoot is not null)
        {
            var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (e.Key == Windows.System.VirtualKey.F6)
            {
                e.Handled = SelectedContent is UIElement selectedContent
                            && FocusRoutingHelper.IsDescendantOf(focusedElement, selectedContent)
                    ? FocusSelectedTab()
                    : SelectedContent is UIElement content
                      && FocusRoutingHelper.FocusFirst(content);
            }
            else if (e.Key == Windows.System.VirtualKey.Tab)
            {
                var shiftDown =
                    (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                         Windows.System.VirtualKey.Shift)
                     & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;

                if (shiftDown
                    && SelectedContent is UIElement selectedContent
                    && FocusRoutingHelper.IsDescendantOf(focusedElement, selectedContent))
                {
                    e.Handled = FocusSelectedTab();
                }
                else if (!shiftDown
                         && focusedElement is BackstageTabItem { IsSelected: true }
                         && SelectedContent is UIElement content)
                {
                    e.Handled = FocusRoutingHelper.FocusFirst(content);
                }
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>Selects a tab item from automation and key-tip paths.</summary>
    internal void SelectTabForAutomation(BackstageTabItem tab)
    {
        var item = Items.Cast<object?>()
            .FirstOrDefault(
                candidate => ReferenceEquals(candidate, tab)
                             || ReferenceEquals(ContainerFromItem(candidate), tab));

        if (item is not null)
        {
            SelectedItem = item;
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

    /// <summary>Gets the logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
            if (SelectedContent is not null)
            {
                yield return SelectedContent;
            }
        }
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonBackstageTabControlAutomationPeer(this);

    private static void OnSelectedContentChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (((BackstageTabControl)sender).SelectedContentHost is { } host)
        {
            host.Content = args.NewValue;
        }
    }

    private static void OnSelectedContentTemplateChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (((BackstageTabControl)sender).SelectedContentHost is { } host)
        {
            var control = (BackstageTabControl)sender;
            host.ContentTemplate = control.SelectedContentTemplate;
            host.ContentTemplateSelector = control.SelectedContentTemplateSelector;
        }
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        ParentBackstage = FocusRoutingHelper.FindAncestor<Backstage>(this);
        if (SelectedIndex < 0)
        {
            if (SelectPreselectedTab() is false)
            {
                SelectFirstAvailableTab();
            }
        }
    }

    private void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        ParentBackstage = null;
    }

    private void HandleItemsVectorChanged(
        Windows.Foundation.Collections.IObservableVector<object> sender,
        Windows.Foundation.Collections.IVectorChangedEventArgs e)
    {
        if (e.CollectionChange
                == Windows.Foundation.Collections.CollectionChange.ItemInserted
            && e.Index < (uint)Items.Count
            && GetTabItem(Items[(int)e.Index]) is { IsSelected: true })
        {
            SelectedIndex = (int)e.Index;
        }

        OnItemsChanged(
            new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OnSelectionChanged(e);
    }

    private void OnBackButtonClick(object sender, RoutedEventArgs e)
    {
        ParentBackstage?.SetIsOpen(false);
    }

    private void SelectFirstAvailableTab()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (GetTabItem(Items[index]) is { IsEnabled: true, Visibility: Visibility.Visible })
            {
                SelectedIndex = index;
                return;
            }
        }
    }

    private bool SelectPreselectedTab()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (GetTabItem(Items[index]) is { IsSelected: true })
            {
                SelectedIndex = index;
                return true;
            }
        }

        return false;
    }

    private BackstageTabItem? GetTabItem(object? item)
    {
        return item as BackstageTabItem
               ?? ContainerFromItem(item) as BackstageTabItem;
    }

    private void UpdateSelectedContent()
    {
        var selectedTabItem = GetTabItem(SelectedItem);
        if (selectedTabItem is null)
        {
            SelectedContent = null;
            SelectedContentTemplate = ContentTemplate;
            SelectedContentTemplateSelector = ContentTemplateSelector;
            SelectedContentStringFormat = ContentStringFormat;
            return;
        }

        SelectedContent = selectedTabItem.Content;
        SelectedContentTemplate =
            selectedTabItem.ContentTemplate ?? ContentTemplate;
        SelectedContentTemplateSelector =
            selectedTabItem.ContentTemplateSelector ?? ContentTemplateSelector;
        SelectedContentStringFormat = ContentStringFormat;
    }

    private bool FocusSelectedTab()
    {
        return GetTabItem(SelectedItem)?.Focus(FocusState.Programmatic) == true;
    }
}

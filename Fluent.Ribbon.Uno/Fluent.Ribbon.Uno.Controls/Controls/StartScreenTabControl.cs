namespace Fluent;

using System.Collections;

/// <summary>
/// A specialized tab control for the Start Screen, with left and right content areas.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// In WPF this extends BackstageTabControl; here it's a standalone control.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_LeftContent, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_RightContent, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_ItemsPanel, Type = typeof(StackPanel))]
public partial class StartScreenTabControl : BackstageTabControl
{
    private const string PART_LeftContent = "PART_LeftContent";
    private const string PART_RightContent = "PART_RightContent";
    private const string PART_ItemsPanel = "PART_ItemsPanel";

    private StackPanel? _itemsPanel;
    private ContentPresenter? _rightContentPresenter;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(StartScreenTabControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of tab items.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftContent"/> dependency property.</summary>
    public static readonly DependencyProperty LeftContentProperty =
        DependencyProperty.Register(
            nameof(LeftContent),
            typeof(object),
            typeof(StartScreenTabControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the left content (e.g., branding, recent documents).
    /// </summary>
    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftContentMargin"/> dependency property.</summary>
    public static readonly DependencyProperty LeftContentMarginProperty =
        DependencyProperty.Register(
            nameof(LeftContentMargin),
            typeof(Thickness),
            typeof(StartScreenTabControl),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Gets or sets the margin around the left content.
    /// </summary>
    public Thickness LeftContentMargin
    {
        get => (Thickness)GetValue(LeftContentMarginProperty);
        set => SetValue(LeftContentMarginProperty, value);
    }

    /// <summary>Identifies the <see cref="RightContent"/> dependency property.</summary>
    public static readonly DependencyProperty RightContentProperty =
        DependencyProperty.Register(
            nameof(RightContent),
            typeof(object),
            typeof(StartScreenTabControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the right content (template gallery area).
    /// </summary>
    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedContent"/> dependency property.</summary>
    public new static readonly DependencyProperty SelectedContentProperty =
        DependencyProperty.Register(
            nameof(SelectedContent),
            typeof(UIElement),
            typeof(StartScreenTabControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content of the currently selected tab.
    /// </summary>
    public new UIElement? SelectedContent
    {
        get => (UIElement?)GetValue(SelectedContentProperty);
        set => SetValue(SelectedContentProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StartScreenTabControl"/> class.
    /// </summary>
    public StartScreenTabControl()
    {
        DefaultStyleKey = typeof(StartScreenTabControl);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsPanel = GetTemplateChild(PART_ItemsPanel) as StackPanel;
        _rightContentPresenter = GetTemplateChild(PART_RightContent) as ContentPresenter;

        SyncItems();
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
    }

    private void SyncItems()
    {
        if (_itemsPanel is null)
        {
            return;
        }

        _itemsPanel.Children.Clear();

        foreach (var item in Items)
        {
            _itemsPanel.Children.Add(item);
        }

        // Select first tab by default
        var firstTab = Items.OfType<BackstageTabItem>().FirstOrDefault();
        if (firstTab is not null)
        {
            SelectTab(firstTab);
        }
    }

    private void SelectTab(BackstageTabItem tab)
    {
        foreach (var item in Items.OfType<BackstageTabItem>())
        {
            item.IsSelected = false;
        }

        tab.IsSelected = true;
        SelectedContent = tab.Content as UIElement;
    }

    internal new void SelectTabForAutomation(BackstageTabItem tab) => SelectTab(tab);

    /// <inheritdoc />
    protected override IEnumerator LogicalChildren
    {
        get
        {
            if (LeftContent is not null)
            {
                yield return LeftContent;
            }

            if (RightContent is not null)
            {
                yield return RightContent;
            }

            if (SelectedContent is not null)
            {
                yield return SelectedContent;
            }
        }
    }

    #endregion
}

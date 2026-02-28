namespace Fluent;

/// <summary>
/// Represents a gallery that is displayed inline within a RibbonGroupBox,
/// showing a subset of items directly in the ribbon with an expand button
/// to show all items in a popup.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_GalleryPanel, Type = typeof(ItemsRepeater))]
[TemplatePart(Name = PART_ExpandButton, Type = typeof(Button))]
[TemplatePart(Name = PART_UpButton, Type = typeof(Button))]
[TemplatePart(Name = PART_DownButton, Type = typeof(Button))]
public partial class InRibbonGallery : Control, IScalableRibbonControl, IHeaderedControl
{
    private ItemsRepeater? _galleryPanel;
    private Button? _expandButton;
    private Button? _upButton;
    private Button? _downButton;
    private ScrollViewer? _scrollViewer;
    private Flyout? _popupFlyout;
#pragma warning disable CS0169
    private int _scrollOffset;
#pragma warning restore CS0169

    private const string PART_GalleryPanel = "PART_GalleryPanel";
    private const string PART_ExpandButton = "PART_ExpandButton";
    private const string PART_UpButton = "PART_UpButton";
    private const string PART_DownButton = "PART_DownButton";

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the gallery header.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of gallery items.
    /// </summary>
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(60.0));

    /// <summary>
    /// Gets or sets the width of each gallery item.
    /// </summary>
    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(
            nameof(ItemHeight),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(24.0));

    /// <summary>
    /// Gets or sets the height of each gallery item.
    /// </summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MaxItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MaxItemsInRow),
            typeof(int),
            typeof(InRibbonGallery),
            new PropertyMetadata(9));

    /// <summary>
    /// Gets or sets the maximum number of items in a row for inline display.
    /// </summary>
    public int MaxItemsInRow
    {
        get => (int)GetValue(MaxItemsInRowProperty);
        set => SetValue(MaxItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="MinItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MinItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MinItemsInRow),
            typeof(int),
            typeof(InRibbonGallery),
            new PropertyMetadata(1));

    /// <summary>
    /// Gets or sets the minimum number of items in a row.
    /// </summary>
    public int MinItemsInRow
    {
        get => (int)GetValue(MinItemsInRowProperty);
        set => SetValue(MinItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxDropDownItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDropDownItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MaxDropDownItemsInRow),
            typeof(int),
            typeof(InRibbonGallery),
            new PropertyMetadata(10));

    /// <summary>
    /// Gets or sets the maximum number of items in a row for dropdown display.
    /// </summary>
    public int MaxDropDownItemsInRow
    {
        get => (int)GetValue(MaxDropDownItemsInRowProperty);
        set => SetValue(MaxDropDownItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(InRibbonGallery),
            new PropertyMetadata(-1));

    /// <summary>
    /// Gets or sets the index of the selected item.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the gallery dropdown popup is open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(InRibbonGallery),
            new PropertyMetadata(RibbonControlSize.Large));

    /// <summary>
    /// Gets or sets the ribbon control size.
    /// </summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(InRibbonGallery),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(ImageSource),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the large icon for collapsed display.
    /// </summary>
    public ImageSource? LargeIcon
    {
        get => (ImageSource?)GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(InRibbonGallery),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph for collapsed display.
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="MenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty MenuItemsProperty =
        DependencyProperty.Register(
            nameof(MenuItems),
            typeof(ObservableCollection<UIElement>),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the menu items shown below the gallery in the popup.
    /// </summary>
    public ObservableCollection<UIElement> MenuItems
    {
        get => (ObservableCollection<UIElement>)GetValue(MenuItemsProperty);
        private set => SetValue(MenuItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="Selectable"/> dependency property.</summary>
    public static readonly DependencyProperty SelectableProperty =
        DependencyProperty.Register(
            nameof(Selectable),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether items in the gallery can be selected.
    /// </summary>
    public bool Selectable
    {
        get => (bool)GetValue(SelectableProperty);
        set => SetValue(SelectableProperty, value);
    }

    /// <summary>Identifies the <see cref="CanCollapseToButton"/> dependency property.</summary>
    public static readonly DependencyProperty CanCollapseToButtonProperty =
        DependencyProperty.Register(
            nameof(CanCollapseToButton),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether this gallery can collapse to a button when scaled down.
    /// </summary>
    public bool CanCollapseToButton
    {
        get => (bool)GetValue(CanCollapseToButtonProperty);
        set => SetValue(CanCollapseToButtonProperty, value);
    }

    /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property.</summary>
    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(
            nameof(IsCollapsed),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(false, OnIsCollapsedChanged));

    /// <summary>
    /// Gets or sets whether the gallery is collapsed to a button.
    /// </summary>
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InRibbonGallery"/> class.
    /// </summary>
    public InRibbonGallery()
    {
        DefaultStyleKey = typeof(InRibbonGallery);
        Items = new ObservableCollection<UIElement>();
        MenuItems = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _galleryPanel = GetTemplateChild(PART_GalleryPanel) as ItemsRepeater;

        if (_expandButton is not null)
        {
            _expandButton.Click -= OnExpandButtonClick;
        }

        _expandButton = GetTemplateChild(PART_ExpandButton) as Button;
        if (_expandButton is not null)
        {
            _expandButton.Click += OnExpandButtonClick;
        }

        if (_upButton is not null)
        {
            _upButton.Click -= OnUpButtonClick;
        }

        _upButton = GetTemplateChild(PART_UpButton) as Button;
        if (_upButton is not null)
        {
            _upButton.Click += OnUpButtonClick;
        }

        if (_downButton is not null)
        {
            _downButton.Click -= OnDownButtonClick;
        }

        _downButton = GetTemplateChild(PART_DownButton) as Button;
        if (_downButton is not null)
        {
            _downButton.Click += OnDownButtonClick;
        }

        // Find the ScrollViewer in the visual tree if present
        _scrollViewer = FindScrollViewer(_galleryPanel);

        SetupGalleryPanel();
        UpdateVisualState();
    }

    #endregion

    #region IScalableRibbonControl

    /// <inheritdoc/>
    public void ScaleTo(RibbonControlSize size)
    {
        Size = size;

        if (CanCollapseToButton)
        {
            IsCollapsed = size == RibbonControlSize.Small;
        }
    }

    /// <summary>
    /// Reduces the gallery by decreasing MaxItemsInRow.
    /// </summary>
    public void Reduce()
    {
        if (MaxItemsInRow > MinItemsInRow)
        {
            MaxItemsInRow--;
            UpdateGalleryLayout();
        }
        else if (CanCollapseToButton && !IsCollapsed)
        {
            IsCollapsed = true;
        }
    }

    /// <summary>
    /// Enlarges the gallery by increasing MaxItemsInRow.
    /// </summary>
    public void Enlarge()
    {
        if (IsCollapsed)
        {
            IsCollapsed = false;
        }
        else
        {
            MaxItemsInRow++;
            UpdateGalleryLayout();
        }
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SetupGalleryPanel();
    }

    private void SetupGalleryPanel()
    {
        if (_galleryPanel is null) return;

        _galleryPanel.Layout = new UniformGridLayout
        {
            MinItemWidth = ItemWidth,
            MinItemHeight = ItemHeight,
            MaximumRowsOrColumns = MaxItemsInRow,
            Orientation = Orientation.Horizontal,
            MinRowSpacing = 0,
            MinColumnSpacing = 0,
        };

        _galleryPanel.ItemsSource = Items;
    }

    private void UpdateGalleryLayout()
    {
        if (_galleryPanel?.Layout is UniformGridLayout layout)
        {
            layout.MaximumRowsOrColumns = MaxItemsInRow;
        }
    }

    private void OnExpandButtonClick(object sender, RoutedEventArgs e)
    {
        ShowPopup();
    }

    private void OnUpButtonClick(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ChangeView(null, Math.Max(0, _scrollViewer.VerticalOffset - ItemHeight), null);
        }
    }

    private void OnDownButtonClick(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ChangeView(null, _scrollViewer.VerticalOffset + ItemHeight, null);
        }
    }

    private void ShowPopup()
    {
        if (_popupFlyout is null)
        {
            _popupFlyout = new Flyout
            {
                Placement = FlyoutPlacementMode.Bottom,
            };
            _popupFlyout.Closed += OnPopupFlyoutClosed;
        }

        var panel = new StackPanel();

        // Move items from the internal inline collection to the popup list
        var expandedItems = new List<UIElement>();
        foreach (var item in Items)
        {
            expandedItems.Add(item);
        }

        // Temporarily clear inline items so they can join the visual tree of the popup
        _galleryPanel!.ItemsSource = null;

        var expandedRepeater = new ItemsRepeater
        {
            Layout = new UniformGridLayout
            {
                MinItemWidth = ItemWidth,
                MinItemHeight = ItemHeight,
                MaximumRowsOrColumns = MaxDropDownItemsInRow,
                Orientation = Orientation.Horizontal,
                MinRowSpacing = 0,
                MinColumnSpacing = 0,
            },
            ItemsSource = expandedItems,
        };

        var galleryScroller = new ScrollViewer
        {
            Content = expandedRepeater,
            MaxHeight = 300,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        panel.Children.Add(galleryScroller);

        // Menu items
        if (MenuItems.Count > 0)
        {
            panel.Children.Add(new Rectangle
            {
                Height = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 4),
            });

            foreach (var menuItem in MenuItems)
            {
                if (menuItem is FrameworkElement mfe && mfe.Parent is Panel parent)
                {
                    parent.Children.Remove(menuItem);
                }

                panel.Children.Add(menuItem);
            }
        }

        _popupFlyout.Content = panel;
        IsDropDownOpen = true;
        _popupFlyout.ShowAt(this);
    }

    private void OnPopupFlyoutClosed(object? sender, object e)
    {
        IsDropDownOpen = false;
        
        // Restore items back to the inline gallery repeater
        SetupGalleryPanel();
    }

    private void UpdateVisualState()
    {
        VisualStateManager.GoToState(this, IsCollapsed ? "CollapsedToButton" : "Inline", true);
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InRibbonGallery gallery)
        {
            if (e.NewValue is not null)
            {
                gallery.SelectedIndex = gallery.Items.IndexOf((e.NewValue as UIElement)!);
            }
        }
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InRibbonGallery gallery)
        {
            gallery.UpdateVisualState();
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject? parent)
    {
        if (parent is null) return null;
        if (parent is ScrollViewer sv) return sv;

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindScrollViewer(child);
            if (result is not null) return result;
        }

        return null;
    }

    #endregion
}

namespace Fluent;

/// <summary>
/// Represents a single tab within a Ribbon control.
/// </summary>
[ContentProperty(Name = nameof(Groups))]
public partial class RibbonTab : TabViewItem, IHeaderedControl
{
    private readonly StackPanel _groupsPanel;
    private readonly ScrollViewer _scrollViewer;
    private bool _isUpdatingLayout;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Groups"/> dependency property.</summary>
    public static readonly DependencyProperty GroupsProperty =
        DependencyProperty.Register(
            nameof(Groups),
            typeof(ObservableCollection<RibbonGroupBox>),
            typeof(RibbonTab),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of groups in this tab.
    /// </summary>
    public ObservableCollection<RibbonGroupBox> Groups
    {
        get => (ObservableCollection<RibbonGroupBox>)GetValue(GroupsProperty);
        private set => SetValue(GroupsProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonTab),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="ContextualTabGroupName"/> dependency property.</summary>
    public static readonly DependencyProperty ContextualTabGroupNameProperty =
        DependencyProperty.Register(
            nameof(ContextualTabGroupName),
            typeof(string),
            typeof(RibbonTab),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the name of the contextual tab group this tab belongs to.
    /// </summary>
    public string ContextualTabGroupName
    {
        get => (string)GetValue(ContextualTabGroupNameProperty);
        set => SetValue(ContextualTabGroupNameProperty, value);
    }

    /// <summary>Identifies the <see cref="IsContextual"/> dependency property.</summary>
    public static readonly DependencyProperty IsContextualProperty =
        DependencyProperty.Register(
            nameof(IsContextual),
            typeof(bool),
            typeof(RibbonTab),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether this is a contextual tab.
    /// </summary>
    public bool IsContextual
    {
        get => (bool)GetValue(IsContextualProperty);
        set => SetValue(IsContextualProperty, value);
    }

    /// <summary>Identifies the <see cref="Group"/> dependency property.</summary>
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(
            nameof(Group),
            typeof(RibbonContextualTabGroup),
            typeof(RibbonTab),
            new PropertyMetadata(null, OnGroupChanged));

    /// <summary>
    /// Gets or sets the contextual tab group this tab belongs to.
    /// </summary>
    public RibbonContextualTabGroup? Group
    {
        get => (RibbonContextualTabGroup?)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    /// <summary>Identifies the <see cref="HasSeparator"/> dependency property.</summary>
    public static readonly DependencyProperty HasSeparatorProperty =
        DependencyProperty.Register(
            nameof(HasSeparator),
            typeof(bool),
            typeof(RibbonTab),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether this tab has a separator before it.
    /// Used to visually separate contextual tab groups.
    /// </summary>
    public bool HasSeparator
    {
        get => (bool)GetValue(HasSeparatorProperty);
        set => SetValue(HasSeparatorProperty, value);
    }

    /// <summary>Identifies the <see cref="ReduceOrder"/> dependency property.</summary>
    public static readonly DependencyProperty ReduceOrderProperty =
        DependencyProperty.Register(
            nameof(ReduceOrder),
            typeof(string),
            typeof(RibbonTab),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the reduce order for group sizing.
    /// Comma-separated list of group names defining the order in which groups are reduced.
    /// </summary>
    public string ReduceOrder
    {
        get => (string)GetValue(ReduceOrderProperty);
        set => SetValue(ReduceOrderProperty, value);
    }

    /// <summary>Identifies the <see cref="ActiveTabBackground"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveTabBackgroundProperty =
        DependencyProperty.Register(
            nameof(ActiveTabBackground),
            typeof(Brush),
            typeof(RibbonTab),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the background color of the active tab header, typically set by a contextual group.
    /// </summary>
    public Brush? ActiveTabBackground
    {
        get => (Brush?)GetValue(ActiveTabBackgroundProperty);
        set => SetValue(ActiveTabBackgroundProperty, value);
    }

    /// <summary>Identifies the <see cref="ActiveTabBorderBrush"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveTabBorderBrushProperty =
        DependencyProperty.Register(
            nameof(ActiveTabBorderBrush),
            typeof(Brush),
            typeof(RibbonTab),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the border brush for the active tab, typically set by a contextual group.
    /// </summary>
    public Brush? ActiveTabBorderBrush
    {
        get => (Brush?)GetValue(ActiveTabBorderBrushProperty);
        set => SetValue(ActiveTabBorderBrushProperty, value);
    }

    #endregion

    #region Tab Header Layout Properties

    /// <summary>Identifies the <see cref="HeaderPadding"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderPaddingProperty =
        DependencyProperty.Register(
            nameof(HeaderPadding),
            typeof(Thickness),
            typeof(RibbonTab),
            new PropertyMetadata(new Thickness(9, 0, 9, 0)));

    /// <summary>
    /// Gets or sets the padding around the tab header text.
    /// Adjusted dynamically by <see cref="RibbonTabsContainer"/> to fit tabs.
    /// </summary>
    public Thickness HeaderPadding
    {
        get => (Thickness)GetValue(HeaderPaddingProperty);
        set => SetValue(HeaderPaddingProperty, value);
    }

    /// <summary>Identifies the <see cref="SeparatorOpacity"/> dependency property.</summary>
    public static readonly DependencyProperty SeparatorOpacityProperty =
        DependencyProperty.Register(
            nameof(SeparatorOpacity),
            typeof(double),
            typeof(RibbonTab),
            new PropertyMetadata(0.0));

    /// <summary>
    /// Gets or sets the opacity of the separator between tabs.
    /// Used when tabs are compressed to help differentiate them.
    /// </summary>
    public double SeparatorOpacity
    {
        get => (double)GetValue(SeparatorOpacityProperty);
        set => SetValue(SeparatorOpacityProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTab"/> class.
    /// </summary>
    public RibbonTab()
    {
        // Use TabViewItem's default style — no custom template needed
        // TabView shows TabViewItem.Content in the content area
        IsClosable = false;

        Groups = new ObservableCollection<RibbonGroupBox>();
        Groups.CollectionChanged += OnGroupsCollectionChanged;

        // Build the content programmatically
        _groupsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            MinHeight = 94,
        };

        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _groupsPanel,
        };

        _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;

        Content = _scrollViewer;
    }

    #endregion

    #region Dynamic Sizing

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGroupSizes();
    }

    /// <summary>
    /// Measures available width and progressively reduces group sizes
    /// (Large → Medium → Small → Collapsed) to fit within the available space.
    /// If a ReduceOrder is specified, groups are reduced in that order;
    /// otherwise, groups are reduced from right to left (last group reduced first).
    /// </summary>
    private void UpdateGroupSizes()
    {
        if (_isUpdatingLayout || Groups.Count == 0) return;
        _isUpdatingLayout = true;

        try
        {
            var availableWidth = _scrollViewer.ActualWidth;
            if (availableWidth <= 0) return;

            // Reset all groups to Large state first
            foreach (var group in Groups)
            {
                group.State = RibbonGroupBoxState.Large;
            }

            // Build the reduce order
            var reduceOrderList = BuildReduceOrder();

            // Progressively reduce groups using the reduce order
            foreach (var (groupIndex, targetState) in reduceOrderList)
            {
                _groupsPanel.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                var desiredWidth = _groupsPanel.DesiredSize.Width;

                if (desiredWidth <= availableWidth) break;

                var group = Groups[groupIndex];
                if (group.State < targetState)
                {
                    group.State = targetState;
                }
            }
        }
        finally
        {
            _isUpdatingLayout = false;
        }
    }

    /// <summary>
    /// Builds the reduce order list — either from the ReduceOrder string or by default right-to-left.
    /// Returns tuples of (groupIndex, targetState).
    /// </summary>
    private List<(int GroupIndex, RibbonGroupBoxState TargetState)> BuildReduceOrder()
    {
        var result = new List<(int, RibbonGroupBoxState)>();

        if (!string.IsNullOrEmpty(ReduceOrder))
        {
            // Parse the ReduceOrder string (comma-separated group headers)
            // Each name maps to the next reduction for that group
            var names = ReduceOrder.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var groupStateTracker = new Dictionary<int, RibbonGroupBoxState>();

            foreach (var name in names)
            {
                // Find the group by header name
                var groupIndex = -1;
                for (int i = 0; i < Groups.Count; i++)
                {
                    if (Groups[i].Header?.ToString() == name)
                    {
                        groupIndex = i;
                        break;
                    }
                }

                if (groupIndex == -1) continue;

                if (!groupStateTracker.ContainsKey(groupIndex))
                {
                    groupStateTracker[groupIndex] = RibbonGroupBoxState.Medium;
                }
                else
                {
                    groupStateTracker[groupIndex] = groupStateTracker[groupIndex] switch
                    {
                        RibbonGroupBoxState.Medium => RibbonGroupBoxState.Small,
                        RibbonGroupBoxState.Small => RibbonGroupBoxState.Collapsed,
                        _ => RibbonGroupBoxState.Collapsed,
                    };
                }

                result.Add((groupIndex, groupStateTracker[groupIndex]));
            }
        }
        else
        {
            // Default: reduce right to left
            var states = new[] { RibbonGroupBoxState.Medium, RibbonGroupBoxState.Small, RibbonGroupBoxState.Collapsed };
            foreach (var targetState in states)
            {
                for (int i = Groups.Count - 1; i >= 0; i--)
                {
                    result.Add((i, targetState));
                }
            }
        }

        return result;
    }

    private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonTab tab)
        {
            if (e.OldValue is RibbonContextualTabGroup oldGroup)
            {
                oldGroup.RemoveTabItem(tab);
            }

            if (e.NewValue is RibbonContextualTabGroup newGroup)
            {
                tab.IsContextual = true;
                tab.ActiveTabBackground = newGroup.Background;

                if (!newGroup.Items.Contains(tab))
                {
                    newGroup.AppendTabItem(tab);
                }
            }
            else
            {
                tab.IsContextual = false;
                tab.ActiveTabBackground = null;
            }
        }
    }

    #endregion

    #region Methods

    private void OnGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncGroups();
    }

    private void SyncGroups()
    {
        _groupsPanel.Children.Clear();
        foreach (var group in Groups)
        {
            _groupsPanel.Children.Add(group);
        }
    }

    #endregion
}

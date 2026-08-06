namespace Fluent;

/// <summary>
/// Represents a single tab item within a Ribbon control.
/// </summary>
[ContentProperty(Name = nameof(Groups))]
public partial class RibbonTabItem : TabViewItem, IHeaderedControl, IKeyTipedControl, ILogicalChildSupport, ISimplifiedStateControl
{
    private readonly StackPanel _groupsPanel;
    private readonly ScrollViewer _scrollViewer;
    private bool _isUpdatingLayout;
    private bool _rerunGroupSizing;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _groupSizingDebounceTimer;
    private const double GroupSizingDebounceMilliseconds = 64;
    private double _lastGroupSizingWidth = double.NaN;
    private bool _lastGroupSizingSimplified;
    private RibbonGroupBoxState[]? _lastGroupSizingStates;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Groups"/> dependency property.</summary>
    public static readonly DependencyProperty GroupsProperty =
        DependencyProperty.Register(
            nameof(Groups),
            typeof(ObservableCollection<RibbonGroupBox>),
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
            typeof(RibbonTabItem),
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
    /// Initializes a new instance of the <see cref="RibbonTabItem"/> class.
    /// </summary>
    public RibbonTabItem()
    {
        // Use TabViewItem's default style — no custom template needed
        // TabView shows TabViewItem.Content in the content area
        IsClosable = false;

        // Stretch the hosted ScrollViewer to fill the tab's content area so the
        // available width used for group reduction reflects the real space (some
        // content-presenter/platform combinations otherwise shrink-wrap the content).
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;

        Groups = new ObservableCollection<RibbonGroupBox>();
        Groups.CollectionChanged += OnGroupsCollectionChanged;

        // Build the content programmatically
        _groupsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Height = RibbonTabControl.DefaultContentHeight,
        };

        _scrollViewer = new ScrollViewer
        {
            Height = RibbonTabControl.DefaultContentHeight,
            HorizontalScrollMode = ScrollMode.Enabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = _groupsPanel,
        };

#if !WINDOWS
        // TODO: Remove this workaround when https://github.com/unoplatform/uno/issues/19504 is fixed.
        RibbonGroupsContainerScrollViewer.SetEnableHorizontalWheelScrolling(_scrollViewer, true);
#endif

        _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;

        Content = _scrollViewer;
        InitializeCompatibility();

        // Ensure an initial layout pass runs once the tab is realized, even if
        // no further size change fires afterwards.
        Loaded += (_, _) => ScheduleUpdateGroupSizes();
        Unloaded += (_, _) => CancelGroupSizingPass();
    }

    internal void SetContentHeight(double contentHeight)
    {
        var height = double.IsFinite(contentHeight)
            ? Math.Max(0, contentHeight)
            : RibbonTabControl.DefaultContentHeight;
        _groupsPanel.Height = height;
        _scrollViewer.Height = height;
        InvalidateGroupSizing();
    }

    #endregion

    #region Dynamic Sizing

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ScheduleUpdateGroupSizes();
    }

    /// <summary>
    /// Debounces group-size recalculations so a burst of size changes — an active
    /// window drag-resize raises <see cref="FrameworkElement.SizeChanged"/> on every
    /// frame — collapses into a single sizing pass once the width settles. This keeps
    /// the number of visual-state rewrites low (native WinUI can raise a fatal stowed
    /// exception when a realized TabView rewrites child visual states repeatedly during
    /// a resize) while still recomputing after every resize, so groups both reduce under
    /// space pressure and re-enlarge symmetrically when space becomes available again.
    /// </summary>
    private void ScheduleUpdateGroupSizes()
    {
        var dispatcher = DispatcherQueue;
        if (dispatcher is null)
        {
            // No dispatcher (e.g. design-time / not yet attached): run inline.
            UpdateGroupSizes();
            return;
        }

        // The first pass for this tab must not wait out the debounce interval: the tab has
        // just been realized and is about to paint at its unreduced size, so a 64 ms delay
        // shows several frames of wrongly-sized groups — the flash seen when switching tabs.
        // Enqueueing instead runs the pass on the next dispatcher tick, which is still
        // outside the layout cycle (reduction may reparent realized elements, which is fatal
        // inside a live measure pass) but lands before the frame is painted.
        if (double.IsNaN(_lastGroupSizingWidth))
        {
            if (dispatcher.TryEnqueue(UpdateGroupSizes))
            {
                return;
            }
        }

        var timer = _groupSizingDebounceTimer;
        if (timer is null)
        {
            timer = dispatcher.CreateTimer();
            timer.IsRepeating = false;
            timer.Interval = TimeSpan.FromMilliseconds(GroupSizingDebounceMilliseconds);
            timer.Tick += OnGroupSizingDebounceTick;
            _groupSizingDebounceTimer = timer;
        }

        // Restart on every request so only the final, settled width drives a pass.
        timer.Stop();
        timer.Start();
    }

    private void OnGroupSizingDebounceTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        UpdateGroupSizes();
    }

    /// <summary>
    /// Measures available width and progressively reduces group sizes
    /// (Large → Medium → Small → Collapsed) to fit within the available space.
    /// Reduction is opt-in: groups are only reduced when a <see cref="RibbonTabItem.ReduceOrder"/>
    /// is specified, and then strictly in that order. Without a ReduceOrder no group is
    /// reduced and any overflow is handled by the horizontal ScrollViewer, matching the
    /// WPF <c>RibbonGroupsContainer</c>.
    /// </summary>
    private void UpdateGroupSizes()
    {
        if (_isUpdatingLayout)
        {
            _rerunGroupSizing = true;
            return;
        }

        if (Groups.Count == 0)
        {
            return;
        }

        // Read the width at execution time (after layout has settled) so we never
        // act on a transient/too-small width captured when the event fired.
        var availableWidth = _scrollViewer.ActualWidth;
        var rootWidth = XamlRoot?.Size.Width ?? double.PositiveInfinity;
        if (double.IsFinite(rootWidth) && rootWidth > 0)
        {
            availableWidth = Math.Min(availableWidth, rootWidth);
        }

        if (availableWidth <= 0)
        {
            return;
        }

        if (IsCurrentGroupSizingValid(availableWidth))
        {
            return;
        }

        _isUpdatingLayout = true;
        _rerunGroupSizing = false;

        var reduceOrder = BuildReduceOrder();

        // Reset all groups to the largest state supported by the active mode and
        // restore any scalable content (such as InRibbonGallery items-per-row) to its
        // maximum. Resetting to the maximum every pass is what makes the pipeline
        // two-way: when the window grows, fewer steps are applied and scalable content
        // re-enlarges symmetrically.
        foreach (var group in Groups)
        {
            group.State = group.GetInitialStateForMode(IsSimplified);
        }

        ResetScalableContent(reduceOrder);

        ReduceToFit(availableWidth, reduceOrder);

        CompleteGroupSizingPass(cacheResult: IsLoaded, availableWidth);
    }

    /// <summary>
    /// Applies the reduce order until the groups fit, re-measuring synchronously after
    /// every step. This mirrors the WPF <c>RibbonGroupsContainer</c>, which resolves the
    /// whole reduction inside a single measure pass. Yielding to the compositor between
    /// steps instead would paint every intermediate size — the ribbon would visibly
    /// expand and then collapse step-by-step on each pass.
    /// </summary>
    private void ReduceToFit(double availableWidth, IReadOnlyList<GroupSizingStep> reduceOrder)
    {
        // Groups are hosted in a horizontally scrolling viewer, so their natural width is
        // measured against an unconstrained width.
        var constraint = new Windows.Foundation.Size(
            double.PositiveInfinity,
            double.PositiveInfinity);

        MeasureGroupsPanel(constraint);

        var reduceOrderIndex = 0;
        while (reduceOrderIndex < reduceOrder.Count
               && Internal.DoubleUtil.GreaterThan(
                   _groupsPanel.DesiredSize.Width,
                   availableWidth))
        {
            var step = reduceOrder[reduceOrderIndex++];
            var group = Groups[step.GroupIndex];

            if (step.IsScale)
            {
                ReduceScalableContent(group);
            }
            else if (group.State >= step.TargetState)
            {
                continue;
            }
            else
            {
                group.State = step.TargetState;
            }

            MeasureGroupsPanel(constraint);
        }
    }

    private void MeasureGroupsPanel(Windows.Foundation.Size constraint)
    {
        _groupsPanel.InvalidateMeasure();
        _groupsPanel.Measure(constraint);
    }

    private void CompleteGroupSizingPass(bool cacheResult, double availableWidth)
    {
        if (cacheResult)
        {
            _lastGroupSizingWidth = availableWidth;
            _lastGroupSizingSimplified = IsSimplified;
            _lastGroupSizingStates = Groups.Select(group => group.State).ToArray();
        }
        else
        {
            InvalidateGroupSizing();
        }

        _isUpdatingLayout = false;
        if (_rerunGroupSizing)
        {
            _rerunGroupSizing = false;
            ScheduleUpdateGroupSizes();
        }
    }

    private void CancelGroupSizingPass()
    {
        _groupSizingDebounceTimer?.Stop();

        if (_isUpdatingLayout)
        {
            _isUpdatingLayout = false;
            InvalidateGroupSizing();
        }
    }

    /// <summary>
    /// Builds the reduce order list from the <see cref="RibbonTabItem.ReduceOrder"/> string.
    /// A bare entry (<c>Name</c>) reduces the group's <see cref="RibbonGroupBoxState"/> by one
    /// step; a parenthesised entry (<c>(Name)</c>) reduces the group's scalable content by one
    /// step (<see cref="IScalableRibbonControl.Reduce"/>), matching WPF's <c>RibbonGroupsContainer</c>.
    /// </summary>
    /// <remarks>
    /// Matching the WPF <c>RibbonGroupsContainer</c> (which returns early when
    /// <c>reduceOrder.Length == 0</c>), group reduction is strictly opt-in: when no
    /// <see cref="RibbonTabItem.ReduceOrder"/> is specified the ribbon never reduces
    /// group states. Content that exceeds the available width overflows into the
    /// horizontal <see cref="ScrollViewer"/> instead of shrinking groups. Automatically
    /// inventing a reduce order would drive space-sensitive controls such as an
    /// <see cref="InRibbonGallery"/> into their collapsed button state at the default
    /// window size, which is not how the WPF original behaves.
    /// </remarks>
    private List<GroupSizingStep> BuildReduceOrder()
    {
        var result = new List<GroupSizingStep>();

        if (string.IsNullOrEmpty(ReduceOrder))
        {
            return result;
        }

        var names = ReduceOrder.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var groupStateTracker = new Dictionary<int, RibbonGroupBoxState>();

        foreach (var name in names)
        {
            // A parenthesised entry such as "(FirstGalleryGroup)" reduces the scalable
            // content of the group; a bare entry reduces the group's own state.
            var isScale = name.Length >= 2 && name[0] == '(' && name[^1] == ')';
            var lookup = isScale ? name[1..^1].Trim() : name;

            var groupIndex = FindGroupIndex(lookup);
            if (groupIndex == -1)
            {
                continue;
            }

            if (isScale)
            {
                result.Add(GroupSizingStep.Scale(groupIndex));
                continue;
            }

            var group = Groups[groupIndex];
            var currentState = groupStateTracker.TryGetValue(groupIndex, out var trackedState)
                ? trackedState
                : group.GetInitialStateForMode(IsSimplified);
            var targetState = group
                .GetStateDefinitionForMode(IsSimplified)
                .ReduceState(currentState);
            groupStateTracker[groupIndex] = targetState;

            if (targetState != currentState)
            {
                result.Add(GroupSizingStep.State(groupIndex, targetState));
            }
        }

        return result;
    }

    // Matches by x:Name first (unambiguous), then by header text, so developers can
    // disambiguate groups that share a header or use a non-string header.
    private int FindGroupIndex(string name)
    {
        for (var i = 0; i < Groups.Count; i++)
        {
            var candidateGroup = Groups[i];
            if (string.Equals(candidateGroup.Name, name, StringComparison.Ordinal)
                || string.Equals(candidateGroup.Header?.ToString(), name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    // Restores the scalable content of every group referenced by a scale step to its
    // maximum. A scale step can only reduce content that a prior scale step enlarged, so
    // enlarging once per scale step exactly undoes the previous pass; enlarge calls made
    // past the maximum are harmless no-ops.
    private void ResetScalableContent(IReadOnlyList<GroupSizingStep> reduceOrder)
    {
        Dictionary<int, int>? scaleCounts = null;

        foreach (var step in reduceOrder)
        {
            if (!step.IsScale)
            {
                continue;
            }

            scaleCounts ??= new Dictionary<int, int>();
            scaleCounts[step.GroupIndex] =
                scaleCounts.TryGetValue(step.GroupIndex, out var existing) ? existing + 1 : 1;
        }

        if (scaleCounts is null)
        {
            return;
        }

        foreach (var (groupIndex, count) in scaleCounts)
        {
            foreach (var scalable in EnumerateScalableControls(Groups[groupIndex]))
            {
                for (var i = 0; i < count; i++)
                {
                    scalable.Enlarge();
                }
            }
        }
    }

    private static void ReduceScalableContent(RibbonGroupBox group)
    {
        foreach (var scalable in EnumerateScalableControls(group))
        {
            scalable.Reduce();
        }
    }

    private static IEnumerable<IScalableRibbonControl> EnumerateScalableControls(RibbonGroupBox group)
    {
        foreach (var item in group.Items)
        {
            if (item is IScalableRibbonControl scalable)
            {
                yield return scalable;
            }
        }
    }

    /// <summary>
    /// A single reduction step: either a group-state reduction or a one-step reduction
    /// of a group's scalable content (a parenthesised entry in the reduce order).
    /// </summary>
    private readonly struct GroupSizingStep
    {
        private GroupSizingStep(int groupIndex, bool isScale, RibbonGroupBoxState targetState)
        {
            GroupIndex = groupIndex;
            IsScale = isScale;
            TargetState = targetState;
        }

        public int GroupIndex { get; }

        public bool IsScale { get; }

        public RibbonGroupBoxState TargetState { get; }

        public static GroupSizingStep State(int groupIndex, RibbonGroupBoxState targetState)
            => new(groupIndex, isScale: false, targetState);

        public static GroupSizingStep Scale(int groupIndex)
            => new(groupIndex, isScale: true, RibbonGroupBoxState.Large);
    }

    private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonTabItem tab)
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
        CancelGroupSizingPass();
        InvalidateGroupSizing();
        SyncGroups();
    }

    private void SyncGroups()
    {
        foreach (var existingGroup in _groupsPanel.Children.OfType<RibbonGroupBox>())
        {
            if (ReferenceEquals(existingGroup.AutomationOwnerTab, this))
            {
                existingGroup.AutomationOwnerTab = null;
            }
        }

        _groupsPanel.Children.Clear();
        foreach (var group in Groups)
        {
            group.AutomationOwnerTab = this;
            _groupsPanel.Children.Add(group);
        }

        // Groups changed — re-evaluate sizing on the next layout pass.
        ScheduleUpdateGroupSizes();
    }

    private bool IsCurrentGroupSizingValid(double availableWidth)
    {
        var states = _lastGroupSizingStates;
        if (states is null
            || !Internal.DoubleUtil.AreClose(_lastGroupSizingWidth, availableWidth)
            || _lastGroupSizingSimplified != IsSimplified
            || states.Length != Groups.Count)
        {
            return false;
        }

        for (var i = 0; i < Groups.Count; i++)
        {
            if (states[i] != Groups[i].State)
            {
                return false;
            }
        }

        return true;
    }

    private void InvalidateGroupSizing()
    {
        _lastGroupSizingStates = null;
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonTabItemAutomationPeer(this);
}

/// <summary>
/// Unpublished convenience name retained for existing Uno samples.
/// </summary>
public partial class RibbonTab : RibbonTabItem
{
}

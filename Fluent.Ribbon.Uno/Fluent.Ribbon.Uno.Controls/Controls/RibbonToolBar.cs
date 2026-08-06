namespace Fluent;

/// <summary>
/// A toolbar control that arranges its children according to layout definitions.
/// Different layouts can be specified for different ribbon sizes, and the toolbar
/// will automatically switch between them.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonToolBar : RibbonControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<FrameworkElement>),
            typeof(RibbonToolBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of toolbar items.
    /// </summary>
    public ObservableCollection<FrameworkElement> Items
    {
        get => (ObservableCollection<FrameworkElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="LayoutDefinitions"/> dependency property.</summary>
    public static readonly DependencyProperty LayoutDefinitionsProperty =
        DependencyProperty.Register(
            nameof(LayoutDefinitions),
            typeof(ObservableCollection<RibbonToolBarLayoutDefinition>),
            typeof(RibbonToolBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of layout definitions for this toolbar.
    /// </summary>
    public ObservableCollection<RibbonToolBarLayoutDefinition> LayoutDefinitions
    {
        get => (ObservableCollection<RibbonToolBarLayoutDefinition>)GetValue(LayoutDefinitionsProperty);
        private set => SetValue(LayoutDefinitionsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonToolBar),
            new PropertyMetadata(false, OnLayoutStateChanged));

    /// <summary>
    /// Gets or sets whether the toolbar is in simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    #endregion

    #region Fields

    private RibbonToolBarPanel? _layoutPanel;
    private bool _templateApplied;
    private string? localizedAutomationName;

    internal FrameworkElement? AutomationContentRoot => _layoutPanel;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToolBar"/> class.
    /// </summary>
    public RibbonToolBar()
    {
        DefaultStyleKey = typeof(RibbonToolBar);
        Items = new ObservableCollection<FrameworkElement>();
        LayoutDefinitions = new ObservableCollection<RibbonToolBarLayoutDefinition>();

        Items.CollectionChanged += (_, _) => InvalidateLayout();
        LayoutDefinitions.CollectionChanged += (_, _) => InvalidateLayout();
        InitializeCompatibility();
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedAutomationName);
        RegisterPropertyChangedCallback(
            HeaderProperty,
            static (sender, _) => ((RibbonToolBar)sender).RefreshLocalizedAutomationName());
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _templateApplied = true;
        _layoutPanel = GetTemplateChild("PART_ContentPanel") as RibbonToolBarPanel;
        RebuildLayout();
    }

    #endregion

    #region Layout

    private static void OnLayoutStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonToolBar toolbar)
        {
            var isSimplified = (bool)e.NewValue;
            foreach (var child in toolbar.Children)
            {
                UpdateChildSimplifiedState(child, isSimplified);
            }

            toolbar.RebuildLayout();
        }
    }

    private void InvalidateLayout()
    {
        RebuildLayout();
    }

    private void RefreshLocalizedAutomationName()
    {
        var name = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(this);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(Header);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = RibbonLocalization.Current.Localization.RibbonToolBarName;
        }

        Fluent.Automation.Peers.AutomationPeerHelpers.UpdatePeerName(
            this,
            ref localizedAutomationName,
            name);
    }

    private void RebuildLayout()
    {
        // Never build layout before the template is applied. PART_ContentPanel only
        // exists after OnApplyTemplate, and reparenting items into it during XAML
        // parsing is rejected by the strict WinUI3 parser.
        if (!_templateApplied || _layoutPanel is null)
        {
            return;
        }

        // Detach every shared control from its current host WHILE the old layout is still rooted,
        // before clearing the panel below. On the native WinUI head, detaching a realized element
        // from a panel that has already been removed from the visual tree corrupts the element's
        // native peer, so re-adding it to a new host then throws COMException 0x800F1000. Ordering
        // matters: detach first, then unroot the old groups. See RibbonGallery.SyncItems for the
        // same rule.
        foreach (var item in Items)
        {
            DetachFromParent(item);
        }

        _layoutPanel.Children.Clear();

        var definition = GetCurrentLayoutDefinition();

        if (definition is null)
        {
            BuildWrapLayout();
            return;
        }

        BuildDefinedLayout(definition);
    }

    /// <summary>
    /// Gets the layout definition matching the current state.
    /// </summary>
    private RibbonToolBarLayoutDefinition? GetCurrentLayoutDefinition()
    {
        if (LayoutDefinitions.Count == 0)
        {
            return null;
        }

        var matchingMode = LayoutDefinitions
            .Where(definition => definition.ForSimplified == IsSimplified)
            .ToList();
        if (matchingMode.Count == 0)
        {
            matchingMode = LayoutDefinitions.ToList();
        }

        var currentSize = RibbonProperties.GetSize(this);
        var exact = matchingMode.FirstOrDefault(definition => definition.Size == currentSize);
        if (exact is not null)
        {
            return exact;
        }

        var preference = currentSize switch
        {
            RibbonControlSize.Large =>
                new[] { RibbonControlSize.Middle, RibbonControlSize.Small },
            RibbonControlSize.Middle =>
                new[] { RibbonControlSize.Small, RibbonControlSize.Large },
            _ =>
                new[] { RibbonControlSize.Middle, RibbonControlSize.Large },
        };

        foreach (var size in preference)
        {
            var fallback = matchingMode.FirstOrDefault(definition => definition.Size == size);
            if (fallback is not null)
            {
                return fallback;
            }
        }

        return matchingMode[0];
    }

    private void BuildWrapLayout()
    {
        var toolBarSize = RibbonProperties.GetSize(this);

        foreach (var item in Items)
        {
            DetachFromParent(item);
#if WINDOWS
            RibbonProperties.SetAppropriateSize(item, toolBarSize);
#else
            var regularSizeDefinition = RibbonProperties.GetSizeDefinition(item);
            var simplifiedSizeDefinition = item is ISimplifiedRibbonControl simplifiedControl
                ? simplifiedControl.SimplifiedSizeDefinition
                : regularSizeDefinition;
            var itemSize = ResolveWrapItemSize(
                toolBarSize,
                regularSizeDefinition,
                simplifiedSizeDefinition,
                IsSimplified);
            if (item is IScalableRibbonControl scalable)
            {
                scalable.ScaleTo(itemSize);
            }
            else
            {
                RibbonProperties.SetSize(item, itemSize);
            }
#endif

            _layoutPanel!.Children.Add(item);
        }

#if WINDOWS
        _layoutPanel!.ConfigureWrapLayout();
#else
        _layoutPanel!.ConfigureWrapLayout(IsSimplified);
#endif
    }

    internal static RibbonControlSize ResolveWrapItemSize(
        RibbonControlSize toolBarSize,
        RibbonControlSizeDefinition regularSizeDefinition,
        RibbonControlSizeDefinition simplifiedSizeDefinition,
        bool isSimplified)
        => (isSimplified ? simplifiedSizeDefinition : regularSizeDefinition)
            .GetSize(toolBarSize);

    private void BuildDefinedLayout(RibbonToolBarLayoutDefinition definition)
    {
        var rows = new List<IReadOnlyList<FrameworkElement>>(definition.Rows.Count);
        var separators = new Dictionary<int, FrameworkElement>();

        // A layout definition can declare more rows than RowCount. Those extra rows wrap
        // into additional side-by-side columns, each separated by a vertical separator.
        var rowCountInColumn = Math.Max(1, Math.Min(definition.RowCount, definition.Rows.Count));

        for (var rowIndex = 0; rowIndex < definition.Rows.Count; rowIndex++)
        {
            var row = definition.Rows[rowIndex];
            var rowElements = new List<FrameworkElement>();

            if (rowIndex != 0 && rowIndex % rowCountInColumn == 0)
            {
                var separator = new RibbonSeparator { Orientation = Orientation.Vertical };
                if (SeparatorStyle is not null)
                {
                    separator.Style = SeparatorStyle;
                }

                separators[rowIndex] = separator;
                _layoutPanel!.Children.Add(separator);
            }

            for (var childIndex = 0; childIndex < row.Children.Count; childIndex++)
            {
                var child = row.Children[childIndex];

                if (child is RibbonToolBarControlDefinition controlDefinition)
                {
                    var control = FindItemByName(controlDefinition.Target);
                    if (control is null)
                    {
                        continue;
                    }

                    DetachFromParent(control);

                    if (control is IScalableRibbonControl scalable)
                    {
                        scalable.ScaleTo(controlDefinition.Size);
                    }

                    control.Width = controlDefinition.Width;

                    _layoutPanel!.Children.Add(control);
                    rowElements.Add(control);
                }
                else if (child is RibbonToolBarControlGroupDefinition groupDefinition)
                {
                    var group = new RibbonToolBarControlGroup
                    {
                        IsFirstInRow = childIndex == 0,
                        IsLastInRow = childIndex == row.Children.Count - 1,
                    };

                    foreach (var groupChild in groupDefinition.Children)
                    {
                        var control = FindItemByName(groupChild.Target);
                        if (control is null)
                        {
                            continue;
                        }

                        DetachFromParent(control);

                        if (control is IScalableRibbonControl scalable)
                        {
                            scalable.ScaleTo(groupChild.Size);
                        }

                        control.Width = groupChild.Width;
                        group.Items.Add(control);
                    }

                    _layoutPanel!.Children.Add(group);
                    rowElements.Add(group);
                }
            }

            rows.Add(rowElements);
        }

#if WINDOWS
        _layoutPanel!.ConfigureCustomLayout(rows, definition.RowCount, separators);
#else
        _layoutPanel!.ConfigureCustomLayout(
            rows,
            definition.RowCount,
            separators,
            centerRowItems: IsSimplified);
#endif
    }

    private FrameworkElement? FindItemByName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return Items.FirstOrDefault(item => item.Name == name);
    }

    private static void DetachFromParent(UIElement element)
    {
        // Resolve the host through the visual parent (the elements are hosted directly in a
        // Panel's Children, whose logical Parent reads null on the native WinUI head). Callers
        // must invoke this while that host is still rooted: detaching a realized element from a
        // panel already removed from the visual tree corrupts its native peer, after which
        // re-adding it throws COMException 0x800F1000. See QuickAccessToolBar.Compatibility.cs.
        if (VisualTreeHelper.GetParent(element) is Panel panel)
        {
            panel.Children.Remove(element);
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonToolBarAutomationPeer(this);
}

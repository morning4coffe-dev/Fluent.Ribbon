namespace Fluent;

/// <summary>
/// A container that groups toolbar items visually, tracking whether it's first/last in its row.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF uses ItemsControl; Uno uses Panel-based approach.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_ItemsHost, Type = typeof(Panel))]
public partial class RibbonToolBarControlGroup : ItemsControl
{
    private const string PART_ItemsHost = "PART_ItemsHost";

    private Panel? _itemsHost;

    private bool _syncScheduled;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonToolBarControlGroup),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of controls in this group.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsFirstInRow"/> dependency property.</summary>
    public static readonly DependencyProperty IsFirstInRowProperty =
        DependencyProperty.Register(
            nameof(IsFirstInRow),
            typeof(bool),
            typeof(RibbonToolBarControlGroup),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether this group is the first in its row.
    /// </summary>
    public bool IsFirstInRow
    {
        get => (bool)GetValue(IsFirstInRowProperty);
        set => SetValue(IsFirstInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="IsLastInRow"/> dependency property.</summary>
    public static readonly DependencyProperty IsLastInRowProperty =
        DependencyProperty.Register(
            nameof(IsLastInRow),
            typeof(bool),
            typeof(RibbonToolBarControlGroup),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether this group is the last in its row.
    /// </summary>
    public bool IsLastInRow
    {
        get => (bool)GetValue(IsLastInRowProperty);
        set => SetValue(IsLastInRowProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToolBarControlGroup"/> class.
    /// </summary>
    public RibbonToolBarControlGroup()
    {
        DefaultStyleKey = typeof(RibbonToolBarControlGroup);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsHost = GetTemplateChild(PART_ItemsHost) as Panel;
        ScheduleSync();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleSync();
    }

    private void ScheduleSync()
    {
        if (_itemsHost is null || _syncScheduled)
        {
            return;
        }

        // Populating the host mutates the visual tree. A control's template is realized inline
        // while an ancestor panel measures it, so mutating the host's children synchronously here
        // would reparent realized elements during a live measure pass - fatal on the native WinUI
        // head (COMException 0x800F1000). Defer the update so it runs after the current layout
        // pass; only fall back to a synchronous sync when no dispatcher is available (a non-UI
        // context, where no measure pass is in flight).
        _syncScheduled = true;

        if (DispatcherQueue?.TryEnqueue(SyncItems) != true)
        {
            SyncItems();
        }
    }

    private void SyncItems()
    {
        _syncScheduled = false;

        // Skip stale groups: a group removed from the layout panel during a rebuild has no parent,
        // and must not steal the shared controls back from the group that now owns them.
        if (_itemsHost is null || Parent is null)
        {
            return;
        }

        if (HostMatchesItems())
        {
            return;
        }

        _itemsHost.Children.Clear();

        foreach (var item in Items)
        {
            // Items are normally detached ahead of time while their previous host is still rooted
            // (see RibbonToolBar.RebuildLayout). This guarded detach only handles an in-place
            // re-sync of this group's own rooted host; it never runs against an unrooted panel,
            // which is what would corrupt the native peer and make Children.Add throw 0x800F1000.
            if (VisualTreeHelper.GetParent(item) is Panel currentHost
                && !ReferenceEquals(currentHost, _itemsHost))
            {
                currentHost.Children.Remove(item);
            }

            _itemsHost.Children.Add(item);
        }
    }

    private bool HostMatchesItems()
    {
        if (_itemsHost!.Children.Count != Items.Count)
        {
            return false;
        }

        for (var index = 0; index < Items.Count; index++)
        {
            if (!ReferenceEquals(_itemsHost.Children[index], Items[index]))
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}

namespace Fluent;

using Windows.System;

/// <summary>
/// Represents a menu item within a Ribbon dropdown or context menu.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_Icon, Type = typeof(IconPresenter))]
public partial class MenuItem : InteractiveMenuItemBase
{
    private const string PART_Icon = "PART_Icon";
    private readonly Fluent.Helpers.ItemsControlBinding itemsBinding;
    private readonly CommandAvailability commandAvailability;

    #region Events

    /// <summary>
    /// Occurs when the menu item is clicked.
    /// </summary>
#if __ANDROID__
    public new event RoutedEventHandler? Click;
#else
    public event RoutedEventHandler? Click;
#endif

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty = HeaderedItemsControl.HeaderProperty;

    /// <summary>
    /// Gets or sets the header text of the menu item.
    /// </summary>
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(MenuItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the description text displayed below the header.
    /// </summary>
#if __IOS__
    public new string Description
#else
    public string Description
#endif
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(MenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon for the menu item.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(MenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command to execute.
    /// </summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(MenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(MenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of sub-menu items.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get
        {
            itemsBinding?.RefreshUnnotifiedNativeItems();
            return (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        }
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="HasSubItems"/> dependency property.</summary>
    public static readonly DependencyProperty HasSubItemsProperty =
        DependencyProperty.Register(
            nameof(HasSubItems),
            typeof(bool),
            typeof(MenuItem),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether this menu item has sub-items.
    /// </summary>
    public bool HasSubItems
    {
        get => (bool)GetValue(HasSubItemsProperty);
        private set => SetValue(HasSubItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(MenuItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="InputGestureText"/> dependency property.</summary>
    public static readonly DependencyProperty InputGestureTextProperty =
        DependencyProperty.Register(
            nameof(InputGestureText),
            typeof(string),
            typeof(MenuItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the keyboard shortcut text displayed on the right side.
    /// </summary>
    public string InputGestureText
    {
        get => (string)GetValue(InputGestureTextProperty);
        set => SetValue(InputGestureTextProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(MenuItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    public MenuItem()
    {
        DefaultStyleKey = typeof(MenuItem);
        menuEnabledConstraint = new EnabledStateConstraint(this);
        commandAvailability = new CommandAvailability(
            this, CommandProperty, CommandParameterProperty,
            _ => UpdateMenuPresentation(), constrainOwner: false);
        Items = Fluent.Helpers.ItemsControlBinding.CreateItems(this);
        itemsBinding = new Fluent.Helpers.ItemsControlBinding(
            this, Items,
            IsItemItsOwnContainerOverride, GetContainerForItemOverride,
            PrepareContainerForItemOverride, ClearContainerForItemOverride,
            OnItemsChanged);
#if WINDOWS
        itemsBinding.UsesNativeGenerator = true;
#endif
        Unloaded += OnMenuUnloaded;
        InitializeMenuPresentation();
        InitializeSubmenuOptions();
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    protected override void OnInvoke() => OnClick();

    /// <inheritdoc/>
    protected override void OnKeyboardInvoke(VirtualKey key)
    {
        if (HasSubItems && IsSplit is false)
        {
            OpenSubmenuAndFocusFirstItem();
            return;
        }

        OnInvoke();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (!e.Handled && IsEnabled && HandleMenuNavigationKey(e.Key))
        {
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Executes the menu item's command and raises <see cref="Click"/>.
    /// Shared by pointer/keyboard input and the automation (Invoke) peer.
    /// </summary>
    internal void InvokeItem()
    {
        if (!CanInvoke)
        {
            return;
        }

        Internal.CommandHelper.Execute(Command, CommandParameter);
        Click?.Invoke(this, new RoutedEventArgs());
    }

    /// <summary>Gets whether this menu item and its command are enabled.</summary>
    protected virtual bool IsEnabledCore => commandAvailability.CanExecute && IsEnabled;

    internal bool CanInvoke
    {
        get
        {
            if (!IsEnabledCore)
            {
                return false;
            }
            return AreMenuAncestorsEnabled();
        }
    }

    internal void InvokeFromQuickAccess() => OnClick();

    // Automation must take the same path as pointer/keyboard input, otherwise invoking a menu
    // item with a screen reader skips check toggling, sub-menu opening and popup dismissal.
    internal void InvokeFromAutomation() => OnClick();

    internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
    {
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is RibbonMenuItemAutomationPeer peer)
        {
            peer.RaiseExpandCollapseStateChanged(oldValue, newValue);
        }
    }

    internal void RaiseCheckedAutomationEvent(bool? oldValue, bool? newValue)
    {
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is RibbonMenuItemAutomationPeer peer)
        {
            peer.RaiseCheckedStateChanged(oldValue, newValue);
        }
    }

    /// <summary>Handles changes to the submenu items or their presentation.</summary>
    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        HasSubItems = Items.Count > 0;

        foreach (var item in Items.OfType<IDropDownItemOwner>())
        {
            item.SetDropDownOwner(QuickAccessSubmenuOwner);
        }

        if (submenuItemsHost is not null
            && (IsDropDownOpen || !ReferenceEquals(QuickAccessSubmenuOwner, this)))
        {
            ValidateSubmenuGenerator();
        }

        if (!HasSubItems)
        {
            IsDropDownOpen = false;
#if WINDOWS
            NativeQuickAccessAnchor?.CloseDropDown();
#endif
        }

        UpdateMenuPresentation();
        UpdateSubmenuDimensions();
    }

#if WINDOWS
    private void OnMenuUnloaded(object sender, RoutedEventArgs e)
    {
        IsDropDownOpen = false;
        QueueNativeSubmenuOpen();
    }
#else
    private void OnMenuUnloaded(object sender, RoutedEventArgs e) => IsDropDownOpen = false;
#endif

    /// <summary>Returns the live submenu container for a source item.</summary>
    public new DependencyObject? ContainerFromItem(object item) => itemsBinding.ContainerFromItem(item);

    /// <summary>Returns the live submenu container at an item index.</summary>
    public new DependencyObject? ContainerFromIndex(int index) => itemsBinding.ContainerFromIndex(index);

    /// <summary>Returns the source item represented by a live submenu container.</summary>
    public new object? ItemFromContainer(DependencyObject container) => itemsBinding.ItemFromContainer(container);

    /// <summary>Returns the index of a live submenu container.</summary>
    public new int IndexFromContainer(DependencyObject container) => itemsBinding.IndexFromContainer(container);

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new RibbonMenuItemAutomationPeer(this);

    #endregion
}

/// <summary>Unpublished convenience name retained for existing Uno markup.</summary>
public partial class RibbonMenuItem : MenuItem
{
}

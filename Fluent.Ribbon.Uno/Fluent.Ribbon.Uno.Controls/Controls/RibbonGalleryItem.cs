namespace Fluent;

/// <summary>
/// Represents a single selectable item in a <see cref="RibbonGallery"/>.
/// </summary>
[ContentProperty(Name = nameof(Content))]
#if WINDOWS
// Native ListBox removal requires ISelectorItem on realized gallery containers.
public partial class RibbonGalleryItem : ListBoxItem, IKeyTipedControl
#else
public partial class RibbonGalleryItem : ContentControl, IKeyTipedControl
#endif
{
    internal object? GalleryOwner { get; set; }
    private readonly CommandAvailability commandAvailability;
    private Windows.System.VirtualKey? pendingActivationKey;
#if !WINDOWS
    private bool isCompactHeight;
#endif

    #region Dependency Properties

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
#if WINDOWS
    public new static readonly DependencyProperty IsSelectedProperty =
#else
    public static readonly DependencyProperty IsSelectedProperty =
#endif
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
#if WINDOWS
    public new bool IsSelected
#else
    public bool IsSelected
#endif
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsPressed"/> dependency property.</summary>
    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.Register(
            nameof(IsPressed),
            typeof(bool),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether this item is currently pressed.
    /// </summary>
    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        private set => SetValue(IsPressedProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="PreviewCommand"/> dependency property.</summary>
    public static readonly DependencyProperty PreviewCommandProperty =
        DependencyProperty.Register(
            nameof(PreviewCommand),
            typeof(ICommand),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command invoked to show a live preview when the pointer
    /// enters this item (e.g. previewing a style/color before it is applied).
    /// </summary>
    public ICommand? PreviewCommand
    {
        get => (ICommand?)GetValue(PreviewCommandProperty);
        set => SetValue(PreviewCommandProperty, value);
    }

    /// <summary>Identifies the <see cref="CancelPreviewCommand"/> dependency property.</summary>
    public static readonly DependencyProperty CancelPreviewCommandProperty =
        DependencyProperty.Register(
            nameof(CancelPreviewCommand),
            typeof(ICommand),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command invoked to cancel a live preview when the pointer
    /// leaves this item.
    /// </summary>
    public ICommand? CancelPreviewCommand
    {
        get => (ICommand?)GetValue(CancelPreviewCommandProperty);
        set => SetValue(CancelPreviewCommandProperty, value);
    }

    /// <summary>Identifies the <see cref="Group"/> dependency property.</summary>
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(
            nameof(Group),
            typeof(string),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the group name for grouping gallery items.
    /// </summary>
    public string Group
    {
        get => (string)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command to execute when this item is clicked.
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
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the item is clicked.
    /// </summary>
#if __ANDROID__
    public new event RoutedEventHandler? Click;
#else
    public event RoutedEventHandler? Click;
#endif

    #endregion

    #region Constructor

    private bool _isPointerOver;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGalleryItem"/> class.
    /// </summary>
    public RibbonGalleryItem()
    {
        DefaultStyleKey = typeof(RibbonGalleryItem);
        IsTabStop = true;
        commandAvailability = new CommandAvailability(
            this, CommandProperty, CommandParameterProperty,
            available =>
            {
                if (!available)
                {
                    CancelPendingActivation();
                }
                UpdateVisualState();
            });
        IsEnabledChanged += (_, _) =>
        {
            if (!IsEnabled)
            {
                CancelPendingActivation();
            }
            UpdateVisualState();
        };
        PointerPressed += OnPointerPressedHandler;
        PointerReleased += OnPointerReleasedHandler;
        PointerEntered += OnPointerEnteredHandler;
        PointerExited += OnPointerExitedHandler;
        PointerCanceled += OnPointerExitedHandler;
        PointerCaptureLost += OnPointerExitedHandler;
#if !WINDOWS
        SizeChanged += OnSizeChanged;
#endif
    }

    #endregion

    #region Methods

    private void OnPointerPressedHandler(object sender, PointerRoutedEventArgs e)
    {
        if (HandleActivationPointerPressed(e.Handled, e.GetCurrentPoint(this).Properties.IsLeftButtonPressed))
        {
            e.Handled = true;
        }
    }

    private void OnPointerReleasedHandler(object sender, PointerRoutedEventArgs e)
    {
        HandleActivationPointerReleased();
    }

    private void OnPointerEnteredHandler(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        UpdateVisualState();

        if (PreviewCommand?.CanExecute(CommandParameter) == true)
        {
            PreviewCommand.Execute(CommandParameter);
        }
    }

    private void OnPointerExitedHandler(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        IsPressed = false;
        UpdateVisualState();

        if (CancelPreviewCommand?.CanExecute(CommandParameter) == true)
        {
            CancelPreviewCommand.Execute(CommandParameter);
        }
    }

    private void UpdateVisualState()
    {
        var state = !IsEnabled ? "Disabled"
            : IsPressed ? "Pressed"
            : IsSelected ? "Selected"
            : _isPointerOver ? "PointerOver"
            : "Normal";
        VisualStateManager.GoToState(this, state, true);
#if !WINDOWS
        VisualStateManager.GoToState(this, isCompactHeight ? "Compact" : "Regular", true);
#endif
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState();
    }

#if !WINDOWS
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = CompactRibbonLayoutMath.UsesCompactGalleryItemPadding(
            e.NewSize.Height);
        if (isCompactHeight == compact)
        {
            return;
        }

        isCompactHeight = compact;
        UpdateVisualState();
    }
#endif

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGalleryItem item)
        {
            item.UpdateVisualState();
            if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(item)
                is Fluent.Automation.Peers.GalleryItemWrapperAutomationPeer peer)
            {
                peer.RaiseIsSelectedChanged((bool)e.OldValue, (bool)e.NewValue);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (HandleActivationKeyDown(e.Key, e.Handled, ReferenceEquals(e.OriginalSource, this)))
        {
            e.Handled = true;
            return;
        }

        if (!e.Handled
            && (GalleryOwner switch
                {
                    RibbonGallery gallery => gallery.HandleGalleryItemKeyDown(this, e),
                    InRibbonGallery gallery => gallery.HandleGalleryItemKeyDown(this, e),
                    _ => false,
                }))
        {
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        if (HandleActivationKeyUp(e.Key, e.Handled, ReferenceEquals(e.OriginalSource, this)))
        {
            e.Handled = true;
        }
        base.OnKeyUp(e);
    }

    /// <summary>Arms an activation key; repeated key-down events do not invoke the item.</summary>
    protected virtual bool HandleActivationKeyDown(
        Windows.System.VirtualKey key, bool handled, bool isOriginalSource)
    {
        if (handled || !isOriginalSource
            || key is not (Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
            || !CanActivate)
        {
            return false;
        }

        pendingActivationKey = key;
        IsPressed = true;
        UpdateVisualState();
        return true;
    }

    /// <summary>Completes one matching, unhandled activation-key release.</summary>
    protected virtual bool HandleActivationKeyUp(
        Windows.System.VirtualKey key, bool handled, bool isOriginalSource)
    {
        if (pendingActivationKey != key)
        {
            return false;
        }

        CancelPendingActivation();
        if (handled || !isOriginalSource)
        {
            return false;
        }

        Activate();
        return true;
    }

    /// <summary>Handles the primary pointer activation shared by native input and derived controls.</summary>
    protected virtual bool HandleActivationPointerPressed(bool handled, bool isPrimaryButton)
    {
        if (handled || !isPrimaryButton || !CanActivate)
        {
            return false;
        }

        IsPressed = true;
        UpdateVisualState();
        Activate();
        return true;
    }

    /// <summary>Clears pointer feedback without invoking a second action.</summary>
    protected virtual void HandleActivationPointerReleased()
    {
        IsPressed = false;
        UpdateVisualState();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        CancelPendingActivation();
        base.OnLostFocus(e);
    }

    private void CancelPendingActivation()
    {
        pendingActivationKey = null;
        IsPressed = false;
        UpdateVisualState();
    }

    /// <summary>Gets whether the item and its command are currently enabled.</summary>
    protected virtual bool IsEnabledCore => commandAvailability.CanExecute && IsEnabled;

    protected internal bool CanActivate
        => IsEnabledCore
           && GalleryOwner is not Control { IsEnabled: false };

    internal void Activate()
    {
        if (!CanActivate)
        {
            return;
        }

        switch (GalleryOwner)
        {
            case RibbonGallery gallery:
                gallery.SelectItem(this);
                break;
            case InRibbonGallery gallery:
                gallery.SelectItem(this);
                break;
            default:
                IsSelected = true;
                break;
        }

        Internal.CommandHelper.Execute(Command, CommandParameter);

        Click?.Invoke(this, new RoutedEventArgs());
    }

    #endregion

    #region IKeyTipedControl

    /// <inheritdoc />
    public KeyTipPressedResult OnKeyTipPressed()
    {
        Activate();
        return KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.GalleryItemWrapperAutomationPeer(this);
}

internal enum GalleryNavigationDirection
{
    Previous,
    Next,
    PreviousRow,
    NextRow,
    First,
    Last,
}

internal static class GalleryNavigationMath
{
    internal static int GetTargetIndex(
        int currentIndex,
        int itemCount,
        int columns,
        Orientation orientation,
        GalleryNavigationDirection direction)
    {
        if (itemCount <= 0)
        {
            return -1;
        }

        currentIndex = Math.Clamp(currentIndex, 0, itemCount - 1);
        columns = Math.Max(1, columns);

        var target = direction switch
        {
            GalleryNavigationDirection.First => 0,
            GalleryNavigationDirection.Last => itemCount - 1,
            GalleryNavigationDirection.Previous => currentIndex - 1,
            GalleryNavigationDirection.Next => currentIndex + 1,
            GalleryNavigationDirection.PreviousRow when orientation == Orientation.Horizontal
                => currentIndex - columns,
            GalleryNavigationDirection.NextRow when orientation == Orientation.Horizontal
                => currentIndex + columns,
            GalleryNavigationDirection.PreviousRow => currentIndex - 1,
            GalleryNavigationDirection.NextRow => currentIndex + 1,
            _ => currentIndex,
        };

        return Math.Clamp(target, 0, itemCount - 1);
    }
}

namespace Fluent;

using System.Collections;

/// <summary>
/// Represents the full-page backstage surface.
/// </summary>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_AdornerLayer, Type = typeof(FrameworkElement))]
public partial class Backstage : RibbonControl
{
    private const string PART_AdornerLayer = "PART_AdornerLayer";

    private WeakReference<UIElement>? focusBackup;
    private Ribbon? parentRibbon;
    private bool? originalHideContextTabs;
    private bool effectiveIsOpen;

    /// <summary>
    /// Occurs when <see cref="IsOpen"/> changes.
    /// </summary>
    public event EventHandler<DependencyPropertyChangedEventArgs>? IsOpenChanged;

    /// <summary>
    /// Gets the portable overlay host used to display the backstage content.
    /// </summary>
    public FrameworkElement? AdornerLayer { get; private set; }

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(false, OnIsOpenChanged));

    /// <summary>Gets or sets whether the backstage is open.</summary>
    public bool IsOpen
    {
        get => effectiveIsOpen;
        set => SetIsOpen(value);
    }

    /// <summary>Identifies the <see cref="CanChangeIsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty CanChangeIsOpenProperty =
        DependencyProperty.Register(
            nameof(CanChangeIsOpen),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true, OnCanChangeIsOpenChanged));

    /// <summary>Gets or sets whether the open state can change.</summary>
    public bool CanChangeIsOpen
    {
        get => (bool)GetValue(CanChangeIsOpenProperty);
        set => SetValue(CanChangeIsOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="HideContextTabsOnOpen"/> dependency property.</summary>
    public static readonly DependencyProperty HideContextTabsOnOpenProperty =
        DependencyProperty.Register(
            nameof(HideContextTabsOnOpen),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether contextual tabs are hidden while open.</summary>
    public bool HideContextTabsOnOpen
    {
        get => (bool)GetValue(HideContextTabsOnOpenProperty);
        set => SetValue(HideContextTabsOnOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="AreAnimationsEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty AreAnimationsEnabledProperty =
        DependencyProperty.Register(
            nameof(AreAnimationsEnabled),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether open and close transitions are animated.</summary>
    public bool AreAnimationsEnabled
    {
        get => (bool)GetValue(AreAnimationsEnabledProperty);
        set => SetValue(AreAnimationsEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="CloseOnEsc"/> dependency property.</summary>
    public static readonly DependencyProperty CloseOnEscProperty =
        DependencyProperty.Register(
            nameof(CloseOnEsc),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether Escape closes the backstage.</summary>
    public bool CloseOnEsc
    {
        get => (bool)GetValue(CloseOnEscProperty);
        set => SetValue(CloseOnEscProperty, value);
    }

    /// <summary>Identifies the <see cref="UseHighestAvailableAdornerLayer"/> dependency property.</summary>
    public static readonly DependencyProperty UseHighestAvailableAdornerLayerProperty =
        DependencyProperty.Register(
            nameof(UseHighestAvailableAdornerLayer),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the highest available portable overlay host should be used.
    /// </summary>
    public bool UseHighestAvailableAdornerLayer
    {
        get => (bool)GetValue(UseHighestAvailableAdornerLayerProperty);
        set => SetValue(UseHighestAvailableAdornerLayerProperty, value);
    }

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(UIElement),
            typeof(Backstage),
            new PropertyMetadata(null, OnContentChanged));

    /// <summary>Gets or sets the backstage content.</summary>
    public UIElement? Content
    {
        get => (UIElement?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Initializes a new instance of the <see cref="Backstage"/> class.</summary>
    public Backstage()
    {
        DefaultStyleKey = typeof(Backstage);
        CanAddToQuickAccessToolBar = false;
        Loaded += OnBackstageLoaded;
        Unloaded += OnBackstageUnloaded;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        AdornerLayer = GetTemplateChild(PART_AdornerLayer) as FrameworkElement;
        UpdateVisualState();
    }

    /// <summary>
    /// Called when popup dismissal is requested for content inside this backstage.
    /// </summary>
    protected virtual void OnDismissPopup(object? sender, DismissPopupEventArgs e)
    {
        if (sender is DependencyObject source
            && ReferenceEquals(source, this) is false
            && PopupService.IsAncestorOf(this, source) is false)
        {
            return;
        }

        if (e.DismissReason is DismissPopupReason.ApplicationLostFocus
            or DismissPopupReason.ShowingKeyTips
            || e.DismissMode != DismissPopupMode.Always)
        {
            return;
        }

        SetIsOpen(false);
    }

    /// <summary>Shows the backstage content.</summary>
    protected virtual bool Show()
    {
        if (Content is null)
        {
            return false;
        }

        focusBackup = FocusRoutingHelper.CaptureFocusedElement(this, onlyWhenOutsideOwner: true);
        ResolveParentRibbon();
        ApplyParentRibbonOpenState();
        UpdateVisualState();

        DispatcherQueue?.TryEnqueue(
            () =>
            {
                if (effectiveIsOpen && Content is not null)
                {
                    FocusRoutingHelper.FocusFirst(Content);
                }
            });

        return true;
    }

    /// <summary>Hides the backstage content.</summary>
    protected virtual void Hide()
    {
        UpdateVisualState();
        RestoreParentRibbonState();
        FocusRoutingHelper.RestoreFocus(ref focusBackup);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            base.OnKeyDown(e);
            return;
        }

        if ((e.Key == Windows.System.VirtualKey.Enter
             || e.Key == Windows.System.VirtualKey.Space)
            && FocusState != FocusState.Unfocused)
        {
            SetIsOpen(!effectiveIsOpen);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape
                 && CloseOnEsc
                 && effectiveIsOpen)
        {
            SetIsOpen(false);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        OnMouseLeftButtonDown(e);
    }

    /// <summary>Handles the WPF-compatible left-button activation hook.</summary>
    protected virtual void OnMouseLeftButtonDown(PointerRoutedEventArgs e)
    {
        if (!e.Handled && ReferenceEquals(e.OriginalSource, this))
        {
            SetIsOpen(!effectiveIsOpen);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    public override KeyTipPressedResult OnKeyTipPressed()
    {
        SetIsOpen(true);
        base.OnKeyTipPressed();

        return KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    public override void OnKeyTipBack()
    {
        SetIsOpen(false);
        base.OnKeyTipBack();
    }

    /// <inheritdoc />
    public override FrameworkElement? CreateQuickAccessItem()
    {
        throw new NotImplementedException();
    }

    /// <summary>Gets the logical children retained for WPF source compatibility.</summary>
    protected override IEnumerator LogicalChildren
    {
        get
        {
            if (Content is not null)
            {
                yield return Content;
            }

            if (Icon is not null)
            {
                yield return Icon;
            }
        }
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonBackstageAutomationPeer(this);

    internal void SetIsOpen(bool isOpen)
    {
        if (CanChangeIsOpen
            && (bool)GetValue(IsOpenProperty) != isOpen)
        {
            SetValue(IsOpenProperty, isOpen);
        }
    }

    private static void OnIsOpenChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var backstage = (Backstage)sender;
        var newValue = (bool)args.NewValue;

        if (backstage.CanChangeIsOpen is false)
        {
            return;
        }

        if (newValue)
        {
            backstage.effectiveIsOpen = true;
            if (backstage.Show() is false)
            {
                backstage.effectiveIsOpen = false;
                backstage.UpdateVisualState();
                return;
            }
        }
        else
        {
            backstage.effectiveIsOpen = false;
            backstage.Hide();
        }

        backstage.IsOpenChanged?.Invoke(backstage, args);
    }

    private static void OnCanChangeIsOpenChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var backstage = (Backstage)sender;
        if ((bool)args.NewValue is false
            || backstage.effectiveIsOpen
               == (bool)backstage.GetValue(IsOpenProperty))
        {
            return;
        }

        if ((bool)backstage.GetValue(IsOpenProperty))
        {
            backstage.effectiveIsOpen = true;
            if (backstage.Show() is false)
            {
                backstage.effectiveIsOpen = false;
                backstage.UpdateVisualState();
            }
        }
        else
        {
            backstage.effectiveIsOpen = false;
            backstage.Hide();
        }
    }

    private static void OnContentChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var backstage = (Backstage)sender;
        if (args.NewValue is null && backstage.effectiveIsOpen)
        {
            backstage.effectiveIsOpen = false;
            backstage.Hide();
        }
    }

    private void OnBackstageLoaded(object sender, RoutedEventArgs args)
    {
        PopupService.DismissPopup += OnDismissPopup;
        ResolveParentRibbon();
        UpdateVisualState();

        if (CanChangeIsOpen && (bool)GetValue(IsOpenProperty))
        {
            effectiveIsOpen = true;
            ApplyParentRibbonOpenState();
            UpdateVisualState();
        }
    }

    private void OnBackstageUnloaded(object sender, RoutedEventArgs args)
    {
        PopupService.DismissPopup -= OnDismissPopup;
        RestoreParentRibbonState();
        focusBackup = null;
        AdornerLayer = null;
    }

    private void ResolveParentRibbon()
    {
        parentRibbon = GetParentRibbon(this);
        if (parentRibbon is null && XamlRoot?.Content is DependencyObject root)
        {
            parentRibbon = FocusRoutingHelper.FindDescendant<Ribbon>(root);
        }
    }

    private void ApplyParentRibbonOpenState()
    {
        if (parentRibbon is null)
        {
            return;
        }

        parentRibbon.ActiveBackstage = this;
        parentRibbon.IsBackstageOrStartScreenOpen = true;
        if (HideContextTabsOnOpen
            && parentRibbon.TitleBar is { } titleBar
            && titleBar.HideContextTabs is false)
        {
            originalHideContextTabs = false;
            titleBar.HideContextTabs = true;
        }
    }

    private void RestoreParentRibbonState()
    {
        if (parentRibbon is null)
        {
            return;
        }

        if (ReferenceEquals(parentRibbon.ActiveBackstage, this))
        {
            parentRibbon.ActiveBackstage = null;
        }

        parentRibbon.IsBackstageOrStartScreenOpen =
            parentRibbon.StartScreen?.IsOpen == true;

        if (originalHideContextTabs.HasValue && parentRibbon.TitleBar is { } titleBar)
        {
            titleBar.HideContextTabs = originalHideContextTabs.Value;
        }

        originalHideContextTabs = null;
        parentRibbon = null;
    }

    private void UpdateVisualState()
    {
        VisualStateManager.GoToState(
            this,
            effectiveIsOpen ? "Open" : "Closed",
            AreAnimationsEnabled);
    }
}

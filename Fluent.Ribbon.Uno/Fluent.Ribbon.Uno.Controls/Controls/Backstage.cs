namespace Fluent;

using System.Collections;
using Microsoft.UI.Dispatching;

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
    private bool isShown;
    private DispatcherQueueTimer? closingAnimationTimer;
    private string? localizedAutomationName;
    private Ribbon? presentationOwner;
    private bool isSynchronizingOpenProperty;
    private bool isClosingForOwner;
    private EffectiveValueConstraint? openStateConstraint;

    internal event EventHandler? PresentationChanged;

    internal bool IsPresentationShown => isShown;

    internal Ribbon? PresentationOwner => presentationOwner;

    /// <summary>Gets whether the surface has content that can be displayed.</summary>
    protected virtual bool HasDisplayContent => Content is not null;

    /// <summary>Gets the content scope which receives focus when the surface opens.</summary>
    protected virtual DependencyObject FocusScope => Content ?? (DependencyObject)this;

    /// <summary>Gets whether this surface participates in its owning ribbon's application state.</summary>
    protected virtual bool AffectsParentRibbon => true;

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
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedAutomationName);
        RegisterPropertyChangedCallback(
            HeaderProperty,
            static (sender, _) => ((Backstage)sender).RefreshLocalizedAutomationName());
        RegisterPropertyChangedCallback(
            VisibilityProperty,
            (_, _) => PresentationChanged?.Invoke(this, EventArgs.Empty));
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        AdornerLayer = GetTemplateChild(PART_AdornerLayer) as FrameworkElement;
        UpdateVisualState();

        if (AdornerLayer is not null)
        {
            AdornerLayer.Visibility = isShown ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Called when popup dismissal is requested for content inside this backstage.
    /// </summary>
    protected virtual void OnDismissPopup(object? sender, DismissPopupEventArgs e)
    {
        if (sender is DependencyObject source
            && ReferenceEquals(source, this) is false
            && PopupService.IsOwnedDescendantOf(this, source) is false)
        {
            return;
        }

        if (e.DismissReason is DismissPopupReason.ApplicationLostFocus
            or DismissPopupReason.ShowingKeyTips
            || e.DismissMode != DismissPopupMode.Always)
        {
            return;
        }

        if (PopupService.ShouldPreserveOpenAncestor(this, sender))
        {
            return;
        }

        SetIsOpen(false);
    }

    /// <summary>Shows the backstage content.</summary>
    protected virtual bool Show()
    {
        if (!HasDisplayContent)
        {
            return false;
        }

        CancelClosingAnimation();
        ResolveParentRibbon();
        if (!ApplyParentRibbonOpenState())
        {
            return false;
        }

        CapturePresentationFocus();
        isShown = true;
        UpdateVisualState();

        // Make the surface visible immediately instead of waiting for the Open transition
        // to apply its setters, so the backstage never has an invisible first frame.
        if (AdornerLayer is not null)
        {
            AdornerLayer.Visibility = Visibility.Visible;
        }

        DispatcherQueue?.TryEnqueue(
            () =>
            {
                if (effectiveIsOpen && IsLoaded && FocusRoutingHelper.IsEffectivelyVisible(this))
                {
                    FocusRoutingHelper.FocusFirst(FocusScope);
                }
            });

        PresentationChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Hides the backstage content.</summary>
    protected virtual void Hide()
    {
        // Decide from our own shown-state rather than from AdornerLayer.Visibility: that
        // property is driven by VisualState setters which are gated behind the OpenStates
        // VisualTransition (180 ms), so a close that happens sooner would still observe the
        // stale Collapsed value and skip the closing animation entirely.
        if (!isClosingForOwner && AreAnimationsEnabled && isShown && AdornerLayer is not null)
        {
            StartClosingAnimation();
            return;
        }

        CompleteHide();
    }

    private void StartClosingAnimation()
    {
        CancelClosingAnimation();
        VisualStateManager.GoToState(this, "Closing", true);

        // The closing surface must be on screen for the whole fade-out. VisualState setters
        // alone cannot guarantee that (see Hide), so drive the visibility directly.
        if (AdornerLayer is not null)
        {
            AdornerLayer.Visibility = Visibility.Visible;
        }

        if (DispatcherQueue is null)
        {
            CompleteHide();
            return;
        }

        closingAnimationTimer = DispatcherQueue.CreateTimer();
        closingAnimationTimer.Interval = TimeSpan.FromMilliseconds(120);
        closingAnimationTimer.IsRepeating = false;
        closingAnimationTimer.Tick += OnClosingAnimationTimerTick;
        closingAnimationTimer.Start();
    }

    private void OnClosingAnimationTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnClosingAnimationTimerTick;
        if (!ReferenceEquals(sender, closingAnimationTimer))
        {
            return;
        }

        closingAnimationTimer = null;
        if (!effectiveIsOpen)
        {
            CompleteHide();
        }
    }

    private void CompleteHide()
    {
        CancelClosingAnimation();
        isShown = false;
        VisualStateManager.GoToState(this, "Closed", false);

        if (AdornerLayer is not null)
        {
            AdornerLayer.Visibility = Visibility.Collapsed;
        }

        RestoreParentRibbonState();
        FocusRoutingHelper.RestoreFocus(ref focusBackup);
        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CancelClosingAnimation()
    {
        if (closingAnimationTimer is null)
        {
            return;
        }

        closingAnimationTimer.Stop();
        closingAnimationTimer.Tick -= OnClosingAnimationTimerTick;
        closingAnimationTimer = null;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            base.OnKeyDown(e);
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Escape
            && (presentationOwner ?? parentRibbon)?.IsKeyTipModeActive == true)
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
        else if (e.Key == Windows.System.VirtualKey.Escape && TryCloseOnEscape())
        {
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    internal bool TryCloseOnEscape()
    {
        if (!CloseOnEsc || !effectiveIsOpen || !CanChangeIsOpen
            || (presentationOwner ?? parentRibbon)?.IsKeyTipModeActive == true)
        {
            return false;
        }

        SetIsOpen(false);
        return !effectiveIsOpen;
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

        return new KeyTipPressedResult(false, effectiveIsOpen);
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
        if (!CanChangeIsOpen && !isClosingForOwner)
        {
            return;
        }

        if ((bool)GetValue(IsOpenProperty) == isOpen && effectiveIsOpen != isOpen)
        {
            isSynchronizingOpenProperty = true;
            try
            {
                SetValue(IsOpenProperty, effectiveIsOpen);
            }
            finally
            {
                isSynchronizingOpenProperty = false;
            }
        }

        if ((bool)GetValue(IsOpenProperty) != isOpen)
        {
            SetValue(IsOpenProperty, isOpen);
        }
    }

    private static void OnIsOpenChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var backstage = (Backstage)sender;
        if (backstage.isSynchronizingOpenProperty)
        {
            return;
        }

        var oldValue = backstage.effectiveIsOpen;
        var requestedValue = (bool)args.NewValue;

        if (!backstage.CanChangeIsOpen && !backstage.isClosingForOwner)
        {
            backstage.HoldOpenState();
            return;
        }

        if (requestedValue)
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
        backstage.RaiseIsOpenAutomationEvent(
            oldValue,
            backstage.effectiveIsOpen);
    }

    private void RaiseIsOpenAutomationEvent(bool oldValue, bool newValue)
    {
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is Fluent.Automation.Peers.RibbonBackstageAutomationPeer peer)
        {
            peer.RaiseIsOpenChanged(oldValue, newValue);
        }
        else if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
                 is Fluent.Automation.Peers.RibbonStartScreenAutomationPeer startScreenPeer)
        {
            startScreenPeer.RaiseIsOpenChanged(oldValue, newValue);
        }
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
            name = RibbonLocalization.Current.Localization.ApplicationMenuName;
        }

        Fluent.Automation.Peers.AutomationPeerHelpers.UpdatePeerName(
            this,
            ref localizedAutomationName,
            name);
    }

    private static void OnCanChangeIsOpenChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var backstage = (Backstage)sender;
        if (!(bool)args.NewValue)
        {
            backstage.HoldOpenState();
            return;
        }

        backstage.openStateConstraint?.Release();
        if (backstage.effectiveIsOpen != (bool)backstage.GetValue(IsOpenProperty))
        {
            backstage.SetIsOpen((bool)backstage.GetValue(IsOpenProperty));
        }
    }

    private void HoldOpenState()
    {
        isSynchronizingOpenProperty = true;
        try
        {
            (openStateConstraint ??= new EffectiveValueConstraint(this, IsOpenProperty, nameof(IsOpen))).Hold(effectiveIsOpen);
        }
        finally
        {
            isSynchronizingOpenProperty = false;
        }
    }

    private static void OnContentChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        ((Backstage)sender).OnBackstageContentChanged((UIElement?)args.NewValue);
    }

    /// <summary>Synchronizes a portable content change with a pending open request.</summary>
    protected virtual void OnBackstageContentChanged(UIElement? content) => RefreshRequestedOpenState();

    /// <summary>Reevaluates an open request when content becomes available or is removed.</summary>
    protected void RefreshRequestedOpenState()
    {
        if (!HasDisplayContent && effectiveIsOpen)
        {
            var oldValue = effectiveIsOpen;
            effectiveIsOpen = false;
            Hide();
            RaiseIsOpenAutomationEvent(oldValue, false);
        }
        else if (HasDisplayContent && !effectiveIsOpen && CanChangeIsOpen && (bool)GetValue(IsOpenProperty))
        {
            SetIsOpen(true);
        }

        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnBackstageLoaded(object sender, RoutedEventArgs args)
    {
        PopupService.DismissPopup += OnDismissPopup;
        AdornerLayer = GetTemplateChild(PART_AdornerLayer) as FrameworkElement;
        ResolveParentRibbon();
        UpdateVisualState();

        if (CanChangeIsOpen && (bool)GetValue(IsOpenProperty))
        {
            if (!effectiveIsOpen)
            {
                SetIsOpen(true);
            }
            else
            {
                isShown = HasDisplayContent;
                CapturePresentationFocus();
                if (!ApplyParentRibbonOpenState())
                {
                    CloseForOwner(restoreFocus: false);
                    return;
                }

                UpdateVisualState();
            }
        }

        PresentationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnBackstageUnloaded(object sender, RoutedEventArgs args)
    {
        PopupService.DismissPopup -= OnDismissPopup;
        CancelClosingAnimation();
        RestoreParentRibbonState();
        if (presentationOwner is null || !effectiveIsOpen)
        {
            focusBackup = null;
            isShown = false;
        }
        AdornerLayer = null;
    }

    private void ResolveParentRibbon()
    {
        parentRibbon = presentationOwner ?? GetParentRibbon(this);
        if (parentRibbon is null && XamlRoot?.Content is DependencyObject root)
        {
            parentRibbon = FocusRoutingHelper.FindDescendant<Ribbon>(root);
        }
    }

    private bool ApplyParentRibbonOpenState()
    {
        if (parentRibbon is null || !AffectsParentRibbon)
        {
            return true;
        }

        if (!parentRibbon.TryActivateApplicationSurface(this))
        {
            return false;
        }

        if (HideContextTabsOnOpen
            && parentRibbon.TitleBar is { } titleBar
            && titleBar.HideContextTabs is false)
        {
            originalHideContextTabs = false;
            titleBar.HideContextTabs = true;
        }

        return true;
    }

    private void RestoreParentRibbonState()
    {
        if (parentRibbon is null)
        {
            return;
        }

        if (!AffectsParentRibbon)
        {
            originalHideContextTabs = null;
            parentRibbon = null;
            return;
        }

        parentRibbon.ReleaseApplicationSurface(this);

        if (originalHideContextTabs.HasValue && parentRibbon.TitleBar is { } titleBar)
        {
            titleBar.HideContextTabs = originalHideContextTabs.Value;
        }

        originalHideContextTabs = null;
        parentRibbon = null;
    }

    internal void SetPresentationOwner(Ribbon? ribbon)
    {
        presentationOwner = ribbon;
        if (ribbon is not null && effectiveIsOpen)
        {
            ResolveParentRibbon();
            CapturePresentationFocus();
            if (!ApplyParentRibbonOpenState())
            {
                CloseForOwner(restoreFocus: false);
            }
        }
    }

    internal WeakReference<UIElement>? TakePresentationFocus()
    {
        var reference = focusBackup;
        focusBackup = null;
        return reference;
    }

    internal void AdoptPresentationFocus(WeakReference<UIElement>? reference)
    {
        if (reference is not null)
        {
            focusBackup = reference;
        }
    }

    internal void CapturePresentationFocus()
    {
        focusBackup ??= XamlRoot is null && presentationOwner is not null
            ? FocusRoutingHelper.CaptureFocusedElement(presentationOwner)
            : FocusRoutingHelper.CaptureFocusedElement(this, onlyWhenOutsideOwner: true);
    }

    internal void FocusPresentation()
    {
        if (effectiveIsOpen && IsLoaded && FocusRoutingHelper.IsEffectivelyVisible(this))
        {
            FocusRoutingHelper.FocusFirst(FocusScope);
        }
    }

    internal void CloseForOwner(bool restoreFocus)
    {
        if (!restoreFocus)
        {
            focusBackup = null;
        }

        isClosingForOwner = true;
        try
        {
            isSynchronizingOpenProperty = true;
            try
            {
                openStateConstraint?.Release();
            }
            finally
            {
                isSynchronizingOpenProperty = false;
            }

            SetIsOpen(false);
            CompleteHide();
        }
        finally
        {
            isClosingForOwner = false;
            if (!CanChangeIsOpen)
            {
                HoldOpenState();
            }
        }
    }

    private void UpdateVisualState()
    {
        VisualStateManager.GoToState(
            this,
            effectiveIsOpen ? "Open" : "Closed",
            AreAnimationsEnabled);
    }
}

namespace Fluent;

using System.Collections;
using System.Reflection;
using Fluent.Extensibility;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

/// <summary>
/// Manages Office-style KeyTips for a <see cref="Ribbon"/>.
/// </summary>
/// <remarks>
/// WinUI and Uno do not expose WPF adorners, so KeyTips are rendered in a
/// window-spanning, non-hit-testable <see cref="Popup"/> overlay.
/// </remarks>
public partial class KeyTipService
{
    /// <summary>
    /// Gets a new list containing the default KeyTip activation keys.
    /// </summary>
    public static IList<VirtualKey> DefaultKeyTipKeys =>
        new List<VirtualKey>
        {
            VirtualKey.Menu,
            VirtualKey.F10,
        };

    private readonly Ribbon _ribbon;
    private readonly Popup _overlayPopup;
    private readonly Canvas _overlayCanvas;
    private readonly List<TargetEntry> _targets = new();
    private readonly Stack<ScopeFrame> _scopeStack = new();
    private readonly KeyEventHandler _rootKeyDownHandler;
    private readonly KeyEventHandler _rootKeyUpHandler;
    private readonly PointerEventHandler _rootPointerPressedHandler;

    private FrameworkElement? _rootElement;
    private WeakReference<UIElement>? _focusBackup;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _altHoldTimer;
    private bool _isAttached;
    private bool _isActive;
    private bool _activationKeyIsDown;
    private bool _activationKeyWasUsedAsModifier;
    private int _scopeRefreshGeneration;
    private string _typedKeys = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTipService"/> class.
    /// </summary>
    public KeyTipService(Ribbon ribbon)
    {
        _ribbon = ribbon ?? throw new ArgumentNullException(nameof(ribbon));
        _rootKeyDownHandler = OnRootKeyDown;
        _rootKeyUpHandler = OnRootKeyUp;
        _rootPointerPressedHandler = OnRootPointerPressed;

        _overlayCanvas = new Canvas
        {
            IsHitTestVisible = false,
        };

        _overlayPopup = new Popup
        {
            Child = _overlayCanvas,
            IsLightDismissEnabled = false,
            IsOpen = false,
        };
    }

    /// <summary>
    /// Gets the keys which activate KeyTip mode.
    /// </summary>
    public IList<VirtualKey> KeyTipKeys { get; } = DefaultKeyTipKeys;

    /// <summary>
    /// Gets whether KeyTip mode is active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets whether at least one KeyTip is currently visible.
    /// </summary>
    public bool AreAnyKeyTipsVisible
        => _isActive && _targets.Any(entry => entry.Visual.Visibility == Visibility.Visible);

    /// <summary>
    /// Gets or sets whether this service processes keyboard input.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Attaches keyboard, pointer, focus, and unload handlers.
    /// </summary>
    public void Attach()
    {
        if (_isAttached || _ribbon.XamlRoot?.Content is not FrameworkElement root)
        {
            return;
        }

        _rootElement = root;
        root.AddHandler(UIElement.KeyDownEvent, _rootKeyDownHandler, true);
        root.AddHandler(UIElement.KeyUpEvent, _rootKeyUpHandler, true);
        root.AddHandler(UIElement.PointerPressedEvent, _rootPointerPressedHandler, true);
        root.LostFocus += OnRootLostFocus;
        _ribbon.Unloaded += OnRibbonUnloaded;
        _isAttached = true;
    }

    /// <summary>
    /// Removes all handlers, timers, visuals, and retained focus references.
    /// </summary>
    public void Detach()
    {
        if (_isAttached && _rootElement is { } root)
        {
            root.RemoveHandler(UIElement.KeyDownEvent, _rootKeyDownHandler);
            root.RemoveHandler(UIElement.KeyUpEvent, _rootKeyUpHandler);
            root.RemoveHandler(UIElement.PointerPressedEvent, _rootPointerPressedHandler);
            root.LostFocus -= OnRootLostFocus;
        }

        if (_isAttached)
        {
            _ribbon.Unloaded -= OnRibbonUnloaded;
        }

        StopAltHoldTimer();
        Hide(restoreFocus: false);
        _focusBackup = null;
        _rootElement = null;
        _isAttached = false;
    }

    /// <summary>
    /// Shows the current KeyTip scope.
    /// </summary>
    public void Show()
    {
        if (!_isAttached)
        {
            Attach();
        }

        if (!IsEnabled
            || _isActive
            || _ribbon.XamlRoot is null
            || _ribbon.Visibility != Visibility.Visible
            || _ribbon.IsCollapsed
            || !_ribbon.IsEnabled)
        {
            return;
        }

        _focusBackup = FocusRoutingHelper.CaptureFocusedElement(_ribbon, onlyWhenOutsideOwner: true);
        _isActive = true;
        _typedKeys = string.Empty;
        _scopeStack.Clear();
        _scopeStack.Push(new ScopeFrame(_ribbon, null));

        var nestedScope = GetInitiallyOpenScope();
        if (nestedScope is not null && !ReferenceEquals(nestedScope, _ribbon))
        {
            _scopeStack.Push(new ScopeFrame(nestedScope, nestedScope));
        }

        OpenOverlay();
        RebuildTargets();
        FocusCurrentScope();
    }

    /// <summary>
    /// Hides KeyTips and restores the focus which was active before KeyTip mode.
    /// </summary>
    public void Hide()
    {
        Hide(restoreFocus: true);
    }

    internal void Initialize()
    {
        Attach();
    }

    internal void Teardown()
    {
        Detach();
    }

    private void Hide(bool restoreFocus)
    {
        _scopeRefreshGeneration++;
        _isActive = false;
        _activationKeyIsDown = false;
        _activationKeyWasUsedAsModifier = false;
        _typedKeys = string.Empty;
        StopAltHoldTimer();
        _targets.Clear();
        _scopeStack.Clear();
        _overlayCanvas.Children.Clear();
        _overlayPopup.IsOpen = false;

        if (restoreFocus)
        {
            FocusRoutingHelper.RestoreFocus(ref _focusBackup);
        }
        else
        {
            _focusBackup = null;
        }
    }

    private void OnRibbonUnloaded(object sender, RoutedEventArgs args)
    {
        Detach();
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (!IsEnabled || !ShouldProcessKeyEvent(_isActive, args.Handled))
        {
            return;
        }

        if (_isActive)
        {
            if (_activationKeyIsDown
                && args.Key is >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9)
            {
                _activationKeyWasUsedAsModifier = true;
                Hide();
                return;
            }

            if (IsActivationKey(args.Key))
            {
                Hide();
                args.Handled = true;
                return;
            }

            if (args.Key == VirtualKey.Escape)
            {
                NavigateBack();
                args.Handled = true;
                return;
            }

            if (args.Key == VirtualKey.Back)
            {
                if (_typedKeys.Length > 0)
                {
                    _typedKeys = _typedKeys[..^1];
                    ApplyPrefixFilter();
                }
                else
                {
                    NavigateBack();
                }

                args.Handled = true;
                return;
            }

            if (TryGetCharacter(args.Key, out var character))
            {
                AppendKey(character);
                args.Handled = true;
            }

            return;
        }

        if (_activationKeyIsDown
            && args.Key is >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9)
        {
            _activationKeyWasUsedAsModifier = true;
            StopAltHoldTimer();
            return;
        }

        if (_activationKeyIsDown && !IsModifierKey(args.Key))
        {
            _activationKeyWasUsedAsModifier = true;
            StopAltHoldTimer();
            return;
        }

        if (!IsActivationKey(args.Key))
        {
            return;
        }

        if (args.Key == VirtualKey.F10 && IsShiftDown())
        {
            return;
        }

        if (args.Key == VirtualKey.Menu)
        {
            _activationKeyIsDown = true;
            _activationKeyWasUsedAsModifier = false;
            StartAltHoldTimer();
            return;
        }

        Show();
        args.Handled = _isActive;
    }

    private void OnRootKeyUp(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key != VirtualKey.Menu || !_activationKeyIsDown)
        {
            return;
        }

        var shouldShow = !_activationKeyWasUsedAsModifier && !_isActive;
        _activationKeyIsDown = false;
        _activationKeyWasUsedAsModifier = false;
        StopAltHoldTimer();

        if (shouldShow)
        {
            Show();
            args.Handled = _isActive;
        }
    }

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        DismissForPointerInput();
    }

    private void OnRootLostFocus(object sender, RoutedEventArgs args)
    {
        if (!_isActive || _ribbon.DispatcherQueue is null)
        {
            return;
        }

        _ribbon.DispatcherQueue.TryEnqueue(() =>
        {
            if (_isActive
                && _ribbon.XamlRoot is { } xamlRoot
                && FocusManager.GetFocusedElement(xamlRoot) is null)
            {
                DismissForWindowDeactivation();
            }
        });
    }

    private void DismissForWindowDeactivation()
    {
        if (!_isActive)
        {
            return;
        }

        PopupService.RaiseDismissPopupEvent(
            _ribbon,
            DismissPopupMode.Always,
            DismissPopupReason.ApplicationLostFocus);
        Hide(restoreFocus: false);
    }

    private void DismissForPointerInput()
    {
        if (_isActive)
        {
            Hide(restoreFocus: false);
        }
    }

    private static bool ShouldProcessKeyEvent(bool isActive, bool isAlreadyHandled)
    {
        return isActive || !isAlreadyHandled;
    }

    private void StartAltHoldTimer()
    {
        StopAltHoldTimer();

        var dispatcherQueue = _ribbon.DispatcherQueue;
        if (dispatcherQueue is null)
        {
            return;
        }

        _altHoldTimer = dispatcherQueue.CreateTimer();
        _altHoldTimer.Interval = TimeSpan.FromMilliseconds(700);
        _altHoldTimer.IsRepeating = false;
        _altHoldTimer.Tick += OnAltHoldTimerTick;
        _altHoldTimer.Start();
    }

    private void StopAltHoldTimer()
    {
        if (_altHoldTimer is null)
        {
            return;
        }

        _altHoldTimer.Stop();
        _altHoldTimer.Tick -= OnAltHoldTimerTick;
        _altHoldTimer = null;
    }

    private void OnAltHoldTimerTick(
        Microsoft.UI.Dispatching.DispatcherQueueTimer sender,
        object args)
    {
        StopAltHoldTimer();
        if (_activationKeyIsDown && !_activationKeyWasUsedAsModifier)
        {
            Show();
        }
    }

    private void AppendKey(char character)
    {
        RefreshClosedScopes();
        var previousKeys = _typedKeys;
        _typedKeys += char.ToUpperInvariant(character);

        var matches = GetMatches(_typedKeys);
        if (matches.Count == 0)
        {
            if (previousKeys.Length == 0)
            {
                Hide();
            }
            else
            {
                _typedKeys = previousKeys;
                ApplyPrefixFilter();
            }

            return;
        }

        var exact = matches.FirstOrDefault(
            entry => string.Equals(entry.Keys, _typedKeys, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            Activate(exact);
            return;
        }

        ApplyPrefixFilter();
    }

    private void ApplyPrefixFilter()
    {
        foreach (var entry in _targets)
        {
            var isMatch = IsEligible(entry.Element)
                          && entry.Keys.StartsWith(_typedKeys, StringComparison.OrdinalIgnoreCase);
            entry.Visual.Visibility = isMatch ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private List<TargetEntry> GetMatches(string prefix)
    {
        return _targets
            .Where(entry => IsEligible(entry.Element)
                            && entry.Keys.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void Activate(TargetEntry entry)
    {
        if (!IsEligible(entry.Element))
        {
            RebuildTargets();
            return;
        }

        if (entry.Element is RibbonTab tab)
        {
            _ribbon.SelectedTab = tab;
            tab.IsSelected = true;
            if (_ribbon.IsMinimized && FindRibbonTabControl() is { } tabControl)
            {
                tabControl.IsDropDownOpen = true;
            }

            PushScope(tab, tab);
            return;
        }

        KeyTipPressedResult result;
        if (entry.Element is IKeyTipedControl keyTipedControl)
        {
            result = keyTipedControl.OnKeyTipPressed();
        }
        else
        {
            InvokeThroughAutomation(entry.Element);
            result = KeyTipPressedResult.Empty;
        }

        if (result.PressedElementOpenedPopup)
        {
            PushScope(entry.Element, entry.Element);
            return;
        }

        Hide(restoreFocus: !result.PressedElementAquiredFocus);
    }

    private void PushScope(FrameworkElement container, FrameworkElement owner)
    {
        _typedKeys = string.Empty;
        _scopeStack.Push(new ScopeFrame(container, owner));
        ScheduleScopeRefresh();
    }

    private void ScheduleScopeRefresh()
    {
        var generation = ++_scopeRefreshGeneration;
        _overlayCanvas.Children.Clear();
        _targets.Clear();

        if (_ribbon.DispatcherQueue is null)
        {
            RebuildTargets();
            return;
        }

        _ribbon.DispatcherQueue.TryEnqueue(() =>
        {
            if (_isActive && generation == _scopeRefreshGeneration)
            {
                RebuildTargets();
            }
        });
    }

    private void NavigateBack()
    {
        if (_scopeStack.Count <= 1)
        {
            Hide();
            return;
        }

        var current = _scopeStack.Pop();
        switch (current.Owner)
        {
            case RibbonTab:
                CloseMinimizedTabPopup();
                break;
            case IKeyTipedControl keyTipedControl:
                keyTipedControl.OnKeyTipBack();
                break;
        }

        _typedKeys = string.Empty;
        RebuildTargets();
        FocusCurrentScope();
    }

    private void RefreshClosedScopes()
    {
        var changed = false;
        while (_scopeStack.Count > 1 && IsScopeClosed(_scopeStack.Peek()))
        {
            _scopeStack.Pop();
            changed = true;
        }

        if (changed)
        {
            _typedKeys = string.Empty;
            RebuildTargets();
            FocusCurrentScope();
        }
    }

    private bool IsScopeClosed(ScopeFrame frame)
    {
        return frame.Owner switch
        {
            ApplicationMenu applicationMenu => !applicationMenu.IsDropDownOpen,
            StartScreen startScreen => !startScreen.IsOpen,
            Backstage backstage => !backstage.IsOpen,
            IDropDownControl dropDownControl => !dropDownControl.IsDropDownOpen,
            RibbonTab => _ribbon.IsMinimized && !IsMinimizedTabPopupOpen(),
            _ => false,
        };
    }

    private FrameworkElement? GetInitiallyOpenScope()
    {
        if (_ribbon.StartScreen is { IsOpen: true } startScreen
            && !_ribbon.Tabs.Any(tab => IsLogicalDescendantOf(startScreen, tab))
            && IsEligible(startScreen))
        {
            return startScreen;
        }

        if (_ribbon.Menu is Backstage { IsOpen: true } backstage
            && IsEligible(backstage))
        {
            return backstage;
        }

        if (_ribbon.Menu is ApplicationMenu { IsDropDownOpen: true } applicationMenu
            && IsEligible(applicationMenu))
        {
            return applicationMenu;
        }

        return null;
    }

    private void OpenOverlay()
    {
        _overlayPopup.XamlRoot = _ribbon.XamlRoot;
        _overlayCanvas.Width = Math.Max(1, _ribbon.XamlRoot?.Size.Width ?? _ribbon.ActualWidth);
        _overlayCanvas.Height = Math.Max(1, _ribbon.XamlRoot?.Size.Height ?? _ribbon.ActualHeight);
        _overlayPopup.IsOpen = true;
    }

    private void FocusCurrentScope()
    {
        if (_scopeStack.Count == 0)
        {
            return;
        }

        var scope = _scopeStack.Peek().Container;
        if (!ReferenceEquals(scope, _ribbon))
        {
            if (_ribbon.XamlRoot is not { } xamlRoot
                || FocusManager.GetFocusedElement(xamlRoot) is not DependencyObject focused
                || !FocusRoutingHelper.IsDescendantOf(focused, scope))
            {
                FocusRoutingHelper.FocusFirst(scope);
            }

            return;
        }

        var tab = _ribbon.SelectedTab;
        if (tab is null || !IsEligible(tab))
        {
            tab = _ribbon.Tabs.FirstOrDefault(IsEligible);
        }

        tab?.Focus(FocusState.Programmatic);
    }

    private void RebuildTargets()
    {
        if (!_isActive || _scopeStack.Count == 0)
        {
            return;
        }

        _targets.Clear();
        _overlayCanvas.Children.Clear();
        _typedKeys = string.Empty;

        var scope = _scopeStack.Peek();
        var seen = new HashSet<FrameworkElement>();

        if (scope.Container is IKeyTipInformationProvider provider)
        {
            foreach (var information in provider.GetKeyTipInformations(hide: false))
            {
                if (information.IsEnabled
                    && IsInScope(information.AssociatedElement, scope)
                    && seen.Add(information.AssociatedElement))
                {
                    AddTarget(information.AssociatedElement, information.VisualTarget, information.Keys);
                }
            }
        }

        CollectVisualTargets(scope.Container, scope, seen);
        CollectLogicalTargets(scope.Container, scope, seen, new HashSet<DependencyObject>());
        ApplyPrefixFilter();
    }

    private void CollectVisualTargets(
        DependencyObject root,
        ScopeFrame scope,
        HashSet<FrameworkElement> seen)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is FrameworkElement element)
            {
                TryAddTarget(element, element, scope, seen);
            }

            if (child is RibbonGroupBox { State: RibbonGroupBoxState.Collapsed })
            {
                continue;
            }

            CollectVisualTargets(child, scope, seen);
        }
    }

    private void CollectLogicalTargets(
        DependencyObject root,
        ScopeFrame scope,
        HashSet<FrameworkElement> seen,
        HashSet<DependencyObject> visited)
    {
        if (!visited.Add(root))
        {
            return;
        }

        foreach (var child in EnumerateLogicalChildren(root))
        {
            TryAddTarget(child, child, scope, seen);
            CollectVisualTargets(child, scope, seen);
            if (ShouldDescendInto(child, scope))
            {
                CollectLogicalTargets(child, scope, seen, visited);
            }
        }
    }

    private bool ShouldDescendInto(FrameworkElement child, ScopeFrame scope)
    {
        if (ReferenceEquals(scope.Container, _ribbon) && child is RibbonTab)
        {
            return false;
        }

        return child switch
        {
            RibbonGroupBox { State: RibbonGroupBoxState.Collapsed } => false,
            ApplicationMenu { IsDropDownOpen: false } => false,
            Backstage { IsOpen: false } => false,
            StartScreen { IsOpen: false } => false,
            IDropDownControl { IsDropDownOpen: false } => false,
            _ => true,
        };
    }

    private void TryAddTarget(
        FrameworkElement element,
        FrameworkElement visualTarget,
        ScopeFrame scope,
        HashSet<FrameworkElement> seen)
    {
        if (!IsInScope(element, scope)
            || !IsEligible(element)
            || !seen.Add(element))
        {
            return;
        }

        var keys = GetKeys(element);
        if (!string.IsNullOrWhiteSpace(keys))
        {
            AddTarget(element, visualTarget, keys);
        }
    }

    private void AddTarget(FrameworkElement element, FrameworkElement visualTarget, string keys)
    {
        var normalizedKeys = keys.Trim().ToUpperInvariant();
        if (normalizedKeys.Length == 0)
        {
            return;
        }

        var visual = new KeyTipVisual
        {
            Text = normalizedKeys,
            IsHitTestVisible = false,
        };

        _overlayCanvas.Children.Add(visual);
        PositionTarget(element, visualTarget, visual);
        _targets.Add(new TargetEntry(element, normalizedKeys, visual));
    }

    private void PositionTarget(
        FrameworkElement element,
        FrameworkElement visualTarget,
        KeyTipVisual visual)
    {
        double left;
        double top;

        try
        {
            var transform = visualTarget.TransformToVisual(_ribbon.XamlRoot?.Content as UIElement);
            var origin = transform.TransformPoint(new Point(0, 0));
            left = origin.X;
            top = origin.Y;
        }
        catch
        {
            left = visualTarget.ActualOffset.X;
            top = visualTarget.ActualOffset.Y;
        }

        if (KeyTip.GetAutoPlacement(element))
        {
            left += KeyTip.GetHorizontalAlignment(element) switch
            {
                HorizontalAlignment.Left => 0,
                HorizontalAlignment.Right => visualTarget.ActualWidth,
                _ => visualTarget.ActualWidth / 2,
            };

            top += KeyTip.GetVerticalAlignment(element) switch
            {
                VerticalAlignment.Top => 0,
                VerticalAlignment.Center => visualTarget.ActualHeight / 2,
                _ => visualTarget.ActualHeight,
            };
        }

        var margin = KeyTip.GetMargin(element);
        Canvas.SetLeft(visual, Math.Max(0, left + margin.Left - margin.Right - 10));
        Canvas.SetTop(visual, Math.Max(0, top + margin.Top - margin.Bottom - 8));
    }

    private bool IsInScope(FrameworkElement element, ScopeFrame scope)
    {
        if (ReferenceEquals(scope.Container, _ribbon))
        {
            if (element is RibbonTab)
            {
                return true;
            }

            return FocusRoutingHelper.FindAncestor<RibbonTab>(element) is null
                   && !_ribbon.Tabs.Any(tab => IsLogicalDescendantOf(element, tab));
        }

        if (ReferenceEquals(element, scope.Container))
        {
            return false;
        }

        return FocusRoutingHelper.IsDescendantOf(element, scope.Container)
               || IsLogicalDescendantOf(element, scope.Container);
    }

    private static bool IsLogicalDescendantOf(
        FrameworkElement element,
        FrameworkElement ancestor)
    {
        return EnumerateLogicalDescendants(ancestor, new HashSet<DependencyObject>())
            .Any(candidate => ReferenceEquals(candidate, element));
    }

    private static IEnumerable<FrameworkElement> EnumerateLogicalDescendants(
        DependencyObject root,
        HashSet<DependencyObject> visited)
    {
        if (!visited.Add(root))
        {
            yield break;
        }

        foreach (var child in EnumerateLogicalChildren(root))
        {
            yield return child;
            foreach (var visualDescendant in EnumerateVisualDescendants(child))
            {
                yield return visualDescendant;
            }

            foreach (var descendant in EnumerateLogicalDescendants(child, visited))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<FrameworkElement> EnumerateVisualDescendants(
        DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is FrameworkElement element)
            {
                yield return element;
            }

            foreach (var descendant in EnumerateVisualDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<FrameworkElement> EnumerateLogicalChildren(DependencyObject root)
    {
        IEnumerable? items = root switch
        {
            Ribbon ribbon => ribbon.Tabs.Cast<object>()
                .Concat(ribbon.Menu is null ? Array.Empty<object>() : new object[] { ribbon.Menu })
                .Concat(ribbon.StartScreen is null
                    ? Array.Empty<object>()
                    : new object[] { ribbon.StartScreen }),
            RibbonTab tab => tab.Groups,
            RibbonGroupBox group => group.Items,
            ApplicationMenu applicationMenu => applicationMenu.Items
                .Cast<object>()
                .Concat(GetContentChildren(applicationMenu.RightPaneContent, applicationMenu.FooterPaneContent)),
            RibbonDropDownButton dropDownButton => dropDownButton.Items
                .Cast<object>()
                .Concat(
                    dropDownButton.Gallery is null
                        ? Array.Empty<object>()
                        : new object[] { dropDownButton.Gallery }),
            RibbonMenuItem menuItem => menuItem.Items,
            StartScreen startScreen => GetContentChildren(
                startScreen.LeftPaneContent,
                startScreen.Content),
            Backstage backstage => GetContentChildren(backstage.Content),
            BackstageTabControl backstageTabControl => backstageTabControl.Items,
            RibbonGallery gallery => gallery.Items,
            ContentControl contentControl when contentControl.Content is not null
                => new[] { contentControl.Content },
            Panel panel => panel.Children,
            _ => null,
        };

        if (items is null)
        {
            yield break;
        }

        foreach (var item in items)
        {
            if (item is FrameworkElement element)
            {
                yield return element;
            }
        }
    }

    private static IEnumerable<object> GetContentChildren(params object?[] children)
    {
        return children.Where(child => child is not null).Cast<object>();
    }

    private static string? GetKeys(FrameworkElement element)
    {
        var attachedKeys = KeyTip.GetKeys(element);
        if (!string.IsNullOrWhiteSpace(attachedKeys))
        {
            return attachedKeys;
        }

        if (element is IKeyTipedControl keyTipedControl
            && !string.IsNullOrWhiteSpace(keyTipedControl.KeyTip))
        {
            return keyTipedControl.KeyTip;
        }

        return element.GetType()
            .GetProperty("KeyTip", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(element) as string;
    }

    private static bool IsEligible(FrameworkElement element)
    {
        return FocusRoutingHelper.IsEffectivelyVisible(element)
               && FocusRoutingHelper.IsEffectivelyEnabled(element);
    }

    private static void InvokeThroughAutomation(FrameworkElement element)
    {
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
        if (peer is null)
        {
            element.Focus(FocusState.Programmatic);
            return;
        }

        if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProvider)
        {
            invokeProvider.Invoke();
            return;
        }

        if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider)
        {
            expandCollapseProvider.Expand();
            return;
        }

        if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
        {
            toggleProvider.Toggle();
            return;
        }

        if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
        {
            selectionItemProvider.Select();
            return;
        }

        element.Focus(FocusState.Programmatic);
    }

    private bool IsActivationKey(VirtualKey key)
    {
        return KeyTipKeys.Contains(key);
    }

    private static bool IsModifierKey(VirtualKey key)
    {
        return key is VirtualKey.Menu
            or VirtualKey.Control
            or VirtualKey.LeftControl
            or VirtualKey.RightControl
            or VirtualKey.Shift
            or VirtualKey.LeftShift
            or VirtualKey.RightShift
            or VirtualKey.LeftWindows
            or VirtualKey.RightWindows;
    }

    private static bool IsShiftDown()
    {
        return (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                & CoreVirtualKeyStates.Down) != 0;
    }

    private static bool TryGetCharacter(VirtualKey key, out char character)
    {
        if (key is >= VirtualKey.A and <= VirtualKey.Z)
        {
            character = (char)('A' + ((int)key - (int)VirtualKey.A));
            return true;
        }

        if (key is >= VirtualKey.Number0 and <= VirtualKey.Number9)
        {
            character = (char)('0' + ((int)key - (int)VirtualKey.Number0));
            return true;
        }

        if (key is >= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9)
        {
            character = (char)('0' + ((int)key - (int)VirtualKey.NumberPad0));
            return true;
        }

        character = default;
        return false;
    }

    private bool IsMinimizedTabPopupOpen()
    {
        return FindRibbonTabControl() is { IsDropDownOpen: true };
    }

    private void CloseMinimizedTabPopup()
    {
        if (_ribbon.IsMinimized && FindRibbonTabControl() is { } tabControl)
        {
            tabControl.IsDropDownOpen = false;
        }
    }

    private RibbonTabControl? FindRibbonTabControl()
    {
        return FocusRoutingHelper.FindDescendant<RibbonTabControl>(_ribbon);
    }

    private sealed record TargetEntry(
        FrameworkElement Element,
        string Keys,
        KeyTipVisual Visual);

    private sealed record ScopeFrame(
        FrameworkElement Container,
        FrameworkElement? Owner);
}

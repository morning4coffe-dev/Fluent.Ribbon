using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Fluent;

/// <summary>
/// Provides Office-style KeyTip navigation for a <see cref="Ribbon"/>.
/// Pressing (and releasing) <c>Alt</c> — or pressing <c>F10</c> — shows the
/// <see cref="KeyTip"/> access keys registered on ribbon elements via the
/// <see cref="KeyTip.KeysProperty"/> attached property. Typing the keys activates
/// the matching element; typing a tab's keys drills into that tab and shows the
/// keytips of its groups. <c>Esc</c> navigates back one level and <c>Alt</c>/click
/// dismisses the keytips.
/// </summary>
/// <remarks>
/// Uno/WinUI has no <c>AdornerLayer</c>, so the keytip chips are hosted in a single
/// window-spanning <see cref="Popup"/> containing a <see cref="Canvas"/>. Each chip is
/// positioned by transforming its target element into the coordinate space of the
/// ribbon's <see cref="UIElement.XamlRoot"/> content.
/// </remarks>
internal sealed partial class KeyTipService
{
    private readonly Ribbon _ribbon;

    private Popup? _popup;
    private Canvas? _canvas;
    private UIElement? _keyboardRoot;

    private readonly List<KeyTipTarget> _targets = new();
    private string _typed = string.Empty;

    // Scope navigation: each entry is the container whose keytips are currently shown.
    private readonly Stack<KeyTipScope> _scopeStack = new();

    private bool _altDown;
    private bool _altUsedAsModifier;
    private bool _active;

    private readonly KeyEventHandler _keyDownHandler;
    private readonly KeyEventHandler _keyUpHandler;

    private sealed record KeyTipTarget(FrameworkElement Element, string Keys, KeyTipVisual Visual, bool IsTab);

    private enum KeyTipScope
    {
        Root,
        Tab,
    }

    public KeyTipService(Ribbon ribbon)
    {
        _ribbon = ribbon;
        _keyDownHandler = OnRootKeyDown;
        _keyUpHandler = OnRootKeyUp;
    }

    /// <summary>Gets whether keytips are currently being displayed.</summary>
    public bool IsActive => _active;

    /// <summary>
    /// Hooks the keyboard on the ribbon's window root. Safe to call multiple times.
    /// </summary>
    public void Initialize()
    {
        var root = _ribbon.XamlRoot?.Content as UIElement;
        if (root is null || ReferenceEquals(root, _keyboardRoot))
        {
            return;
        }

        DetachKeyboard();

        _keyboardRoot = root;
        _keyboardRoot.AddHandler(UIElement.KeyDownEvent, _keyDownHandler, handledEventsToo: true);
        _keyboardRoot.AddHandler(UIElement.KeyUpEvent, _keyUpHandler, handledEventsToo: true);
    }

    /// <summary>Removes keyboard hooks and dismisses any visible keytips.</summary>
    public void Teardown()
    {
        Hide();
        DetachKeyboard();
    }

    private void DetachKeyboard()
    {
        if (_keyboardRoot is null)
        {
            return;
        }

        _keyboardRoot.RemoveHandler(UIElement.KeyDownEvent, _keyDownHandler);
        _keyboardRoot.RemoveHandler(UIElement.KeyUpEvent, _keyUpHandler);
        _keyboardRoot = null;
    }

    #region Keyboard

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu)
        {
            _altDown = true;
            _altUsedAsModifier = false;
            return;
        }

        if (e.Key == VirtualKey.F10 && !_active)
        {
            Show();
            e.Handled = true;
            return;
        }

        if (_altDown)
        {
            // Alt used together with another key acts as a normal accelerator, not a keytip trigger.
            _altUsedAsModifier = true;
        }

        if (!_active)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Escape:
                PopScope();
                e.Handled = true;
                break;

            case VirtualKey.Back:
                if (_typed.Length > 0)
                {
                    _typed = _typed.Substring(0, _typed.Length - 1);
                    FilterVisuals();
                }
                e.Handled = true;
                break;

            default:
                var ch = KeyToChar(e.Key);
                if (ch is not null)
                {
                    AppendKey(ch.Value);
                    e.Handled = true;
                }
                break;
        }
    }

    private void OnRootKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not (VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu))
        {
            return;
        }

        var wasModifier = _altUsedAsModifier;
        _altDown = false;
        _altUsedAsModifier = false;

        if (wasModifier)
        {
            return;
        }

        if (_active)
        {
            Hide();
        }
        else
        {
            Show();
        }

        e.Handled = true;
    }

    private static char? KeyToChar(VirtualKey key)
    {
        if (key >= VirtualKey.A && key <= VirtualKey.Z)
        {
            return (char)('A' + (key - VirtualKey.A));
        }

        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
        {
            return (char)('0' + (key - VirtualKey.Number0));
        }

        if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
        {
            return (char)('0' + (key - VirtualKey.NumberPad0));
        }

        return null;
    }

    #endregion

    #region Show / Hide

    /// <summary>Enters keytip mode at the root scope.</summary>
    public void Show()
    {
        if (_ribbon.XamlRoot is null)
        {
            return;
        }

        EnsureOverlay();
        if (_popup is null || _canvas is null)
        {
            return;
        }

        _active = true;
        _scopeStack.Clear();
        _scopeStack.Push(KeyTipScope.Root);
        _typed = string.Empty;

        BuildTargets(KeyTipScope.Root, null);
        _popup.IsOpen = _targets.Count > 0;
        _active = _popup.IsOpen;
    }

    /// <summary>Dismisses keytips and resets navigation state.</summary>
    public void Hide()
    {
        _active = false;
        _typed = string.Empty;
        _scopeStack.Clear();
        ClearVisuals();

        if (_popup is not null)
        {
            _popup.IsOpen = false;
        }
    }

    private void PopScope()
    {
        _typed = string.Empty;

        if (_scopeStack.Count > 1)
        {
            _scopeStack.Pop();
            var scope = _scopeStack.Peek();
            BuildTargets(scope, scope == KeyTipScope.Tab ? _ribbon.SelectedTab : null);
        }
        else
        {
            Hide();
        }
    }

    private void EnsureOverlay()
    {
        if (_popup is not null)
        {
            _popup.XamlRoot = _ribbon.XamlRoot;
            if (_canvas is not null)
            {
                SizeCanvasToRoot();
            }
            return;
        }

        _canvas = new Canvas { IsHitTestVisible = false };
        _popup = new Popup
        {
            XamlRoot = _ribbon.XamlRoot,
            Child = _canvas,
            IsLightDismissEnabled = false,
        };

        SizeCanvasToRoot();
    }

    private void SizeCanvasToRoot()
    {
        if (_canvas is null)
        {
            return;
        }

        if (_ribbon.XamlRoot?.Content is FrameworkElement root)
        {
            _canvas.Width = root.ActualWidth;
            _canvas.Height = root.ActualHeight;
        }
    }

    #endregion

    #region Targets

    private void BuildTargets(KeyTipScope scope, RibbonTab? tab)
    {
        ClearVisuals();
        _typed = string.Empty;

        var container = scope == KeyTipScope.Tab && tab is not null ? (DependencyObject)tab : _ribbon;

        foreach (var element in EnumerateKeyTipElements(container))
        {
            var keys = KeyTip.GetKeys(element);
            if (string.IsNullOrEmpty(keys))
            {
                continue;
            }

            var isTab = element is RibbonTab;

            // At the root, show only tabs and chrome (elements not inside a tab's content).
            // Inside a tab, show only that tab's descendants (never the tab headers again).
            if (scope == KeyTipScope.Root && !isTab && IsInsideAnyTab(element))
            {
                continue;
            }

            if (scope == KeyTipScope.Tab && isTab)
            {
                continue;
            }

            AddVisual(element, keys!, isTab);
        }
    }

    private void AddVisual(FrameworkElement element, string keys, bool isTab)
    {
        if (_canvas is null)
        {
            return;
        }

        var visual = new KeyTipVisual { Text = keys.ToUpperInvariant() };
        _canvas.Children.Add(visual);
        PositionVisual(element, visual);

        _targets.Add(new KeyTipTarget(element, keys.ToUpperInvariant(), visual, isTab));
    }

    private void PositionVisual(FrameworkElement element, KeyTipVisual visual)
    {
        if (_ribbon.XamlRoot?.Content is not UIElement root)
        {
            return;
        }

        try
        {
            var transform = element.TransformToVisual(root);
            var origin = transform.TransformPoint(new Point(0, 0));

            var w = element.ActualWidth;
            var h = element.ActualHeight;

            // Rough chip size estimate (measured lazily by the framework afterwards).
            var chipW = Math.Max(16, (visual.Text?.Length ?? 1) * 9 + 8);
            const double chipH = 16;

            double x = KeyTip.GetHorizontalAlignment(element) switch
            {
                HorizontalAlignment.Left => origin.X,
                HorizontalAlignment.Right => origin.X + w - chipW,
                _ => origin.X + (w / 2) - (chipW / 2),
            };

            double y = KeyTip.GetVerticalAlignment(element) switch
            {
                VerticalAlignment.Top => origin.Y - chipH,
                VerticalAlignment.Center => origin.Y + (h / 2) - (chipH / 2),
                _ => origin.Y + h - (chipH / 2),
            };

            var margin = KeyTip.GetMargin(element);
            x += margin.Left - margin.Right;
            y += margin.Top - margin.Bottom;

            Canvas.SetLeft(visual, Math.Max(0, x));
            Canvas.SetTop(visual, Math.Max(0, y));
        }
        catch
        {
            // Positioning can fail if the element isn't arranged yet; leave it at 0,0.
        }
    }

    private void AppendKey(char c)
    {
        var candidate = _typed + char.ToUpperInvariant(c);
        var matches = _targets.Where(t => t.Keys.StartsWith(candidate, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matches.Count == 0)
        {
            // No match — ignore the keystroke (keep current keytips visible).
            return;
        }

        _typed = candidate;

        var exact = matches.FirstOrDefault(t => string.Equals(t.Keys, candidate, StringComparison.OrdinalIgnoreCase));
        if (exact is not null && matches.Count == 1)
        {
            Activate(exact);
            return;
        }

        FilterVisuals();
    }

    private void FilterVisuals()
    {
        foreach (var target in _targets)
        {
            var isMatch = target.Keys.StartsWith(_typed, StringComparison.OrdinalIgnoreCase);
            target.Visual.Visibility = isMatch ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void Activate(KeyTipTarget target)
    {
        if (target.IsTab && target.Element is RibbonTab tab)
        {
            _ribbon.IsMinimized = false;
            _ribbon.SelectedTab = tab;
            _scopeStack.Push(KeyTipScope.Tab);

            // Defer building the tab's keytips until its content is realized/arranged.
            _ = _ribbon.DispatcherQueue?.TryEnqueue(() => BuildTargets(KeyTipScope.Tab, tab));
            return;
        }

        Hide();
        InvokeElement(target.Element);
    }

    private static void InvokeElement(FrameworkElement element)
    {
        try
        {
            var peer = FrameworkElementAutomationPeer.FromElement(element)
                       ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);

            if (peer is null)
            {
                return;
            }

            if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
            {
                invoke.Invoke();
            }
            else if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle)
            {
                toggle.Toggle();
            }
            else if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expand)
            {
                if (expand.ExpandCollapseState == ExpandCollapseState.Expanded)
                {
                    expand.Collapse();
                }
                else
                {
                    expand.Expand();
                }
            }
            else if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider select)
            {
                select.Select();
            }
        }
        catch
        {
            // Automation invocation is best-effort; ignore controls that don't support it.
        }
    }

    private void ClearVisuals()
    {
        _canvas?.Children.Clear();
        _targets.Clear();
    }

    #endregion

    #region Tree helpers

    private static IEnumerable<FrameworkElement> EnumerateKeyTipElements(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is FrameworkElement fe)
            {
                if (fe.Visibility == Visibility.Visible && !string.IsNullOrEmpty(KeyTip.GetKeys(fe)))
                {
                    yield return fe;
                }
            }

            foreach (var descendant in EnumerateKeyTipElements(child))
            {
                yield return descendant;
            }
        }
    }

    private bool IsInsideAnyTab(DependencyObject element)
    {
        var current = VisualTreeHelper.GetParent(element);
        while (current is not null && !ReferenceEquals(current, _ribbon))
        {
            if (current is RibbonTab)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    #endregion
}

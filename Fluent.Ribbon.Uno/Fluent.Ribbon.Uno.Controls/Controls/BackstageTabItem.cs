namespace Fluent;

using System.Collections;

/// <summary>
/// Represents a selectable content item in a <see cref="BackstageTabControl"/>.
/// </summary>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_Header, Type = typeof(FrameworkElement))]
public partial class BackstageTabItem :
    ContentControl,
    IHeaderedControl,
    IKeyTipedControl,
    ILogicalChildSupport
{
    private const string PART_Header = "PART_Header";
    private bool _isPointerOver;
    private bool _isPressed;

    internal FrameworkElement? HeaderContentHost { get; private set; }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(BackstageTabItem),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the icon.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(BackstageTabItem),
            new PropertyMetadata(null));

    /// <inheritdoc />
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(BackstageTabItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>Gets or sets whether the tab is selected.</summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(BackstageTabItem),
            new PropertyMetadata(null));

    /// <inheritdoc />
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(BackstageTabItem),
            new PropertyMetadata(null));

    /// <inheritdoc />
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(BackstageTabItem),
            new PropertyMetadata(null));

    /// <inheritdoc />
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the Uno showcase glyph convenience property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(BackstageTabItem),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets an optional Fluent icon glyph.</summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Initializes a new instance of the <see cref="BackstageTabItem"/> class.</summary>
    public BackstageTabItem()
    {
        DefaultStyleKey = typeof(BackstageTabItem);
        IsTabStop = true;
        IsEnabledChanged += (_, _) => UpdateVisualState();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        HeaderContentHost = GetTemplateChild(PART_Header) as FrameworkElement;
        UpdateVisualState();
    }

    /// <summary>Handles content replacement.</summary>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (IsSelected && TabControlParent is { } tabControl)
        {
            tabControl.SelectedContent = newContent;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isPressed = true;
        UpdateVisualState();
        OnMouseLeftButtonDown(e);
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        UpdateVisualState();
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        _isPressed = false;
        UpdateVisualState();
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPressed = false;
        UpdateVisualState();
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPressed = false;
        UpdateVisualState();
    }

    /// <summary>Handles the WPF-compatible left-button activation hook.</summary>
    protected virtual void OnMouseLeftButtonDown(PointerRoutedEventArgs e)
    {
        if (!e.Handled
            && (ReferenceEquals(e.OriginalSource, this) || IsSelected is false))
        {
            SelectInOwningControl();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        SelectInOwningControl();
    }

    /// <summary>Handles selection.</summary>
    protected virtual void OnSelected(RoutedEventArgs e)
    {
        UpdateVisualState();
        if (FocusState == Microsoft.UI.Xaml.FocusState.Unfocused)
        {
            Focus(FocusState.Programmatic);
        }
    }

    /// <summary>Handles deselection.</summary>
    protected virtual void OnUnselected(RoutedEventArgs e)
    {
        UpdateVisualState();
    }

    /// <inheritdoc />
    public KeyTipPressedResult OnKeyTipPressed()
    {
        SelectInOwningControl();
        var acquiredFocus = Focus(FocusState.Programmatic);
        return new KeyTipPressedResult(
            pressedElementAquiredFocus: acquiredFocus,
            pressedElementOpenedPopup: false);
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
    }

    /// <inheritdoc />
    void ILogicalChildSupport.AddLogicalChild(object child)
    {
    }

    /// <inheritdoc />
    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
    }

    /// <summary>Gets the logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
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

            if (Header is not null)
            {
                yield return Header;
            }
        }
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonBackstageTabItemAutomationPeer(this);

    private StartScreenTabControl? StartScreenTabControlParent =>
        FocusRoutingHelper.FindAncestor<StartScreenTabControl>(this);

    private BackstageTabControl? TabControlParent =>
        StartScreenTabControlParent is null
            ? FocusRoutingHelper.FindAncestor<BackstageTabControl>(this)
            : null;

    private static void OnIsSelectedChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var tabItem = (BackstageTabItem)sender;
        if ((bool)args.NewValue)
        {
            if (tabItem.StartScreenTabControlParent is { } startScreenTabControl)
            {
                startScreenTabControl.SelectTabForAutomation(tabItem);
            }
            else if (tabItem.TabControlParent is { } tabControl
                && ReferenceEquals(tabControl.SelectedItem, tabItem) is false)
            {
                tabControl.SelectTabForAutomation(tabItem);
            }

            tabItem.OnSelected(new RoutedEventArgs());
        }
        else
        {
            tabItem.OnUnselected(new RoutedEventArgs());
        }

        var owningBackstageTabControl = tabItem.TabControlParent;
        var requiresPortableSelectionEvent =
#if WINDOWS
            tabItem.StartScreenTabControlParent is not null;
#else
            true;
#endif
        if (requiresPortableSelectionEvent
            && (owningBackstageTabControl is null
             || owningBackstageTabControl.Items.Contains(tabItem))
            && Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(tabItem)
                is Fluent.Automation.Peers.RibbonBackstageTabItemAutomationPeer peer)
        {
            peer.RaiseIsSelectedChanged(
                (bool)args.OldValue,
                (bool)args.NewValue);
        }
    }

    private void SelectInOwningControl()
    {
        if (StartScreenTabControlParent is { } startScreenTabControl)
        {
            startScreenTabControl.SelectTabForAutomation(this);
            return;
        }

        if (TabControlParent is { } backstageTabControl)
        {
            backstageTabControl.SelectTabForAutomation(this);
        }
    }

    private void UpdateVisualState()
    {
#if WINDOWS
        const string disabledState = "Disabled";
#else
        const string disabledState = "DisabledPortable";
#endif
        var commonState = !IsEnabled
            ? disabledState
            : _isPressed
                ? "Pressed"
                : _isPointerOver
                    ? "PointerOver"
                    : "Normal";
        VisualStateManager.GoToState(
            this,
            IsSelected ? "Selected" : "Unselected",
            true);
        VisualStateManager.GoToState(this, commonState, true);
    }
}

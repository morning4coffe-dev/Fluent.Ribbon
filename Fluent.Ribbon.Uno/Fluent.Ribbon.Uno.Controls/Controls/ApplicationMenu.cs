namespace Fluent;

using System.Collections;
using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents the application menu (File menu) with a two-pane layout.
/// The left pane contains menu items and the right pane shows additional content.
/// This is the classic "big button" file menu that appeared in Office 2007/2010.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_Button, Type = typeof(WinUIButton))]
public partial class ApplicationMenu : DropDownButton
{
    private const string PART_Button = "PART_Button";

    private WinUIButton? _button;
    private ButtonPointerClickFallback? _buttonClickFallback;
    private Flyout? _flyout;
    private Grid? _rootPanel;
    private ItemsControl? _leftPane;
    private Rectangle? _verticalSeparator;
    private Rectangle? _footerSeparator;
    private FrameworkElement? _keyboardRoot;
    private WeakReference<UIElement>? _focusBackup;
    private bool _focusLastItemOnOpen;
    private string? localizedAutomationName;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="RightPaneContent"/> dependency property.</summary>
    public static readonly DependencyProperty RightPaneContentProperty =
        DependencyProperty.Register(
            nameof(RightPaneContent),
            typeof(object),
            typeof(ApplicationMenu),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content displayed in the right pane.
    /// </summary>
    public object? RightPaneContent
    {
        get => GetValue(RightPaneContentProperty);
        set => SetValue(RightPaneContentProperty, value);
    }

    /// <summary>Identifies the <see cref="RightPaneWidth"/> dependency property.</summary>
    public static readonly DependencyProperty RightPaneWidthProperty =
        DependencyProperty.Register(
            nameof(RightPaneWidth),
            typeof(double),
            typeof(ApplicationMenu),
            new PropertyMetadata(300.0));

    /// <summary>
    /// Gets or sets the width of the right pane.
    /// </summary>
    public double RightPaneWidth
    {
        get => (double)GetValue(RightPaneWidthProperty);
        set => SetValue(RightPaneWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="FooterPaneContent"/> dependency property.</summary>
    public static readonly DependencyProperty FooterPaneContentProperty =
        DependencyProperty.Register(
            nameof(FooterPaneContent),
            typeof(object),
            typeof(ApplicationMenu),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content displayed in the footer pane.
    /// </summary>
    public object? FooterPaneContent
    {
        get => GetValue(FooterPaneContentProperty);
        set => SetValue(FooterPaneContentProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMenu"/> class.
    /// </summary>
    public ApplicationMenu()
    {
        DefaultStyleKey = typeof(ApplicationMenu);
        CanAddToQuickAccessToolBar = false;
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedDefaults);
        RegisterPropertyChangedCallback(
            HeaderProperty,
            static (sender, _) => ((ApplicationMenu)sender).RefreshHeaderMetadata());
        RegisterPropertyChangedCallback(
            ItemsControl.ItemsSourceProperty,
            static (sender, _) => ((ApplicationMenu)sender).RefreshItemsSource());
        Unloaded += OnApplicationMenuUnloaded;
    }

    /// <inheritdoc />
    protected override bool UsesDefaultDropDownButtonTemplateBehavior => false;

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_button is not null)
        {
            _button.Click -= OnButtonClick;
            _buttonClickFallback?.Dispose();
            _buttonClickFallback = null;
        }

        _button = GetTemplateChild(PART_Button) as WinUIButton;

        if (_button is not null)
        {
            _button.IsTabStop = false;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                _button,
                Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            _button.Click += OnButtonClick;
            _buttonClickFallback = ButtonPointerClickFallback.Attach(
                _button,
                () =>
                {
                    Focus(FocusState.Pointer);
                    ToggleDropDown();
                });
            var ownerId = Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(this);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                _button,
                $"{(string.IsNullOrWhiteSpace(ownerId) ? nameof(ApplicationMenu) : ownerId)}.Button");

            RefreshButtonName();
        }
    }

    private void RefreshLocalizedDefaults()
    {
        var localization = RibbonLocalization.Current.Localization;
        Fluent.Automation.Peers.AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
            this,
            HeaderProperty,
            localization.BackstageButtonText);
        Fluent.Automation.Peers.AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
            this,
            KeyTipProperty,
            localization.BackstageButtonKeyTip);
        RefreshButtonName();
        RefreshOuterAutomationName();
    }

    private void RefreshButtonName()
    {
        if (_button is null)
        {
            return;
        }

        var name = Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(Header);
        Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
            _button,
            string.IsNullOrWhiteSpace(name)
                ? RibbonLocalization.Current.Localization.ApplicationMenuName
                : name);
    }

    private void RefreshHeaderMetadata()
    {
        RefreshButtonName();
        RefreshOuterAutomationName();
    }

    private void RefreshOuterAutomationName()
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

    #endregion

    #region Methods

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        Focus(FocusState.Pointer);
        ToggleDropDown();
    }

    private void ToggleDropDown()
    {
        if (IsDropDownOpen)
        {
            Close();
        }
        else
        {
            ShowDropDown();
        }
    }

    /// <summary>
    /// Opens the application menu.
    /// </summary>
    public void Open()
    {
        ShowDropDown();
    }

    /// <summary>
    /// Closes the application menu.
    /// </summary>
    public void Close()
    {
        _flyout?.Hide();
        IsDropDownOpen = false;
    }

    /// <inheritdoc />
    public override void CloseDropDown() => Close();

    private protected override void HideDropDownPopup() => _flyout?.Hide();

    private protected override void ShowDropDown()
    {
        if (IsDropDownOpen)
        {
            return;
        }

        // Build the flyout + its content ONCE and reuse it. Rebuilding the panes on
        // every open — which reparents the menu Items out of the previous (now torn
        // down) flyout panel and into a fresh one — corrupts the items' native peers
        // on WinUI3, so the left pane came up empty on the second open. Building once
        // keeps every item in a single, stable visual parent for the control's life.
        if (_flyout is null)
        {
            BuildFlyout();
        }

        // The menu surface is theme-aware, so refresh the theme brushes (and the
        // opaque presenter style) each time — cheap, and it reparents nothing.
        ApplyThemeBrushes();

        _focusBackup = FocusRoutingHelper.CaptureFocusedElement(this);
        IsDropDownOpen = true;
        var requestedFlyout = _flyout;
        FlyoutShowHelper.ShowDeferred(
            requestedFlyout,
            (FrameworkElement?)_button ?? this,
            () => IsDropDownOpen && ReferenceEquals(_flyout, requestedFlyout));
    }

    private void BuildFlyout()
    {
        ConfigureMenuItemOwners();

        // Build the two-pane dropdown content
        var rootPanel = new Grid { MinWidth = 400 };
        rootPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var mainPanel = new Grid();
        mainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left pane — menu items
        var leftPane = new ItemsControl
        {
            MinWidth = 200,
            ItemsSource = ItemsSource ?? Items,
            ItemTemplate = ItemTemplate,
            ItemTemplateSelector = ItemTemplateSelector,
            ItemContainerStyle = ItemContainerStyle,
            ItemsPanel = ItemsPanel,
            FlowDirection = FlowDirection,
        };
        _leftPane = leftPane;

        Grid.SetColumn(leftPane, 0);
        mainPanel.Children.Add(leftPane);

        // Right pane — additional content
        if (RightPaneContent is not null)
        {
            mainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) }); // separator
            mainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(RightPaneWidth) });

            _verticalSeparator = new Rectangle
            {
                Width = 1,
                Fill = MenuSeparatorBrush,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(_verticalSeparator, 1);
            mainPanel.Children.Add(_verticalSeparator);

            var rightPane = new ContentPresenter
            {
                Content = RightPaneContent,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(rightPane, 2);
            mainPanel.Children.Add(rightPane);
        }

        Grid.SetRow(mainPanel, 0);
        rootPanel.Children.Add(mainPanel);

        // Footer pane
        if (FooterPaneContent is not null)
        {
            _footerSeparator = new Rectangle
            {
                Height = 1,
                Fill = MenuSeparatorBrush,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 0),
            };
            Grid.SetRow(_footerSeparator, 0);
            rootPanel.Children.Add(_footerSeparator);

            var footer = new ContentPresenter
            {
                Content = FooterPaneContent,
                Padding = new Thickness(12, 8, 12, 8),
            };
            Grid.SetRow(footer, 1);
            rootPanel.Children.Add(footer);
        }

        _rootPanel = rootPanel;

        // Escape must dismiss the menu even when focus is inside the flyout. The keyboard
        // root handler never sees those key events because the flyout content lives in a
        // separate popup visual tree, so handle Escape on the flyout content itself.
        rootPanel.KeyDown += OnFlyoutContentKeyDown;

        _flyout = new Flyout { Placement = FlyoutPlacementMode.Bottom, Content = rootPanel };
        _flyout.Opened += OnFlyoutOpened;
        _flyout.Closed += OnFlyoutClosed;
    }

    private void RefreshItemsSource()
    {
        ConfigureMenuItemOwners();
        if (_leftPane is not null)
        {
            _leftPane.ItemsSource = ItemsSource ?? Items;
            _leftPane.FlowDirection = FlowDirection;
        }
    }

    private void ConfigureMenuItemOwners()
    {
        var items = ItemsSource as System.Collections.IEnumerable ?? Items;
        foreach (var item in items.Cast<object>().OfType<IDropDownItemOwner>())
        {
            item.SetDropDownOwner(this);
        }
    }

    private void OnFlyoutOpened(object? sender, object args)
    {
        RaiseDropDownOpened();
        AttachKeyboardRoot();

        if (_leftPane is null)
        {
            return;
        }

        if (_focusLastItemOnOpen)
        {
            FocusLast(_leftPane);
        }
        else
        {
            FocusRoutingHelper.FocusFirst(_leftPane);
        }

        _focusLastItemOnOpen = false;
    }

    private void OnFlyoutClosed(object? sender, object args)
    {
        IsDropDownOpen = false;
        RaiseDropDownClosed();
        DetachKeyboardRoot();

        var focused = XamlRoot is { } xamlRoot
            ? FocusManager.GetFocusedElement(xamlRoot) as DependencyObject
            : null;
        var shouldRestore = _rootPanel is not null
                            && (focused is null
                                || FocusRoutingHelper.IsDescendantOf(focused, _rootPanel));
        if (shouldRestore)
        {
            if (Focus(FocusState.Programmatic))
            {
                _focusBackup = null;
            }
            else
            {
                FocusRoutingHelper.RestoreFocus(ref _focusBackup);
            }
        }
        else
        {
            _focusBackup = null;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        if (args.Handled)
        {
            base.OnKeyDown(args);
            return;
        }

        switch (args.Key)
        {
            case Windows.System.VirtualKey.Down when !IsDropDownOpen:
                _focusLastItemOnOpen = false;
                Open();
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.Up when !IsDropDownOpen:
                _focusLastItemOnOpen = true;
                Open();
                args.Handled = true;
                break;
            case Windows.System.VirtualKey.Enter:
            case Windows.System.VirtualKey.Space:
                if (IsDropDownOpen)
                {
                    Close();
                }
                else
                {
                    Open();
                }

                args.Handled = true;
                break;
            case Windows.System.VirtualKey.Escape when IsDropDownOpen:
                Close();
                args.Handled = true;
                break;
        }

        base.OnKeyDown(args);
    }

    private void AttachKeyboardRoot()
    {
        if (_keyboardRoot is not null
            || XamlRoot?.Content is not FrameworkElement root)
        {
            return;
        }

        _keyboardRoot = root;
        root.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyboardRootKeyDown), true);
    }

    private void DetachKeyboardRoot()
    {
        if (_keyboardRoot is null)
        {
            return;
        }

        _keyboardRoot.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyboardRootKeyDown));
        _keyboardRoot = null;
    }

    private void OnFlyoutContentKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (!args.Handled
            && IsDropDownOpen
            && args.Key == Windows.System.VirtualKey.Escape)
        {
            Close();
            args.Handled = true;
        }
    }

    private void OnKeyboardRootKeyDown(object sender, KeyRoutedEventArgs args)
    {
        var ribbon = FocusRoutingHelper.FindAncestor<Ribbon>(this);
        if (ribbon is null && XamlRoot?.Content is DependencyObject root)
        {
            ribbon = FocusRoutingHelper.FindDescendant<Ribbon>(root);
        }

        if (ribbon?.AreAnyKeyTipsVisible == true)
        {
            return;
        }

        if (!args.Handled
            && IsDropDownOpen
            && args.Key == Windows.System.VirtualKey.Escape)
        {
            Close();
            args.Handled = true;
        }
    }

    private void OnApplicationMenuUnloaded(object sender, RoutedEventArgs args)
    {
        DetachKeyboardRoot();
        _focusBackup = null;
        if (IsDropDownOpen)
        {
            Close();
        }
    }

    private static bool FocusLast(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = count - 1; index >= 0; index--)
        {
            if (FocusLast(VisualTreeHelper.GetChild(root, index)))
            {
                return true;
            }
        }

        return root is Control
        {
            IsTabStop: true,
            IsEnabled: true,
            Visibility: Visibility.Visible,
        } control && control.Focus(FocusState.Programmatic);
    }

    /// <summary>Application menus cannot be added to quick access.</summary>
    public override FrameworkElement? CreateQuickAccessItem()
    {
        throw new NotImplementedException();
    }

    /// <summary>Handles context-menu opening.</summary>
    protected virtual void OnContextMenuOpening(ContextMenuEventArgs e)
    {
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected override IEnumerator LogicalChildren
    {
        get
        {
            var baseEnumerator = base.LogicalChildren;
            while (baseEnumerator.MoveNext())
            {
                yield return baseEnumerator.Current;
            }

            if (RightPaneContent is not null)
            {
                yield return RightPaneContent;
            }

            if (FooterPaneContent is not null)
            {
                yield return FooterPaneContent;
            }
        }
    }

    /// <inheritdoc />
    public override KeyTipPressedResult OnKeyTipPressed()
    {
        Open();
        return new KeyTipPressedResult(
            pressedElementAquiredFocus: false,
            pressedElementOpenedPopup: true);
    }

    private void ApplyThemeBrushes()
    {
        if (_rootPanel is not null)
        {
            _rootPanel.Background = MenuBackgroundBrush;
        }

        if (_verticalSeparator is not null)
        {
            _verticalSeparator.Fill = MenuSeparatorBrush;
        }

        if (_footerSeparator is not null)
        {
            _footerSeparator.Fill = MenuSeparatorBrush;
        }

        // Force an opaque presenter (also picks up the current theme). Without this the
        // default acrylic FlyoutPresenter renders the colorful ribbon behind it as a smeared blur.
        var presenterStyle = new Style(typeof(FlyoutPresenter));
        presenterStyle.Setters.Add(new Setter(Control.BackgroundProperty, MenuBackgroundBrush));
        presenterStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        presenterStyle.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(8)));
        presenterStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        presenterStyle.Setters.Add(new Setter(Control.BorderBrushProperty, MenuSeparatorBrush));
        presenterStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 900.0));
        presenterStyle.Setters.Add(new Setter(ScrollViewer.HorizontalScrollModeProperty, ScrollMode.Disabled));
        presenterStyle.Setters.Add(new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled));

        if (_flyout is not null)
        {
            _flyout.FlyoutPresenterStyle = presenterStyle;
        }
    }

    // Opaque, theme-aware brushes for the menu surface (mirrors RibbonContentBrush / RibbonBorderBrush).
    private Brush MenuBackgroundBrush =>
        new SolidColorBrush(ActualTheme == ElementTheme.Dark
            ? Windows.UI.Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E)
            : Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));

    private Brush MenuSeparatorBrush =>
        new SolidColorBrush(ActualTheme == ElementTheme.Dark
            ? Windows.UI.Color.FromArgb(0xFF, 0x40, 0x40, 0x40)
            : Windows.UI.Color.FromArgb(0xFF, 0xD4, 0xD4, 0xD4));

    #endregion
}

namespace Fluent;

/// <summary>
/// Represents the application menu (File menu) with a two-pane layout.
/// The left pane contains menu items and the right pane shows additional content.
/// This is the classic "big button" file menu that appeared in Office 2007/2010.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_Button, Type = typeof(Button))]
public partial class ApplicationMenu : Control
{
    private const string PART_Button = "PART_Button";

    private Button? _button;
    private Flyout? _flyout;
    private Grid? _rootPanel;
    private Rectangle? _verticalSeparator;
    private Rectangle? _footerSeparator;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(ApplicationMenu),
            new PropertyMetadata("File"));

    /// <summary>
    /// Gets or sets the header text displayed on the application menu button.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(ApplicationMenu),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of menu items in the left pane.
    /// </summary>
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

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

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(ApplicationMenu),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the dropdown is open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(ApplicationMenu),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph for the menu button.
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMenu"/> class.
    /// </summary>
    public ApplicationMenu()
    {
        DefaultStyleKey = typeof(ApplicationMenu);
        Items = new ObservableCollection<UIElement>();
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_button is not null)
        {
            _button.Click -= OnButtonClick;
        }

        _button = GetTemplateChild(PART_Button) as Button;

        if (_button is not null)
        {
            _button.Click += OnButtonClick;
        }
    }

    #endregion

    #region Methods

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        ShowDropDown();
    }

    private void ShowDropDown()
    {
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

        IsDropDownOpen = true;
        _flyout!.ShowAt((FrameworkElement?)_button ?? this);
    }

    private void BuildFlyout()
    {
        // Build the two-pane dropdown content
        var rootPanel = new Grid { MinWidth = 400 };
        rootPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var mainPanel = new Grid();
        mainPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left pane — menu items
        var leftPane = new StackPanel { MinWidth = 200 };
        foreach (var item in Items)
        {
            // Detach from any prior parent (e.g. the logical XAML parent) exactly once.
            if (item is FrameworkElement fe && fe.Parent is Panel panel)
            {
                panel.Children.Remove(item);
            }

            leftPane.Children.Add(item);
        }

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

        _flyout = new Flyout { Placement = FlyoutPlacementMode.Bottom, Content = rootPanel };
        _flyout.Closed += (s, e) => IsDropDownOpen = false;
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

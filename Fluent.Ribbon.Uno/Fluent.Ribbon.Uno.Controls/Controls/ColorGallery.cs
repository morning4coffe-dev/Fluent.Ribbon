namespace Fluent;

/// <summary>
/// A gallery control for color selection with automatic/no-color/standard/theme colors.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// This is a simplified version; the WPF original supports theme colors, standard colors,
/// recent colors, and a "More Colors" dialog.
/// </remarks>
[ContentProperty(Name = nameof(ThemeColors))]
[TemplatePart(Name = PART_AutomaticButton, Type = typeof(Button))]
[TemplatePart(Name = PART_NoColorButton, Type = typeof(Button))]
[TemplatePart(Name = PART_MoreColorsButton, Type = typeof(Button))]
[TemplatePart(Name = PART_ThemeColorsSection, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PART_ThemeColorsPanel, Type = typeof(Panel))]
[TemplatePart(Name = PART_StandardColorsPanel, Type = typeof(Panel))]
[TemplatePart(Name = PART_RecentColorsSection, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PART_RecentColorsPanel, Type = typeof(Panel))]
public partial class ColorGallery : Control
{
    private const string PART_AutomaticButton = "PART_AutomaticButton";
    private const string PART_NoColorButton = "PART_NoColorButton";
    private const string PART_MoreColorsButton = "PART_MoreColorsButton";
    private const string PART_ThemeColorsSection = "PART_ThemeColorsSection";
    private const string PART_ThemeColorsPanel = "PART_ThemeColorsPanel";
    private const string PART_StandardColorsPanel = "PART_StandardColorsPanel";
    private const string PART_RecentColorsSection = "PART_RecentColorsSection";
    private const string PART_RecentColorsPanel = "PART_RecentColorsPanel";

    private const int MaxRecentColors = 10;

    private Panel? _themeColorsPanel;
    private Panel? _standardColorsPanel;
    private Panel? _recentColorsPanel;
    private FrameworkElement? _themeColorsSection;
    private FrameworkElement? _recentColorsSection;
    private Button? _automaticButton;
    private Button? _noColorButton;
    private Button? _moreColorsButton;

    private static ObservableCollection<Windows.UI.Color>? _recentColors;

    /// <summary>
    /// Gets the shared collection of recently selected colors.
    /// </summary>
    public static ObservableCollection<Windows.UI.Color> RecentColors =>
        _recentColors ??= new ObservableCollection<Windows.UI.Color>();

    #region Dependency Properties

    /// <summary>Identifies the <see cref="SelectedColor"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register(
            nameof(SelectedColor),
            typeof(Windows.UI.Color?),
            typeof(ColorGallery),
            new PropertyMetadata(null, OnSelectedColorChanged));

    /// <summary>
    /// Gets or sets the currently selected color.
    /// </summary>
    public Windows.UI.Color? SelectedColor
    {
        get => (Windows.UI.Color?)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    /// <summary>Identifies the <see cref="ThemeColors"/> dependency property.</summary>
    public static readonly DependencyProperty ThemeColorsProperty =
        DependencyProperty.Register(
            nameof(ThemeColors),
            typeof(ObservableCollection<Windows.UI.Color>),
            typeof(ColorGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of theme colors.
    /// </summary>
    public ObservableCollection<Windows.UI.Color> ThemeColors
    {
        get => (ObservableCollection<Windows.UI.Color>)GetValue(ThemeColorsProperty);
        private set => SetValue(ThemeColorsProperty, value);
    }

    /// <summary>Identifies the <see cref="StandardColors"/> dependency property.</summary>
    public static readonly DependencyProperty StandardColorsProperty =
        DependencyProperty.Register(
            nameof(StandardColors),
            typeof(ObservableCollection<Windows.UI.Color>),
            typeof(ColorGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of standard colors.
    /// </summary>
    public ObservableCollection<Windows.UI.Color> StandardColors
    {
        get => (ObservableCollection<Windows.UI.Color>)GetValue(StandardColorsProperty);
        private set => SetValue(StandardColorsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsAutomaticColorButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsAutomaticColorButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsAutomaticColorButtonVisible),
            typeof(bool),
            typeof(ColorGallery),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the "Automatic" color button is visible.
    /// </summary>
    public bool IsAutomaticColorButtonVisible
    {
        get => (bool)GetValue(IsAutomaticColorButtonVisibleProperty);
        set => SetValue(IsAutomaticColorButtonVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="IsNoColorButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsNoColorButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsNoColorButtonVisible),
            typeof(bool),
            typeof(ColorGallery),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the "No Color" button is visible.
    /// </summary>
    public bool IsNoColorButtonVisible
    {
        get => (bool)GetValue(IsNoColorButtonVisibleProperty);
        set => SetValue(IsNoColorButtonVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="IsMoreColorsButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsMoreColorsButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsMoreColorsButtonVisible),
            typeof(bool),
            typeof(ColorGallery),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the "More Colors" button is visible.
    /// </summary>
    public bool IsMoreColorsButtonVisible
    {
        get => (bool)GetValue(IsMoreColorsButtonVisibleProperty);
        set => SetValue(IsMoreColorsButtonVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="Columns"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(
            nameof(Columns),
            typeof(int),
            typeof(ColorGallery),
            new PropertyMetadata(10));

    /// <summary>
    /// Gets or sets the number of color columns.
    /// </summary>
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>Identifies the <see cref="Mode"/> dependency property.</summary>
    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(
            nameof(Mode),
            typeof(ColorGalleryMode),
            typeof(ColorGallery),
            new PropertyMetadata(ColorGalleryMode.StandardColors, OnModeChanged));

    /// <summary>
    /// Gets or sets the color layout mode.
    /// </summary>
    public ColorGalleryMode Mode
    {
        get => (ColorGalleryMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>Identifies the <see cref="ChipWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ChipWidthProperty =
        DependencyProperty.Register(
            nameof(ChipWidth),
            typeof(double),
            typeof(ColorGallery),
            new PropertyMetadata(13.0, OnChipSizeChanged));

    /// <summary>
    /// Gets or sets the width of each color chip.
    /// </summary>
    public double ChipWidth
    {
        get => (double)GetValue(ChipWidthProperty);
        set => SetValue(ChipWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ChipHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ChipHeightProperty =
        DependencyProperty.Register(
            nameof(ChipHeight),
            typeof(double),
            typeof(ColorGallery),
            new PropertyMetadata(13.0, OnChipSizeChanged));

    /// <summary>
    /// Gets or sets the height of each color chip.
    /// </summary>
    public double ChipHeight
    {
        get => (double)GetValue(ChipHeightProperty);
        set => SetValue(ChipHeightProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the selected color changes.
    /// </summary>
    public event EventHandler<Windows.UI.Color?>? SelectedColorChanged;

    /// <summary>
    /// Occurs when the "More Colors" button is clicked.
    /// </summary>
    public event EventHandler? MoreColorsRequested;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorGallery"/> class.
    /// </summary>
    public ColorGallery()
    {
        DefaultStyleKey = typeof(ColorGallery);
        ThemeColors = new ObservableCollection<Windows.UI.Color>();
        StandardColors = CreateDefaultStandardColors();

        ThemeColors.CollectionChanged += OnColorsCollectionChanged;
        StandardColors.CollectionChanged += OnColorsCollectionChanged;
        RecentColors.CollectionChanged += OnColorsCollectionChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_automaticButton is not null)
        {
            _automaticButton.Click -= OnAutomaticButtonClick;
        }

        if (_noColorButton is not null)
        {
            _noColorButton.Click -= OnNoColorButtonClick;
        }

        if (_moreColorsButton is not null)
        {
            _moreColorsButton.Click -= OnMoreColorsButtonClick;
        }

        _themeColorsSection = GetTemplateChild(PART_ThemeColorsSection) as FrameworkElement;
        _themeColorsPanel = GetTemplateChild(PART_ThemeColorsPanel) as Panel;
        _standardColorsPanel = GetTemplateChild(PART_StandardColorsPanel) as Panel;
        _recentColorsSection = GetTemplateChild(PART_RecentColorsSection) as FrameworkElement;
        _recentColorsPanel = GetTemplateChild(PART_RecentColorsPanel) as Panel;
        _automaticButton = GetTemplateChild(PART_AutomaticButton) as Button;
        _noColorButton = GetTemplateChild(PART_NoColorButton) as Button;
        _moreColorsButton = GetTemplateChild(PART_MoreColorsButton) as Button;

        if (_automaticButton is not null)
        {
            _automaticButton.Click += OnAutomaticButtonClick;
        }

        if (_noColorButton is not null)
        {
            _noColorButton.Click += OnNoColorButtonClick;
        }

        if (_moreColorsButton is not null)
        {
            _moreColorsButton.Click += OnMoreColorsButtonClick;
        }

        RebuildSwatches();
    }

    #endregion

    #region Methods

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorGallery gallery)
        {
            if (e.NewValue is Windows.UI.Color color)
            {
                gallery.TrackRecentColor(color);
            }

            gallery.SelectedColorChanged?.Invoke(gallery, (Windows.UI.Color?)e.NewValue);
        }
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorGallery gallery)
        {
            gallery.RebuildSwatches();
        }
    }

    private static void OnChipSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorGallery gallery)
        {
            gallery.RebuildSwatches();
        }
    }

    private void TrackRecentColor(Windows.UI.Color color)
    {
        var recent = RecentColors;
        var existing = -1;
        for (var i = 0; i < recent.Count; i++)
        {
            if (recent[i].Equals(color))
            {
                existing = i;
                break;
            }
        }

        if (existing == 0)
        {
            return;
        }

        if (existing > 0)
        {
            recent.RemoveAt(existing);
        }

        recent.Insert(0, color);

        while (recent.Count > MaxRecentColors)
        {
            recent.RemoveAt(recent.Count - 1);
        }
    }

    private void OnColorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildSwatches();
    }

    private void RebuildSwatches()
    {
        var showThemeColors = Mode == ColorGalleryMode.ThemeColors;
        var standardColors = Mode == ColorGalleryMode.HighlightColors
            ? CreateHighlightColors()
            : StandardColors;

        BuildSwatches(_themeColorsPanel, ThemeColors);
        BuildSwatches(_standardColorsPanel, standardColors);
        BuildSwatches(_recentColorsPanel, RecentColors);

        if (_themeColorsSection is not null)
        {
            _themeColorsSection.Visibility = showThemeColors && ThemeColors is { Count: > 0 }
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (_recentColorsSection is not null)
        {
            _recentColorsSection.Visibility = RecentColors is { Count: > 0 }
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void BuildSwatches(Panel? host, IReadOnlyList<Windows.UI.Color>? colors)
    {
        if (host is null)
        {
            return;
        }

        host.Children.Clear();

        if (colors is null || colors.Count == 0)
        {
            return;
        }

        var columns = Math.Max(1, Columns);
        var chipWidth = ChipWidth > 0 ? ChipWidth : 13.0;
        var chipHeight = ChipHeight > 0 ? ChipHeight : 13.0;
        StackPanel? row = null;

        for (var i = 0; i < colors.Count; i++)
        {
            if (i % columns == 0)
            {
                row = new StackPanel { Orientation = Orientation.Horizontal };
                host.Children.Add(row);
            }

            var color = colors[i];
            var swatch = new Button
            {
                Width = chipWidth,
                Height = chipHeight,
                MinWidth = 0,
                MinHeight = 0,
                Margin = new Thickness(1),
                Padding = new Thickness(0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)),
                Background = new SolidColorBrush(color),
                Tag = color,
            };

            ToolTipService.SetToolTip(swatch, color.ToString());
            swatch.Click += OnSwatchClick;
            row!.Children.Add(swatch);
        }
    }

    private void OnSwatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Windows.UI.Color color })
        {
            SelectedColor = color;
        }
    }

    private void OnAutomaticButtonClick(object sender, RoutedEventArgs e)
    {
        // "Automatic" maps to the default automatic color (black).
        SelectedColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
    }

    private void OnNoColorButtonClick(object sender, RoutedEventArgs e)
    {
        SelectedColor = null;
    }

    private void OnMoreColorsButtonClick(object sender, RoutedEventArgs e)
    {
        MoreColorsRequested?.Invoke(this, EventArgs.Empty);
    }

    private static ObservableCollection<Windows.UI.Color> CreateDefaultStandardColors()
    {
        return new ObservableCollection<Windows.UI.Color>
        {
            Windows.UI.Color.FromArgb(255, 192, 0, 0),     // Dark Red
            Windows.UI.Color.FromArgb(255, 255, 0, 0),     // Red
            Windows.UI.Color.FromArgb(255, 255, 192, 0),   // Orange
            Windows.UI.Color.FromArgb(255, 255, 255, 0),   // Yellow
            Windows.UI.Color.FromArgb(255, 146, 208, 80),  // Light Green
            Windows.UI.Color.FromArgb(255, 0, 176, 80),    // Green
            Windows.UI.Color.FromArgb(255, 0, 176, 240),   // Light Blue
            Windows.UI.Color.FromArgb(255, 0, 112, 192),   // Blue
            Windows.UI.Color.FromArgb(255, 0, 32, 96),     // Dark Blue
            Windows.UI.Color.FromArgb(255, 112, 48, 160),  // Purple
        };
    }

    private static IReadOnlyList<Windows.UI.Color> CreateHighlightColors()
    {
        return new[]
        {
            Windows.UI.Color.FromArgb(255, 255, 255, 0),   // Yellow
            Windows.UI.Color.FromArgb(255, 0, 255, 0),     // Bright Green
            Windows.UI.Color.FromArgb(255, 0, 255, 255),   // Turquoise
            Windows.UI.Color.FromArgb(255, 255, 0, 255),   // Pink
            Windows.UI.Color.FromArgb(255, 0, 0, 255),     // Blue
            Windows.UI.Color.FromArgb(255, 255, 0, 0),     // Red
            Windows.UI.Color.FromArgb(255, 0, 0, 128),     // Dark Blue
            Windows.UI.Color.FromArgb(255, 0, 128, 128),   // Teal
            Windows.UI.Color.FromArgb(255, 0, 128, 0),     // Green
            Windows.UI.Color.FromArgb(255, 128, 0, 128),   // Violet
            Windows.UI.Color.FromArgb(255, 128, 0, 0),     // Dark Red
            Windows.UI.Color.FromArgb(255, 128, 128, 0),   // Dark Yellow
            Windows.UI.Color.FromArgb(255, 128, 128, 128), // Gray 50%
            Windows.UI.Color.FromArgb(255, 192, 192, 192), // Gray 25%
            Windows.UI.Color.FromArgb(255, 0, 0, 0),       // Black
        };
    }

    #endregion
}

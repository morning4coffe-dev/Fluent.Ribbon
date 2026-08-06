namespace Fluent;

using Fluent.Helpers;
using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

using Microsoft.UI.Xaml.Automation;

/// <summary>
/// A gallery control for color selection with automatic/no-color/standard/theme colors.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// This is a simplified version; the WPF original supports theme colors, standard colors,
/// recent colors, and a "More Colors" dialog.
/// </remarks>
[ContentProperty(Name = nameof(ThemeColors))]
[TemplatePart(Name = PART_AutomaticButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_NoColorButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_MoreColorsButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_ThemeColorsSection, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PART_ThemeColorsHeader, Type = typeof(TextBlock))]
[TemplatePart(Name = PART_ThemeColorsPanel, Type = typeof(Panel))]
[TemplatePart(Name = PART_StandardColorsHeader, Type = typeof(TextBlock))]
[TemplatePart(Name = PART_StandardColorsPanel, Type = typeof(Panel))]
[TemplatePart(Name = PART_RecentColorsSection, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PART_RecentColorsHeader, Type = typeof(TextBlock))]
[TemplatePart(Name = PART_RecentColorsPanel, Type = typeof(Panel))]
public partial class ColorGallery : Control
{
    private const string PART_AutomaticButton = "PART_AutomaticButton";
    private const string PART_NoColorButton = "PART_NoColorButton";
    private const string PART_MoreColorsButton = "PART_MoreColorsButton";
    private const string PART_ThemeColorsSection = "PART_ThemeColorsSection";
    private const string PART_ThemeColorsHeader = "PART_ThemeColorsHeader";
    private const string PART_ThemeColorsPanel = "PART_ThemeColorsPanel";
    private const string PART_StandardColorsHeader = "PART_StandardColorsHeader";
    private const string PART_StandardColorsPanel = "PART_StandardColorsPanel";
    private const string PART_RecentColorsSection = "PART_RecentColorsSection";
    private const string PART_RecentColorsHeader = "PART_RecentColorsHeader";
    private const string PART_RecentColorsPanel = "PART_RecentColorsPanel";

    private Panel? _themeColorsPanel;
    private Panel? _standardColorsPanel;
    private Panel? _recentColorsPanel;
    private FrameworkElement? _themeColorsSection;
    private FrameworkElement? _recentColorsSection;
    private TextBlock? _themeColorsHeader;
    private TextBlock? _standardColorsHeader;
    private TextBlock? _recentColorsHeader;
    private WinUIButton? _automaticButton;
    private WinUIButton? _noColorButton;
    private WinUIButton? _moreColorsButton;

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

    /// <summary>Identifies the additive mutable standard-colors collection.</summary>
    public static readonly DependencyProperty MutableStandardColorsProperty =
        DependencyProperty.Register(
            nameof(MutableStandardColors),
            typeof(ObservableCollection<Windows.UI.Color>),
            typeof(ColorGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the mutable Uno standard-colors collection.
    /// </summary>
    /// <remarks>
    /// WPF source compatibility requires <see cref="StandardColors"/> to remain a static
    /// <see cref="Windows.UI.Color"/> array. Use this collection when an application needs
    /// to customize the standard-mode palette at runtime.
    /// </remarks>
    public ObservableCollection<Windows.UI.Color> MutableStandardColors
    {
        get => (ObservableCollection<Windows.UI.Color>)GetValue(MutableStandardColorsProperty);
        private set => SetValue(MutableStandardColorsProperty, value);
    }

    /// <summary>Gets the mutable Uno standard-colors collection.</summary>
    public ObservableCollection<Windows.UI.Color> StandardColorsCollection => MutableStandardColors;

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
        set => SetValue(ColumnsProperty, Math.Max(1, value));
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
        set => SetValue(ChipWidthProperty, Math.Max(0, value));
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
        set => SetValue(ChipHeightProperty, Math.Max(0, value));
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the selected color changes.
    /// </summary>
    public event RoutedEventHandler? SelectedColorChanged;

    /// <summary>
    /// Occurs when the selected color changes and supplies the new nullable color value.
    /// </summary>
    /// <remarks>
    /// This additive Uno event preserves the value-carrying behavior formerly exposed by
    /// <c>SelectedColorChanged</c>; the WPF-compatible event now uses
    /// <see cref="RoutedEventHandler"/>.
    /// </remarks>
    public event EventHandler<Windows.UI.Color?>? SelectedColorValueChanged;

    /// <summary>
    /// Occurs when the "More Colors" button is clicked.
    /// </summary>
    public event EventHandler? MoreColorsRequested;

    /// <summary>
    /// Occurs when the WPF-compatible synchronous custom-color workflow is requested.
    /// </summary>
    public event EventHandler<MoreColorsExecutingEventArgs>? MoreColorsExecuting;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorGallery"/> class.
    /// </summary>
    public ColorGallery()
    {
        DefaultStyleKey = typeof(ColorGallery);
        ThemeColors = new ObservableCollection<Windows.UI.Color>();
        MutableStandardColors = new ObservableCollection<Windows.UI.Color>(StandardColors);

        ThemeColors.CollectionChanged += OnColorsCollectionChanged;
        MutableStandardColors.CollectionChanged += OnColorsCollectionChanged;
        RecentColors.CollectionChanged += OnColorsCollectionChanged;
        InitializeCompatibility();
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedValues);
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
        _themeColorsHeader = GetTemplateChild(PART_ThemeColorsHeader) as TextBlock;
        _themeColorsPanel = GetTemplateChild(PART_ThemeColorsPanel) as Panel;
        _standardColorsHeader = GetTemplateChild(PART_StandardColorsHeader) as TextBlock;
        _standardColorsPanel = GetTemplateChild(PART_StandardColorsPanel) as Panel;
        _recentColorsSection = GetTemplateChild(PART_RecentColorsSection) as FrameworkElement;
        _recentColorsHeader = GetTemplateChild(PART_RecentColorsHeader) as TextBlock;
        _recentColorsPanel = GetTemplateChild(PART_RecentColorsPanel) as Panel;
        _automaticButton = GetTemplateChild(PART_AutomaticButton) as WinUIButton;
        _noColorButton = GetTemplateChild(PART_NoColorButton) as WinUIButton;
        _moreColorsButton = GetTemplateChild(PART_MoreColorsButton) as WinUIButton;

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

        ApplyCompatibilityTemplateParts();
        RefreshLocalizedValues();
        RebuildSwatches();
    }

    #endregion

    #region Methods

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorGallery gallery)
        {
            gallery.UpdateSelectionCompatibility((Windows.UI.Color?)e.NewValue);
            gallery.SelectedColorChanged?.Invoke(gallery, new RoutedEventArgs());
            gallery.SelectedColorValueChanged?.Invoke(gallery, (Windows.UI.Color?)e.NewValue);
        }
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorGallery gallery)
        {
            gallery.UpdateGradientsCompatibility();
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

    }

    private void OnColorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateGradientsCompatibility();
        RebuildSwatches();
    }

    private void RebuildSwatches()
    {
        var showThemeColors = Mode == ColorGalleryMode.ThemeColors;
        IReadOnlyList<Windows.UI.Color> standardColors = Mode switch
        {
            ColorGalleryMode.HighlightColors => HighlightColors,
            ColorGalleryMode.ThemeColors => StandardThemeColors,
            _ => MutableStandardColors,
        };

        _colorSwatches.Clear();
        BuildSwatches(_themeColorsPanel, showThemeColors ? ThemeColors : null, "Theme");
        BuildSwatches(_themeGradientsPanel, showThemeColors ? ThemeGradients : null, "ThemeGradient");
        BuildSwatches(_standardColorsPanel, standardColors);
        BuildSwatches(_standardGradientsPanel, showThemeColors ? StandardGradients : null, "StandardGradient");
        BuildSwatches(_recentColorsPanel, showThemeColors ? RecentColors : null, "Recent");

        if (_themeColorsSection is not null)
        {
            _themeColorsSection.Visibility = showThemeColors && ThemeColors is { Count: > 0 }
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (_recentColorsSection is not null)
        {
            _recentColorsSection.Visibility = showThemeColors && RecentColors is { Count: > 0 }
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        UpdateSelectionCompatibility(SelectedColor);
    }

    private void RefreshLocalizedValues()
    {
        var localization = RibbonLocalization.Current.Localization;
        SetLocalizedContent(_automaticButton, localization.Automatic);
        SetLocalizedContent(_noColorButton, localization.NoColor);
        SetLocalizedContent(_moreColorsButton, localization.MoreColors);
        SetLocalizedText(_themeColorsHeader, localization.ThemeColors);
        SetLocalizedText(_standardColorsHeader, localization.StandardColors);
        SetLocalizedText(_recentColorsHeader, localization.RecentColors);
        RefreshSwatchLocalization();
        UpdateSelectionCompatibility(SelectedColor);
    }

    private static void SetLocalizedContent(ContentControl? control, string value)
    {
        if (control is null)
        {
            return;
        }

        Fluent.Automation.Peers.AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
            control,
            ContentControl.ContentProperty,
            value);
        Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(control, value);
    }

    private static void SetLocalizedText(TextBlock? textBlock, string value)
    {
        if (textBlock is not null)
        {
            Fluent.Automation.Peers.AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
                textBlock,
                TextBlock.TextProperty,
                value);
        }
    }

    private void RefreshSwatchLocalization()
    {
        var localization = RibbonLocalization.Current.Localization;
        foreach (var swatch in _colorSwatches)
        {
            if (swatch.Tag is not Windows.UI.Color color)
            {
                continue;
            }

            var description =
                Fluent.Automation.Peers.AutomationPeerHelpers.GetColorDescription(
                    color,
                    localization);
            Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                swatch,
                description);
            Fluent.Automation.Peers.AutomationPeerHelpers.SetToolTipIfUnsetOrGenerated(
                swatch,
                description);
        }
    }

    private void BuildSwatches(
        Panel? host,
        IReadOnlyList<Windows.UI.Color>? colors,
        string automationPrefix = "Standard")
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
        var targetSize = TouchTargetGeometry.ResolveCompactTargetSize(this);
        var hitSize = TouchTargetGeometry.GetSwatchHitSize(chipWidth, chipHeight, targetSize);
        StackPanel? row = null;

        for (var i = 0; i < colors.Count; i++)
        {
            if (i % columns == 0)
            {
                row = new StackPanel { Orientation = Orientation.Horizontal };
                host.Children.Add(row);
            }

            var color = colors[i];
            var chip = new Border
            {
                Width = chipWidth,
                Height = chipHeight,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 200, 200)),
                Background = new SolidColorBrush(color),
            };
            var swatch = new WinUIButton
            {
                MinWidth = hitSize.Width,
                MinHeight = hitSize.Height,
                Margin = new Thickness(1),
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = chip,
                Tag = color,
            };

            var colorDescription =
                Fluent.Automation.Peers.AutomationPeerHelpers.GetColorDescription(
                    color,
                    RibbonLocalization.Current.Localization);
            AutomationProperties.SetAutomationId(swatch, $"ColorGallery{automationPrefix}Color{i}");
            Fluent.Automation.Peers.AutomationPeerHelpers.SetToolTipIfUnsetOrGenerated(
                swatch,
                colorDescription);
            Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                swatch,
                colorDescription);
            swatch.Click += OnSwatchClick;
            row!.Children.Add(swatch);
            _colorSwatches.Add(swatch);
        }
    }

    internal void RefreshTouchTargetGeometry() => RebuildSwatches();

    private void OnSwatchClick(object sender, RoutedEventArgs e)
    {
        if (sender is WinUIButton { Tag: Windows.UI.Color color })
        {
            SelectedColor = color;
        }
    }

    private void OnAutomaticButtonClick(object sender, RoutedEventArgs e)
    {
        SelectedColor = null;
    }

    private void OnNoColorButtonClick(object sender, RoutedEventArgs e)
    {
        SelectedColor = Microsoft.UI.Colors.Transparent;
    }

    private async void OnMoreColorsButtonClick(object sender, RoutedEventArgs e)
    {
        await ExecuteMoreColorsAsync();
    }

    #endregion
}

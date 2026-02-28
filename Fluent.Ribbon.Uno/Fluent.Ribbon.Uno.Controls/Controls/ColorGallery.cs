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
public partial class ColorGallery : Control
{
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

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the selected color changes.
    /// </summary>
    public event EventHandler<Windows.UI.Color?>? SelectedColorChanged;

    /// <summary>
    /// Occurs when the "More Colors" button is clicked.
    /// </summary>
#pragma warning disable CS0067
    public event EventHandler? MoreColorsRequested;
#pragma warning restore CS0067

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
    }

    #endregion

    #region Methods

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorGallery gallery)
        {
            gallery.SelectedColorChanged?.Invoke(gallery, (Windows.UI.Color?)e.NewValue);
        }
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

    #endregion
}

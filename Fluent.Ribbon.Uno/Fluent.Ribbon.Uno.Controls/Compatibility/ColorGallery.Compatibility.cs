namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Automation;
using Windows.UI;

/// <summary>
/// Portable WPF-compatible palette, gradient, selection, and custom-color behavior.
/// </summary>
public partial class ColorGallery
{
    private const string PartThemeGradientsPanel = "PART_ThemeGradientsPanel";
    private const string PartStandardGradientsPanel = "PART_StandardGradientsPanel";

    private readonly List<WinUIButton> _colorSwatches = new();
    private Panel? _themeGradientsPanel;
    private Panel? _standardGradientsPanel;
    private WinUIButton? _selectedSwatch;

    /// <summary>Fixed highlight colors used by WPF Fluent.Ribbon.</summary>
    public static readonly Color[] HighlightColors =
    [
        Rgb(0xFF, 0xFF, 0x00),
        Rgb(0x00, 0xFF, 0x00),
        Rgb(0x00, 0xFF, 0xFF),
        Rgb(0xFF, 0x00, 0xFF),
        Rgb(0x00, 0x00, 0xFF),
        Rgb(0xFF, 0x00, 0x00),
        Rgb(0x00, 0x00, 0x80),
        Rgb(0x00, 0x80, 0x80),
        Rgb(0x00, 0x80, 0x00),
        Rgb(0x80, 0x00, 0x80),
        Rgb(0x80, 0x00, 0x00),
        Rgb(0x80, 0x80, 0x00),
        Rgb(0x80, 0x80, 0x80),
        Rgb(0xC0, 0xC0, 0xC0),
        Rgb(0x00, 0x00, 0x00),
    ];

    /// <summary>Fixed standard colors used by WPF Fluent.Ribbon.</summary>
    public static readonly Color[] StandardColors =
    [
        Rgb(0xFF, 0xFF, 0xFF),
        Rgb(0xFF, 0x00, 0x00),
        Rgb(0xC0, 0x50, 0x4D),
        Rgb(0xD1, 0x63, 0x49),
        Rgb(0xDD, 0x84, 0x84),
        Rgb(0xCC, 0xCC, 0xCC),
        Rgb(0xFF, 0xC0, 0x00),
        Rgb(0xF7, 0x96, 0x46),
        Rgb(0xD1, 0x90, 0x49),
        Rgb(0xF3, 0xA4, 0x47),
        Rgb(0xA5, 0xA5, 0xA5),
        Rgb(0xFF, 0xFF, 0x00),
        Rgb(0x9B, 0xBB, 0x59),
        Rgb(0xCC, 0xB4, 0x00),
        Rgb(0xDF, 0xCE, 0x04),
        Rgb(0x66, 0x66, 0x66),
        Rgb(0x00, 0xB0, 0x50),
        Rgb(0x4B, 0xAC, 0xC6),
        Rgb(0x8F, 0xB0, 0x8C),
        Rgb(0xA5, 0xB5, 0x92),
        Rgb(0x33, 0x33, 0x33),
        Rgb(0x00, 0x4D, 0xBB),
        Rgb(0x4F, 0x81, 0xBD),
        Rgb(0x64, 0x6B, 0x86),
        Rgb(0x80, 0x9E, 0xC2),
        Rgb(0x00, 0x00, 0x00),
        Rgb(0x9B, 0x00, 0xD3),
        Rgb(0x80, 0x64, 0xA2),
        Rgb(0x9E, 0x7C, 0x7C),
        Rgb(0x9C, 0x85, 0xC0),
    ];

    /// <summary>Fixed standard colors displayed alongside theme colors.</summary>
    public static readonly Color[] StandardThemeColors =
    [
        Rgb(0xC0, 0x00, 0x00),
        Rgb(0xFF, 0x00, 0x00),
        Rgb(0xFF, 0xC0, 0x00),
        Rgb(0xFF, 0xFF, 0x00),
        Rgb(0x92, 0xD0, 0x50),
        Rgb(0x00, 0xB0, 0x50),
        Rgb(0x00, 0xB0, 0xF0),
        Rgb(0x00, 0x70, 0xC0),
        Rgb(0x00, 0x20, 0x60),
        Rgb(0x70, 0x30, 0xA0),
    ];

    /// <summary>Identifies the standard gradient-row count.</summary>
    public static readonly DependencyProperty StandardColorGridRowsProperty =
        DependencyProperty.Register(
            nameof(StandardColorGridRows),
            typeof(int),
            typeof(ColorGallery),
            new PropertyMetadata(0, OnGradientInputChanged));

    /// <summary>Identifies the theme gradient-row count.</summary>
    public static readonly DependencyProperty ThemeColorGridRowsProperty =
        DependencyProperty.Register(
            nameof(ThemeColorGridRows),
            typeof(int),
            typeof(ColorGallery),
            new PropertyMetadata(0, OnGradientInputChanged));

    /// <summary>Identifies the external theme-color source.</summary>
    public static readonly DependencyProperty ThemeColorsSourceProperty =
        DependencyProperty.Register(
            nameof(ThemeColorsSource),
            typeof(IEnumerable<Color>),
            typeof(ColorGallery),
            new PropertyMetadata(null, OnThemeColorsSourceChanged));

    /// <summary>Identifies the generated theme gradients.</summary>
    public static readonly DependencyProperty ThemeGradientsProperty =
        DependencyProperty.Register(
            nameof(ThemeGradients),
            typeof(Color[]),
            typeof(ColorGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the generated standard gradients.</summary>
    public static readonly DependencyProperty StandardGradientsProperty =
        DependencyProperty.Register(
            nameof(StandardGradients),
            typeof(Color[]),
            typeof(ColorGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Identifies the WPF-compatible selected-color routed event.
    /// </summary>
    /// <remarks>
    /// WinUI does not provide public custom routed-event registration. The corresponding
    /// CLR event is raised directly and therefore does not bubble through the visual tree.
    /// </remarks>
    public static readonly RoutedEvent SelectedColorChangedEvent = CreateSelectedColorChangedEvent();

    /// <summary>Gets or sets the number of standard gradient rows.</summary>
    public int StandardColorGridRows
    {
        get => (int)GetValue(StandardColorGridRowsProperty);
        set => SetValue(StandardColorGridRowsProperty, Math.Max(0, value));
    }

    /// <summary>Gets or sets the number of theme gradient rows.</summary>
    public int ThemeColorGridRows
    {
        get => (int)GetValue(ThemeColorGridRowsProperty);
        set => SetValue(ThemeColorGridRowsProperty, Math.Max(0, value));
    }

    /// <summary>Gets or sets the source copied into <see cref="ThemeColors"/>.</summary>
    public IEnumerable<Color>? ThemeColorsSource
    {
        get => (IEnumerable<Color>?)GetValue(ThemeColorsSourceProperty);
        set => SetValue(ThemeColorsSourceProperty, value);
    }

    /// <summary>Gets the generated theme-color gradients.</summary>
    public Color[]? ThemeGradients
    {
        get => (Color[]?)GetValue(ThemeGradientsProperty);
        private set => SetValue(ThemeGradientsProperty, value);
    }

    /// <summary>Gets the generated standard-theme gradients.</summary>
    public Color[]? StandardGradients
    {
        get => (Color[]?)GetValue(StandardGradientsProperty);
        private set => SetValue(StandardGradientsProperty, value);
    }

    /// <summary>Gets or sets a host-provided cross-platform custom color picker.</summary>
    public IColorGalleryCustomColorPicker? CustomColorPicker { get; set; }

    /// <summary>
    /// Occurs when no picker was assigned, allowing the host to inject one for this request.
    /// </summary>
    public event EventHandler<ColorGalleryCustomColorPickerRequestedEventArgs>? CustomColorPickerRequested;

    private static Color Rgb(byte red, byte green, byte blue) =>
        Color.FromArgb(byte.MaxValue, red, green, blue);

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(RoutedEvent))]
    private static RoutedEvent CreateSelectedColorChangedEvent()
    {
        var constructor = typeof(RoutedEvent).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(string)],
            modifiers: null);
        if (constructor?.Invoke([nameof(SelectedColorChanged)]) is RoutedEvent routedEvent)
        {
            return routedEvent;
        }

        // WinUI does not expose custom routed-event registration. The object is a stable,
        // non-null metadata token; the CLR event itself is raised directly.
        return (RoutedEvent)RuntimeHelpers.GetUninitializedObject(typeof(RoutedEvent));
    }

    private void InitializeCompatibility()
    {
        UpdateGradientsCompatibility();
    }

    private void ApplyCompatibilityTemplateParts()
    {
        _themeGradientsPanel = GetTemplateChild(PartThemeGradientsPanel) as Panel;
        _standardGradientsPanel = GetTemplateChild(PartStandardGradientsPanel) as Panel;
    }

    private void UpdateSelectionCompatibility(Color? color)
    {
        var selectedColorText = RibbonLocalization.Current.Localization.SelectedColor;
        _selectedSwatch = null;
        foreach (var swatch in _colorSwatches)
        {
            var isSelected = swatch.Tag is Color swatchColor
                             && color.HasValue
                             && swatchColor.Equals(color.Value);
            swatch.BorderThickness = new Thickness(isSelected ? 3 : 1);
            AutomationProperties.SetHelpText(swatch, isSelected ? selectedColorText : string.Empty);
            AutomationProperties.SetItemStatus(swatch, isSelected ? selectedColorText : string.Empty);
            if (isSelected)
            {
                _selectedSwatch = swatch;
            }
        }

        if (_automaticButton is not null)
        {
            AutomationProperties.SetHelpText(
                _automaticButton,
                color.HasValue ? string.Empty : selectedColorText);
            AutomationProperties.SetItemStatus(
                _automaticButton,
                color.HasValue ? string.Empty : selectedColorText);
        }

        if (_noColorButton is not null)
        {
            AutomationProperties.SetHelpText(
                _noColorButton,
                color == Microsoft.UI.Colors.Transparent ? selectedColorText : string.Empty);
            AutomationProperties.SetItemStatus(
                _noColorButton,
                color == Microsoft.UI.Colors.Transparent ? selectedColorText : string.Empty);
        }
    }

    private void UpdateGradientsCompatibility()
    {
        if (Mode != ColorGalleryMode.ThemeColors || Columns < 1)
        {
            ThemeGradients = null;
            StandardGradients = null;
            return;
        }

        ThemeGradients = ThemeColorGridRows > 0
            ? GenerateGradients(ThemeColors, ThemeColorGridRows)
            : null;
        StandardGradients = StandardColorGridRows > 0
            ? GenerateGradients(StandardThemeColors, StandardColorGridRows)
            : null;
    }

    private Color[] GenerateGradients(IReadOnlyList<Color> source, int rows)
    {
        var result = new Color[Columns * rows];
        var count = Math.Min(Columns, source.Count);
        for (var column = 0; column < count; column++)
        {
            var gradient = ColorGalleryGradientGenerator.GetGradient(source[column], rows);
            for (var row = 0; row < rows; row++)
            {
                result[column + (row * Columns)] = gradient[row];
            }
        }

        return result;
    }

    private async Task ExecuteMoreColorsAsync()
    {
        if (MoreColorsExecuting is not null)
        {
            var args = new MoreColorsExecutingEventArgs();
            MoreColorsExecuting(this, args);
            if (!args.Canceled)
            {
                ApplyCustomColor(args.Color);
            }

            MoreColorsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        var picker = CustomColorPicker;
        if (picker is null)
        {
            var requested = new ColorGalleryCustomColorPickerRequestedEventArgs(
                SelectedColor ?? Microsoft.UI.Colors.Black);
            CustomColorPickerRequested?.Invoke(this, requested);
            picker = requested.Picker;
        }

        if (picker is null && XamlRoot is not null)
        {
            picker = ContentDialogColorGalleryCustomColorPicker.Instance;
        }

        if (picker is not null)
        {
            var context = new ColorGalleryCustomColorPickerContext(
                this,
                SelectedColor ?? Microsoft.UI.Colors.Black);
            var result = await picker.PickColorAsync(context);
            if (result.IsAccepted)
            {
                ApplyCustomColor(result.Color);
            }
        }

        // This additive Uno event remains an explicit host notification. On platforms
        // without a usable XamlRoot it is also the non-silent fallback hook.
        MoreColorsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyCustomColor(Color color)
    {
        TrackRecentColor(color);
        SelectedColor = color;
    }

    private WinUIButton? GetSelectedSwatchForAutomation()
    {
        return _selectedSwatch;
    }

    private static void OnGradientInputChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (ColorGallery)sender;
        gallery.UpdateGradientsCompatibility();
        gallery.RebuildSwatches();
    }

    private static void OnThemeColorsSourceChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (ColorGallery)sender;
        gallery.ThemeColors.Clear();
        if (args.NewValue is IEnumerable<Color> colors)
        {
            foreach (var color in colors)
            {
                gallery.ThemeColors.Add(color);
            }
        }
    }
}

/// <summary>Generates stable light-to-dark color gradients.</summary>
public static class ColorGalleryGradientGenerator
{
    /// <summary>Creates <paramref name="count"/> shades from light to dark.</summary>
    public static Windows.UI.Color[] GetGradient(Windows.UI.Color color, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        const double darkBrightness = 0.15;
        const double lightBrightness = 0.85;
        var result = new Windows.UI.Color[count];
        for (var index = 0; index < count; index++)
        {
            var position = count == 1 ? 0.5 : (double)index / (count - 1);
            var brightness = lightBrightness - (position * (lightBrightness - darkBrightness));
            result[index] = WithBrightness(color, brightness);
        }

        return result;
    }

    private static Windows.UI.Color WithBrightness(Windows.UI.Color color, double brightness)
    {
        var current = (color.R + color.G + color.B) / (255.0 * 3.0);
        if (current <= double.Epsilon)
        {
            var channel = (byte)Math.Round(Math.Clamp(brightness, 0, 1) * byte.MaxValue);
            return Windows.UI.Color.FromArgb(color.A, channel, channel, channel);
        }

        if (brightness >= current)
        {
            var amount = (brightness - current) / Math.Max(double.Epsilon, 1 - current);
            return Windows.UI.Color.FromArgb(
                color.A,
                Blend(color.R, byte.MaxValue, amount),
                Blend(color.G, byte.MaxValue, amount),
                Blend(color.B, byte.MaxValue, amount));
        }

        var scale = brightness / current;
        return Windows.UI.Color.FromArgb(
            color.A,
            Scale(color.R, scale),
            Scale(color.G, scale),
            Scale(color.B, scale));
    }

    private static byte Blend(byte source, byte target, double amount) =>
        (byte)Math.Round(source + ((target - source) * Math.Clamp(amount, 0, 1)));

    private static byte Scale(byte source, double scale) =>
        (byte)Math.Round(source * Math.Clamp(scale, 0, 1));
}

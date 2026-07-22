namespace Fluent;

using Microsoft.UI.Xaml.Automation;
using Windows.UI;

/// <summary>Describes a custom color picker invocation.</summary>
public sealed class ColorGalleryCustomColorPickerContext
{
    /// <summary>Initializes a custom color picker context.</summary>
    public ColorGalleryCustomColorPickerContext(ColorGallery gallery, Color initialColor)
    {
        Gallery = gallery ?? throw new ArgumentNullException(nameof(gallery));
        InitialColor = initialColor;
    }

    /// <summary>Gets the gallery requesting a color.</summary>
    public ColorGallery Gallery { get; }

    /// <summary>Gets the initially selected color.</summary>
    public Color InitialColor { get; }

    /// <summary>Gets the current XAML root, when one is available.</summary>
    public XamlRoot? XamlRoot => Gallery.XamlRoot;
}

/// <summary>Represents the result of a custom color picker.</summary>
public readonly record struct ColorGalleryCustomColorPickerResult(bool IsAccepted, Color Color)
{
    /// <summary>Creates an accepted result.</summary>
    public static ColorGalleryCustomColorPickerResult Accepted(Color color) => new(true, color);

    /// <summary>Creates a canceled result.</summary>
    public static ColorGalleryCustomColorPickerResult Canceled(Color initialColor) =>
        new(false, initialColor);
}

/// <summary>Provides a cross-platform custom color picker.</summary>
public interface IColorGalleryCustomColorPicker
{
    /// <summary>Requests a custom color.</summary>
    Task<ColorGalleryCustomColorPickerResult> PickColorAsync(
        ColorGalleryCustomColorPickerContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Allows a host to inject a picker for an individual request.</summary>
public sealed class ColorGalleryCustomColorPickerRequestedEventArgs : EventArgs
{
    /// <summary>Initializes request event data.</summary>
    public ColorGalleryCustomColorPickerRequestedEventArgs(Color initialColor)
    {
        InitialColor = initialColor;
    }

    /// <summary>Gets the initially selected color.</summary>
    public Color InitialColor { get; }

    /// <summary>Gets or sets the picker supplied by the host.</summary>
    public IColorGalleryCustomColorPicker? Picker { get; set; }
}

/// <summary>Uses the built-in WinUI color picker in a content dialog.</summary>
public sealed class ContentDialogColorGalleryCustomColorPicker : IColorGalleryCustomColorPicker
{
    /// <summary>Gets the shared stateless picker instance.</summary>
    public static ContentDialogColorGalleryCustomColorPicker Instance { get; } = new();

    private ContentDialogColorGalleryCustomColorPicker()
    {
    }

    /// <inheritdoc />
    public async Task<ColorGalleryCustomColorPickerResult> PickColorAsync(
        ColorGalleryCustomColorPickerContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.XamlRoot is null)
        {
            throw new InvalidOperationException(
                "The built-in custom color picker requires a connected XamlRoot. "
                + "Inject IColorGalleryCustomColorPicker from the host on this platform.");
        }

        var picker = new ColorPicker
        {
            Color = context.InitialColor,
            IsAlphaEnabled = true,
            IsAlphaSliderVisible = true,
            IsAlphaTextInputVisible = true,
        };

        AutomationProperties.SetAutomationId(picker, "ColorGalleryCustomColorPicker");

        var dialog = new ContentDialog
        {
            XamlRoot = context.XamlRoot,
            Title = "More Colors",
            PrimaryButtonText = "Select",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = picker,
        };

        AutomationProperties.SetAutomationId(dialog, "ColorGalleryCustomColorDialog");
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary
            ? ColorGalleryCustomColorPickerResult.Accepted(picker.Color)
            : ColorGalleryCustomColorPickerResult.Canceled(context.InitialColor);
    }
}

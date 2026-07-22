using System.Runtime.CompilerServices;
using Fluent.Converters;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Fluent;

/// <summary>Describes the result of adapting a WPF-compatible object icon for WinUI.</summary>
public enum CompatibilityIconConversionStatus
{
    /// <summary>The icon was cleared explicitly.</summary>
    Empty,

    /// <summary>The value is available as an <see cref="ImageSource"/> for current ribbon templates.</summary>
    ImageSource,

    /// <summary>The value is valid WinUI content but the current core template accepts only an image source.</summary>
    PreservedWinUIContent,

    /// <summary>The value is preserved but has no known safe WinUI rendering conversion.</summary>
    Unsupported
}

/// <summary>Contains the explicit outcome of adapting an object-typed compatibility icon.</summary>
/// <param name="Status">The conversion outcome.</param>
/// <param name="ImageSource">The image source accepted by current core templates, when available.</param>
/// <param name="PreservedValue">The original or safely-created WinUI content when it cannot be used by an image template.</param>
/// <param name="Error">A diagnostic explaining why the current core template cannot render the value.</param>
public sealed record CompatibilityIconConversionResult(
    CompatibilityIconConversionStatus Status,
    ImageSource? ImageSource,
    object? PreservedValue,
    string? Error)
{
    /// <summary>Gets whether the current core image template can render this result.</summary>
    public bool CanRenderInCurrentTemplate =>
        Status is CompatibilityIconConversionStatus.Empty or CompatibilityIconConversionStatus.ImageSource;
}

/// <summary>
/// Converts WPF-compatible object icon values without silently discarding unsupported content.
/// </summary>
public static class CompatibilityIconAdapter
{
    private static readonly ConditionalWeakTable<DependencyObject, ResultHolder> LastResults = new();

    /// <summary>Converts an icon value to the safest available WinUI representation.</summary>
    public static CompatibilityIconConversionResult Convert(object? value)
    {
        switch (value)
        {
            case null:
                return new(
                    CompatibilityIconConversionStatus.Empty,
                    null,
                    null,
                    null);
            case ImageSource imageSource:
                return new(
                    CompatibilityIconConversionStatus.ImageSource,
                    imageSource,
                    imageSource,
                    null);
            case Uri uri:
                return CreateBitmapImage(uri, uri);
            case string text when Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out var uri):
                return CreateBitmapImage(uri, text);
            case string text:
                return Unsupported(
                    text,
                    $"Icon string '{text}' is not a valid relative or absolute URI.");
            case ImageIconSource imageIconSource when imageIconSource.ImageSource is { } source:
                return new(
                    CompatibilityIconConversionStatus.ImageSource,
                    source,
                    imageIconSource,
                    null);
            case BitmapIconSource bitmapIconSource when bitmapIconSource.UriSource is { } uri:
                return CreateBitmapImage(uri, bitmapIconSource);
            case IconSource iconSource:
                return PreserveIconSource(iconSource);
            case IconElement iconElement:
                return PreservedWinUIContent(
                    iconElement,
                    "The icon element is preserved, but current Fluent.Ribbon.Uno core icon slots use ImageSource.");
            case UIElement element:
                return PreservedWinUIContent(
                    element,
                    "The UIElement is preserved, but current Fluent.Ribbon.Uno core icon slots use ImageSource.");
            default:
                return Unsupported(
                    value,
                    $"Icon value type '{value.GetType().FullName}' has no reviewed WinUI image conversion.");
        }
    }

    /// <summary>
    /// Returns the most recent adaptation result recorded for a compatibility wrapper.
    /// </summary>
    public static CompatibilityIconConversionResult GetLastResult(DependencyObject owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return LastResults.TryGetValue(owner, out var holder)
            ? holder.Result
            : Convert(null);
    }

    public static void ApplyToImageSourceProperty(
        DependencyObject owner,
        DependencyProperty targetProperty,
        object? value)
    {
        var result = Convert(value);
        LastResults.GetOrCreateValue(owner).Result = result;

        if (result.Status is CompatibilityIconConversionStatus.Empty)
        {
            owner.SetValue(targetProperty, null);
        }
        else if (result.ImageSource is { } imageSource)
        {
            owner.SetValue(targetProperty, imageSource);
        }
    }

    public static void RecordNonRenderableValue(DependencyObject owner, object? value)
    {
        var result = Convert(value);
        if (result.CanRenderInCurrentTemplate && value is not null)
        {
            result = result with
            {
                Status = CompatibilityIconConversionStatus.PreservedWinUIContent,
                Error = "This compatibility icon slot has no corresponding core ImageSource property; the value remains available on the facade."
            };
        }

        LastResults.GetOrCreateValue(owner).Result = result;
    }

    private static CompatibilityIconConversionResult CreateBitmapImage(Uri uri, object preservedValue)
    {
        try
        {
            var image = ObjectToImageConverter.CreateImageSource(uri, default);
            if (image is null)
            {
                return Unsupported(
                    preservedValue,
                    $"The URI '{uri}' did not produce an ImageSource.");
            }

            return new(
                CompatibilityIconConversionStatus.ImageSource,
                image,
                preservedValue,
                null);
        }
        catch (Exception exception) when (exception is ArgumentException
                                          or InvalidOperationException
                                          or NotSupportedException)
        {
            return Unsupported(
                preservedValue,
                $"The URI '{uri}' could not be converted to BitmapImage: {exception.Message}");
        }
    }

    private static CompatibilityIconConversionResult PreserveIconSource(IconSource iconSource)
    {
        try
        {
            return PreservedWinUIContent(
                iconSource.CreateIconElement(),
                "The IconSource was converted to IconElement, but current Fluent.Ribbon.Uno core icon slots use ImageSource.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            return Unsupported(
                iconSource,
                $"The IconSource could not create an IconElement: {exception.Message}");
        }
    }

    private static CompatibilityIconConversionResult PreservedWinUIContent(
        object value,
        string error) =>
        new(
            CompatibilityIconConversionStatus.PreservedWinUIContent,
            null,
            value,
            error);

    private static CompatibilityIconConversionResult Unsupported(object value, string error) =>
        new(
            CompatibilityIconConversionStatus.Unsupported,
            null,
            value,
            error);

    private sealed class ResultHolder
    {
        internal CompatibilityIconConversionResult Result { get; set; } =
            new(CompatibilityIconConversionStatus.Empty, null, null, null);
    }
}

namespace Fluent.Converters;

/// <summary>
/// Converts an object (string path, Uri, or ImageSource) to an <see cref="Microsoft.UI.Xaml.Controls.Image"/> element.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, simplified for Uno/WinUI.
/// WPF version handles System.Drawing.Icon, BitmapFrame selection, DPI scaling, and P/Invoke.
/// Uno version handles string paths, Uri, and ImageSource.
/// </remarks>
public partial class ObjectToImageConverter : MarkupExtension, IValueConverter, global::Fluent.IMultiValueConverter
{
    /// <summary>
    /// Default instance.
    /// </summary>
    public static readonly ObjectToImageConverter Default = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is null)
        {
            return null;
        }

        var desiredSize = ParseDesiredSize(parameter);

        var imageSource = ConvertToImageSource(value);
        if (imageSource is null)
        {
            return null;
        }

        // If target type wants ImageSource directly
        if (targetType == typeof(ImageSource))
        {
            return imageSource;
        }

        // Otherwise return an Image element
        var image = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = imageSource,
            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
        };

        if (desiredSize.Width > 0)
        {
            image.Width = desiredSize.Width;
        }

        if (desiredSize.Height > 0)
        {
            image.Height = desiredSize.Height;
        }

        return image;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Converts various object types to an <see cref="ImageSource"/>.
    /// </summary>
    protected static ImageSource? ConvertToImageSource(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is ImageSource imageSource)
        {
            return imageSource;
        }

        if (value is string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
        }

        if (value is Uri uri)
        {
            return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
        }

        return null;
    }

    private static Windows.Foundation.Size ParseDesiredSize(object? parameter)
    {
        if (parameter is Windows.Foundation.Size size)
        {
            return size;
        }

        if (parameter is double d)
        {
            return new Windows.Foundation.Size(d, d);
        }

        if (parameter is int i)
        {
            return new Windows.Foundation.Size(i, i);
        }

        if (parameter is string s && double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return new Windows.Foundation.Size(parsed, parsed);
        }

        return new Windows.Foundation.Size(0, 0);
    }
}

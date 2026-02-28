namespace Fluent.Converters;

/// <summary>
/// Converts <c>null</c> to <c>true</c> and not <c>null</c> to <c>false</c>.
/// </summary>
public sealed class IsNullConverter : IValueConverter
{
    /// <summary>
    /// A singleton instance for <see cref="IsNullConverter"/>.
    /// </summary>
    public static readonly IsNullConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is null;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}

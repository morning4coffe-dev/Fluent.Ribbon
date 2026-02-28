namespace Fluent.Converters;

/// <summary>
/// Holds static instances of commonly used converters.
/// </summary>
public static class StaticConverters
{
    /// <summary>
    /// Gets a static instance of <see cref="ColorToSolidColorBrushConverter"/>.
    /// </summary>
    public static readonly ColorToSolidColorBrushConverter ColorToSolidColorBrushConverter = new();

    /// <summary>
    /// Gets a static instance of <see cref="InverseBoolConverter"/>.
    /// </summary>
    public static readonly InverseBoolConverter InverseBoolConverter = new();

    /// <summary>
    /// Gets a static instance of <see cref="IsNullConverter"/>.
    /// </summary>
    public static readonly IsNullConverter IsNullConverter = IsNullConverter.Instance;

    /// <summary>
    /// Gets a static instance of <see cref="BoolToVisibilityConverter"/>.
    /// </summary>
    public static readonly BoolToVisibilityConverter BoolToVisibilityConverter = new();

    /// <summary>
    /// Gets a static instance of <see cref="NullToCollapsedConverter"/>.
    /// </summary>
    public static readonly NullToCollapsedConverter NullToCollapsedConverter = new();

    /// <summary>
    /// Gets a static instance of <see cref="EqualsToVisibilityConverter"/>.
    /// </summary>
    public static readonly EqualsToVisibilityConverter EqualsToVisibilityConverter = new();

    /// <summary>
    /// Gets a static instance of <see cref="InvertNumericConverter"/>.
    /// </summary>
    public static readonly InvertNumericConverter InvertNumericConverter = new();

    /// <summary>
    /// Gets a static instance of <see cref="CornerRadiusConverter"/>.
    /// </summary>
    public static readonly CornerRadiusConverter CornerRadiusConverter = new();

    /// <summary>
    /// Gets a static instance of <see cref="SpinnerTextToValueConverter"/>.
    /// </summary>
    public static readonly SpinnerTextToValueConverter SpinnerTextToValueConverter = SpinnerTextToValueConverter.DefaultInstance;

    /// <summary>
    /// Gets a static instance of <see cref="ExtractLeftRightFromThicknessConverter"/>.
    /// </summary>
    public static readonly ExtractLeftRightFromThicknessConverter ExtractLeftRightFromThicknessConverter = ExtractLeftRightFromThicknessConverter.Default;

    /// <summary>
    /// Gets a static instance of <see cref="ObjectToImageConverter"/>.
    /// </summary>
    public static readonly ObjectToImageConverter ObjectToImageConverter = ObjectToImageConverter.Default;

    /// <summary>
    /// Gets a static instance of <see cref="IconConverter"/>.
    /// </summary>
    public static readonly IconConverter IconConverter = IconConverter.Default;

    /// <summary>
    /// Gets a static instance of <see cref="SizeDefinitionConverter"/>.
    /// </summary>
    public static readonly SizeDefinitionConverter SizeDefinitionConverter = SizeDefinitionConverter.Default;

    /// <summary>
    /// Gets a static instance of <see cref="RibbonGroupBoxStateDefinitionConverter"/>.
    /// </summary>
    public static readonly RibbonGroupBoxStateDefinitionConverter RibbonGroupBoxStateDefinitionConverter = RibbonGroupBoxStateDefinitionConverter.Default;

    /// <summary>
    /// Gets a static instance of <see cref="ThicknessConverter"/>.
    /// </summary>
    public static readonly ThicknessConverter ThicknessConverter = new();
}

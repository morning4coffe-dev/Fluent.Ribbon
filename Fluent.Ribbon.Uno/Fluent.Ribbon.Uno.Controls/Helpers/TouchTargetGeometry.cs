namespace Fluent.Helpers;

using Windows.Foundation;

internal static class TouchTargetGeometry
{
    internal const double DefaultCompactTargetSize = 24;
    private const string CompactTargetResourceKey = "RibbonCompactTargetSize";
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        FrameworkElement,
        GeneratedTargetSize> GeneratedTargetSizes = new();

    internal static double NormalizeTargetSize(double value)
        => double.IsFinite(value) && value >= DefaultCompactTargetSize
            ? value
            : DefaultCompactTargetSize;

    internal static Size GetSwatchHitSize(double chipWidth, double chipHeight, double targetSize)
    {
        var normalizedTarget = NormalizeTargetSize(targetSize);
        return new Size(
            Math.Max(Math.Max(0, chipWidth), normalizedTarget),
            Math.Max(Math.Max(0, chipHeight), normalizedTarget));
    }

    internal static double ResolveCompactTargetSize(FrameworkElement element)
    {
        for (DependencyObject? current = element;
             current is FrameworkElement frameworkElement;
             current = VisualTreeHelper.GetParent(current))
        {
            if (frameworkElement.Resources.TryGetValue(
                    CompactTargetResourceKey,
                    out var localValue)
                && TryConvertTargetSize(localValue, out var localSize))
            {
                return localSize;
            }
        }

        if (Application.Current?.Resources.TryGetValue(
                CompactTargetResourceKey,
                out var applicationValue) == true
            && TryConvertTargetSize(applicationValue, out var applicationSize))
        {
            return applicationSize;
        }

        return DefaultCompactTargetSize;
    }

    internal static void PropagateCompactTargetSize(
        FrameworkElement source,
        FrameworkElement target)
    {
        var targetSize = ResolveCompactTargetSize(source);
        if (GeneratedTargetSizes.TryGetValue(target, out var generated))
        {
            if (!target.Resources.TryGetValue(
                    CompactTargetResourceKey,
                    out var current)
                || !TryConvertTargetSize(current, out var currentSize)
                || !currentSize.Equals(generated.Value))
            {
                GeneratedTargetSizes.Remove(target);
                return;
            }

            target.Resources[CompactTargetResourceKey] = targetSize;
            generated.Value = targetSize;
            return;
        }

        if (target.Resources.TryGetValue(CompactTargetResourceKey, out _))
        {
            return;
        }

        target.Resources[CompactTargetResourceKey] = targetSize;
        GeneratedTargetSizes.Add(
            target,
            new GeneratedTargetSize { Value = targetSize });
    }

    private static bool TryConvertTargetSize(object? value, out double size)
    {
        if (value is IConvertible convertible)
        {
            try
            {
                size = NormalizeTargetSize(
                    convertible.ToDouble(
                        System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            catch (Exception exception)
                when (exception is FormatException
                      or InvalidCastException
                      or OverflowException)
            {
            }
        }

        size = DefaultCompactTargetSize;
        return false;
    }

    private sealed class GeneratedTargetSize
    {
        internal required double Value { get; set; }
    }
}

using System.Globalization;
using System.Reflection;
using Microsoft.UI.Xaml;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class ConverterCompatibilityTests
{
    private static readonly Type[] SingleValueSignature =
    [
        typeof(object),
        typeof(Type),
        typeof(object),
        typeof(CultureInfo)
    ];

    private static readonly Type[] MultiValueConvertSignature =
    [
        typeof(object[]),
        typeof(Type),
        typeof(object),
        typeof(CultureInfo)
    ];

    private static readonly Type[] MultiValueConvertBackSignature =
    [
        typeof(object),
        typeof(Type[]),
        typeof(object),
        typeof(CultureInfo)
    ];

    [TestCase(typeof(global::Fluent.Converters.ApplicationMenuRightScrollViewerExtractorConverter))]
    [TestCase(typeof(global::Fluent.Converters.ColorToSolidColorBrushValueConverter))]
    [TestCase(typeof(global::Fluent.Converters.CornerRadiusConverter))]
    [TestCase(typeof(global::Fluent.Converters.EqualsToVisibilityConverter))]
    [TestCase(typeof(global::Fluent.Converters.ExtractLeftRightFromThicknessConverter))]
    [TestCase(typeof(global::Fluent.Converters.InverseBoolConverter))]
    [TestCase(typeof(global::Fluent.Converters.InvertNumericConverter))]
    [TestCase(typeof(global::Fluent.Converters.IsNullConverter))]
    [TestCase(typeof(global::Fluent.Converters.SpinnerTextToValueConverter))]
    public void SingleValueCultureOverloads_AreDeclaredAndVirtual(Type converterType)
    {
        Assert.Multiple(() =>
        {
            AssertVirtualMethod(converterType, "Convert", SingleValueSignature);
            AssertVirtualMethod(converterType, "ConvertBack", SingleValueSignature);
        });
    }

    [TestCase(typeof(global::Fluent.Converters.CornerRadiusConverter))]
    [TestCase(typeof(global::Fluent.Converters.ObjectToImageConverter))]
    [TestCase(typeof(global::Fluent.Converters.ThicknessConverter))]
    public void MultiValueCultureOverloads_AreDeclaredAndVirtual(Type converterType)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(global::Fluent.IMultiValueConverter).IsAssignableFrom(converterType),
                Is.True,
                converterType.FullName);
            AssertVirtualMethod(converterType, "Convert", MultiValueConvertSignature);
            AssertVirtualMethod(converterType, "ConvertBack", MultiValueConvertBackSignature);
        });
    }

    [Test]
    public void ThicknessConverter_MultiValueOverloadBuildsExpectedThickness()
    {
        var converter = new global::Fluent.Converters.ThicknessConverter();

        var result = converter.Convert(
            [1, 2.5, "3", 4L],
            typeof(Thickness),
            parameter: string.Empty,
            CultureInfo.InvariantCulture);

        Assert.That(result, Is.EqualTo(new Thickness(1, 2.5, 3, 4)));
    }

    [Test]
    public void SpinnerConverter_CultureOverloadsUseSuppliedCulture()
    {
        var converter = new global::Fluent.Converters.SpinnerTextToValueConverter();
        var culture = CultureInfo.GetCultureInfo("de-DE");

        var parsed = converter.Convert(
            "1,5",
            typeof(double),
            Tuple.Create("0.0", 0D),
            culture);
        var formatted = converter.ConvertBack(
            1.5D,
            typeof(string),
            "0.0",
            culture);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.EqualTo(1.5D));
            Assert.That(formatted, Is.EqualTo("1,5"));
        });
    }

    private static void AssertVirtualMethod(Type converterType, string name, Type[] parameters)
    {
        var method = converterType.GetMethod(
            name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            binder: null,
            parameters,
            modifiers: null);

        Assert.That(method, Is.Not.Null, $"{converterType.FullName}.{name}");
        Assert.That(method!.IsVirtual, Is.True, $"{converterType.FullName}.{name}");
    }
}

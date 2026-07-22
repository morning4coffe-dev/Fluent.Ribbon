using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class CompatibilityRuntimeHardeningTests
{
    [Test]
    public void IconAdapter_ReportsEmptyAndUnsupportedValuesWithoutDroppingContent()
    {
        var empty = global::Fluent.CompatibilityIconAdapter.Convert(null);
        var value = new object();
        var unsupported = global::Fluent.CompatibilityIconAdapter.Convert(value);
        var invalidUri = global::Fluent.CompatibilityIconAdapter.Convert("http://[invalid");

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(global::Fluent.CompatibilityIconConversionStatus.Empty));
            Assert.That(empty.CanRenderInCurrentTemplate, Is.True);

            Assert.That(
                unsupported.Status,
                Is.EqualTo(global::Fluent.CompatibilityIconConversionStatus.Unsupported));
            Assert.That(unsupported.PreservedValue, Is.SameAs(value));
            Assert.That(unsupported.ImageSource, Is.Null);
            Assert.That(unsupported.Error, Is.Not.Empty);

            Assert.That(
                invalidUri.Status,
                Is.EqualTo(global::Fluent.CompatibilityIconConversionStatus.Unsupported));
            Assert.That(invalidUri.PreservedValue, Is.EqualTo("http://[invalid"));
            Assert.That(invalidUri.Error, Does.Contain("valid"));
        });
    }

    [Test]
    public void IconAdapter_PublicResultExposesWinUiRepresentations()
    {
        var resultType = typeof(global::Fluent.CompatibilityIconConversionResult);

        Assert.Multiple(() =>
        {
            Assert.That(resultType.GetProperty("ImageSource")!.PropertyType, Is.EqualTo(typeof(ImageSource)));
            Assert.That(resultType.GetProperty("PreservedValue")!.PropertyType, Is.EqualTo(typeof(object)));
            Assert.That(resultType.GetProperty("Error")!.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(
                typeof(global::Fluent.CompatibilityIconAdapter).GetMethod(
                    nameof(global::Fluent.CompatibilityIconAdapter.GetLastResult),
                    [typeof(DependencyObject)]),
                Is.Not.Null);
        });
    }

    [Test]
    public void RuntimeSmokeHelper_IsPublicAndReturnsDetailedResult()
    {
        var run = typeof(global::Fluent.CompatibilityRuntimeSmoke).GetMethod(
            nameof(global::Fluent.CompatibilityRuntimeSmoke.Run),
            Type.EmptyTypes);
        var resultType = typeof(global::Fluent.CompatibilityRuntimeSmokeResult);

        Assert.Multiple(() =>
        {
            Assert.That(run, Is.Not.Null);
            Assert.That(run!.IsStatic, Is.True);
            Assert.That(run.ReturnType, Is.EqualTo(resultType));
            Assert.That(resultType.GetProperty("WrapperCount"), Is.Not.Null);
            Assert.That(resultType.GetProperty("AppliedTemplateCount"), Is.Not.Null);
            Assert.That(resultType.GetProperty("QuickAccessCloneCount"), Is.Not.Null);
            Assert.That(resultType.GetProperty("CommandExecutionCount"), Is.Not.Null);
            Assert.That(resultType.GetProperty("Failures"), Is.Not.Null);
            Assert.That(resultType.GetProperty("Succeeded"), Is.Not.Null);
        });
    }

    [Test]
    public void RuntimeSmokeHelper_RequiresConcreteWinUiUiThread()
    {
        Assert.That(
            () => global::Fluent.CompatibilityRuntimeSmoke.Run(),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("WinUI"));
    }

    [TestCase(typeof(global::Fluent.Button))]
    [TestCase(typeof(global::Fluent.ToggleButton))]
    [TestCase(typeof(global::Fluent.TextBox))]
    [TestCase(typeof(global::Fluent.ComboBox))]
    [TestCase(typeof(global::Fluent.DropDownButton))]
    public void Facade_ReRegistersObjectIconForExplicitAdaptation(Type facade)
    {
        var iconProperty = facade.GetProperty(
            "Icon",
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.DeclaredOnly);
        var iconField = facade.GetField(
            "IconProperty",
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.DeclaredOnly);

        Assert.Multiple(() =>
        {
            Assert.That(iconProperty, Is.Not.Null);
            Assert.That(iconProperty!.PropertyType, Is.EqualTo(typeof(object)));
            Assert.That(iconField, Is.Not.Null);
            Assert.That(iconField!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
        });
    }
}

namespace FluentUno.Tests.Data;

using System.ComponentModel;
using NUnit.Framework;

[TestFixture]
public class RibbonControlSizeDefinitionTests
{
    [Test]
    public void ConstructorShouldAcceptWpfMiddleSpelling()
    {
        var definition = new Fluent.RibbonControlSizeDefinition("Large Middle Small");

        Assert.Multiple(() =>
        {
            Assert.That(definition.Large, Is.EqualTo(Fluent.RibbonControlSize.Large));
            Assert.That(definition.Medium, Is.EqualTo(Fluent.RibbonControlSize.Medium));
            Assert.That(definition.Small, Is.EqualTo(Fluent.RibbonControlSize.Small));
        });
    }

    [Test]
    public void ConstructorShouldRepeatLastSpecifiedSize()
    {
        var definition = new Fluent.RibbonControlSizeDefinition("Small");

        Assert.Multiple(() =>
        {
            Assert.That(definition.Large, Is.EqualTo(Fluent.RibbonControlSize.Small));
            Assert.That(definition.Medium, Is.EqualTo(Fluent.RibbonControlSize.Small));
            Assert.That(definition.Small, Is.EqualTo(Fluent.RibbonControlSize.Small));
        });
    }

    [Test]
    public void MiddleShouldRemainACompatibleAlias()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Fluent.RibbonControlSize.Middle, Is.EqualTo(Fluent.RibbonControlSize.Medium));
            Assert.That(Fluent.RibbonGroupBoxState.Middle, Is.EqualTo(Fluent.RibbonGroupBoxState.Medium));
        });
    }

    [Test]
    public void ImplicitConversionsShouldPreserveWpfContract()
    {
        Fluent.RibbonControlSizeDefinition definition = "Large Middle Small";
        string serialized = definition;

        Assert.Multiple(() =>
        {
            Assert.That(definition.Middle, Is.EqualTo(Fluent.RibbonControlSize.Middle));
            Assert.That(serialized, Is.EqualTo("Large Middle Small"));
        });
    }

    [Test]
    public void TypeConverterShouldSupportWpfXamlStrings()
    {
        var converter = TypeDescriptor.GetConverter(typeof(Fluent.RibbonControlSizeDefinition));
        var definition = (Fluent.RibbonControlSizeDefinition)converter.ConvertFromInvariantString(
            "Large Middle Small")!;

        Assert.That(definition.Middle, Is.EqualTo(Fluent.RibbonControlSize.Middle));
    }
}

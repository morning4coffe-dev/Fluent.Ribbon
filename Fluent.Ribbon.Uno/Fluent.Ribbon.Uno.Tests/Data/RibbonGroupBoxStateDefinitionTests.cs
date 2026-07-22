namespace FluentUno.Tests.Data;

using System.ComponentModel;
using NUnit.Framework;

[TestFixture]
public class RibbonGroupBoxStateDefinitionTests
{
    [Test]
    public void ImplicitConversionsShouldPreserveWpfContract()
    {
        Fluent.RibbonGroupBoxStateDefinition definition = "Large Middle Small Collapsed";
        string serialized = definition;

        Assert.Multiple(() =>
        {
            Assert.That(
                definition.States,
                Is.EqualTo(new[]
                {
                    Fluent.RibbonGroupBoxState.Large,
                    Fluent.RibbonGroupBoxState.Medium,
                    Fluent.RibbonGroupBoxState.Small,
                    Fluent.RibbonGroupBoxState.Collapsed,
                }));
            Assert.That(serialized, Is.EqualTo("Large,Middle,Small,Collapsed"));
        });
    }

    [Test]
    public void TypeConverterShouldSupportWpfXamlStrings()
    {
        var converter = TypeDescriptor.GetConverter(typeof(Fluent.RibbonGroupBoxStateDefinition));
        var definition = (Fluent.RibbonGroupBoxStateDefinition)converter.ConvertFromInvariantString(
            "Large Middle Small Collapsed")!;

        Assert.That(definition.States[1], Is.EqualTo(Fluent.RibbonGroupBoxState.Middle));
    }
}

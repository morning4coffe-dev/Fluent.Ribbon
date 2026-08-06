namespace FluentUno.Tests.Helpers;

using Fluent.Helpers;
using NUnit.Framework;

[TestFixture]
public sealed class PropertyValueHelperTests
{
    [Test]
    public void MostDerivedMatchingPropertyWins()
    {
        Assert.That(
            PropertyValueHelper.GetPublicPropertyValue(
                new DerivedTaggedValue(),
                "tag"),
            Is.EqualTo("derived"));
    }

    private class BaseTaggedValue
    {
        public object Tag { get; } = "base";
    }

    private sealed class DerivedTaggedValue : BaseTaggedValue
    {
        public new object Tag { get; } = "derived";
    }
}

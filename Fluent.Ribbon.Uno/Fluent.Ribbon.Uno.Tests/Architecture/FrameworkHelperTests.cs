namespace FluentUno.Tests.Architecture;

using NUnit.Framework;

[TestFixture]
public class FrameworkHelperTests
{
    [Test]
    public void PresentationFrameworkVersionShouldDescribeTheUnoXamlAssembly()
    {
        Assert.That(Fluent.FrameworkHelper.PresentationFrameworkVersion, Is.Not.Null);
    }
}

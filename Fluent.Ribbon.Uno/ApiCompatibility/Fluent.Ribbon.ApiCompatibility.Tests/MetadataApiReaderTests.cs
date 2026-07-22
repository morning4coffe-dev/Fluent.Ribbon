using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class MetadataApiReaderTests
{
    [Test]
    public void Read_ExtractsPublicAndProtectedApiWithoutLoadingDependencies()
    {
        var assembly = new MetadataApiReader(new FrameworkTypeNormalizer())
            .Read(typeof(MetadataFixture).Assembly.Location);

        Assert.That(assembly.Types, Does.ContainKey(typeof(MetadataFixture).FullName!));
        var fixture = assembly.Types[typeof(MetadataFixture).FullName!];
        Assert.That(fixture.Members, Does.ContainKey($"P:{typeof(MetadataFixture).FullName}::Value()"));
        Assert.That(
            fixture.Members.Keys,
            Does.Contain($"M:{typeof(MetadataFixture).FullName}::OnValueChanged(System.String)"));
        Assert.That(
            fixture.Members.Keys,
            Has.None.EqualTo($"M:{typeof(MetadataFixture).FullName}::Hidden()"));
    }

    [Test]
    public void Read_CapturesLiteralValuesAndOptionalParameterDefaults()
    {
        var assembly = new MetadataApiReader(new FrameworkTypeNormalizer())
            .ReadCandidate(typeof(MetadataFixture).Assembly.Location);
        var fixture = assembly.Types[typeof(MetadataFixture).FullName!];
        var fixtureMode = assembly.Types[typeof(MetadataFixtureMode).FullName!];

        Assert.Multiple(() =>
        {
            Assert.That(
                fixture.Members[$"F:{typeof(MetadataFixture).FullName}::Answer"].LiteralValue,
                Is.EqualTo(new ApiConstant("Int32", "42")));
            Assert.That(
                fixtureMode.Members[$"F:{typeof(MetadataFixtureMode).FullName}::Second"].LiteralValue,
                Is.EqualTo(new ApiConstant("Int32", "2")));

            var configure = fixture.Members.Values.Single(
                member => member.Identity.StartsWith(
                    $"M:{typeof(MetadataFixture).FullName}::Configure(",
                    StringComparison.Ordinal));
            Assert.That(configure.Parameters[0].DefaultValue, Is.EqualTo(new ApiConstant("Int32", "3")));
            Assert.That(configure.Parameters[1].DefaultValue, Is.EqualTo(new ApiConstant("NullReference", "null")));
            Assert.That(configure.Parameters[2].DefaultValue, Is.EqualTo(new ApiConstant("String", "\"ribbon\"")));
            Assert.That(configure.Parameters[3].DefaultValue, Is.EqualTo(new ApiConstant("Int32", "2")));
        });
    }
}

public class MetadataFixture
{
    public const int Answer = 42;

    public string? Value { get; set; }

    public void Configure(
        int count = 3,
        string? name = null,
        string label = "ribbon",
        MetadataFixtureMode mode = MetadataFixtureMode.Second)
    {
    }

    protected virtual void OnValueChanged(string value)
    {
    }

    private void Hidden()
    {
    }
}

public enum MetadataFixtureMode
{
    First = 1,
    Second = 2
}

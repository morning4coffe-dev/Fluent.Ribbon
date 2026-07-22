using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class ApiAssemblyMergerTests
{
    [Test]
    public void Merge_CombinesTypesFromMultipleCandidateAssemblies()
    {
        var firstType = Type("Fluent.Button");
        var secondType = Type("Fluent.RibbonButton");

        var merged = new ApiAssemblyMerger().Merge(
        [
            Candidate("controls.dll", firstType),
            Candidate("compatibility.dll", secondType)
        ]);

        Assert.That(merged.Types.Keys, Is.EquivalentTo(["Fluent.Button", "Fluent.RibbonButton"]));
    }

    [Test]
    public void Merge_RejectsDuplicatePublicTypeNames()
    {
        var duplicate = Type("Fluent.Button");

        Assert.That(
            () => new ApiAssemblyMerger().Merge(
            [
                Candidate("controls.dll", duplicate),
                Candidate("compatibility.dll", duplicate)
            ]),
            Throws.TypeOf<InvalidDataException>()
                .With.Message.Contains("Fluent.Button")
                .And.Message.Contains("controls.dll")
                .And.Message.Contains("compatibility.dll"));
    }

    private static CandidateApiAssembly Candidate(string source, params ApiType[] types)
        => new(source, new ApiAssembly(types.ToDictionary(static type => type.Name, StringComparer.Ordinal)));

    private static ApiType Type(string name)
        => new(
            name,
            ApiTypeKind.Class,
            ApiAccessibility.Public,
            0,
            "System.Object",
            new HashSet<string>(StringComparer.Ordinal),
            false,
            false,
            new Dictionary<string, ApiMember>(StringComparer.Ordinal));
}

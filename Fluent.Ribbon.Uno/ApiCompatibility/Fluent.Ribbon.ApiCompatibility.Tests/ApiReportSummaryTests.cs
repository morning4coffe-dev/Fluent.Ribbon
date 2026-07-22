using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class ApiReportSummaryTests
{
    [Test]
    public void Write_GroupsByCategoryAndOrdersMostAffectedTypesFirst()
    {
        ApiIssue[] issues =
        [
            Issue(ApiIssueKind.MissingMember, "M:Fluent.Button::First()"),
            Issue(ApiIssueKind.IncompatibleMember, "P:Fluent.Button::Value"),
            Issue(ApiIssueKind.MissingMember, "M:Fluent.Gallery::Open()"),
            Issue(ApiIssueKind.MissingType, "Fluent.Window")
        ];
        using var writer = new StringWriter();

        ApiReportSummary.Write(writer, issues);

        var output = writer.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("MissingType: 1"));
            Assert.That(output, Does.Contain("MissingMember: 2"));
            Assert.That(output, Does.Contain("IncompatibleMember: 1"));
            Assert.That(
                output.IndexOf("Fluent.Button: 2", StringComparison.Ordinal),
                Is.LessThan(output.IndexOf("Fluent.Gallery: 1", StringComparison.Ordinal)));
            Assert.That(output, Does.Contain("Fluent.Button: 2 (IncompatibleMember=1, MissingMember=1)"));
            Assert.That(output, Does.Contain("Fluent.Window: 1 (MissingType=1)"));
        });
    }

    private static ApiIssue Issue(ApiIssueKind kind, string symbol)
    {
        return new ApiIssue(kind, $"{kind}:{symbol}", symbol, "details");
    }
}

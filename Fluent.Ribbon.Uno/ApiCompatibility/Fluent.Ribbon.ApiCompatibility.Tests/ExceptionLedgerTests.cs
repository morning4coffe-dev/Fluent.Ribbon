using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class ExceptionLedgerTests
{
    [Test]
    public void Parse_ApprovesOnlyExactIssueIds()
    {
        var ledger = ExceptionLedger.Parse(
            """
            {
              "schemaVersion": 1,
              "exceptions": [
                {
                  "id": "missing-type:Fluent.RibbonWindow",
                  "reason": "WPF window chrome has no cross-platform Uno equivalent."
                }
              ]
            }
            """);
        var approved = new ApiIssue(
            ApiIssueKind.MissingType,
            "missing-type:Fluent.RibbonWindow",
            "Fluent.RibbonWindow",
            "missing");
        var other = approved with { Id = "missing-type:Fluent.Other" };

        Assert.That(ledger.TryApprove(approved, out var entry), Is.True);
        Assert.That(entry!.Reason, Does.Contain("window chrome"));
        Assert.That(ledger.TryApprove(other, out _), Is.False);
    }

    [Test]
    public void Parse_RejectsDuplicateIds()
    {
        const string Json =
            """
            {
              "schemaVersion": 1,
              "exceptions": [
                { "id": "missing-type:Fluent.A", "reason": "first" },
                { "id": "missing-type:Fluent.A", "reason": "second" }
              ]
            }
            """;

        Assert.That(
            () => ExceptionLedger.Parse(Json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("Duplicate"));
    }

    [Test]
    public void Parse_SchemaTwoRequiresDetailFingerprint()
    {
        const string Json =
            """
            {
              "schemaVersion": 2,
              "exceptions": [
                { "id": "missing-type:Fluent.A", "reason": "approved" }
              ]
            }
            """;

        Assert.That(
            () => ExceptionLedger.Parse(Json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("detailFingerprint"));
    }

    [Test]
    public void Parse_RejectsMalformedDetailFingerprint()
    {
        const string Json =
            """
            {
              "schemaVersion": 2,
              "exceptions": [
                {
                  "id": "missing-type:Fluent.A",
                  "reason": "approved",
                  "detailFingerprint": "sha256:not-a-sha256-value"
                }
              ]
            }
            """;

        Assert.That(
            () => ExceptionLedger.Parse(Json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("unsupported"));
    }

    [Test]
    public void TryApprove_SchemaTwoRequiresMatchingNormalizedDetails()
    {
        const string Details = "return type expected System.Int32, actual System.String; accessibility expected Public, actual Protected";
        var fingerprint = IssueDetailFingerprint.Compute(Details);
        var ledger = ExceptionLedger.Parse(
            $$"""
            {
              "schemaVersion": 2,
              "exceptions": [
                {
                  "id": "incompatible-member:M:Fluent.A::Value()",
                  "reason": "reviewed framework difference",
                  "detailFingerprint": "{{fingerprint}}"
                }
              ]
            }
            """);
        var equivalent = new ApiIssue(
            ApiIssueKind.IncompatibleMember,
            "incompatible-member:M:Fluent.A::Value()",
            "M:Fluent.A::Value()",
            " accessibility expected   Public, actual Protected; return type expected System.Int32, actual System.String ");
        var mutated = equivalent with
        {
            Details = "accessibility expected Public, actual Private; return type expected System.Int32, actual System.String"
        };

        Assert.Multiple(() =>
        {
            Assert.That(ledger.TryApprove(equivalent, out _), Is.True);
            Assert.That(ledger.TryApprove(mutated, out _), Is.False);
            var mismatch = ledger.FindFingerprintMismatches([mutated]).Single();
            Assert.That(mismatch.Issue, Is.EqualTo(mutated));
            Assert.That(mismatch.ActualDetailFingerprint, Is.EqualTo(IssueDetailFingerprint.Compute(mutated.Details)));
        });
    }
}

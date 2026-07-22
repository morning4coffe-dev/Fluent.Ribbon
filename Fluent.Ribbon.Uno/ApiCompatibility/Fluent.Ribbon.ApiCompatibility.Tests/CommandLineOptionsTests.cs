using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class CommandLineOptionsTests
{
    [Test]
    public void Parse_KeepsSingleCandidateUsageWorking()
    {
        var options = CommandLineOptions.Parse(
        [
            "--reference", "reference.dll",
            "--candidate", "candidate.dll"
        ]);

        Assert.That(options.CandidatePaths, Is.EqualTo([Path.GetFullPath("candidate.dll")]));
    }

    [Test]
    public void Parse_PreservesAllRepeatedCandidateArguments()
    {
        var options = CommandLineOptions.Parse(
        [
            "--reference", "reference.dll",
            "--candidate", "controls.dll",
            "--candidate", "compatibility.dll"
        ]);

        Assert.That(
            options.CandidatePaths,
            Is.EqualTo(
            [
                Path.GetFullPath("controls.dll"),
                Path.GetFullPath("compatibility.dll")
            ]));
    }

    [Test]
    public void Parse_RequiresAtLeastOneCandidate()
    {
        Assert.That(
            () => CommandLineOptions.Parse(["--reference", "reference.dll"]),
            Throws.ArgumentException.With.Message.Contains("At least one --candidate"));
    }
}

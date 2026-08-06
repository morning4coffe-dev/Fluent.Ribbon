namespace FluentUno.Tests.Architecture;

using System;
using System.IO;
using NUnit.Framework;

[TestFixture]
public sealed class RibbonTabScrollingWorkaroundTests
{
    private const string IssueUrl = "https://github.com/unoplatform/uno/issues/19504";
    private const string WorkaroundCall =
        "RibbonGroupsContainerScrollViewer.SetEnableHorizontalWheelScrolling(_scrollViewer, true);";

    [Test]
    public void HorizontalWheelWorkaroundShouldRemainUnoOnlyAndTrackTheUpstreamIssue()
    {
        var source = File.ReadAllText(Path.Combine(FindControlsDirectory(), "RibbonTab.cs"));
        var callIndex = source.IndexOf(WorkaroundCall, StringComparison.Ordinal);
        Assert.That(callIndex, Is.GreaterThanOrEqualTo(0));

        var guardIndex = source.LastIndexOf("#if !WINDOWS", callIndex, StringComparison.Ordinal);
        var endGuardIndex = source.IndexOf("#endif", callIndex, StringComparison.Ordinal);
        var todoIndex = source.LastIndexOf("// TODO:", callIndex, StringComparison.Ordinal);
        Assert.That(todoIndex, Is.GreaterThanOrEqualTo(0));

        var issueIndex = source.IndexOf(IssueUrl, todoIndex, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(guardIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(endGuardIndex, Is.GreaterThan(callIndex));
            Assert.That(todoIndex, Is.GreaterThan(guardIndex).And.LessThan(callIndex));
            Assert.That(issueIndex, Is.GreaterThan(todoIndex).And.LessThan(callIndex));
        });
    }

    private static string FindControlsDirectory()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno",
                "Fluent.Ribbon.Uno.Controls",
                "Controls");
            if (File.Exists(Path.Combine(candidate, "RibbonTab.cs")))
            {
                return candidate;
            }

            candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno.Controls",
                "Controls");
            if (File.Exists(Path.Combine(candidate, "RibbonTab.cs")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not find the Fluent.Ribbon.Uno controls directory.");
    }
}

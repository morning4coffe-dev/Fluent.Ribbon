namespace FluentUno.Tests.Controls;

using System.IO;
using Fluent;
using NUnit.Framework;

[TestFixture]
public sealed class RibbonGroupSizingTests
{
    [Test]
    public void ExplicitSizeDefinitionShouldOverrideTheGroupCap()
    {
        var definition = new RibbonControlSizeDefinition(
            RibbonControlSize.Large,
            RibbonControlSize.Large,
            RibbonControlSize.Small);

        Assert.That(
            RibbonGroupBox.ResolveDefinedOrPreferredSize(
                hasExplicitDefinition: true,
                definition,
                RibbonGroupBoxState.Medium,
                preferred: RibbonControlSize.Large,
                cap: RibbonControlSize.Medium),
            Is.EqualTo(RibbonControlSize.Large));
    }

    [Test]
    public void AuthoredSizeWithoutDefinitionShouldRespectTheGroupCap()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                RibbonGroupBox.ResolveDefinedOrPreferredSize(
                    hasExplicitDefinition: false,
                    default,
                    RibbonGroupBoxState.Medium,
                    preferred: RibbonControlSize.Large,
                    cap: RibbonControlSize.Medium),
                Is.EqualTo(RibbonControlSize.Medium));
            Assert.That(
                RibbonGroupBox.ResolveDefinedOrPreferredSize(
                    hasExplicitDefinition: false,
                    default,
                    RibbonGroupBoxState.Medium,
                    preferred: RibbonControlSize.Small,
                    cap: RibbonControlSize.Medium),
                Is.EqualTo(RibbonControlSize.Small));
        });
    }

    [Test]
    public void DefinitionCanStartAtMediumInALargeGroup()
    {
        var definition = new RibbonControlSizeDefinition(
            RibbonControlSize.Medium,
            RibbonControlSize.Medium,
            RibbonControlSize.Small);

        Assert.That(
            RibbonGroupBox.ResolveDefinedOrPreferredSize(
                hasExplicitDefinition: true,
                definition,
                RibbonGroupBoxState.Large,
                preferred: RibbonControlSize.Large,
                cap: RibbonControlSize.Large),
            Is.EqualTo(RibbonControlSize.Medium));
    }

    [Test]
    public void ExplicitAllLargeDefinitionShouldNotBeTreatedAsUnset()
    {
        Assert.That(
            RibbonControl.IsUnsetSizeDefinition(
                new RibbonControlSizeDefinition(
                    RibbonControlSize.Large,
                    RibbonControlSize.Large,
                    RibbonControlSize.Large)),
            Is.False);
        Assert.That(
            RibbonControl.IsUnsetSizeDefinition(
                new RibbonControlSizeDefinition(
                    (RibbonControlSize)(-1),
                    (RibbonControlSize)(-1),
                    (RibbonControlSize)(-1))),
            Is.True);
    }

    [Test]
    public void CollapsedPopupShouldRecursivelyEnlargePanelChildren()
    {
        var source = File.ReadAllText(FindRibbonGroupBoxSource());

        Assert.Multiple(() =>
        {
            Assert.That(
                source,
                Does.Contain("ApplyPopupItemSize(item);"));
            Assert.That(
                source,
                Does.Contain("ApplyPopupItemSize(child);"));
            Assert.That(
                source,
                Does.Contain("RibbonControl.TryGetEffectiveSizeDefinition("));
            Assert.That(
                source,
                Does.Contain("RibbonProperties.TryGetEffectiveSimplifiedSizeDefinition("));
        });
    }

    private static string FindRibbonGroupBoxSource()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno",
                "Fluent.Ribbon.Uno.Controls",
                "Controls",
                "RibbonGroupBox.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno.Controls",
                "Controls",
                "RibbonGroupBox.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not find RibbonGroupBox.cs.");
    }
}

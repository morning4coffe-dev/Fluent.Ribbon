namespace FluentUno.Tests.Architecture;

using System.IO;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class InRibbonGallerySubscriptionTests
{
    [Test]
    public void ItemsSourceSubscriptionShouldDetachWhenTheGalleryUnloads()
    {
        var source = File.ReadAllText(FindSourceFile());
        var compatibilitySource = File.ReadAllText(FindCompatibilitySourceFile());
        var automationSource = File.ReadAllText(FindAutomationSourceFile());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("Loaded += OnGalleryLoaded;"));
            Assert.That(source, Does.Contain("Unloaded += OnGalleryUnloaded;"));
            Assert.That(source, Does.Contain("UnsubscribeItemsSource();"));
            Assert.That(
                source,
                Does.Contain("_subscribedItemsSource.CollectionChanged -= OnItemsSourceCollectionChanged;"));
            Assert.That(
                source,
                Does.Contain("newSource is INotifyCollectionChanged newNotify && IsLoaded"));
            Assert.That(
                source,
                Does.Contain("_isRebuildingItemsSource = true;"));
            Assert.That(
                source,
                Does.Contain("RefreshSelectionContainerState();"));
            Assert.That(
                compatibilitySource,
                Does.Contain("clone.CopySourceItemMappingsFrom(owner, owner._quickAccessTransferredItems);"));
            Assert.That(
                compatibilitySource,
                Does.Contain("private void SyncActiveQuickAccessClone()"));
            Assert.That(
                source,
                Does.Contain("SyncActiveQuickAccessClone();"));
            Assert.That(
                automationSource,
                Does.Contain("OwnerGallery.FindSelectionContainer(OwnerGallery.SelectedItem)"));
        });
    }

    private static string FindSourceFile()
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
                "InRibbonGallery.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno.Controls",
                "Controls",
                "InRibbonGallery.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not find InRibbonGallery.cs.");
    }

    private static string FindCompatibilitySourceFile()
        => FindSourceFile(
            "Fluent.Ribbon.Uno.Controls",
            "Compatibility",
            "InRibbonGallery.Compatibility.cs");

    private static string FindAutomationSourceFile()
        => FindSourceFile(
            "Fluent.Ribbon.Uno.Controls",
            "Automation",
            "Peers",
            "RibbonControlAutomationPeers.cs");

    private static string FindSourceFile(params string[] relativePath)
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            foreach (var prefix in new[]
                     {
                         Path.Combine(directory.FullName, "Fluent.Ribbon.Uno"),
                         directory.FullName,
                     })
            {
                var candidate = relativePath.Aggregate(prefix, Path.Combine);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not find {Path.Combine(relativePath)}.");
    }
}

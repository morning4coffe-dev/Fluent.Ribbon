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
        var quickAccessSource = File.ReadAllText(FindSourceFile(
            "Fluent.Ribbon.Uno.Controls",
            "Compatibility",
            "InRibbonGallery.QuickAccess.cs"));
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
                Does.Contain("newSource is INotifyCollectionChanged newNotify && (IsLoaded || _activeQuickAccessClone is not null)"));
            Assert.That(
                source,
                Does.Contain("_isRebuildingItemsSource = true;"));
            Assert.That(
                source,
                Does.Contain("RefreshSelectionContainerState();"));
            Assert.That(
                source,
                Does.Contain("return owner.FindSelectionContainer(selectionValue, requireCurrentItem);"));
            Assert.That(source, Does.Contain("owner.GetSelectionValue(container)"));
            Assert.That(quickAccessSource, Does.Contain("clone.Items = Items;"));
            Assert.That(
                quickAccessSource,
                Does.Contain("bindings.Bind(SelectedItemProperty, twoWay: true);"));
            Assert.That(
                quickAccessSource,
                Does.Contain("copy._quickAccessDataActive = false;"));
            Assert.That(
                quickAccessSource,
                Does.Contain("_quickAccessObservedItemsSource.CollectionChanged -= _quickAccessItemsSourceChanged;"));
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

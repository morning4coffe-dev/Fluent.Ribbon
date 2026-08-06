namespace FluentUno.Tests.Controls;

using Fluent;
using NUnit.Framework;
using Windows.Foundation;

[TestFixture]
public sealed class CompactRibbonLayoutTests
{
    [Test]
    public void SimplifiedToolbarShouldUseItsSimplifiedSizeDefinition()
    {
        var regular = new RibbonControlSizeDefinition(
            RibbonControlSize.Large,
            RibbonControlSize.Large,
            RibbonControlSize.Small);
        var simplified = new RibbonControlSizeDefinition(
            RibbonControlSize.Large,
            RibbonControlSize.Medium,
            RibbonControlSize.Small);

        Assert.Multiple(() =>
        {
            Assert.That(
                RibbonToolBar.ResolveWrapItemSize(
                    RibbonControlSize.Medium,
                    regular,
                    simplified,
                    isSimplified: true),
                Is.EqualTo(RibbonControlSize.Medium));
            Assert.That(
                RibbonToolBar.ResolveWrapItemSize(
                    RibbonControlSize.Medium,
                    regular,
                    simplified,
                    isSimplified: false),
                Is.EqualTo(RibbonControlSize.Large));
        });
    }

    [Test]
    public void SimplifiedToolbarItemsShouldShareOneCenteredRow()
    {
        var rects = RibbonToolBarPanel.BuildSingleRowRects(
            new[]
            {
                new Size(40, 48),
                new Size(60, 24),
                new Size(50, 30),
            },
            availableHeight: 52);

        Assert.Multiple(() =>
        {
            Assert.That(rects, Has.Count.EqualTo(3));
            Assert.That(rects[0], Is.EqualTo(new Rect(0, 2, 40, 48)));
            Assert.That(rects[1], Is.EqualTo(new Rect(40, 14, 60, 24)));
            Assert.That(rects[2], Is.EqualTo(new Rect(100, 11, 50, 30)));
            Assert.That(
                RibbonToolBarPanel.GetCenteredRowOffset(28, 24),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void CompactGalleryItemsShouldFitTheInlineViewport()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                CompactRibbonLayoutMath.FitInlineGalleryItemHeight(56, 48),
                Is.EqualTo(48));
            Assert.That(
                CompactRibbonLayoutMath.FitInlineGalleryItemHeight(40, 48),
                Is.EqualTo(40));
            Assert.That(
                CompactRibbonLayoutMath.UsesCompactGalleryItemPadding(48),
                Is.True);
            Assert.That(
                CompactRibbonLayoutMath.UsesCompactGalleryItemPadding(56),
                Is.False);
            Assert.That(
                CompactRibbonLayoutMath.ResolveInRibbonGalleryDisplayState(
                    isCollapsed: true,
                    isSimplified: true,
                    RibbonControlSize.Medium),
                Is.EqualTo("CollapsedToButtonSimplified"));
            Assert.That(
                CompactRibbonLayoutMath.ResolveInRibbonGalleryDisplayState(
                    isCollapsed: true,
                    isSimplified: false,
                    RibbonControlSize.Small),
                Is.EqualTo("CollapsedToButtonCompact"));
        });
    }
}

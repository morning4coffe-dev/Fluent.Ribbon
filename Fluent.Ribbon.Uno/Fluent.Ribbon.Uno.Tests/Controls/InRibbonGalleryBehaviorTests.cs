namespace FluentUno.Tests.Controls;

using Fluent;
using Microsoft.UI.Xaml.Controls;
using NUnit.Framework;

[TestFixture]
public class InRibbonGalleryBehaviorTests
{
    [Test]
    public void ReduceShouldStopAtTheConfiguredMinimum()
    {
        var current = 4;

        current = GalleryLayoutMath.ReduceItemsInRow(current, 2);
        current = GalleryLayoutMath.ReduceItemsInRow(current, 2);
        current = GalleryLayoutMath.ReduceItemsInRow(current, 2);

        Assert.That(current, Is.EqualTo(2));
    }

    [Test]
    public void EnlargeShouldStopAtTheConfiguredMaximum()
    {
        var current = 2;

        current = GalleryLayoutMath.EnlargeItemsInRow(current, 4);
        current = GalleryLayoutMath.EnlargeItemsInRow(current, 4);
        current = GalleryLayoutMath.EnlargeItemsInRow(current, 4);

        Assert.That(current, Is.EqualTo(4));
    }

    [Test]
    public void CurrentItemsShouldRemainValidWhenMinimumExceedsMaximum()
    {
        var current = GalleryLayoutMath.ClampCurrentItemsInRow(
            current: 1,
            minimum: 5,
            maximum: 3);

        Assert.That(current, Is.EqualTo(3));
    }

    [Test]
    public void MinimumColumnsShouldOverrideAConstrainedWidth()
    {
        var columns = GalleryLayoutMath.ComputeColumns(
            availableWidth: 15,
            effectiveCellWidth: 10,
            itemCount: 4,
            minimumColumns: 3,
            maximumColumns: 5,
            orientation: Orientation.Horizontal);

        Assert.That(columns, Is.EqualTo(3));
    }

    [Test]
    public void VerticalOrientationShouldAlwaysUseOneColumn()
    {
        var columns = GalleryLayoutMath.ComputeColumns(
            availableWidth: 100,
            effectiveCellWidth: 10,
            itemCount: 4,
            minimumColumns: 3,
            maximumColumns: 5,
            orientation: Orientation.Vertical);

        Assert.That(columns, Is.EqualTo(1));
    }

    [Test]
    public void AutoLayoutShouldBeSquareLikeWithoutAWidthConstraint()
    {
        var columns = GalleryLayoutMath.ComputeColumns(
            availableWidth: double.PositiveInfinity,
            effectiveCellWidth: 10,
            itemCount: 5,
            minimumColumns: 0,
            maximumColumns: 0,
            orientation: Orientation.Horizontal);

        Assert.That(columns, Is.EqualTo(3));
    }

    [Test]
    public void WidthSmallerThanOneCellShouldFallBackToSquareLikeLayout()
    {
        var columns = GalleryLayoutMath.ComputeColumns(
            availableWidth: 5,
            effectiveCellWidth: 10,
            itemCount: 5,
            minimumColumns: 0,
            maximumColumns: 0,
            orientation: Orientation.Horizontal);

        Assert.That(columns, Is.EqualTo(3));
    }

    [Test]
    public void MaximumColumnsShouldCapAutoLayout()
    {
        var columns = GalleryLayoutMath.ComputeColumns(
            availableWidth: double.PositiveInfinity,
            effectiveCellWidth: 10,
            itemCount: 8,
            minimumColumns: 0,
            maximumColumns: 2,
            orientation: Orientation.Horizontal);

        Assert.That(columns, Is.EqualTo(2));
    }
}

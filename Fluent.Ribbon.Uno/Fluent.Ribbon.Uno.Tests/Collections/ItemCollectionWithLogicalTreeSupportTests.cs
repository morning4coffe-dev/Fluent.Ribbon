namespace FluentUno.Tests.Collections;

using System.Collections.Generic;
using Fluent.Collections;
using NUnit.Framework;

[TestFixture]
public class ItemCollectionWithLogicalTreeSupportTests
{
    [Test]
    public void OwnershipShouldNotifyParent()
    {
        var parent = new LogicalChildOwner();
        var collection = new ItemCollectionWithLogicalTreeSupport<string>(parent);

        collection.Add("One");
        collection.ReleaseLogicalOwnership();
        collection.AquireLogicalOwnership();

        Assert.Multiple(() =>
        {
            Assert.That(parent.Added, Is.EqualTo(new[] { "One", "One" }));
            Assert.That(parent.Removed, Is.EqualTo(new[] { "One" }));
            Assert.That(collection.GetLogicalChildren(), Is.EqualTo(new[] { "One" }));
        });
    }

    private sealed class LogicalChildOwner : Fluent.ILogicalChildSupport
    {
        public List<object> Added { get; } = [];

        public List<object> Removed { get; } = [];

        public void AddLogicalChild(object child)
        {
            Added.Add(child);
        }

        public void RemoveLogicalChild(object child)
        {
            Removed.Add(child);
        }
    }
}

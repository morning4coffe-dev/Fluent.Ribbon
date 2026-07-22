namespace FluentUno.Tests.Collections;

using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using Fluent.Collections;
using NUnit.Framework;

[TestFixture]
public class CollectionSyncHelperTests
{
    [Test]
    public void TargetShouldFollowSourceChanges()
    {
        var source = new ObservableCollection<string> { "One", "Two" };
        var target = new ArrayList();
        _ = new CollectionSyncHelper<string>(source, target);

        source.Insert(1, "Middle");
        source.Move(2, 0);
        source.Remove("One");

        Assert.That(target.Cast<string>(), Is.EqualTo(source));
    }
}

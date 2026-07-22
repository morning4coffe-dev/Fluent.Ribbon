namespace Fluent.Collections;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

/// <summary>
/// Synchronizes a target collection with a source collection in one direction.
/// </summary>
public class CollectionSyncHelper<TItem>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionSyncHelper{TItem}"/> class.
    /// </summary>
    public CollectionSyncHelper(ObservableCollection<TItem> source, IList target)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));

        SyncTarget();
        Source.CollectionChanged += SourceOnCollectionChanged;
    }

    /// <summary>
    /// Gets the source collection.
    /// </summary>
    public ObservableCollection<TItem> Source { get; }

    /// <summary>
    /// Gets the synchronized target collection.
    /// </summary>
    public IList Target { get; }

    private void SyncTarget()
    {
        Target.Clear();
        foreach (var item in Source)
        {
            Target.Add(item);
        }
    }

    private void SourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (var index = 0; index < e.NewItems?.Count; index++)
                {
                    Target.Insert(e.NewStartingIndex + index, e.NewItems![index]);
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (var item in e.OldItems)
                    {
                        Target.Remove(item);
                    }
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems is not null)
                {
                    foreach (var item in e.OldItems)
                    {
                        Target.Remove(item);
                    }
                }

                if (e.NewItems is not null)
                {
                    foreach (var item in e.NewItems)
                    {
                        Target.Add(item);
                    }
                }

                break;

            case NotifyCollectionChangedAction.Move:
                if (e.OldItems is { Count: > 0 })
                {
                    var item = e.OldItems[0];
                    Target.Remove(item);
                    Target.Insert(e.NewStartingIndex, item);
                }

                break;

            case NotifyCollectionChangedAction.Reset:
                SyncTarget();
                break;
        }
    }
}

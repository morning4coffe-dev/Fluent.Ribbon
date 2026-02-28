namespace Fluent;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that notifies an owner when items
/// are added or removed, allowing the owner to manage logical tree membership.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. WPF has explicit AddLogicalChild/RemoveLogicalChild;
/// in WinUI, this collection mainly provides notification hooks for owners that need
/// to react to child changes.
/// </remarks>
public class ItemCollectionWithLogicalTreeSupport<T> : ObservableCollection<T>
    where T : DependencyObject
{
    private readonly Action<T>? _onItemAdded;
    private readonly Action<T>? _onItemRemoved;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCollectionWithLogicalTreeSupport{T}"/> class.
    /// </summary>
    /// <param name="onItemAdded">Callback when an item is added.</param>
    /// <param name="onItemRemoved">Callback when an item is removed.</param>
    public ItemCollectionWithLogicalTreeSupport(
        Action<T>? onItemAdded = null,
        Action<T>? onItemRemoved = null)
    {
        _onItemAdded = onItemAdded;
        _onItemRemoved = onItemRemoved;
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);
        _onItemAdded?.Invoke(item);
    }

    /// <inheritdoc/>
    protected override void RemoveItem(int index)
    {
        var removedItem = this[index];
        base.RemoveItem(index);
        _onItemRemoved?.Invoke(removedItem);
    }

    /// <inheritdoc/>
    protected override void SetItem(int index, T item)
    {
        var replacedItem = this[index];
        base.SetItem(index, item);
        _onItemRemoved?.Invoke(replacedItem);
        _onItemAdded?.Invoke(item);
    }

    /// <inheritdoc/>
    protected override void ClearItems()
    {
        var items = new List<T>(this);
        base.ClearItems();

        foreach (var item in items)
        {
            _onItemRemoved?.Invoke(item);
        }
    }
}

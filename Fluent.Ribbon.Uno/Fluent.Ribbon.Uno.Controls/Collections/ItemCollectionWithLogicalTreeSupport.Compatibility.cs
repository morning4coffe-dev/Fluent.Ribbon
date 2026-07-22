namespace Fluent.Collections;

/// <summary>
/// Collection that coordinates logical-child ownership with a parent control.
/// </summary>
public class ItemCollectionWithLogicalTreeSupport<TItem> : ObservableCollection<TItem>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCollectionWithLogicalTreeSupport{TItem}"/> class.
    /// </summary>
    public ItemCollectionWithLogicalTreeSupport(ILogicalChildSupport parent)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <summary>
    /// Gets whether the parent currently owns the collection items logically.
    /// </summary>
    public bool IsOwningItems { get; private set; } = true;

    /// <summary>
    /// Gets the parent that manages logical children.
    /// </summary>
    public ILogicalChildSupport Parent { get; }

    /// <summary>
    /// Adds all items to the logical tree.
    /// </summary>
    public void AquireLogicalOwnership()
    {
        if (IsOwningItems)
        {
            return;
        }

        IsOwningItems = true;
        foreach (var item in Items)
        {
            AddLogicalChild(item);
        }
    }

    /// <summary>
    /// Removes all items from the logical tree.
    /// </summary>
    public void ReleaseLogicalOwnership()
    {
        if (!IsOwningItems)
        {
            return;
        }

        foreach (var item in Items)
        {
            RemoveLogicalChild(item);
        }

        IsOwningItems = false;
    }

    /// <summary>
    /// Returns the items currently owned by the parent.
    /// </summary>
    public IEnumerable<TItem> GetLogicalChildren()
    {
        return IsOwningItems ? Items : Enumerable.Empty<TItem>();
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, TItem item)
    {
        base.InsertItem(index, item);
        AddLogicalChild(item);
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        RemoveLogicalChild(this[index]);
        base.RemoveItem(index);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, TItem item)
    {
        RemoveLogicalChild(this[index]);
        base.SetItem(index, item);
        AddLogicalChild(item);
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        foreach (var item in Items)
        {
            RemoveLogicalChild(item);
        }

        base.ClearItems();
    }

    private void AddLogicalChild(TItem item)
    {
        if (IsOwningItems && item is not null)
        {
            Parent.AddLogicalChild(item);
        }
    }

    private void RemoveLogicalChild(TItem item)
    {
        if (IsOwningItems && item is not null)
        {
            Parent.RemoveLogicalChild(item);
        }
    }
}

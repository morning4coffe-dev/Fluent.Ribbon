namespace Fluent.Helpers;

using System.Collections;
using Windows.Foundation.Collections;

/// <summary>
/// Connects a native item collection to controls whose templates host live containers directly.
/// </summary>
internal sealed class ItemsControlBinding
{
    internal static readonly DependencyProperty HasItemsProperty =
        DependencyProperty.RegisterAttached("HasItems", typeof(bool), typeof(ItemsControlBinding), new PropertyMetadata(false));

    private readonly ItemsControl owner;
    private readonly ObservableCollection<UIElement> containers;
    private readonly Func<object, bool> isOwnContainer;
    private readonly Func<DependencyObject> createContainer;
    private readonly Action<DependencyObject, object> prepareContainer;
    private readonly Action<DependencyObject, object> clearContainer;
    private readonly Action<NotifyCollectionChangedEventArgs> itemsChanged;
    private List<Entry> entries = [];
    private bool isSuspended;
    private int presentationLeases;
    private ItemsControl? customItemsHost;
    private readonly HashSet<DependencyObject> nativeContainers = [];
    private SourceSubscription? sourceSubscription;
#if !WINDOWS
    private NativeItemsObservation? nativeItemsObservation;
#endif

    internal ItemsControlBinding(
        ItemsControl owner,
        ObservableCollection<UIElement> containers,
        Func<object, bool> isOwnContainer,
        Func<DependencyObject> createContainer,
        Action<DependencyObject, object> prepareContainer,
        Action<DependencyObject, object> clearContainer,
        Action<NotifyCollectionChangedEventArgs> itemsChanged)
    {
        this.owner = owner;
        this.containers = containers;
        this.isOwnContainer = isOwnContainer;
        this.createContainer = createContainer;
        this.prepareContainer = prepareContainer;
        this.clearContainer = clearContainer;
        this.itemsChanged = itemsChanged;
        if (containers is AuthoredItemCollection authoredItems)
        {
            authoredItems.Binding = this;
        }

        containers.CollectionChanged += OnContainersChanged;
        owner.Items.VectorChanged += OnNativeItemsChanged;
        owner.RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, OnItemsSourceChanged);
        owner.RegisterPropertyChangedCallback(ItemsControl.ItemTemplateProperty, OnPresentationChanged);
        owner.RegisterPropertyChangedCallback(ItemsControl.ItemTemplateSelectorProperty, OnPresentationChanged);
        owner.RegisterPropertyChangedCallback(ItemsControl.ItemContainerStyleProperty, OnPresentationChanged);
        owner.RegisterPropertyChangedCallback(ItemsControl.ItemContainerStyleSelectorProperty, OnPresentationChanged);
        owner.RegisterPropertyChangedCallback(ItemsControl.DisplayMemberPathProperty, OnPresentationChanged);
        owner.RegisterPropertyChangedCallback(ItemsControl.ItemsPanelProperty, OnPresentationChanged);
        owner.Loaded += OnLoaded;
        owner.Unloaded += OnUnloaded;
    }

    internal bool IsUpdating { get; private set; }
    internal bool IsSuspended => isSuspended;
    internal bool UsesNativeGenerator { get; set; }

    internal DependencyObject AcquireNativeContainer(object? item)
    {
        if (!entries.Any(candidate => SameItem(candidate.Item, item)))
        {
            Refresh();
        }
        var entry = entries.FirstOrDefault(candidate =>
            SameItem(candidate.Item, item) && !nativeContainers.Contains(candidate.Container));
        if (entry is null)
        {
            throw new InvalidOperationException("The native generator requested an item without a canonical container.");
        }
        nativeContainers.Add(entry.Container);
        return entry.Container;
    }

    internal bool ReleaseNativeContainer(DependencyObject container)
    {
        nativeContainers.Remove(container);
        return entries.Any(entry => ReferenceEquals(entry.Container, container));
    }

    internal IDisposable AcquirePresentationLease()
    {
        presentationLeases++;
        var lease = new PresentationLease(this);
        try
        {
            if (presentationLeases == 1)
            {
                OnLoaded(owner, new RoutedEventArgs());
            }

            return lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    internal static ObservableCollection<UIElement> CreateItems(ItemsControl owner) => new AuthoredItemCollection(owner);

    internal void SynchronizePanel(Panel panel)
    {
        if (isSuspended)
        {
            return;
        }

        if (HasCustomItemsPanel())
        {
            if (customItemsHost is null)
            {
                foreach (var item in containers)
                {
                    ItemsControlHelper.DetachFromParent(item);
                }

                customItemsHost = new ItemsControl
                {
                    IsTabStop = false,
                    ItemsSource = containers,
                };
            }

            customItemsHost.ItemsPanel = owner.ItemsPanel;
            if (!ReferenceEquals(VisualTreeHelper.GetParent(customItemsHost), panel))
            {
                ItemsControlHelper.DetachFromParent(customItemsHost);
                panel.Children.Clear();
                panel.Children.Add(customItemsHost);
            }

            return;
        }

        if (customItemsHost is not null)
        {
            customItemsHost.ItemsSource = null;
            foreach (var item in containers)
            {
                ItemsControlHelper.DetachFromParent(item);
            }
            ItemsControlHelper.DetachFromParent(customItemsHost);
            customItemsHost = null;
        }

        panel.Children.Clear();
        foreach (var item in containers)
        {
            ItemsControlHelper.DetachFromParent(item);
            panel.Children.Add(item);
        }
    }

    private bool HasCustomItemsPanel()
    {
        if (owner.ItemsPanel is null)
        {
            return false;
        }

        if (owner.ReadLocalValue(ItemsControl.ItemsPanelProperty) != DependencyProperty.UnsetValue)
        {
            return true;
        }

        for (var style = owner.Style; style is not null; style = style.BasedOn)
        {
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == ItemsControl.ItemsPanelProperty))
            {
                return true;
            }
        }

        return false;
    }

    internal UIElement? ContainerFromIndex(int index)
    {
        RefreshUnnotifiedNativeItems();
        return index >= 0 && index < containers.Count ? containers[index] : null;
    }

    internal int IndexFromContainer(DependencyObject container)
    {
        RefreshUnnotifiedNativeItems();
        return container is UIElement element ? containers.IndexOf(element) : -1;
    }

    internal object? ItemFromContainer(DependencyObject container)
    {
        RefreshUnnotifiedNativeItems();
        return entries.FirstOrDefault(entry => ReferenceEquals(entry.Container, container))?.Item;
    }

    internal UIElement? ContainerFromItem(object? item)
    {
        RefreshUnnotifiedNativeItems();
        // Prefer reference identity, including the particular occurrence of an authored visual.
        var entry = entries.FirstOrDefault(entry => ReferenceEquals(entry.Item, item));
        entry ??= entries.FirstOrDefault(entry => Equals(entry.Item, item));
        return entry?.Container;
    }

    internal void RefreshUnnotifiedNativeItems()
    {
#if !WINDOWS
        // Uno's ItemCollection indexer changes its backing list without VectorChanged.
        // Reconcile those replacements by slot, not by greedily matching repeated models.
        if (IsUpdating || isSuspended || owner.ItemsSource is not null || owner.Items.Count != entries.Count)
        {
            return;
        }

        for (var index = 0; index < entries.Count; index++)
        {
            if (!SameItem(entries[index].Item, owner.Items[index]))
            {
                RefreshCore(
                    entries.Cast<Entry?>().ToArray(),
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                return;
            }
        }
#endif
    }

    internal void Refresh() => RefreshCore(null, null);

    private void RefreshCore(
        IReadOnlyList<Entry?>? preferredEntries,
        NotifyCollectionChangedEventArgs? change)
    {
        if (IsUpdating)
        {
            return;
        }

        // Reading the source snapshot, rather than an intermediate native Reset/Insert sequence,
        // makes a source replacement or range notification one container/selection transaction.
        var source = owner.ItemsSource as IEnumerable ?? owner.Items;
        var items = source.Cast<object?>().ToArray();
        var available = new List<Entry>(entries);
        var replacement = new List<Entry>(items.Length);
        IsUpdating = true;
        try
        {
            for (var index = 0; index < items.Length; index++)
            {
                var item = items[index];
                var hasPreferredEntry = preferredEntries?.Count == items.Length;
                var entry = hasPreferredEntry ? preferredEntries![index] : null;
                if (entry is not null && !SameItem(entry.Item, item))
                {
                    entry = null;
                }
                if (!hasPreferredEntry)
                {
                    entry = available.FirstOrDefault(candidate => SameItem(candidate.Item, item));
                }

                if (entry is not null)
                {
                    available.Remove(entry);
                }
                else
                {
                    var container = isOwnContainer(item!)
                        ? item as UIElement
                        : createContainer() as UIElement;
                    if (container is null)
                    {
                        throw new InvalidOperationException("An item container must be a UIElement.");
                    }

                    entry = new Entry(item, container);
                    Prepare(entry);
                }

                if (replacement.Any(candidate => ReferenceEquals(candidate.Container, entry.Container)))
                {
                    throw new InvalidOperationException("The same UIElement cannot appear in an items view twice.");
                }

                replacement.Add(entry);
            }

            var changed = entries.Count != replacement.Count
                          || !entries.SequenceEqual(replacement);
            entries = replacement;
            owner.SetValue(HasItemsProperty, entries.Count > 0);

            foreach (var removed in available)
            {
                Clear(removed);
            }

            for (var index = containers.Count - 1; index >= 0; index--)
            {
                if (!entries.Any(entry => ReferenceEquals(entry.Container, containers[index])))
                {
                    containers.RemoveAt(index);
                }
            }

            foreach (var removed in available)
            {
                if (!UsesNativeGenerator)
                {
                    ItemsControlHelper.DetachFromParent(removed.Container);
                }
            }

            for (var index = 0; index < entries.Count; index++)
            {
                var container = entries[index].Container;
                var currentIndex = containers.IndexOf(container);
                if (currentIndex < 0)
                {
                    containers.Insert(index, container);
                }
                else if (currentIndex != index)
                {
                    containers.Move(currentIndex, index);
                }
            }

            if (!changed && change is null)
            {
                return;
            }
        }
        finally
        {
            IsUpdating = false;
        }

        UpdateNativeItemsObservation();
        itemsChanged(change ?? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnContainersChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (IsUpdating)
        {
            return;
        }

        // The public Uno Items collection remains the authored-UIElement collection. Mirror it
        // into the native collection too, so native ItemsControl consumers see the same items.
        IsUpdating = true;
        try
        {
            var removed = entries.Where(entry => !containers.Contains(entry.Container)).ToArray();
            foreach (var entry in removed)
            {
                Clear(entry);
            }

            var previousEntries = entries;
            entries = containers.Select(container =>
                previousEntries.FirstOrDefault(entry => ReferenceEquals(entry.Container, container))
                ?? new Entry(container, container)).ToList();
            owner.SetValue(HasItemsProperty, entries.Count > 0);

            if (owner.ItemsSource is null)
            {
                SynchronizeNativeItems(args);
                foreach (var entry in entries.Except(previousEntries))
                {
                    Prepare(entry);
                }
            }
        }
        finally
        {
            IsUpdating = false;
        }

        UpdateNativeItemsObservation();
        itemsChanged(args);
    }

    private void SynchronizeNativeItems(NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (var offset = 0; offset < args.NewItems!.Count; offset++)
                {
                    var index = args.NewStartingIndex + offset;
                    owner.Items.Insert(index, entries[index].Item!);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                for (var offset = 0; offset < args.OldItems!.Count; offset++)
                {
                    owner.Items.RemoveAt(args.OldStartingIndex);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                for (var offset = 0; offset < args.NewItems!.Count; offset++)
                {
                    var index = args.NewStartingIndex + offset;
                    owner.Items[index] = entries[index].Item!;
                }
                break;
            case NotifyCollectionChangedAction.Move:
                var moved = new List<object>();
                for (var offset = 0; offset < args.OldItems!.Count; offset++)
                {
                    moved.Add(owner.Items[args.OldStartingIndex]);
                    owner.Items.RemoveAt(args.OldStartingIndex);
                }
                for (var offset = 0; offset < moved.Count; offset++)
                {
                    owner.Items.Insert(args.NewStartingIndex + offset, moved[offset]);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                owner.Items.Clear();
                foreach (var entry in entries)
                {
                    owner.Items.Add(entry.Item!);
                }
                break;
        }
    }

    private void OnNativeItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        if (IsUpdating || isSuspended
            || (sourceSubscription is not null && ReferenceEquals(sourceSubscription.Source, owner.ItemsSource)))
        {
            return;
        }

        var index = (int)args.Index;
        var change = args.CollectionChange switch
        {
            CollectionChange.ItemInserted
                when index >= 0 && index <= entries.Count && owner.Items.Count == entries.Count + 1
                => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, owner.Items[index], index),
            CollectionChange.ItemRemoved
                when index >= 0 && index < entries.Count && owner.Items.Count == entries.Count - 1
                => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, entries[index].Item, index),
            CollectionChange.ItemChanged
                when index >= 0 && index < entries.Count && owner.Items.Count == entries.Count
                => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, owner.Items[index], entries[index].Item, index),
            CollectionChange.Reset
                => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset),
            _ => null,
        };

        if (change is null)
        {
            Refresh();
        }
        else
        {
            OnSourceCollectionChanged(change);
        }
    }

    private void OnItemsSourceChanged(DependencyObject sender, DependencyProperty property)
    {
        StopNativeItemsObservation();
        ObserveSource();
        Refresh();
        UpdateNativeItemsObservation();
    }

    private void ObserveSource()
    {
        sourceSubscription?.Dispose();
        sourceSubscription = !isSuspended && owner.ItemsSource is INotifyCollectionChanged source
            ? new SourceSubscription(this, source)
            : null;
    }

    private void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        var preferred = entries.Cast<Entry?>().ToList();
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add
                when args.NewItems is not null && args.NewStartingIndex >= 0 && args.NewStartingIndex <= preferred.Count:
                preferred.InsertRange(args.NewStartingIndex, Enumerable.Repeat<Entry?>(null, args.NewItems.Count));
                break;
            case NotifyCollectionChangedAction.Remove
                when args.OldItems is not null && args.OldStartingIndex >= 0
                     && args.OldStartingIndex + args.OldItems.Count <= preferred.Count:
                preferred.RemoveRange(args.OldStartingIndex, args.OldItems.Count);
                break;
            case NotifyCollectionChangedAction.Replace
                when args.OldItems is not null && args.NewItems is not null
                     && args.OldStartingIndex >= 0 && args.NewStartingIndex == args.OldStartingIndex
                     && args.OldStartingIndex + args.OldItems.Count <= preferred.Count:
                var oldEntries = preferred.GetRange(args.OldStartingIndex, args.OldItems.Count);
                preferred.RemoveRange(args.OldStartingIndex, args.OldItems.Count);
                preferred.InsertRange(args.NewStartingIndex, args.NewItems.Cast<object?>().Select((item, index) =>
                    index < oldEntries.Count && SameItem(oldEntries[index]!.Item, item) ? oldEntries[index] : null));
                break;
            case NotifyCollectionChangedAction.Move
                when args.OldItems is not null && args.OldStartingIndex >= 0 && args.NewStartingIndex >= 0
                     && args.OldStartingIndex + args.OldItems.Count <= preferred.Count
                     && args.NewStartingIndex <= preferred.Count - args.OldItems.Count:
                var moved = preferred.GetRange(args.OldStartingIndex, args.OldItems.Count);
                preferred.RemoveRange(args.OldStartingIndex, args.OldItems.Count);
                preferred.InsertRange(args.NewStartingIndex, moved);
                break;
            default:
                RefreshCore(null, args);
                return;
        }

        RefreshCore(preferred, args);
    }

    private static bool SameItem(object? left, object? right)
        => ReferenceEquals(left, right) || (left is not null && left.GetType().IsValueType && Equals(left, right));

    private void OnPresentationChanged(DependencyObject sender, DependencyProperty property)
    {
        if (IsUpdating)
        {
            return;
        }

        IsUpdating = true;
        try
        {
            foreach (var entry in entries)
            {
                Prepare(entry);
            }
        }
        finally
        {
            IsUpdating = false;
        }

        itemsChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (isSuspended)
        {
            isSuspended = false;
            owner.Items.VectorChanged += OnNativeItemsChanged;
        }

        ObserveSource();
        RefreshUnnotifiedNativeItems();
        Refresh();
        UpdateNativeItemsObservation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (owner.IsLoaded || presentationLeases != 0)
        {
            return;
        }

        isSuspended = true;
        owner.Items.VectorChanged -= OnNativeItemsChanged;
        sourceSubscription?.Dispose();
        sourceSubscription = null;
        StopNativeItemsObservation();

    }

    private void UpdateNativeItemsObservation()
    {
#if !WINDOWS
        if (ShouldObserveNativeItems)
        {
            nativeItemsObservation ??= new NativeItemsObservation(this);
        }
        else
        {
            StopNativeItemsObservation();
        }
#endif
    }

    private void StopNativeItemsObservation()
    {
#if !WINDOWS
        nativeItemsObservation?.Dispose();
        nativeItemsObservation = null;
#endif
    }

#if !WINDOWS
    private bool ShouldObserveNativeItems
        => !isSuspended && (owner.IsLoaded || presentationLeases != 0) && owner.ItemsSource is null && owner.Items.Count > 0;

    // Uno's silent indexer offers no notification to subscribe to. Limit checks to
    // loaded native-Items views: one allocation-free O(n) scan per 100 ms when unchanged.
    private sealed class NativeItemsObservation : IDisposable
    {
        private readonly WeakReference<ItemsControlBinding> binding;
        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(100) };

        internal NativeItemsObservation(ItemsControlBinding binding)
        {
            this.binding = new WeakReference<ItemsControlBinding>(binding);
            timer.Tick += OnTick;
            timer.Start();
        }

        private void OnTick(object? sender, object args)
        {
            if (binding.TryGetTarget(out var target)
                && ReferenceEquals(target.nativeItemsObservation, this))
            {
                if (target.ShouldObserveNativeItems)
                {
                    target.RefreshUnnotifiedNativeItems();
                    return;
                }

                target.StopNativeItemsObservation();
            }
            else
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Tick -= OnTick;
        }
    }
#endif

    private sealed class PresentationLease(ItemsControlBinding binding) : IDisposable
    {
        private ItemsControlBinding? binding = binding;

        public void Dispose()
        {
            if (binding is not { } current)
            {
                return;
            }

            binding = null;
            current.presentationLeases--;
            if (current.presentationLeases == 0 && !current.owner.IsLoaded)
            {
                current.OnUnloaded(current.owner, new RoutedEventArgs());
            }
        }
    }

    private void Prepare(Entry entry)
    {
        if (entry.Container is FrameworkElement element)
        {
            var style = owner.ItemContainerStyle
                        ?? owner.ItemContainerStyleSelector?.SelectStyle(entry.Item, element);
            if ((entry.AppliedStyle is not null && ReferenceEquals(element.Style, entry.AppliedStyle))
                || element.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue)
            {
                if (style is null)
                {
                    if (entry.AppliedStyle is not null)
                    {
                        element.ClearValue(FrameworkElement.StyleProperty);
                    }
                }
                else
                {
                    element.Style = style;
                }

                entry.AppliedStyle = style;
            }
        }

        prepareContainer(entry.Container, entry.Item!);
    }

    private void Clear(Entry entry)
    {
        clearContainer(entry.Container, entry.Item!);
        if (entry.Container is FrameworkElement element
            && entry.AppliedStyle is not null
            && ReferenceEquals(element.Style, entry.AppliedStyle))
        {
            element.ClearValue(FrameworkElement.StyleProperty);
        }
    }

    internal static void PrepareContent(ItemsControl owner, DependencyObject container, object? item)
    {
        if (ReferenceEquals(container, item))
        {
            return;
        }

        if (container is FrameworkElement element)
        {
            element.DataContext = item;
        }

        var template = owner.ItemTemplate ?? owner.ItemTemplateSelector?.SelectTemplate(item, container);
        switch (container)
        {
            case MenuItem menuItem:
                SetContent(menuItem, MenuItem.HeaderProperty, owner, item, template);
                SetTemplate(menuItem, HeaderedItemsControl.HeaderTemplateProperty, template);
                break;
            case HeaderedItemsControl headered:
                SetContent(headered, HeaderedItemsControl.HeaderProperty, owner, item, template);
                SetTemplate(headered, HeaderedItemsControl.HeaderTemplateProperty, template);
                break;
            case ContentControl content:
                SetContent(content, ContentControl.ContentProperty, owner, item, template);
                SetTemplate(content, ContentControl.ContentTemplateProperty, template);
                break;
            case ContentPresenter presenter:
                SetContent(presenter, ContentPresenter.ContentProperty, owner, item, template);
                SetTemplate(presenter, ContentPresenter.ContentTemplateProperty, template);
                break;
        }
    }

    internal static void ClearContent(DependencyObject container, object? item)
    {
        if (ReferenceEquals(container, item))
        {
            return;
        }

        switch (container)
        {
            case MenuItem menuItem:
                menuItem.ClearValue(MenuItem.HeaderProperty);
                menuItem.ClearValue(HeaderedItemsControl.HeaderTemplateProperty);
                break;
            case HeaderedItemsControl headered:
                headered.ClearValue(HeaderedItemsControl.HeaderProperty);
                headered.ClearValue(HeaderedItemsControl.HeaderTemplateProperty);
                break;
            case ContentControl content:
                content.ClearValue(ContentControl.ContentProperty);
                content.ClearValue(ContentControl.ContentTemplateProperty);
                break;
            case ContentPresenter presenter:
                presenter.ClearValue(ContentPresenter.ContentProperty);
                presenter.ClearValue(ContentPresenter.ContentTemplateProperty);
                break;
        }

        if (container is FrameworkElement element)
        {
            element.ClearValue(FrameworkElement.DataContextProperty);
        }
    }

    private static void SetContent(
        FrameworkElement container,
        DependencyProperty property,
        ItemsControl owner,
        object? item,
        DataTemplate? template)
    {
        if (template is null && !string.IsNullOrEmpty(owner.DisplayMemberPath))
        {
            container.SetBinding(property, new Binding
            {
                Source = item,
                Path = new PropertyPath(owner.DisplayMemberPath),
                Mode = BindingMode.OneWay,
            });
        }
        else if (!ReferenceEquals(container.GetValue(property), item) || container.GetBindingExpression(property) is not null)
        {
            container.ClearValue(property);
            container.SetValue(property, item);
        }
    }

    private static void SetTemplate(DependencyObject container, DependencyProperty property, DataTemplate? template)
    {
        if (template is null)
        {
            container.ClearValue(property);
        }
        else if (!ReferenceEquals(container.GetValue(property), template))
        {
            container.SetValue(property, template);
        }
    }

    private sealed class Entry(object? item, UIElement container)
    {
        internal object? Item { get; } = item;
        internal UIElement Container { get; } = container;
        internal Style? AppliedStyle { get; set; }
    }

    private sealed class SourceSubscription : IDisposable
    {
        private readonly WeakReference<ItemsControlBinding> binding;
        internal INotifyCollectionChanged Source { get; }

        internal SourceSubscription(ItemsControlBinding binding, INotifyCollectionChanged source)
        {
            this.binding = new WeakReference<ItemsControlBinding>(binding);
            Source = source;
            source.CollectionChanged += OnChanged;
        }

        private void OnChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (binding.TryGetTarget(out var target))
            {
                if (ReferenceEquals(target.sourceSubscription, this))
                {
                    target.OnSourceCollectionChanged(args);
                }
            }
            else
            {
                Dispose();
            }
        }

        public void Dispose() => Source.CollectionChanged -= OnChanged;
    }

    private sealed class AuthoredItemCollection(ItemsControl owner) : ObservableCollection<UIElement>
    {
        internal ItemsControlBinding? Binding { get; set; }

        private void EnsureWritable()
        {
            if (owner.ItemsSource is not null && Binding?.IsUpdating != true)
            {
                throw new InvalidOperationException(
                    "Items cannot be modified while ItemsSource is set. Modify the source collection instead.");
            }
        }

        protected override void InsertItem(int index, UIElement item)
        {
            EnsureWritable();
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, UIElement item)
        {
            EnsureWritable();
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            EnsureWritable();
            base.RemoveItem(index);
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            EnsureWritable();
            base.MoveItem(oldIndex, newIndex);
        }

        protected override void ClearItems()
        {
            EnsureWritable();
            base.ClearItems();
        }
    }
}

namespace Fluent;

/// <summary>
/// Provides helper methods for managing logical child collections in Uno/WinUI.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. WPF has explicit LogicalTreeHelper.AddLogicalChild/RemoveLogicalChild;
/// WinUI manages the visual tree automatically. This helper provides collection synchronization
/// for controls that need to track logical children separately from visual children.
/// </remarks>
public static class CollectionSyncHelper
{
    /// <summary>
    /// Synchronizes a source collection to a target panel's children.
    /// Keeps the panel children in sync as items are added/removed from the source.
    /// </summary>
    /// <param name="source">The source collection to observe.</param>
    /// <param name="targetPanel">The panel whose children should be synchronized.</param>
    /// <returns>A disposable that removes the synchronization when disposed.</returns>
    public static IDisposable Synchronize(ObservableCollection<UIElement> source, Panel targetPanel)
    {
        // Initial sync
        targetPanel.Children.Clear();
        foreach (var item in source)
        {
            targetPanel.Children.Add(item);
        }

        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                    {
                        var index = e.NewStartingIndex;
                        foreach (UIElement item in e.NewItems)
                        {
                            targetPanel.Children.Insert(index++, item);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                    {
                        foreach (UIElement item in e.OldItems)
                        {
                            targetPanel.Children.Remove(item);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems is not null && e.NewItems is not null)
                    {
                        for (var i = 0; i < e.OldItems.Count; i++)
                        {
                            var oldItem = (UIElement)e.OldItems[i]!;
                            var idx = targetPanel.Children.IndexOf(oldItem);
                            if (idx >= 0 && i < e.NewItems.Count)
                            {
                                targetPanel.Children[idx] = (UIElement)e.NewItems[i]!;
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems is not null && e.OldItems.Count > 0)
                    {
                        var movedItem = (UIElement)e.OldItems[0]!;
                        targetPanel.Children.Remove(movedItem);
                        targetPanel.Children.Insert(e.NewStartingIndex, movedItem);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    targetPanel.Children.Clear();
                    foreach (var item in source)
                    {
                        targetPanel.Children.Add(item);
                    }
                    break;
            }
        }

        source.CollectionChanged += OnCollectionChanged;

        return new ActionDisposable(() => source.CollectionChanged -= OnCollectionChanged);
    }

    private sealed class ActionDisposable : IDisposable
    {
        private Action? _action;

        public ActionDisposable(Action action) => _action = action;

        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
        }
    }
}

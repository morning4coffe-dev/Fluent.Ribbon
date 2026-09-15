namespace Fluent;

public partial class InRibbonGallery
{
    private QuickAccessContentLease? _quickAccessPanelLease;
    private QuickAccessContentLease? _quickAccessSupplementalLease;
    private StackPanel? _popupSupplementalPanel;
    private StackPanel? _borrowedQuickAccessSupplementalPanel;
    private bool _quickAccessSelectionUpdating;
    private bool _quickAccessFiltersUpdating;
    private bool _waitingForQuickAccessGalleryClose;
    private Popup? _quickAccessClosingGalleryPopup;
    private EventHandler<object>? _quickAccessGalleryClosed;
    private WeakReference<InRibbonGallery>? _pendingQuickAccessGallery;
    private readonly List<WeakReference<InRibbonGallery>> _quickAccessDataCopies = [];
    private bool _quickAccessDataActive;
    private bool _quickAccessDataSourceObserved;
    private INotifyCollectionChanged? _quickAccessObservedItemsSource;
    private NotifyCollectionChangedEventHandler? _quickAccessItemsSourceChanged;

    private InRibbonGallery? QuickAccessGalleryOwner =>
        _quickAccessOwner is { } owner && owner.TryGetTarget(out var gallery) ? gallery : null;

    private void BindQuickAccessGallery(InRibbonGallery clone)
    {
        clone.CanAddToQuickAccessToolBar = false;
        clone.Items.CollectionChanged -= clone.OnItemsCollectionChanged;
        clone.Items = Items;
        _quickAccessDataCopies.Add(new(clone));
        clone._quickAccessDataActive = true;
        var bindings = QuickAccessBindingSession.For(this, clone);
        bindings.BindCommon();
        foreach (var property in new[]
                 {
                     HeaderTemplateProperty, HeaderTemplateSelectorProperty, IconGlyphProperty,
                     ItemTemplateProperty, ItemWidthProperty, ItemHeightProperty,
                     MinItemsInDropDownRowProperty, MaxItemsInDropDownRowProperty,
                     MaxDropDownHeightProperty, MaxDropDownWidthProperty, DropDownHeightProperty,
                     DropDownWidthProperty, ResizeModeProperty, GroupByProperty, GroupByAdvancedProperty,
                     OrientationProperty, SelectableProperty, ExpandButtonContentTemplateProperty,
                 })
        {
            bindings.Bind(property);
        }

        foreach (var property in new[] { HeaderProperty, IconProperty, LargeIconProperty, MediumIconProperty, ExpandButtonContentProperty })
        {
            bindings.BindPresentation(property, property);
        }
        bindings.Bind(MenuProperty);

        var source = new WeakReference<InRibbonGallery>(this);
        var target = new WeakReference<InRibbonGallery>(clone);
        NotifyCollectionChangedEventHandler filtersChanged = (_, _) =>
        {
            if (source.TryGetTarget(out var owner) && target.TryGetTarget(out var copy))
            {
                CopyQuickAccessFilters(owner, copy);
            }
        };
        Filters.CollectionChanged += filtersChanged;
        bindings.Activated += () =>
        {
            if (source.TryGetTarget(out var owner) && target.TryGetTarget(out var copy))
            {
                copy._quickAccessDataActive = true;
                owner.Filters.CollectionChanged += filtersChanged;
                CopyQuickAccessFilters(owner, copy);
                owner.RefreshQuickAccessDataObservation(refreshItems: true);
                owner.SyncActiveQuickAccessClone();
            }
        };
        bindings.Deactivated += () =>
        {
            if (source.TryGetTarget(out var owner))
            {
                owner.Filters.CollectionChanged -= filtersChanged;
            }

            if (target.TryGetTarget(out var copy))
            {
                copy._quickAccessDataActive = false;
                copy.IsDropDownOpen = false;
                copy.ReleaseQuickAccessGalleryContent();
            }
            if (source.TryGetTarget(out var currentOwner))
            {
                currentOwner.RefreshQuickAccessDataObservation();
            }
        };
        bindings.Bind(SelectedItemProperty, twoWay: true);
        bindings.Bind(SelectedIndexProperty, twoWay: true);
        bindings.Bind(
            SelectedFilterProperty, SelectedFilterProperty, twoWay: true,
            convertBack: (owner, value) =>
                target.TryGetTarget(out var copy) && copy._quickAccessFiltersUpdating
                    ? owner.GetValue(SelectedFilterProperty)
                    : value);
        if (!_quickAccessDataSourceObserved)
        {
            _quickAccessDataSourceObserved = true;
            RegisterPropertyChangedCallback(ItemsSourceProperty, (_, _) => RefreshQuickAccessDataObservation());
        }
        RefreshQuickAccessDataObservation(refreshItems: true);
    }

    private void RefreshQuickAccessDataObservation(bool refreshItems = false)
    {
        _quickAccessDataCopies.RemoveAll(reference => !reference.TryGetTarget(out _));
        var active = _quickAccessDataCopies.Any(reference =>
            reference.TryGetTarget(out var copy) && copy._quickAccessDataActive);
        var desired = active ? ItemsSource as INotifyCollectionChanged : null;
        var wasObserved = desired is not null
                          && (ReferenceEquals(desired, _quickAccessObservedItemsSource)
                              || ReferenceEquals(desired, _subscribedItemsSource));
        if (!ReferenceEquals(desired, _quickAccessObservedItemsSource))
        {
            if (_quickAccessObservedItemsSource is not null && _quickAccessItemsSourceChanged is not null)
            {
                _quickAccessObservedItemsSource.CollectionChanged -= _quickAccessItemsSourceChanged;
            }
            _quickAccessObservedItemsSource = desired;
            _quickAccessItemsSourceChanged = null;
            if (desired is not null)
            {
                var weak = new WeakReference<InRibbonGallery>(this);
                NotifyCollectionChangedEventHandler? handler = null;
                handler = (sender, args) =>
                {
                    if (weak.TryGetTarget(out var owner))
                    {
                        owner.RefreshQuickAccessDataObservation();
                        if (ReferenceEquals(owner._quickAccessObservedItemsSource, sender)
                            && !ReferenceEquals(owner._subscribedItemsSource, sender))
                        {
                            owner.OnItemsSourceCollectionChanged(sender, args);
                        }
                    }
                    else if (sender is INotifyCollectionChanged retired)
                    {
                        retired.CollectionChanged -= handler;
                    }
                };
                _quickAccessItemsSourceChanged = handler;
                desired.CollectionChanged += handler;
            }
        }

        if (active && refreshItems && !wasObserved && ItemsSource is not null
            && _subscribedItemsSource is null && _activeQuickAccessClone is null)
        {
            RebuildItemsFromSource();
        }
    }

    private static void CopyQuickAccessFilters(InRibbonGallery source, InRibbonGallery clone)
    {
        var selected = source.SelectedFilter;
        clone._quickAccessFiltersUpdating = true;
        try
        {
            foreach (var retired in clone.Filters.Where(filter => !source.Filters.Contains(filter)).ToArray())
            {
                clone.Filters.Remove(retired);
            }
            for (var index = 0; index < source.Filters.Count; index++)
            {
                var filter = source.Filters[index];
                var currentIndex = clone.Filters.IndexOf(filter);
                if (currentIndex < 0)
                {
                    clone.Filters.Insert(index, filter);
                }
                else if (currentIndex != index)
                {
                    clone.Filters.Move(currentIndex, index);
                }
            }

            clone.SelectedFilter = selected;
        }
        finally
        {
            clone._quickAccessFiltersUpdating = false;
        }
    }

    private StackPanel UpdateQuickAccessFilterButtons()
    {
        // An unrooted bar can still be completing native child-unload callbacks.
        // Retire it intact instead of detaching/reusing its realized buttons.
        if (_popupFilterBar?.IsLoaded != true)
        {
            _popupFilterBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Margin = new Thickness(4),
            };
        }

        var buttons = new List<UIElement>();
        for (var index = 0; index < Filters.Count; index++)
        {
            var filter = Filters[index];
            var button = _popupFilterBar.Children
                .OfType<Microsoft.UI.Xaml.Controls.Button>()
                .FirstOrDefault(item => ReferenceEquals(item.Tag, filter));
            if (button is null)
            {
                button = new Microsoft.UI.Xaml.Controls.Button
                {
                    Tag = filter,
                    MinHeight = 24,
                    Padding = new Thickness(8, 2, 8, 2),
                };
                button.SetBinding(ContentControl.ContentProperty, new Binding
                {
                    Source = filter,
                    Path = new PropertyPath(nameof(GalleryGroupFilter.Title)),
                });
                button.Click += OnFilterButtonClick;
            }
            button.FontWeight = ReferenceEquals(filter, SelectedFilter)
                ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(button, $"InRibbonGalleryFilter_{index}");
            buttons.Add(button);
        }
        SynchronizeQuickAccessPanel(_popupFilterBar, buttons);
        return _popupFilterBar;
    }

    private bool PrepareQuickAccessGalleryContent()
    {
        if (QuickAccessGalleryOwner is not { } owner || _popupScroller is null)
        {
            return false;
        }

        if (_quickAccessPanelLease is not null)
        {
            var panelReady = _quickAccessPanelLease.Acquire();
            var menuReady = _quickAccessSupplementalLease?.Acquire() == true;
            return panelReady && menuReady;
        }

        if (_waitingForQuickAccessGalleryClose)
        {
            return false;
        }

        if (owner._activeQuickAccessClone is { } other && !ReferenceEquals(other, this))
        {
            owner._pendingQuickAccessGallery = new(this);
            other.IsDropDownOpen = false;
            other.ReleaseQuickAccessGalleryContent();
            if (owner._activeQuickAccessClone is not null)
            {
                return false;
            }
        }

        if (owner._popup?.IsOpen == true)
        {
            _waitingForQuickAccessGalleryClose = true;
            var popup = owner._popup;
            _quickAccessClosingGalleryPopup = popup;
            _quickAccessGalleryClosed = (_, _) =>
            {
                CancelQuickAccessGalleryCloseWait();
                if (IsDropDownOpen && IsLoaded)
                {
                    ShowPopup();
                }
            };
            popup.Closed += _quickAccessGalleryClosed;
            owner.IsDropDownOpen = false;
            return false;
        }

        owner.ApplyTemplate();
        if (owner._galleryPanel is not { } panel || owner._scrollViewer is null)
        {
            throw new InvalidOperationException("The gallery template has no transferable items panel.");
        }

        if (!owner.IsLoaded && owner.ItemsSource is INotifyCollectionChanged notify)
        {
            if (!ReferenceEquals(owner._subscribedItemsSource, notify)
                && !ReferenceEquals(owner._quickAccessObservedItemsSource, notify))
            {
                owner.RebuildItemsFromSource();
            }
            owner.SubscribeItemsSource(notify);
        }

        owner.EnsurePopup();
        owner.RebuildPopupSupplementalContent();
        if (owner._popupSupplementalPanel is not { } supplemental || owner._popupPanel is null)
        {
            throw new InvalidOperationException("The source gallery has no supplemental content host.");
        }

        OnQuickAccessCloneOpened(this, EventArgs.Empty);
        _borrowedOwnerPanel = panel;
        _quickAccessSupplementalLease = new QuickAccessContentLease(
            supplemental,
            () =>
            {
                owner._popupPanel.Children.Remove(supplemental);
                _popupPanel?.Children.Remove(supplemental);
            },
            () =>
            {
                _borrowedQuickAccessSupplementalPanel = null;
                owner._popupPanel.Children.Add(supplemental);
            },
            () =>
            {
                _borrowedQuickAccessSupplementalPanel = supplemental;
                RebuildPopupSupplementalContent();
            },
            borrowed => OnQuickAccessGalleryTransferred(owner, borrowed),
            () => IsDropDownOpen = false);
        _quickAccessPanelLease = new QuickAccessContentLease(
            panel,
            () =>
            {
                if (ReferenceEquals(owner._scrollViewer.Content, panel))
                {
                    owner._scrollViewer.Content = null;
                }

                if (_popupScroller is not null && ReferenceEquals(_popupScroller.Content, panel))
                {
                    _popupScroller.Content = null;
                }
            },
            () =>
            {
                panel.ConfigureGrouping(null);
                owner.ConfigurePanel(panel, owner.MinItemsInRow, owner.GetCurrentItemsInRow());
                owner._scrollViewer.Content = panel;
            },
            () =>
            {
                ConfigurePanel(panel, MinItemsInDropDownRow, MaxItemsInDropDownRow);
                _popupScroller.Content = panel;
                PreparePopupContent();
            },
            borrowed => OnQuickAccessGalleryTransferred(owner, borrowed),
            () => IsDropDownOpen = false);
        var ready = _quickAccessPanelLease.Acquire();
        return _quickAccessSupplementalLease.Acquire() && ready;
    }

    private void ReleaseQuickAccessGalleryContent()
    {
        CancelQuickAccessGalleryCloseWait();
        _quickAccessPanelLease?.Release();
        _quickAccessSupplementalLease?.Release();
    }

    private void CancelQuickAccessGalleryCloseWait()
    {
        if (_quickAccessClosingGalleryPopup is { } popup && _quickAccessGalleryClosed is { } closed)
        {
            popup.Closed -= closed;
        }
        _quickAccessClosingGalleryPopup = null;
        _quickAccessGalleryClosed = null;
        _waitingForQuickAccessGalleryClose = false;
    }

    private void OnQuickAccessGalleryTransferred(InRibbonGallery owner, bool borrowed)
    {
        if (!borrowed
            && _quickAccessPanelLease is { IsBorrowed: false, IsMoving: false }
            && _quickAccessSupplementalLease is { IsBorrowed: false, IsMoving: false })
        {
            OnQuickAccessCloneClosed(this, EventArgs.Empty);
            _borrowedQuickAccessSupplementalPanel = null;
            _quickAccessPanelLease = null;
            _quickAccessSupplementalLease = null;
            if (owner.IsDropDownOpen && owner.IsLoaded)
            {
                owner.ShowPopup();
            }
            else if (owner._pendingQuickAccessGallery is { } pending && pending.TryGetTarget(out var next)
                     && next.IsDropDownOpen && next.IsLoaded)
            {
                owner._pendingQuickAccessGallery = null;
                next.ShowPopup();
            }
        }
        else if (borrowed)
        {
            owner.SyncInlineChildren();
        }

        if (IsDropDownOpen && IsLoaded
            && _quickAccessPanelLease?.IsBorrowed == true && _quickAccessSupplementalLease?.IsBorrowed == true)
        {
            ShowPopup();
        }
    }

    private void UpdateQuickAccessSupplementalItems()
    {
        _popupSupplementalPanel ??= new StackPanel();
        var children = new List<UIElement>();
        if (Menu is not null)
        {
            children.Add(Menu);
        }

        children.AddRange(MenuItems);
        SynchronizeQuickAccessPanel(_popupSupplementalPanel, children);
    }

    private static void SynchronizeQuickAccessPanel(Panel panel, IReadOnlyList<UIElement> children)
    {
        for (var index = panel.Children.Count - 1; index >= 0; index--)
        {
            if (!children.Contains(panel.Children[index]))
            {
                panel.Children.RemoveAt(index);
            }
        }

        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var currentIndex = panel.Children.IndexOf(child);
            if (currentIndex == index)
            {
                continue;
            }

            if (currentIndex >= 0)
            {
                panel.Children.RemoveAt(currentIndex);
            }
            else if (VisualTreeHelper.GetParent(child) is not null)
            {
                throw new InvalidOperationException("Gallery supplemental content is already owned by another visual host.");
            }

            panel.Children.Insert(index, child);
        }
    }

}

namespace Fluent;

public partial class RibbonGroupBox
{
    private WeakReference<RibbonGroupBox>? _quickAccessGroupSource;
    private RibbonGroupBox? _activeQuickAccessGroup;
    private WeakReference<RibbonGroupBox>? _pendingQuickAccessGroup;
    private QuickAccessContentLease? _quickAccessGroupLease;
    private IDisposable? _quickAccessGroupObservation;
    private Button? _quickAccessPopupLauncher;
    private Button? _inlineQuickAccessLauncher;
    private bool _waitingForQuickAccessSourceClose;
    private Popup? _quickAccessClosingSourcePopup;
    private EventHandler<object>? _quickAccessSourceClosed;
    private Panel? _quickAccessInlinePanelHost;
    private bool _quickAccessOriginalSnapped;

    private RibbonGroupBox? QuickAccessGroupSource =>
        _quickAccessGroupSource is { } source && source.TryGetTarget(out var group) ? group : null;

    private void BindQuickAccessGroup(RibbonGroupBox clone)
    {
        clone._quickAccessGroupSource = new(this);
        // Expose the canonical collection without giving its UIElements another native ItemsControl owner.
        clone.Items = Items;
        var bindings = QuickAccessBindingSession.For(this, clone);
        bindings.BindCommon();
        foreach (var property in new[]
                 {
                     HeaderTemplateProperty, HeaderTemplateSelectorProperty, IconGlyphProperty,
                     IsLauncherVisibleProperty, IsLauncherEnabledProperty, LauncherCommandProperty,
                     LauncherCommandParameterProperty, LauncherCommandTargetProperty,
                     LauncherTextProperty, LauncherKeysProperty, IsSimplifiedProperty,
                     ItemTemplateProperty, ItemTemplateSelectorProperty, ItemContainerStyleProperty,
                     ItemContainerStyleSelectorProperty, DisplayMemberPathProperty, HasItemsProperty,
                 })
        {
            bindings.Bind(property);
        }

        foreach (var property in new[] { HeaderProperty, IconProperty, LargeIconProperty, MediumIconProperty, LauncherIconProperty, LauncherToolTipProperty })
        {
            bindings.BindPresentation(property, property);
        }

        var source = new WeakReference<RibbonGroupBox>(this);
        clone.LauncherClick += (_, args) =>
        {
            if (source.TryGetTarget(out var owner))
            {
                owner.LauncherClick?.Invoke(owner, args);
            }
        };
        var target = new WeakReference<RibbonGroupBox>(clone);
        bindings.Deactivated += () =>
        {
            if (target.TryGetTarget(out var copy))
            {
                copy.IsDropDownOpen = false;
                copy._quickAccessGroupLease?.Release();
            }
        };
    }

    private bool PrepareQuickAccessGroupContent()
    {
        try
        {
            return PrepareQuickAccessGroupContentCore();
        }
        catch
        {
            FailQuickAccessGroupContent();
            throw;
        }
    }

    private void FailQuickAccessGroupContent()
    {
        _quickAccessGroupObservation?.Dispose();
        _quickAccessGroupObservation = null;
        if (_quickAccessGroupLease is null && QuickAccessGroupSource is { } source
                                          && ReferenceEquals(source._activeQuickAccessGroup, this))
        {
            source._activeQuickAccessGroup = null;
            source.IsSnapped = _quickAccessOriginalSnapped;
        }
        IsDropDownOpen = false;
    }

    private bool PrepareQuickAccessGroupContentCore()
    {
        if (!IsLoaded || QuickAccessGroupSource is not { } source || _popupItemsPanel is null)
        {
            return false;
        }

        if (_quickAccessGroupLease is not null)
        {
            return _quickAccessGroupLease.Acquire();
        }

        if (_waitingForQuickAccessSourceClose)
        {
            return false;
        }

        if (source._activeQuickAccessGroup is { } other && !ReferenceEquals(other, this))
        {
            source._pendingQuickAccessGroup = new(this);
            other.IsDropDownOpen = false;
            other._quickAccessGroupLease?.Release();
            if (source._activeQuickAccessGroup is not null)
            {
                return false;
            }
        }

        if (source._collapsedPopup?.IsOpen == true)
        {
            _waitingForQuickAccessSourceClose = true;
            var sourcePopup = source._collapsedPopup;
            _quickAccessClosingSourcePopup = sourcePopup;
            _quickAccessSourceClosed = (_, _) =>
            {
                CancelQuickAccessSourceCloseWait();
                if (IsDropDownOpen && IsLoaded)
                {
                    source.SyncItems();
                    ExpandForAutomation();
                }
            };
            sourcePopup.Closed += _quickAccessSourceClosed;
            source.IsDropDownOpen = false;
            return false;
        }

        if (source._itemsPanel is null)
        {
            source.ApplyTemplate();
        }

        if (source._itemsPanel is not { } panel)
        {
            throw new InvalidOperationException("The group template has no transferable items panel.");
        }

        var originalParent = VisualTreeHelper.GetParent(panel) as Panel
                             ?? panel.Parent as Panel
                             ?? source._quickAccessInlinePanelHost;
        if (originalParent is null || !originalParent.Children.Contains(panel))
        {
            throw new InvalidOperationException(
                $"The group items panel has no restorable template owner. Loaded={source.IsLoaded}, State={source.State}, " +
                $"VisualParent={VisualTreeHelper.GetParent(panel)?.GetType().Name}, Parent={panel.Parent?.GetType().Name}.");
        }

        var originalIndex = originalParent.Children.IndexOf(panel);
        var placeholder = new Border { Width = panel.ActualWidth, Height = panel.ActualHeight };
        var wasSnapped = source.IsSnapped;
        _quickAccessOriginalSnapped = wasSnapped;
        source.IsSnapped = true;
        source._activeQuickAccessGroup = this;
        _quickAccessGroupObservation = source.itemsBinding.AcquirePresentationLease();
        source.SyncItems();
        _quickAccessGroupLease = new QuickAccessContentLease(
            panel,
            () =>
            {
                if (originalParent.Children.Contains(panel))
                {
                    originalParent.Children.Remove(panel);
                    originalParent.Children.Insert(Math.Min(originalIndex, originalParent.Children.Count), placeholder);
                }

                _popupItemsPanel?.Children.Remove(panel);
            },
            () =>
            {
                originalParent.Children.Remove(placeholder);
                if (!originalParent.Children.Contains(panel))
                {
                    originalParent.Children.Insert(Math.Min(originalIndex, originalParent.Children.Count), panel);
                }
                source.IsSnapped = wasSnapped;
                source._activeQuickAccessGroup = null;
                _popupItemsPanel?.Children.Clear();
                LauncherButton = _inlineQuickAccessLauncher;
                _inlineQuickAccessLauncher = null;
                source.SyncItems();
                source.UpdateItemSizes();
            },
            () =>
            {
                _popupItemsPanel.Children.Add(panel);
                source.SyncItems();
                foreach (var item in source.Items)
                {
                    ApplyPopupItemSize(item);
                }

                AddQuickAccessLauncher();
            },
            borrowed =>
            {
                if (!borrowed)
                {
                    _quickAccessGroupObservation?.Dispose();
                    _quickAccessGroupObservation = null;
                    _quickAccessGroupLease = null;
                    if (source.IsDropDownOpen && source.IsLoaded)
                    {
                        source.ExpandForAutomation();
                    }
                    else if (source._pendingQuickAccessGroup is { } pending && pending.TryGetTarget(out var next)
                             && next.IsDropDownOpen && next.IsLoaded)
                    {
                        source._pendingQuickAccessGroup = null;
                        next.ExpandForAutomation();
                    }
                }

                if (IsDropDownOpen && IsLoaded)
                {
                    source.SyncItems();
                    ExpandForAutomation();
                }
            },
            FailQuickAccessGroupContent);
        return _quickAccessGroupLease.Acquire();
    }

    private void CancelQuickAccessSourceCloseWait()
    {
        if (_quickAccessClosingSourcePopup is { } popup && _quickAccessSourceClosed is { } closed)
        {
            popup.Closed -= closed;
        }
        _quickAccessClosingSourcePopup = null;
        _quickAccessSourceClosed = null;
        _waitingForQuickAccessSourceClose = false;
    }

    private void AddQuickAccessLauncher()
    {
        if (_popupItemsPanel is null)
        {
            return;
        }

        if (_quickAccessPopupLauncher is null)
        {
            _quickAccessPopupLauncher = new Button { Size = RibbonControlSize.Small, CanAddToQuickAccessToolBar = false };
            _quickAccessPopupLauncher.Click += OnLauncherButtonClick;
            _quickAccessPopupLauncher.SetBinding(VisibilityProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(IsLauncherVisible)),
                Converter = new Fluent.Converters.BoolToVisibilityConverter(),
            });
            _quickAccessPopupLauncher.SetBinding(RibbonButton.HeaderProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(LauncherText)),
            });
        }

        _inlineQuickAccessLauncher = LauncherButton;
        LauncherButton = _quickAccessPopupLauncher;
        UpdateLauncherMetadata();
        _popupItemsPanel.Children.Add(_quickAccessPopupLauncher);
    }

    private void ReleaseActiveQuickAccessGroup()
    {
        if (_activeQuickAccessGroup is { } clone)
        {
            clone.IsDropDownOpen = false;
            clone._quickAccessGroupLease?.Release();
        }
    }
}

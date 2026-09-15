namespace Fluent;

using Windows.Foundation;
using Windows.System;

public partial class RibbonTabControl
{
    private Popup? _minimizedPopup;
    private Border? _popupRoot;
    private ContentPresenter? _popupContentPresenter;
    private ContentPresenter? _contentOwner;
    private object? _presentedContent;
    private RibbonTabItem? _observedContentTab;
    private readonly List<(DependencyProperty Property, long Token)> _contentCallbacks = new();
    private WeakReference<UIElement>? _popupFocusBackup;
    private Ribbon? _popupRibbon;
    private XamlRoot? _popupXamlRoot;
    private int _presentationGeneration;
    private bool _popupReportedOpen;
    private bool _focusContentAfterPresentation;
    private int _completedPresentationGeneration = -1;
    private FrameworkElement? _contentReadinessRoot;
    private int _popupRequestVersion;
    private int? _closingPopupRequestVersion;
    private FrameworkElement? _focusReadinessRoot;
    private int _focusReadinessGeneration;
    private readonly Dictionary<FrameworkElement, ContentRelease> _releasingContent = new();

    internal bool IsMinimizedPopupVisible => _minimizedPopup?.IsOpen == true;

    internal event EventHandler? ContentPresentationChanged;

    internal bool IsContentPresented(RibbonTabItem tab)
    {
        var destination = IsMinimized && IsDropDownOpen ? _popupContentPresenter : _contentPresenter;
        if (!IsLoaded || _completedPresentationGeneration != _presentationGeneration
            || !ReferenceEquals(SelectedItem, tab) || !TabItems.Contains(tab)
            || destination is null || !destination.IsLoaded || !ReferenceEquals(_contentOwner, destination)
            || !ReferenceEquals(_presentedContent, tab.Content)
            || !ReferenceEquals(destination.Content, tab.Content)
            || (IsMinimized && !IsMinimizedPopupVisible))
        {
            return false;
        }

        if (tab.ContentTemplate is null && tab.ContentTemplateSelector is null
            && tab.Content is FrameworkElement content)
        {
            return content.IsLoaded && FocusRoutingHelper.IsDescendantOf(content, destination);
        }

        return tab.Content is null || VisualTreeHelper.GetChildrenCount(destination) > 0;
    }

    internal void PrepareSelectionPresentation()
    {
        if (IsMinimizedPopupVisible && IsDropDownOpen && XamlRoot is { } root
            && FocusManager.GetFocusedElement(root) is DependencyObject focused
            && _popupRoot is not null && FocusRoutingHelper.IsDescendantOf(focused, _popupRoot))
        {
            _focusContentAfterPresentation = true;
        }
    }

    private void ObserveSelectedContent()
    {
        var tab = SelectedItem as RibbonTabItem;
        if (ReferenceEquals(tab, _observedContentTab))
        {
            return;
        }

        StopObservingSelectedContent();
        _observedContentTab = tab;
        if (tab is null)
        {
            return;
        }

        foreach (var property in new[]
                 {
                     ContentControl.ContentProperty,
                     ContentControl.ContentTemplateProperty,
                     ContentControl.ContentTemplateSelectorProperty,
                     FrameworkElement.DataContextProperty,
                 })
        {
            var token = tab.RegisterPropertyChangedCallback(property, (_, _) => UpdateSelectedContent());
            _contentCallbacks.Add((property, token));
        }
    }

    private void StopObservingSelectedContent()
    {
        if (_observedContentTab is { } tab)
        {
            foreach (var (property, token) in _contentCallbacks)
            {
                tab.UnregisterPropertyChangedCallback(property, token);
            }
        }

        _contentCallbacks.Clear();
        _observedContentTab = null;
    }

    private void UpdateContentPresentation()
    {
        if (_contentPresenter is not null)
        {
            _contentPresenter.Visibility = IsMinimized ? Visibility.Collapsed : Visibility.Visible;
        }

        var generation = ++_presentationGeneration;
        StopWaitingForContentReadiness();
        StopWaitingForContentFocus();
        ContentPresentationChanged?.Invoke(this, EventArgs.Empty);
        if (!IsLoaded || XamlRoot is null || _contentPresenter is null)
        {
            return;
        }

        EnqueuePresentation(() => BeginContentPresentation(generation));
    }

    private void BeginContentPresentation(int generation)
    {
        if (generation != _presentationGeneration || !IsLoaded || XamlRoot is null)
        {
            return;
        }

        var tab = SelectedItem as RibbonTabItem;
        if (tab is null || !TabItems.Contains(tab) || tab.Visibility != Visibility.Visible || !tab.IsEnabled)
        {
            IsDropDownOpen = false;
            ReleasePresentedContent();
            return;
        }

        var showPopup = IsMinimized && IsDropDownOpen;
        if (showPopup)
        {
            EnsureMinimizedPopup();
        }

        var destination = showPopup ? _popupContentPresenter : _contentPresenter;
        var content = tab.Content;
        if (showPopup && content is null)
        {
            IsDropDownOpen = false;
            return;
        }

        if (ReferenceEquals(destination, _contentOwner)
            && ReferenceEquals(content, _presentedContent)
            && ReferenceEquals(destination?.Content, content))
        {
            ApplyContentMetadata(destination!, tab);
            UpdatePopupPlacement();
            if (showPopup)
            {
                _minimizedPopup!.IsOpen = true;
            }

            CommitContentPresentation(generation, destination!, tab);
            return;
        }

        var previousOwner = _contentOwner;
        _focusContentAfterPresentation |= showPopup && _popupReportedOpen
            && XamlRoot is { } root
            && FocusManager.GetFocusedElement(root) is DependencyObject focused
            && _popupRoot is not null && FocusRoutingHelper.IsDescendantOf(focused, _popupRoot);
        ReleasePresentedContent();
        if (previousOwner is null)
        {
            CompleteContentPresentation(generation, destination!, tab, content, 0);
        }
        else
        {
            // ContentPresenter can release its native visual child after its Content setter returns.
            // Cross that dispatcher boundary before assigning the same element to another presenter.
            EnqueuePresentation(() => CompleteContentPresentation(generation, destination!, tab, content, 0));
        }
    }

    private void CompleteContentPresentation(
        int generation,
        ContentPresenter destination,
        RibbonTabItem tab,
        object? content,
        int attempt)
    {
        if (generation != _presentationGeneration || !IsLoaded || XamlRoot is null
            || !ReferenceEquals(SelectedItem, tab) || !TabItems.Contains(tab))
        {
            return;
        }

        if (content is FrameworkElement pendingRelease && _releasingContent.ContainsKey(pendingRelease))
        {
            return;
        }

        if (content is UIElement element && VisualTreeHelper.GetParent(element) is not null)
        {
            if (attempt >= 3)
            {
                IsDropDownOpen = false;
                throw new InvalidOperationException(
                    "The selected ribbon content must be released from its previous visual parent before presentation.");
            }

            EnqueuePresentation(
                () => CompleteContentPresentation(generation, destination, tab, content, attempt + 1));
            return;
        }

        ApplyContentMetadata(destination, tab);
        destination.Content = content;
        _contentOwner = destination;
        _presentedContent = content;
        if (ReferenceEquals(destination, _popupContentPresenter) && IsMinimized && IsDropDownOpen)
        {
            if (content is null)
            {
                IsDropDownOpen = false;
                return;
            }

            UpdatePopupPlacement();
            _minimizedPopup!.IsOpen = true;
        }

        CommitContentPresentation(generation, destination, tab);
    }

    private void CommitContentPresentation(int generation, ContentPresenter destination, RibbonTabItem tab)
    {
        if (generation != _presentationGeneration || !ReferenceEquals(SelectedItem, tab)
            || !ReferenceEquals(_contentOwner, destination) || !ReferenceEquals(_presentedContent, tab.Content))
        {
            return;
        }

        _completedPresentationGeneration = generation;
        StopWaitingForContentReadiness();
        if (IsMinimized && !IsDropDownOpen)
        {
            return;
        }

        if (IsContentPresented(tab))
        {
            RestoreContentFocusAfterPresentation(generation, destination);
            ContentPresentationChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _contentReadinessRoot = destination;
            destination.LayoutUpdated += OnContentReadinessLayoutUpdated;
        }
    }

    private void OnContentReadinessLayoutUpdated(object? sender, object args)
    {
        if (_contentReadinessRoot is null)
        {
            return;
        }

        if (_completedPresentationGeneration != _presentationGeneration || !IsLoaded)
        {
            StopWaitingForContentReadiness();
            return;
        }

        if (SelectedItem is RibbonTabItem tab && IsContentPresented(tab))
        {
            StopWaitingForContentReadiness();
            if (_contentOwner is { } owner)
            {
                RestoreContentFocusAfterPresentation(_presentationGeneration, owner);
            }

            ContentPresentationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void StopWaitingForContentReadiness()
    {
        if (_contentReadinessRoot is { } root)
        {
            root.LayoutUpdated -= OnContentReadinessLayoutUpdated;
            _contentReadinessRoot = null;
        }
    }

    private void RestoreContentFocusAfterPresentation(int generation, ContentPresenter destination)
    {
        if (!_focusContentAfterPresentation)
        {
            return;
        }

        StopWaitingForContentFocus();
        if (generation != _presentationGeneration || !IsMinimizedPopupVisible)
        {
            return;
        }

        if (FocusRoutingHelper.FocusFirst(destination))
        {
            _focusContentAfterPresentation = false;
            return;
        }

        _focusReadinessGeneration = generation;
        _focusReadinessRoot = destination;
        destination.LayoutUpdated += OnContentFocusLayoutUpdated;
    }

    private void OnContentFocusLayoutUpdated(object? sender, object args)
    {
        var target = _focusReadinessRoot;
        if (target is null)
        {
            return;
        }

        var generation = _focusReadinessGeneration;
        StopWaitingForContentFocus();
        if (generation == _presentationGeneration && IsMinimizedPopupVisible)
        {
            _focusContentAfterPresentation = false;
            FocusRoutingHelper.FocusFirst(target);
        }
    }

    private void StopWaitingForContentFocus()
    {
        if (_focusReadinessRoot is { } root)
        {
            root.LayoutUpdated -= OnContentFocusLayoutUpdated;
            _focusReadinessRoot = null;
        }
    }

    private static void ApplyContentMetadata(ContentPresenter presenter, RibbonTabItem tab)
    {
        if (!ReferenceEquals(presenter.ContentTemplate, tab.ContentTemplate))
        {
            presenter.ContentTemplate = tab.ContentTemplate;
        }

        if (!ReferenceEquals(presenter.ContentTemplateSelector, tab.ContentTemplateSelector))
        {
            presenter.ContentTemplateSelector = tab.ContentTemplateSelector;
        }

        if (!ReferenceEquals(presenter.DataContext, tab.DataContext))
        {
            presenter.DataContext = tab.DataContext;
        }
    }

    private void ReleasePresentedContent()
    {
        StopWaitingForContentReadiness();
        _completedPresentationGeneration = -1;
        if (_contentOwner is not null)
        {
            if (_presentedContent is FrameworkElement content && content.IsLoaded
                && FocusRoutingHelper.IsDescendantOf(content, _contentOwner)
                && !_releasingContent.ContainsKey(content))
            {
                var release = new ContentRelease(content, () => OnPresentedContentUnloaded(content));
                _releasingContent.Add(content, release);
            }

            _contentOwner.Content = null;
            if (_contentOwner.ContentTemplate is not null)
            {
                _contentOwner.ContentTemplate = null;
            }

            if (_contentOwner.ContentTemplateSelector is not null)
            {
                _contentOwner.ContentTemplateSelector = null;
            }

            if (_contentOwner.ReadLocalValue(DataContextProperty) != DependencyProperty.UnsetValue)
            {
                _contentOwner.ClearValue(DataContextProperty);
            }
        }

        _contentOwner = null;
        _presentedContent = null;
    }

    private void OnPresentedContentUnloaded(FrameworkElement content)
    {
        if (!_releasingContent.Remove(content, out var release))
        {
            return;
        }

        release.Dispose();
        if (IsLoaded && XamlRoot is not null)
        {
            UpdateContentPresentation();
        }
    }

    private void EnqueuePresentation(Action action)
    {
        if (DispatcherQueue?.TryEnqueue(() => action()) != true)
        {
            throw new InvalidOperationException("Could not enqueue ribbon content presentation.");
        }
    }

    private void EnsureMinimizedPopup()
    {
        if (_minimizedPopup is not null)
        {
            if (!ReferenceEquals(_minimizedPopup.XamlRoot, XamlRoot))
            {
                _minimizedPopup.XamlRoot = XamlRoot;
            }

            return;
        }

        _popupContentPresenter = new ContentPresenter
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        _popupRoot = new Border
        {
            Background = Background,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Child = new ScrollViewer
            {
                Content = _popupContentPresenter,
                HorizontalScrollMode = ScrollMode.Enabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Enabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                ZoomMode = ZoomMode.Disabled,
                TabNavigation = KeyboardNavigationMode.Cycle,
            },
        };
        _popupRoot.KeyDown += OnPopupKeyDown;
        _minimizedPopup = new Popup
        {
            Child = _popupRoot,
            XamlRoot = XamlRoot,
            IsLightDismissEnabled = true,
        };
        _minimizedPopup.Opened += OnMinimizedPopupOpened;
        _minimizedPopup.Closed += OnMinimizedPopupClosed;
    }

    private void CapturePopupFocus()
    {
        _popupFocusBackup ??= FocusRoutingHelper.CaptureFocusedElement(this);
    }

    private void OnMinimizedPopupOpened(object? sender, object args)
    {
        if (!IsMinimizedPopupVisible)
        {
            return;
        }

        if (!IsMinimized || !IsDropDownOpen || !IsLoaded)
        {
            _minimizedPopup!.IsOpen = false;
            return;
        }

        _focusContentAfterPresentation = true;
        if (_popupReportedOpen)
        {
            FocusReadyPopupContent();
            return;
        }

        _popupReportedOpen = true;
        _popupRibbon = FocusRoutingHelper.FindAncestor<Ribbon>(this);
        _popupRibbon?.RegisterKeyTipInputRoot(_popupRoot!);
        _popupXamlRoot = XamlRoot;
        if (_popupXamlRoot is not null)
        {
            _popupXamlRoot.Changed += OnPopupRootChanged;
        }

        PopupService.RegisterOpenDropDown(this);
        RaisePopupAutomationState(false, true);
        DropDownOpened?.Invoke(this, EventArgs.Empty);
        FocusReadyPopupContent();
    }

    private void FocusReadyPopupContent()
    {
        if (SelectedItem is RibbonTabItem tab && IsContentPresented(tab) && _contentOwner is { } owner)
        {
            RestoreContentFocusAfterPresentation(_presentationGeneration, owner);
        }
    }

    private void OnMinimizedPopupClosed(object? sender, object args)
    {
        if (IsMinimizedPopupVisible)
        {
            _closingPopupRequestVersion = null;
            return;
        }

        var supersededClose = _closingPopupRequestVersion is { } closingVersion
                              && closingVersion < _popupRequestVersion
                              && IsMinimized && IsDropDownOpen && IsLoaded;
        _closingPopupRequestVersion = null;
        var wasOpen = _popupReportedOpen;
        _popupReportedOpen = false;
        if (!supersededClose)
        {
            ++_popupRequestVersion;
            _isUpdatingDropDownState = true;
            try
            {
                IsDropDownOpen = false;
            }
            finally
            {
                _isUpdatingDropDownState = false;
            }
        }

        PopupService.UnregisterOpenDropDown(this);
        _popupRibbon?.UnregisterKeyTipInputRoot(_popupRoot!);
        _popupRibbon = null;
        if (_popupXamlRoot is not null)
        {
            _popupXamlRoot.Changed -= OnPopupRootChanged;
            _popupXamlRoot = null;
        }

        if (!supersededClose && XamlRoot is { } root
            && (FocusManager.GetFocusedElement(root) is not DependencyObject focused
                || (_popupRoot is not null && FocusRoutingHelper.IsDescendantOf(focused, _popupRoot))))
        {
            if (!FocusRoutingHelper.RestoreFocus(ref _popupFocusBackup))
            {
                (SelectedItem as RibbonTabItem)?.Focus(FocusState.Programmatic);
            }
        }

        StopWaitingForContentFocus();
        if (!supersededClose)
        {
            _popupFocusBackup = null;
            _focusContentAfterPresentation = false;
        }

        UpdateContentPresentation();
        if (wasOpen)
        {
            RaisePopupAutomationState(true, false);
            DropDownClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CloseMinimizedPopup()
    {
        if (_minimizedPopup?.IsOpen == true)
        {
            _closingPopupRequestVersion = _popupRequestVersion;
            _minimizedPopup.IsOpen = false;
        }
    }

    private void ResetContentPresentation()
    {
        ++_presentationGeneration;
        CloseMinimizedPopup();
        ++_presentationGeneration;
        ReleasePresentedContent();
        StopWaitingForContentFocus();
        foreach (var release in _releasingContent.Values)
        {
            release.Dispose();
        }

        _releasingContent.Clear();
        _popupFocusBackup = null;
        _focusContentAfterPresentation = false;
    }

    private void OnPopupKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Escape && !args.Handled && TryClosePopupOnEscape())
        {
            args.Handled = true;
        }
    }

    internal bool TryClosePopupOnEscape()
    {
        if (!IsDropDownOpen || _popupRibbon?.IsKeyTipModeActive == true)
        {
            return false;
        }

        IsDropDownOpen = false;
        return !IsDropDownOpen;
    }

    private void OnPopupRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdatePopupPlacement();

    private void UpdatePopupPlacement()
    {
        if (_minimizedPopup is null || _popupRoot is null || XamlRoot is not { } root
            || !IsLoaded || root.Size.Width <= 0 || root.Size.Height <= 0)
        {
            return;
        }

        var anchor = (FrameworkElement?)FocusRoutingHelper.FindAncestor<Ribbon>(this) ?? this;
        var origin = anchor.TransformToVisual(root.Content as UIElement).TransformPoint(default);
        var bottom = origin.Y + anchor.ActualHeight;
        var width = Math.Min(Math.Max(1, anchor.ActualWidth), root.Size.Width);
        var desiredHeight = (double.IsFinite(ContentHeight) && ContentHeight > 0
            ? ContentHeight
            : DefaultContentHeight) + 2;
        var below = Math.Max(0, root.Size.Height - bottom);
        var above = Math.Max(0, origin.Y);
        var placeBelow = below >= desiredHeight || below >= above;
        var height = Math.Min(desiredHeight, Math.Max(1, placeBelow ? below : above));
        _popupRoot.Width = width;
        _popupRoot.Height = height;
        _popupRoot.MaxWidth = root.Size.Width;
        _popupRoot.MaxHeight = root.Size.Height;
        _popupRoot.Background = Background;
        _popupRoot.BorderBrush = BorderBrush;
        _popupRoot.FlowDirection = FlowDirection;
        _minimizedPopup.HorizontalOffset = Math.Clamp(origin.X, 0, Math.Max(0, root.Size.Width - width));
        _minimizedPopup.VerticalOffset = Math.Clamp(
            placeBelow ? bottom : origin.Y - height,
            0,
            Math.Max(0, root.Size.Height - height));
    }

    private void RaisePopupAutomationState(bool oldOpen, bool newOpen)
    {
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is Fluent.Automation.Peers.RibbonTabControlAutomationPeer peer)
        {
            peer.RaiseSelectedTabExpandCollapseChanged(
                GetSelectedTabExpandCollapseState(IsMinimized, oldOpen),
                GetSelectedTabExpandCollapseState(IsMinimized, newOpen));
        }
    }

    private sealed class ContentRelease : IDisposable
    {
        private readonly Dictionary<FrameworkElement, RoutedEventHandler> pending = new();
        private readonly Action completed;

        internal ContentRelease(FrameworkElement root, Action completed)
        {
            this.completed = completed;
            Observe(root);
        }

        private void Observe(DependencyObject element)
        {
            if (element is FrameworkElement { IsLoaded: true } frameworkElement)
            {
                RoutedEventHandler unloaded = (_, _) => OnUnloaded(frameworkElement);
                pending.Add(frameworkElement, unloaded);
                frameworkElement.Unloaded += unloaded;
            }

            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
            {
                Observe(VisualTreeHelper.GetChild(element, index));
            }
        }

        private void OnUnloaded(FrameworkElement element)
        {
            if (!pending.Remove(element, out var handler))
            {
                return;
            }

            element.Unloaded -= handler;
            if (pending.Count == 0)
            {
                completed();
            }
        }

        public void Dispose()
        {
            foreach (var (element, handler) in pending)
            {
                element.Unloaded -= handler;
            }

            pending.Clear();
        }
    }
}

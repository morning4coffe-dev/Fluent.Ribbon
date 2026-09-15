namespace Fluent;

public partial class Ribbon
{
    private Popup? _startScreenPopup;
    private Grid? _startScreenPopupRoot;
    private ScrollViewer? _startScreenContentHost;
    private StartScreen? _hostedStartScreen;
    private XamlRoot? _startScreenXamlRoot;
    private int _startScreenPresentationGeneration;
    private bool _isUpdatingStartScreenPopup;
    private bool _startScreenClosePending;
    private Binding? _startScreenDataContextBinding;
    private WeakReference<StartScreen>? _releasedStartScreen;
    private bool _isSwitchingApplicationSurface;

    private static void OnStartScreenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var ribbon = (Ribbon)sender;
        if (args.OldValue is StartScreen oldScreen)
        {
            oldScreen.PresentationChanged -= ribbon.OnStartScreenPresentationChanged;
            oldScreen.CloseForOwner(restoreFocus: true);
            oldScreen.SetPresentationOwner(null);
            ribbon.ReleaseStartScreenHost();
            if (ribbon._startScreenDataContextBinding is not null && ReferenceEquals(
                    oldScreen.GetBindingExpression(FrameworkElement.DataContextProperty)?.ParentBinding,
                    ribbon._startScreenDataContextBinding))
            {
                oldScreen.ClearValue(FrameworkElement.DataContextProperty);
            }

            ribbon._startScreenDataContextBinding = null;
        }

        if (args.NewValue is StartScreen newScreen)
        {
            if (newScreen.PresentationOwner is { } previousOwner && !ReferenceEquals(previousOwner, ribbon))
            {
                throw new InvalidOperationException("A StartScreen cannot be assigned to more than one ribbon.");
            }

            newScreen.SetPresentationOwner(ribbon);
            newScreen.PresentationChanged += ribbon.OnStartScreenPresentationChanged;
            if (VisualTreeHelper.GetParent(newScreen) is null
                && newScreen.ReadLocalValue(FrameworkElement.DataContextProperty) == DependencyProperty.UnsetValue
                && newScreen.GetBindingExpression(FrameworkElement.DataContextProperty) is null)
            {
                ribbon._startScreenDataContextBinding = new Binding
                {
                    Source = ribbon,
                    Path = new PropertyPath(nameof(DataContext)),
                    Mode = BindingMode.OneWay,
                };
                newScreen.SetBinding(FrameworkElement.DataContextProperty, ribbon._startScreenDataContextBinding);
            }
        }

        ribbon.UpdateStartScreenPresentation();
    }

    private void OnStartScreenPresentationChanged(object? sender, EventArgs args)
    {
        if (ReferenceEquals(sender, StartScreen))
        {
            UpdateStartScreenPresentation();
        }
    }

    private void UpdateStartScreenPresentation()
    {
        var generation = ++_startScreenPresentationGeneration;
        if (!IsLoaded || XamlRoot is null)
        {
            return;
        }

        if (DispatcherQueue?.TryEnqueue(() => PresentStartScreen(generation)) != true)
        {
            throw new InvalidOperationException("Could not enqueue the ribbon StartScreen presentation.");
        }
    }

    private void PresentStartScreen(int generation, int releaseAttempt = 0)
    {
        if (generation != _startScreenPresentationGeneration || !IsLoaded || XamlRoot is not { } root)
        {
            return;
        }

        if (StartScreen is not { } screen)
        {
            ReleaseStartScreenHost();
            UpdateApplicationSurfaceState();
            return;
        }

        var existingParent = VisualTreeHelper.GetParent(screen);
        if (existingParent is not null && !ReferenceEquals(_hostedStartScreen, screen))
        {
            if (_releasedStartScreen?.TryGetTarget(out var released) == true && ReferenceEquals(released, screen))
            {
                if (releaseAttempt >= 3)
                {
                    throw new InvalidOperationException("The previous StartScreen visual host did not release its content.");
                }

                if (DispatcherQueue?.TryEnqueue(() => PresentStartScreen(generation, releaseAttempt + 1)) != true)
                {
                    throw new InvalidOperationException("Could not finish releasing the previous StartScreen host.");
                }

                return;
            }

            // An explicitly authored visual host is intentional (including inline demonstrations).
            UpdateApplicationSurfaceState();
            return;
        }

        EnsureStartScreenHost();
        if (!ReferenceEquals(_startScreenPopup!.XamlRoot, root))
        {
            _startScreenPopup.XamlRoot = root;
        }
        if (!ReferenceEquals(_hostedStartScreen, screen))
        {
            _startScreenContentHost!.Content = screen;
            _hostedStartScreen = screen;
            _releasedStartScreen = null;
        }

        UpdateStartScreenViewport();
        _isUpdatingStartScreenPopup = true;
        try
        {
            var show = screen.IsPresentationShown && screen.Visibility == Visibility.Visible;
            if (show)
            {
                screen.CapturePresentationFocus();
            }

            if (!show && _startScreenPopup.IsOpen)
            {
                _startScreenClosePending = true;
            }

            _startScreenPopup.IsOpen = show;
        }
        finally
        {
            _isUpdatingStartScreenPopup = false;
        }

        UpdateApplicationSurfaceState();
    }

    private void EnsureStartScreenHost()
    {
        if (_startScreenPopup is not null)
        {
            return;
        }

        _startScreenContentHost = new ScrollViewer
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            HorizontalScrollMode = ScrollMode.Enabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Enabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            ZoomMode = ZoomMode.Disabled,
            TabNavigation = KeyboardNavigationMode.Cycle,
        };
        _startScreenPopupRoot = new Grid();
        _startScreenPopupRoot.SetBinding(FrameworkElement.DataContextProperty, new Binding
        {
            Source = this,
            Path = new PropertyPath(nameof(DataContext)),
            Mode = BindingMode.OneWay,
        });
        _startScreenPopupRoot.Children.Add(_startScreenContentHost);
        _startScreenPopup = new Popup
        {
            Child = _startScreenPopupRoot,
            IsLightDismissEnabled = false,
        };
        _startScreenPopup.Opened += OnStartScreenPopupOpened;
        _startScreenPopup.Closed += OnStartScreenPopupClosed;
    }

    private void OnStartScreenPopupOpened(object? sender, object args)
    {
        if (!ReferenceEquals(_startScreenXamlRoot, XamlRoot))
        {
            if (_startScreenXamlRoot is not null)
            {
                _startScreenXamlRoot.Changed -= OnStartScreenRootChanged;
            }

            _startScreenXamlRoot = XamlRoot;
            if (_startScreenXamlRoot is not null)
            {
                _startScreenXamlRoot.Changed += OnStartScreenRootChanged;
            }
        }

        RegisterKeyTipInputRoot(_startScreenPopupRoot!);
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_startScreenPopup?.IsOpen == true)
            {
                UpdateStartScreenViewport();
                _hostedStartScreen?.FocusPresentation();
            }
        });
    }

    private void OnStartScreenPopupClosed(object? sender, object args)
    {
        var requestedClose = _startScreenClosePending;
        _startScreenClosePending = false;
        if (_startScreenPopup?.IsOpen == true)
        {
            return;
        }

        if (_startScreenXamlRoot is not null)
        {
            _startScreenXamlRoot.Changed -= OnStartScreenRootChanged;
            _startScreenXamlRoot = null;
        }

        UnregisterKeyTipInputRoot(_startScreenPopupRoot!);
        if (!requestedClose && !_isUpdatingStartScreenPopup
            && IsLoaded
            && ReferenceEquals(_hostedStartScreen, StartScreen)
            && _hostedStartScreen is { IsOpen: true, Visibility: Visibility.Visible } screen)
        {
            screen.CloseForOwner(restoreFocus: true);
        }

        UpdateApplicationSurfaceState();
    }

    private void OnStartScreenRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) =>
        UpdateStartScreenViewport();

    private void UpdateStartScreenViewport()
    {
        if (_startScreenPopupRoot is null || XamlRoot is not { } root)
        {
            return;
        }

        _startScreenPopupRoot.Width = Math.Max(1, root.Size.Width);
        _startScreenPopupRoot.Height = Math.Max(1, root.Size.Height);
        _startScreenPopupRoot.Background = _hostedStartScreen?.Background ?? Background;
        _startScreenPopupRoot.FlowDirection = FlowDirection;
        _startScreenPopup!.HorizontalOffset = 0;
        _startScreenPopup.VerticalOffset = 0;
    }

    internal bool TryActivateApplicationSurface(Backstage surface)
    {
        if (_isSwitchingApplicationSurface)
        {
            return ReferenceEquals(ActiveBackstage, surface);
        }

        if (ActiveBackstage is { } previous && !ReferenceEquals(previous, surface))
        {
            if (previous.IsOpen && !previous.CanChangeIsOpen)
            {
                return false;
            }

            _isSwitchingApplicationSurface = true;
            try
            {
                surface.AdoptPresentationFocus(previous.TakePresentationFocus());
                previous.CloseForOwner(restoreFocus: false);
            }
            finally
            {
                _isSwitchingApplicationSurface = false;
            }
        }

        ActiveBackstage = surface;
        surface.PresentationChanged -= OnApplicationSurfacePresentationChanged;
        surface.PresentationChanged += OnApplicationSurfacePresentationChanged;
        UpdateApplicationSurfaceState();
        return true;
    }

    internal void ReleaseApplicationSurface(Backstage surface)
    {
        surface.PresentationChanged -= OnApplicationSurfacePresentationChanged;
        if (ReferenceEquals(ActiveBackstage, surface))
        {
            ActiveBackstage = null;
        }

        UpdateApplicationSurfaceState();
    }

    private void OnApplicationSurfacePresentationChanged(object? sender, EventArgs args) =>
        UpdateApplicationSurfaceState();

    private void UpdateApplicationSurfaceState()
    {
        IsBackstageOrStartScreenOpen =
            ActiveBackstage is { Visibility: Visibility.Visible } active
            && (active.IsOpen || active.IsPresentationShown);
    }

    private void CloseStartScreenPresentation()
    {
        ++_startScreenPresentationGeneration;
        ActiveBackstage?.CloseForOwner(restoreFocus: false);
        StartScreen?.CloseForOwner(restoreFocus: false);
        ReleaseStartScreenHost();
        UpdateApplicationSurfaceState();
    }

    private void ReleaseStartScreenHost()
    {
        ++_startScreenPresentationGeneration;
        _isUpdatingStartScreenPopup = true;
        try
        {
            if (_startScreenPopup is not null)
            {
                if (_startScreenPopup.IsOpen)
                {
                    _startScreenClosePending = true;
                }

                _startScreenPopup.IsOpen = false;
            }

            if (_startScreenContentHost is not null)
            {
                _startScreenContentHost.Content = null;
            }

            if (_hostedStartScreen is not null)
            {
                _releasedStartScreen = new WeakReference<StartScreen>(_hostedStartScreen);
            }

            _hostedStartScreen = null;
        }
        finally
        {
            _isUpdatingStartScreenPopup = false;
        }
    }
}

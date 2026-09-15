namespace Fluent;

public partial class RibbonComboBox
{
    private ResizeableContentControl? _comboResizeHost;
    private FrameworkElement? _comboInputRoot;
    private Popup? _comboPopup;
    private XamlRoot? _comboPopupRoot;
    private Border? _comboPopupBorder;
    private ScrollViewer? _comboPopupScroller;
    private ContentPresenter? _comboMenuPresenter;
    private long _comboWidthToken;
    private long _comboHeightToken;
    private bool _updatingComboPopup;
    private bool _synchronizingComboPopupBorder;
    private double _comboBorderMinimumHeight;
    private double _comboBorderMaximumHeight = double.PositiveInfinity;

    Popup? IDropDownControl.DropDownPopup => _comboPopup;

    private void InitializeComboPopup()
    {
        foreach (var property in new[]
                 {
                     ResizeModeProperty, MaxDropDownHeightProperty,
                     InputWidthProperty, FlowDirectionProperty,
                 })
        {
            RegisterPropertyChangedCallback(property, (_, _) => UpdateComboPopupDimensions());
        }

        RegisterPropertyChangedCallback(
            DropDownHeightProperty, (_, _) => UpdateComboPopupDimensions(resetHeight: true));
        RegisterPropertyChangedCallback(MenuProperty, (_, _) => UpdateComboMenuContent());
        SizeChanged += (_, _) => UpdateComboPopupDimensions();
        ActualThemeChanged += (_, _) => UpdateComboPopupDimensions();
        Unloaded += (_, _) =>
        {
            IsDropDownOpen = false;
            ObserveComboPopupViewport(null);
            PopupService.UnregisterOpenDropDown(this);
        };
    }

    private void ApplyComboPopupTemplate()
    {
        if (_comboResizeHost is not null)
        {
            _comboResizeHost.UnregisterPropertyChangedCallback(WidthProperty, _comboWidthToken);
            _comboResizeHost.UnregisterPropertyChangedCallback(HeightProperty, _comboHeightToken);
            _comboResizeHost.SizeChanged -= OnComboResizeHostSizeChanged;
        }

        _comboResizeHost = GetTemplateChild("PART_PopupContentControl") as ResizeableContentControl;
        _comboInputRoot = GetTemplateChild("InputRoot") as FrameworkElement;
        _comboPopup = GetTemplateChild("Popup") as Popup ?? GetTemplateChild("PART_Popup") as Popup;
        _comboPopupBorder = GetTemplateChild("PopupBorder") as Border;
        _comboBorderMinimumHeight = _comboPopupBorder?.MinHeight ?? 0;
        _comboBorderMaximumHeight = _comboPopupBorder?.MaxHeight ?? double.PositiveInfinity;
        _comboPopupScroller = GetTemplateChild("ScrollViewer") as ScrollViewer;
        _comboMenuPresenter = GetTemplateChild("PART_MenuPresenter") as ContentPresenter;
        if (_comboResizeHost is not null)
        {
            _comboWidthToken = _comboResizeHost.RegisterPropertyChangedCallback(WidthProperty, OnComboUserResize);
            _comboHeightToken = _comboResizeHost.RegisterPropertyChangedCallback(HeightProperty, OnComboUserResize);
            _comboResizeHost.SizeChanged += OnComboResizeHostSizeChanged;
        }

        UpdateComboPopupDimensions(resetHeight: true);
        UpdateComboMenuContent();
    }

    private void UpdateComboMenuContent()
    {
        if (_comboMenuPresenter is not null && !ReferenceEquals(_comboMenuPresenter.Content, Menu))
        {
            _comboMenuPresenter.Content = Menu;
        }
    }

    private void UpdateComboPopupDimensions(bool resetHeight = false)
    {
        if (_comboResizeHost is null || _updatingComboPopup)
        {
            return;
        }

        _updatingComboPopup = true;
        try
        {
            PopupResizeHelper.Apply(
                _comboResizeHost, this, ResizeMode,
                Math.Max(InputWidth, _comboInputRoot?.ActualWidth ?? 0),
                _comboInputRoot?.ActualHeight ?? 0,
                MaxDropDownHeight);
            if (resetHeight)
            {
                // WPF's DropDownHeight caps the initial list viewport, rather than
                // adding empty space to a short list or replacing native selection.
                _comboResizeHost.Height = double.NaN;
                if (_comboPopupBorder is not null)
                {
                    _comboPopupBorder.MinHeight = _comboBorderMinimumHeight;
                    _comboPopupBorder.MaxHeight = _comboBorderMaximumHeight;
                    _comboPopupBorder.Height = double.NaN;
                }

                if (_comboPopupScroller is not null)
                {
                    var initialHeight = double.IsNaN(DropDownHeight)
                        ? (XamlRoot?.Size.Height ?? double.PositiveInfinity) * 2 / 3
                        : PopupResizeHelper.NormalizeMaximum(DropDownHeight);
                    _comboPopupScroller.MaxHeight = Math.Min(
                        initialHeight, PopupResizeHelper.NormalizeMaximum(MaxDropDownHeight));
                }
            }
        }
        finally
        {
            _updatingComboPopup = false;
        }

        SynchronizeComboPopupBorder();
    }

    private void OnComboUserResize(DependencyObject sender, DependencyProperty property)
    {
        if (_updatingComboPopup)
        {
            return;
        }

        if (_comboPopupScroller is not null)
        {
            _comboPopupScroller.MaxHeight = double.PositiveInfinity;
        }

        SynchronizeComboPopupBorder();
    }

    private void OnComboResizeHostSizeChanged(object sender, SizeChangedEventArgs args) =>
        SynchronizeComboPopupBorder();

    private void SynchronizeComboPopupBorder()
    {
        if (_updatingComboPopup || _synchronizingComboPopupBorder
            || _comboPopupBorder is null || _comboResizeHost is null)
        {
            return;
        }

        _synchronizingComboPopupBorder = true;
        try
        {
            var padding = _comboPopupBorder.Padding;
            var border = _comboPopupBorder.BorderThickness;
            if (!double.IsNaN(_comboResizeHost.Width))
            {
                _comboPopupBorder.Width = _comboResizeHost.Width
                                         + padding.Left + padding.Right + border.Left + border.Right;
            }

            if (!double.IsNaN(_comboResizeHost.Height))
            {
                var height = Math.Clamp(
                    _comboResizeHost.Height + padding.Top + padding.Bottom + border.Top + border.Bottom,
                    _comboBorderMinimumHeight, _comboBorderMaximumHeight);
                // Native ComboBox recalculates Height from its list alone. A user
                // resize constrains the complete surface, including header/footer.
                _comboPopupBorder.MinHeight = _comboBorderMinimumHeight;
                _comboPopupBorder.MaxHeight = height;
                _comboPopupBorder.MinHeight = height;
                _comboPopupBorder.Height = height;
            }
        }
        finally
        {
            _synchronizingComboPopupBorder = false;
        }
    }

    private void ObserveComboPopupViewport(XamlRoot? root)
    {
        if (_comboPopupRoot is not null)
        {
            _comboPopupRoot.Changed -= OnComboPopupViewportChanged;
        }

        _comboPopupRoot = root;
        if (root is not null)
        {
            root.Changed += OnComboPopupViewportChanged;
        }
    }

    private void OnComboPopupViewportChanged(XamlRoot sender, XamlRootChangedEventArgs args) =>
        UpdateComboPopupDimensions();
}

namespace Fluent;

using Windows.System;

/// <summary>
/// WPF-compatible, portable members for <see cref="RibbonTabItem"/>.
/// </summary>
public partial class RibbonTabItem
{
    /// <summary>Identifies the <see cref="HasLeftGroupBorder"/> dependency property.</summary>
    public static readonly DependencyProperty HasLeftGroupBorderProperty =
        DependencyProperty.Register(
            nameof(HasLeftGroupBorder),
            typeof(bool),
            typeof(RibbonTabItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether this tab starts its contextual group.</summary>
    public bool HasLeftGroupBorder
    {
        get => (bool)GetValue(HasLeftGroupBorderProperty);
        set => SetValue(HasLeftGroupBorderProperty, value);
    }

    /// <summary>Identifies the <see cref="HasRightGroupBorder"/> dependency property.</summary>
    public static readonly DependencyProperty HasRightGroupBorderProperty =
        DependencyProperty.Register(
            nameof(HasRightGroupBorder),
            typeof(bool),
            typeof(RibbonTabItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether this tab ends its contextual group.</summary>
    public bool HasRightGroupBorder
    {
        get => (bool)GetValue(HasRightGroupBorderProperty);
        set => SetValue(HasRightGroupBorderProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonTabItem),
            new PropertyMetadata(false));

    /// <summary>Gets whether simplified ribbon layout is active for this tab.</summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        private set => SetValue(IsSimplifiedProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(RibbonTabItem),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the selector used to choose a tab-header template.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Gets the scroll viewer hosting this tab's groups.</summary>
    public ScrollViewer GroupsContainer => _scrollViewer;

    internal RibbonTabControl? TabControlParent
    {
        get
        {
            DependencyObject? current = this;
            while ((current = VisualTreeHelper.GetParent(current)) is not null)
            {
                if (current is RibbonTabControl tabControl)
                {
                    return tabControl;
                }
            }

            return null;
        }
    }

    private void InitializeCompatibility()
    {
        RegisterPropertyChangedCallback(
            IsSelectedProperty,
            static (sender, _) => ((RibbonTabItem)sender).OnCompatibilitySelectionChanged());
        Tapped += OnCompatibilityTapped;
        DoubleTapped += OnCompatibilityDoubleTapped;
    }

    private void OnCompatibilitySelectionChanged()
    {
        if (IsSelected)
        {
            StartBringIntoView();
            OnSelected(new RoutedEventArgs());
        }
        else
        {
            OnUnselected(new RoutedEventArgs());
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        if ((args.Key == VirtualKey.Enter || args.Key == VirtualKey.Space)
            && TabControlParent is { IsMinimized: true } tabControl)
        {
            tabControl.SelectedItem = this;
            tabControl.IsDropDownOpen = true;
            args.Handled = true;
        }

        base.OnKeyDown(args);
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        if (IsContextual && Group?.Visibility == Visibility.Collapsed)
        {
            return default;
        }

        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        Group?.UpdateInnerVisiblityAndGroupBorders();
        return result;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Group?.UpdateInnerVisiblityAndGroupBorders();
    }

    private void OnCompatibilityTapped(object sender, TappedRoutedEventArgs args)
    {
        if (Visibility != Visibility.Visible)
        {
            return;
        }

        if (TabControlParent is { } tabControl)
        {
            if (ReferenceEquals(tabControl.SelectedItem, this) && tabControl.IsMinimized)
            {
                tabControl.IsDropDownOpen = !tabControl.IsDropDownOpen;
            }
            else
            {
                tabControl.SelectedItem = this;
                if (tabControl.IsMinimized)
                {
                    tabControl.IsDropDownOpen = true;
                }
            }

            tabControl.RaiseRequestBackstageClose();
        }
        else
        {
            IsSelected = true;
        }
    }

    private void OnCompatibilityDoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
    {
        if (TabControlParent is { CanMinimize: true } tabControl)
        {
            tabControl.IsMinimized = !tabControl.IsMinimized;
            args.Handled = true;
        }
    }

    /// <summary>Called when this tab becomes selected.</summary>
    protected virtual void OnSelected(RoutedEventArgs args)
    {
    }

    /// <summary>Called when this tab becomes unselected.</summary>
    protected virtual void OnUnselected(RoutedEventArgs args)
    {
    }

    /// <inheritdoc />
    public KeyTipPressedResult OnKeyTipPressed()
    {
        IsSelected = true;

        if (TabControlParent is { IsMinimized: true } tabControl)
        {
            tabControl.SelectedItem = this;
            tabControl.IsDropDownOpen = true;
            return new KeyTipPressedResult(true, true);
        }

        return KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
        if (TabControlParent is { IsMinimized: true } tabControl)
        {
            tabControl.IsDropDownOpen = false;
        }
    }

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
        foreach (var group in Groups)
        {
            group.IsSimplified = isSimplified;
        }
    }

    string? IKeyTipedControl.KeyTip
    {
        get => KeyTip;
        set => KeyTip = value ?? string.Empty;
    }
}

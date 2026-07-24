namespace Fluent;

using Microsoft.UI.Xaml.Automation;
using Windows.System;

/// <summary>
/// WPF-compatible, portable members for <see cref="RibbonGroupBox"/>.
/// </summary>
public partial class RibbonGroupBox :
    IQuickAccessItemProvider,
    IDropDownControl,
    IKeyTipedControl,
    ISimplifiedStateControl,
    IMediumIconProvider,
    ILargeIconProvider,
    ILogicalChildSupport
{
    private bool _isUpdatingCompatibilityDropDown;

    /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the group-header template.</summary>
    public new DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplateSelector"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the group-header template selector.</summary>
    public new DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the key tip used to open a collapsed group.</summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherKeys"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherKeysProperty =
        DependencyProperty.Register(
            nameof(LauncherKeys),
            typeof(string),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null, OnLauncherMetadataChanged));

    /// <summary>Gets or sets the launcher key tip.</summary>
    public string? LauncherKeys
    {
        get => (string?)GetValue(LauncherKeysProperty);
        set => SetValue(LauncherKeysProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherIconProperty =
        DependencyProperty.Register(
            nameof(LauncherIcon),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>Gets or sets launcher icon content.</summary>
    public object? LauncherIcon
    {
        get => GetValue(LauncherIconProperty);
        set => SetValue(LauncherIconProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherText"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherTextProperty =
        DependencyProperty.Register(
            nameof(LauncherText),
            typeof(string),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null, OnLauncherMetadataChanged));

    /// <summary>Gets or sets the accessible launcher text.</summary>
    public string? LauncherText
    {
        get => (string?)GetValue(LauncherTextProperty);
        set => SetValue(LauncherTextProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherCommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherCommandParameterProperty =
        DependencyProperty.Register(
            nameof(LauncherCommandParameter),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the launcher command parameter.</summary>
    public object? LauncherCommandParameter
    {
        get => GetValue(LauncherCommandParameterProperty);
        set => SetValue(LauncherCommandParameterProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherCommandTarget"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherCommandTargetProperty =
        DependencyProperty.Register(
            nameof(LauncherCommandTarget),
            typeof(UIElement),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the portable launcher command target.</summary>
    public UIElement? LauncherCommandTarget
    {
        get => (UIElement?)GetValue(LauncherCommandTargetProperty);
        set => SetValue(LauncherCommandTargetProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherToolTipProperty =
        DependencyProperty.Register(
            nameof(LauncherToolTip),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null, OnLauncherMetadataChanged));

    /// <summary>Gets or sets launcher tooltip content.</summary>
    public object? LauncherToolTip
    {
        get => GetValue(LauncherToolTipProperty);
        set => SetValue(LauncherToolTipProperty, value);
    }

    /// <summary>Identifies the <see cref="IsLauncherEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsLauncherEnabledProperty =
        DependencyProperty.Register(
            nameof(IsLauncherEnabled),
            typeof(bool),
            typeof(RibbonGroupBox),
            new PropertyMetadata(true, OnLauncherMetadataChanged));

    /// <summary>Gets or sets whether the launcher button is enabled.</summary>
    public bool IsLauncherEnabled
    {
        get => (bool)GetValue(IsLauncherEnabledProperty);
        set => SetValue(IsLauncherEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherButton"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherButtonProperty =
        DependencyProperty.Register(
            nameof(LauncherButton),
            typeof(Button),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>Gets the current launcher button template part.</summary>
    public Button? LauncherButton
    {
        get => (Button?)GetValue(LauncherButtonProperty);
        private set => SetValue(LauncherButtonProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(RibbonGroupBox),
            new PropertyMetadata(false, OnCompatibilityDropDownChanged));

    /// <inheritdoc />
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSeparatorVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsSeparatorVisibleProperty =
        DependencyProperty.Register(
            nameof(IsSeparatorVisible),
            typeof(bool),
            typeof(RibbonGroupBox),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether the trailing group separator is visible.</summary>
    public bool IsSeparatorVisible
    {
        get => (bool)GetValue(IsSeparatorVisibleProperty);
        set => SetValue(IsSeparatorVisibleProperty, value);
    }

    /// <summary>Identifies quick-access eligibility.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <inheritdoc />
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    /// <inheritdoc />
    public event EventHandler? DropDownOpened;

    /// <inheritdoc />
    public event EventHandler? DropDownClosed;

    /// <inheritdoc />
    public Popup? DropDownPopup => _collapsedPopup;

    /// <inheritdoc />
    public bool IsContextMenuOpened { get; set; }

    /// <summary>Gets whether the group is represented by a popup button.</summary>
    public bool IsInButtonState =>
        State is RibbonGroupBoxState.Collapsed or RibbonGroupBoxState.QuickAccess;

    /// <summary>Gets the normal header presenter when the template exposes it.</summary>
    public ContentControl? HeaderContentControl { get; private set; }

    /// <summary>Gets the collapsed header presenter when the template exposes it.</summary>
    public ContentControl? CollapsedHeaderContentControl { get; private set; }

    /// <summary>
    /// Gets or sets whether the WinUI group is represented by a cached snapshot.
    /// Snapshot rendering is WPF-specific, so Uno retains only the portable state.
    /// </summary>
    public bool IsSnapped { get; set; }

    private void InitializeCompatibility()
    {
    }

    private void ApplyCompatibilityTemplateParts(Button? launcherButton)
    {
        LauncherButton = launcherButton;
        HeaderContentControl = GetTemplateChild(PART_HeaderPresenter) as ContentControl;
        CollapsedHeaderContentControl =
            GetTemplateChild("PART_CollapsedHeaderContentControl") as ContentControl;
        UpdateLauncherMetadata();
    }

    private static void OnLauncherMetadataChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        ((RibbonGroupBox)sender).UpdateLauncherMetadata();
    }

    private void UpdateLauncherMetadata()
    {
        if (LauncherButton is null)
        {
            return;
        }

        LauncherButton.IsEnabled = IsLauncherEnabled;
        LauncherButton.Command = LauncherCommand;
        LauncherButton.CommandParameter = LauncherCommandParameter;
        ToolTipService.SetToolTip(LauncherButton, LauncherToolTip);

        var name = LauncherText ?? Header?.ToString();
        if (!string.IsNullOrWhiteSpace(name))
        {
            AutomationProperties.SetName(LauncherButton, name);
        }

        if (!string.IsNullOrWhiteSpace(LauncherKeys))
        {
            AutomationProperties.SetAccessKey(LauncherButton, LauncherKeys);
        }
    }

    private static void OnCompatibilityDropDownChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var groupBox = (RibbonGroupBox)sender;
        if (groupBox._isUpdatingCompatibilityDropDown)
        {
            return;
        }

        if ((bool)args.NewValue && !groupBox.IsInButtonState)
        {
            groupBox._isUpdatingCompatibilityDropDown = true;
            groupBox.IsDropDownOpen = false;
            groupBox._isUpdatingCompatibilityDropDown = false;
            return;
        }

        if ((bool)args.NewValue)
        {
            groupBox.ExpandForAutomation();
            groupBox.DropDownOpened?.Invoke(groupBox, EventArgs.Empty);
        }
        else
        {
            groupBox.CollapseForAutomation();
            groupBox.DropDownClosed?.Invoke(groupBox, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        if (!IsInButtonState || args.Handled)
        {
            base.OnKeyDown(args);
            return;
        }

        switch (args.Key)
        {
            case VirtualKey.Space:
            case VirtualKey.Enter:
                IsDropDownOpen = true;
                args.Handled = true;
                break;
            case VirtualKey.Escape:
                IsDropDownOpen = false;
                args.Handled = true;
                break;
        }

        base.OnKeyDown(args);
    }

    /// <inheritdoc />
    public virtual FrameworkElement CreateQuickAccessItem()
    {
        var quickAccessButton = new RibbonButton
        {
            Header = Header,
            LargeIcon = LargeIcon as ImageSource,
            MediumIcon = MediumIcon as ImageSource,
            CanAddToQuickAccessToolBar = false,
        };

        var automationName = Header?.ToString() ?? nameof(RibbonGroupBox);
        AutomationProperties.SetName(quickAccessButton, automationName);
        AutomationProperties.SetAutomationId(
            quickAccessButton,
            string.IsNullOrWhiteSpace(Name)
                ? $"RibbonGroupBoxQuickAccess_{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}"
                : $"{Name}_QuickAccess");

        quickAccessButton.Click += (_, _) => IsDropDownOpen = true;
        return quickAccessButton;
    }

    /// <summary>Resets reduction state and asks the parent container to remeasure.</summary>
    public bool TryClearCacheAndResetStateAndScaleAndNotifyParentRibbonGroupsContainer()
    {
        if (State == RibbonGroupBoxState.QuickAccess)
        {
            return false;
        }

        StateIntermediate = GetInitialStateForMode(IsSimplified);
        State = StateIntermediate;
        ScaleIntermediate = 0;
        InvalidateMeasure();

        if (VisualTreeHelper.GetParent(this) is RibbonGroupsContainer container)
        {
            container.GroupBoxCacheClearedAndStateAndScaleResetted(this);
        }

        return true;
    }

    /// <inheritdoc />
    public KeyTipPressedResult OnKeyTipPressed()
    {
        Focus(FocusState.Programmatic);
        if (IsInButtonState)
        {
            IsDropDownOpen = true;
            return new KeyTipPressedResult(true, true);
        }

        return new KeyTipPressedResult(true, false);
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
        IsDropDownOpen = false;
    }

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
    }

    /// <inheritdoc />
    void ILogicalChildSupport.AddLogicalChild(object child)
    {
    }

    /// <inheritdoc />
    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual System.Collections.IEnumerator LogicalChildren
    {
        get
        {
            foreach (var item in Items)
            {
                yield return item;
            }

            if (Icon is not null)
            {
                yield return Icon;
            }

            if (Header is not null)
            {
                yield return Header;
            }
        }
    }
}

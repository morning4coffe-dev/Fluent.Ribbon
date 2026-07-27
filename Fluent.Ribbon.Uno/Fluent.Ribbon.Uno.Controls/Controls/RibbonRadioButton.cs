namespace Fluent;

/// <summary>
/// Represents a RadioButton control within a Ribbon.
/// </summary>
[ContentProperty(Name = nameof(Header))]
public partial class RibbonRadioButton : RadioButton, IScalableRibbonControl, IHeaderedControl, IMediumIconProvider, IQuickAccessItemProvider
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonRadioButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the radio button.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="MediumIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(ImageSource),
            typeof(RibbonRadioButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon displayed beside the radio button.
    /// </summary>
    public ImageSource? MediumIcon
    {
        get => (ImageSource?)GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonRadioButton),
            new PropertyMetadata(RibbonControlSize.Medium, OnSizeChanged));

    /// <summary>
    /// Gets or sets the size of the control.
    /// </summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonRadioButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="GroupName"/> dependency property.</summary>
    public new static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(
            nameof(GroupName),
            typeof(string),
            typeof(RibbonRadioButton),
            new PropertyMetadata(string.Empty, OnGroupNameChanged));

    /// <summary>
    /// Gets or sets the name of the radio button group.
    /// </summary>
    public new string GroupName
    {
        get => (string)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonRadioButton"/> class.
    /// </summary>
    public RibbonRadioButton()
    {
        DefaultStyleKey = typeof(RibbonRadioButton);
        QuickAccessHelper.AttachContextMenu(this);
    }

    #endregion

    #region IScalableRibbonControl

    /// <inheritdoc/>
    public void ScaleTo(RibbonControlSize size)
    {
        Size = size;
    }

    #endregion

    #region Methods

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonRadioButton radioButton)
        {
            radioButton.UpdateVisualState();
        }
    }

    private static void OnGroupNameChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (sender is RibbonRadioButton radioButton)
        {
            ((Microsoft.UI.Xaml.Controls.RadioButton)radioButton).GroupName =
                (string)args.NewValue;
        }
    }

    private void UpdateVisualState()
    {
        var stateName = Size switch
        {
            RibbonControlSize.Large => "Large",
            RibbonControlSize.Medium => "Medium",
            RibbonControlSize.Small => "Small",
            _ => "Medium"
        };

        VisualStateManager.GoToState(this, stateName, true);
    }

    #endregion

    #region IQuickAccessItemProvider

    /// <inheritdoc />
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new RibbonRadioButton
        {
            Header = QuickAccessHelper.ClonePresentationValue(Header),
            MediumIcon = MediumIcon,
            Size = RibbonControlSize.Small,
            GroupName = GroupName,
            CanAddToQuickAccessToolBar = false,
        };
        ((Microsoft.UI.Xaml.Controls.RadioButton)clone).GroupName =
            string.IsNullOrWhiteSpace(GroupName)
                ? $"FluentQat.{Guid.NewGuid():N}"
                : $"FluentQat.{GroupName}";

        RibbonControl.BindQuickAccessItem(this, clone);
        var synchronizing = false;
        var weakSource = new WeakReference<RibbonRadioButton>(this);
        var weakClone = new WeakReference<RibbonRadioButton>(clone);
        RoutedEventHandler? sourceChecked = null;
        RoutedEventHandler? sourceUnchecked = null;

        clone.IsChecked = IsChecked;
        sourceChecked = (_, _) => SynchronizeClone(true, sourceChecked);
        sourceUnchecked = (_, _) => SynchronizeClone(false, sourceUnchecked);
        Checked += sourceChecked;
        Unchecked += sourceUnchecked;
        clone.Checked += (_, _) => SynchronizeSource(true);
        clone.Unchecked += (_, _) => SynchronizeSource(false);
        return clone;

        void SynchronizeClone(bool value, RoutedEventHandler? handler)
        {
            if (!weakClone.TryGetTarget(out var liveClone))
            {
                if (value)
                {
                    Checked -= handler;
                }
                else
                {
                    Unchecked -= handler;
                }

                return;
            }

            if (synchronizing || liveClone.IsChecked == value)
            {
                return;
            }

            synchronizing = true;
            liveClone.IsChecked = value;
            synchronizing = false;
        }

        void SynchronizeSource(bool value)
        {
            if (!weakSource.TryGetTarget(out var liveSource)
                || synchronizing
                || liveSource.IsChecked == value)
            {
                return;
            }

            synchronizing = true;
            liveSource.IsChecked = value;
            synchronizing = false;
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonRadioButtonAutomationPeer(this);
}

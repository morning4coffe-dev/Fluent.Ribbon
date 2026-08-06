namespace Fluent;

/// <summary>
/// Represents a ComboBox control within a Ribbon.
/// </summary>
public partial class RibbonComboBox : ComboBox, IHeaderedControl, IScalableRibbonControl, IMediumIconProvider, IDropDownControl, IQuickAccessItemProvider, IRibbonHeaderAlignable
{
    private FrameworkElement? _headerPresenter;
    private TextBox? _editableTextBox;
    private RibbonComboBox? _quickAccessOwner;
    private object? _borrowedTopPopupContent;
    private UIElement? _borrowedMenu;
    private int _automationSelectedIndex;
    private object? _automationSelectedItem;
    private string _automationValue = string.Empty;

    #region Events

    /// <inheritdoc />
    public new event EventHandler? DropDownOpened;

    /// <inheritdoc />
    public new event EventHandler? DropDownClosed;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the combo box.
    /// </summary>
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="MediumIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(ImageSource),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon displayed beside the combo box.
    /// </summary>
    public ImageSource? MediumIcon
    {
        get => (ImageSource?)GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon for the combo box.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(RibbonComboBox),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonComboBox),
            new PropertyMetadata(RibbonControlSize.Medium, OnSizeChanged));

    /// <summary>
    /// Gets or sets the size of the control within the ribbon.
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
            typeof(RibbonComboBox),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="InputWidth"/> dependency property.</summary>
    public static readonly DependencyProperty InputWidthProperty =
        DependencyProperty.Register(
            nameof(InputWidth),
            typeof(double),
            typeof(RibbonComboBox),
            new PropertyMetadata(100.0));

    /// <summary>
    /// Gets or sets the width of the input area.
    /// </summary>
    public double InputWidth
    {
        get => (double)GetValue(InputWidthProperty);
        set => SetValue(InputWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ResizeMode"/> dependency property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(RibbonComboBox),
            new PropertyMetadata(ContextMenuResizeMode.None));

    /// <summary>
    /// Gets or sets the context menu resize mode.
    /// </summary>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Identifies the <see cref="DropDownHeight"/> dependency property.</summary>
    public static readonly DependencyProperty DropDownHeightProperty =
        DependencyProperty.Register(
            nameof(DropDownHeight),
            typeof(double),
            typeof(RibbonComboBox),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the initial drop down height.
    /// </summary>
    public double DropDownHeight
    {
        get => (double)GetValue(DropDownHeightProperty);
        set => SetValue(DropDownHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static new readonly DependencyProperty IsDropDownOpenProperty =
        Microsoft.UI.Xaml.Controls.ComboBox.IsDropDownOpenProperty;

    /// <summary>
    /// Gets or sets a value indicating whether the drop down is currently open.
    /// </summary>
    public new bool IsDropDownOpen
    {
        get => base.IsDropDownOpen;
        set => base.IsDropDownOpen = value;
    }

    /// <summary>Identifies the <see cref="TopPopupContent"/> dependency property.</summary>
    public static readonly DependencyProperty TopPopupContentProperty =
        DependencyProperty.Register(
            nameof(TopPopupContent),
            typeof(object),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets content to show on the top side of the Popup.
    /// </summary>
    public object? TopPopupContent
    {
        get => GetValue(TopPopupContentProperty);
        set => SetValue(TopPopupContentProperty, value);
    }

    /// <summary>Identifies the <see cref="TopPopupContentTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty TopPopupContentTemplateProperty =
        DependencyProperty.Register(
            nameof(TopPopupContentTemplate),
            typeof(DataTemplate),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the data template for top popup content.
    /// </summary>
    public DataTemplate? TopPopupContentTemplate
    {
        get => (DataTemplate?)GetValue(TopPopupContentTemplateProperty);
        set => SetValue(TopPopupContentTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="Menu"/> dependency property.</summary>
    public static readonly DependencyProperty MenuProperty =
        DependencyProperty.Register(
            nameof(Menu),
            typeof(UIElement),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets a menu to show at the bottom of the combo box drop down.
    /// </summary>
    public UIElement? Menu
    {
        get => (UIElement?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonComboBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the ribbon is in Simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonComboBox"/> class.
    /// </summary>
    public RibbonComboBox()
    {
        DefaultStyleKey = typeof(RibbonComboBox);
        _automationSelectedIndex = SelectedIndex;
        _automationSelectedItem = SelectedItem;
        _automationValue = GetAutomationValue();
        base.DropDownOpened += OnNativeDropDownOpened;
        base.DropDownClosed += OnNativeDropDownClosed;
        DropDownOpened += (_, _) => { };
        DropDownClosed += (_, _) => { };
        QuickAccessHelper.AttachContextMenu(this);
        RegisterPropertyChangedCallback(
            Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty,
            static (sender, _) => ((RibbonComboBox)sender).UpdateEditableAutomationName());
        RegisterPropertyChangedCallback(
            HeaderProperty,
            static (sender, _) => ((RibbonComboBox)sender).UpdateEditableAutomationName());
        RegisterPropertyChangedCallback(
            PlaceholderTextProperty,
            static (sender, _) => ((RibbonComboBox)sender).UpdateEditableAutomationName());
        RegisterPropertyChangedCallback(
            SelectedIndexProperty,
            static (sender, _) => ((RibbonComboBox)sender).OnAutomationSelectionChanged());
        RegisterPropertyChangedCallback(
            TextProperty,
            static (sender, _) => ((RibbonComboBox)sender).OnAutomationValueChanged());
    }

    #endregion

    #region Template

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _headerPresenter = GetTemplateChild("HeaderText") as FrameworkElement;
        _editableTextBox = GetTemplateChild("EditableText") as TextBox;
        UpdateEditableAutomationName();
        UpdateVisualState();
    }

    private void UpdateEditableAutomationName()
    {
        if (_editableTextBox is not null)
        {
            var name = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(this);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Fluent.Automation.Peers.AutomationPeerHelpers
                    .GetHeaderOrPlaceholderName(this);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                    _editableTextBox,
                    name);
            }
        }
    }

    /// <inheritdoc />
    FrameworkElement? IRibbonHeaderAlignable.HeaderPresenter => _headerPresenter;

    #endregion

    #region IScalableRibbonControl

    /// <inheritdoc />
    public void ScaleTo(RibbonControlSize size)
    {
        Size = size;
    }

    #endregion

    #region Methods

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonComboBox comboBox)
        {
            comboBox.UpdateVisualState();
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

    private void OnNativeDropDownOpened(object? sender, object e)
    {
        PopupService.RegisterOpenDropDown(this);
        DropDownOpened?.Invoke(this, EventArgs.Empty);
    }

    private void OnNativeDropDownClosed(object? sender, object e)
    {
        PopupService.UnregisterOpenDropDown(this);
        DropDownClosed?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutomationSelectionChanged()
    {
        var oldIndex = _automationSelectedIndex;
        var oldItem = _automationSelectedItem;
        var newIndex = SelectedIndex;
        var newItem = SelectedItem;
        _automationSelectedIndex = newIndex;
        _automationSelectedItem = newItem;

#if !WINDOWS
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is Fluent.Automation.Peers.RibbonComboBoxAccessibleAutomationPeer peer)
        {
            peer.RaiseSelectionChanged(oldItem, oldIndex, newItem, newIndex);
        }
#endif

        OnAutomationValueChanged();
    }

    private void OnAutomationValueChanged()
    {
        var oldValue = _automationValue;
        var newValue = GetAutomationValue();
        _automationValue = newValue;
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is Fluent.Automation.Peers.RibbonComboBoxAccessibleAutomationPeer peer)
        {
            peer.RaiseValueChanged(oldValue, newValue);
        }
    }

    private string GetAutomationValue()
        => Text ?? Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(SelectedItem);

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
        var clone = new RibbonComboBox
        {
            Header = QuickAccessHelper.ClonePresentationValue(Header),
            MediumIcon = MediumIcon,
            IconGlyph = IconGlyph,
            Size = RibbonControlSize.Small,
            InputWidth = Math.Min(InputWidth, 120d),
            IsEditable = IsEditable,
            ResizeMode = ResizeMode,
            DropDownHeight = DropDownHeight,
            MaxDropDownHeight = MaxDropDownHeight,
            TopPopupContent = TopPopupContent is UIElement ? null : TopPopupContent,
            TopPopupContentTemplate = TopPopupContentTemplate,
            CanAddToQuickAccessToolBar = false,
            _quickAccessOwner = this,
        };

        if (ItemsSource is not null)
        {
            RibbonControl.Synchronize(
                this,
                ItemsControl.ItemsSourceProperty,
                clone,
                ItemsControl.ItemsSourceProperty);
        }
        else
        {
            foreach (var item in Items)
            {
                clone.Items.Add(CreateQuickAccessItemData(item));
            }
        }

        RibbonControl.BindQuickAccessItem(this, clone);
        BindOneWay(DisplayMemberPathProperty);
        BindOneWay(SelectedValuePathProperty);
        BindOneWay(PlaceholderTextProperty);
        BindOneWay(Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty);
        if (Header is not UIElement)
        {
            BindOneWay(HeaderProperty);
        }

        BindTwoWay(SelectedIndexProperty);
        if (IsEditable)
        {
            BindTwoWay(TextProperty);
        }

        clone.DropDownOpened += OnQuickAccessDropDownOpened;
        clone.DropDownClosed += OnQuickAccessDropDownClosed;

        return clone;

        void BindOneWay(DependencyProperty property) =>
            RibbonControl.Synchronize(this, property, clone, property);

        void BindTwoWay(DependencyProperty property)
        {
            RibbonControl.Synchronize(this, property, clone, property);
            RibbonControl.Synchronize(clone, property, this, property);
        }
    }

    private static void OnQuickAccessDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is not RibbonComboBox { _quickAccessOwner: { } owner } clone)
        {
            return;
        }

        if (owner.TopPopupContent is UIElement topContent)
        {
            owner.TopPopupContent = null;
            clone._borrowedTopPopupContent = topContent;
            clone.TopPopupContent = topContent;
        }

        if (owner.Menu is { } menu)
        {
            owner.Menu = null;
            clone._borrowedMenu = menu;
            clone.Menu = menu;
        }
    }

    private static void OnQuickAccessDropDownClosed(object? sender, EventArgs e)
    {
        if (sender is not RibbonComboBox { _quickAccessOwner: { } owner } clone)
        {
            return;
        }

        if (clone._borrowedTopPopupContent is { } topContent)
        {
            clone.TopPopupContent = null;
            owner.TopPopupContent = topContent;
            clone._borrowedTopPopupContent = null;
        }

        if (clone._borrowedMenu is { } menu)
        {
            clone.Menu = null;
            owner.Menu = menu;
            clone._borrowedMenu = null;
        }
    }

    private static object CreateQuickAccessItemData(object item)
    {
        var name = Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(
            item is ContentControl contentControl ? contentControl.Content : item);
        return item is Control control
            ? new ComboBoxItem
            {
                Content = name,
                IsEnabled = control.IsEnabled,
            }
            : name;
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonComboBoxAccessibleAutomationPeer(this);
}

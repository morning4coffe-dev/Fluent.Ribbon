namespace Fluent;

using System.Collections;

/// <summary>Provides the WPF-compatible ribbon button.</summary>
public partial class Button : RibbonButton, IQuickAccessItemProvider, IRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl
{
    /// <summary>Identifies the object-typed compatibility icon property.</summary>
    public new static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(Button),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Gets or sets icon content.</summary>
    public new object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the WPF-compatible size-definition property.</summary>
    public new static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(Button),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSizeDefinitionChanged));

    /// <summary>Gets or sets the typed ribbon size definition.</summary>
    public new RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the WPF-compatible simplified size-definition property.</summary>
    public new static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(Button),
            new PropertyMetadata(
                default(RibbonControlSizeDefinition),
                OnSimplifiedSizeDefinitionChanged));

    /// <summary>Gets or sets the typed simplified size definition.</summary>
    public new RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the object-typed large-icon property.</summary>
    public new static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(Button),
            new PropertyMetadata(null, OnLargeIconChanged));

    /// <summary>Gets or sets the large icon.</summary>
    public new object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the object-typed medium-icon property.</summary>
    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(Button),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the medium icon.</summary>
    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the inherited quick-access availability property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Identifies the header-template property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(Button),
            new PropertyMetadata(null, OnHeaderPresentationChanged));

    /// <summary>Gets or sets the template used to display the header.</summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the header-template-selector property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(Button),
            new PropertyMetadata(null, OnHeaderPresentationChanged));

    /// <summary>Gets or sets the selector used to choose a header template.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Identifies whether invocation closes an ancestor drop-down.</summary>
    public static readonly DependencyProperty IsDefinitiveProperty =
        DependencyProperty.Register(
            nameof(IsDefinitive),
            typeof(bool),
            typeof(Button),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether invocation closes an ancestor drop-down.</summary>
    public bool IsDefinitive
    {
        get => (bool)GetValue(IsDefinitiveProperty);
        set => SetValue(IsDefinitiveProperty, value);
    }

    /// <summary>Initializes a WPF-compatible ribbon button.</summary>
    public Button()
    {
        Click += (_, _) => OnClick();
        RegisterPropertyChangedCallback(
            RibbonButton.HeaderProperty,
            static (sender, _) => ((Button)sender).ApplyHeaderPresentation());
    }

    /// <inheritdoc />
    public new virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new Button
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        RibbonControl.BindQuickAccessItem(this, clone);
        BindPresentation(RibbonButton.HeaderProperty);
        BindPresentation(IconProperty);
        BindPresentation(LargeIconProperty);
        BindPresentation(MediumIconProperty);
        clone.Click += (_, _) => Fluent.Modern.Commands.RibbonInvoker.Invoke(this);
        return clone;

        void BindPresentation(DependencyProperty property) =>
            QuickAccessHelper.SynchronizePresentationValue(this, property, clone, property);
    }

    /// <inheritdoc />
    public new virtual KeyTipPressedResult OnKeyTipPressed() => base.OnKeyTipPressed();

    /// <inheritdoc />
    public new virtual void OnKeyTipBack() => base.OnKeyTipBack();

    /// <inheritdoc />
    public new void UpdateSimplifiedState(bool isSimplified) =>
        base.UpdateSimplifiedState(isSimplified);

    /// <summary>Handles primary activation.</summary>
    protected virtual void OnClick()
    {
        if (IsDefinitive)
        {
            PopupService.RaiseDismissPopupEvent(
                this,
                DismissPopupMode.Always,
                DismissPopupReason.Undefined);
        }
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren => EnumerateLogicalChildren().GetEnumerator();

    private IEnumerable<object> EnumerateLogicalChildren()
    {
        if (Icon is not null)
        {
            yield return Icon;
        }

        if (Header is not null)
        {
            yield return Header;
        }
    }

    private static void OnSizeDefinitionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((RibbonButton)sender).SizeDefinition =
            ((RibbonControlSizeDefinition)args.NewValue).ToString();

    private static void OnSimplifiedSizeDefinitionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((RibbonButton)sender).SimplifiedSizeDefinition =
            ((RibbonControlSizeDefinition)args.NewValue).ToString();

    private static void OnLargeIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonButton.LargeIconProperty,
            args.NewValue);

    private static void OnMediumIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonButton.MediumIconProperty,
            args.NewValue);

    private static void OnIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonButton.IconProperty,
            args.NewValue);

    private static void OnHeaderPresentationChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((Button)sender).ApplyHeaderPresentation();

    private void ApplyHeaderPresentation() =>
        ContentTemplate =
            HeaderTemplateSelector?.SelectTemplate(Header, this)
            ?? HeaderTemplate;

    RibbonControlSizeDefinition IRibbonControl.SizeDefinition
    {
        get => SizeDefinition;
        set => SizeDefinition = value;
    }

    RibbonControlSizeDefinition ISimplifiedRibbonControl.SimplifiedSizeDefinition
    {
        get => SimplifiedSizeDefinition;
        set => SimplifiedSizeDefinition = value;
    }

    object? ILargeIconProvider.LargeIcon
    {
        get => LargeIcon;
        set => LargeIcon = value;
    }

    object? IMediumIconProvider.MediumIcon
    {
        get => MediumIcon;
        set => MediumIcon = value;
    }
}

namespace Fluent;

public partial class DropDownButton
{
    public new static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(DropDownButton),
            new PropertyMetadata(null, OnIconChanged));

    public new object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public new static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(DropDownButton),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSizeDefinitionChanged));

    public new RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    public new static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(DropDownButton),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSimplifiedSizeDefinitionChanged));

    public new RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    public new static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(DropDownButton),
            new PropertyMetadata(null, OnLargeIconChanged));

    public new object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(DropDownButton),
            new PropertyMetadata(null, OnMediumIconChanged));

    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    private static void OnSizeDefinitionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((RibbonDropDownButton)sender).SizeDefinition =
            ((RibbonControlSizeDefinition)args.NewValue).ToString();

    private static void OnSimplifiedSizeDefinitionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((RibbonDropDownButton)sender).SimplifiedSizeDefinition =
            ((RibbonControlSizeDefinition)args.NewValue).ToString();

    private static void OnLargeIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonDropDownButton.LargeIconProperty,
            args.NewValue);

    private static void OnMediumIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonDropDownButton.MediumIconProperty,
            args.NewValue);

    private static void OnIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonDropDownButton.IconProperty,
            args.NewValue);
}

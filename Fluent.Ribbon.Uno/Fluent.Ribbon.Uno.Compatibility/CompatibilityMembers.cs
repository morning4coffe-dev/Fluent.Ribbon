using Microsoft.UI.Xaml;

namespace Fluent;

public partial class ToggleButton
{
    /// <summary>Identifies the object-typed compatibility icon property.</summary>
    public new static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(ToggleButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Gets or sets icon content while retaining unsupported WinUI content on the facade.</summary>
    public new object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public new static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(ToggleButton),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSizeDefinitionChanged));

    /// <summary>Gets or sets the WPF-compatible size definition.</summary>
    public new RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="SimplifiedSizeDefinition"/> dependency property.</summary>
    public new static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(ToggleButton),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSimplifiedSizeDefinitionChanged));

    /// <summary>Gets or sets the WPF-compatible simplified size definition.</summary>
    public new RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="LargeIcon"/> dependency property.</summary>
    public new static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(ToggleButton),
            new PropertyMetadata(null, OnLargeIconChanged));

    /// <summary>Gets or sets the large icon using the WPF-compatible object type.</summary>
    public new object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="MediumIcon"/> dependency property.</summary>
    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(ToggleButton),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the medium icon using the WPF-compatible object type.</summary>
    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the inherited quick-access availability property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    private static void OnSizeDefinitionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((RibbonToggleButton)sender).SizeDefinition =
            CompatibilityValueConverter.ToSizeDefinitionString((RibbonControlSizeDefinition)args.NewValue);
    }

    private static void OnSimplifiedSizeDefinitionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((RibbonToggleButton)sender).SimplifiedSizeDefinition =
            CompatibilityValueConverter.ToSizeDefinitionString((RibbonControlSizeDefinition)args.NewValue);
    }

    private static void OnLargeIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonToggleButton.LargeIconProperty,
            args.NewValue);
    }

    private static void OnMediumIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonToggleButton.MediumIconProperty,
            args.NewValue);
    }

    private static void OnIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonToggleButton.IconProperty,
            args.NewValue);
}

public partial class CheckBox
{
    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(CheckBox),
            new PropertyMetadata(false));

    /// <summary>Gets whether the wrapper is using simplified ribbon behavior.</summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        private set => SetValue(IsSimplifiedProperty, value);
    }

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(CheckBox),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSizeDefinitionChanged));

    /// <summary>Gets or sets the WPF-compatible size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="SimplifiedSizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(CheckBox),
            new PropertyMetadata(default(RibbonControlSizeDefinition)));

    /// <summary>Gets or sets the WPF-compatible simplified size definition.</summary>
    public RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(CheckBox),
            new PropertyMetadata(null, OnNonRenderableIconChanged));

    /// <summary>Gets or sets the icon using the WPF-compatible object type.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(CheckBox),
            new PropertyMetadata(null, OnNonRenderableIconChanged));

    /// <summary>Gets or sets the large icon using the WPF-compatible object type.</summary>
    public object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="MediumIcon"/> dependency property.</summary>
    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(CheckBox),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the medium icon using the WPF-compatible object type.</summary>
    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="CanAddToQuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Gets or sets whether the control may be offered for quick access.</summary>
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    private static void OnSizeDefinitionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        RibbonProperties.SetSizeDefinition(
            sender,
            CompatibilityValueConverter.ToSizeDefinitionString((RibbonControlSizeDefinition)args.NewValue));
    }

    private static void OnMediumIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonCheckBox.MediumIconProperty,
            args.NewValue);
    }

    private static void OnNonRenderableIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.RecordNonRenderableValue(sender, args.NewValue);
}

public partial class RadioButton
{
    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RadioButton),
            new PropertyMetadata(false));

    /// <summary>Gets whether the wrapper is using simplified ribbon behavior.</summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        private set => SetValue(IsSimplifiedProperty, value);
    }

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(RadioButton),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSizeDefinitionChanged));

    /// <summary>Gets or sets the WPF-compatible size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="SimplifiedSizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(RadioButton),
            new PropertyMetadata(default(RibbonControlSizeDefinition)));

    /// <summary>Gets or sets the WPF-compatible simplified size definition.</summary>
    public RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(RadioButton),
            new PropertyMetadata(null, OnNonRenderableIconChanged));

    /// <summary>Gets or sets the icon using the WPF-compatible object type.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(RadioButton),
            new PropertyMetadata(null, OnNonRenderableIconChanged));

    /// <summary>Gets or sets the large icon using the WPF-compatible object type.</summary>
    public object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="MediumIcon"/> dependency property.</summary>
    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(RadioButton),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the medium icon using the WPF-compatible object type.</summary>
    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="CanAddToQuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Gets or sets whether the control may be offered for quick access.</summary>
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    private static void OnSizeDefinitionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        RibbonProperties.SetSizeDefinition(
            sender,
            CompatibilityValueConverter.ToSizeDefinitionString((RibbonControlSizeDefinition)args.NewValue));
    }

    private static void OnMediumIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonRadioButton.MediumIconProperty,
            args.NewValue);
    }

    private static void OnNonRenderableIconChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.RecordNonRenderableValue(sender, args.NewValue);
}

public partial class TextBox
{
    /// <summary>Identifies the object-typed compatibility icon property.</summary>
    public new static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(TextBox),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Gets or sets icon content while retaining unsupported WinUI content on the facade.</summary>
    public new object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(TextBox),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSizeDefinitionChanged));

    /// <summary>Gets or sets the WPF-compatible size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="SimplifiedSizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(TextBox),
            new PropertyMetadata(default(RibbonControlSizeDefinition)));

    /// <summary>Gets or sets the WPF-compatible simplified size definition.</summary>
    public RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="MediumIcon"/> dependency property.</summary>
    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(TextBox),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the medium icon using the WPF-compatible object type.</summary>
    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="CanAddToQuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Gets or sets whether the control may be offered for quick access.</summary>
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    private static void OnSizeDefinitionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        RibbonProperties.SetSizeDefinition(
            sender,
            CompatibilityValueConverter.ToSizeDefinitionString((RibbonControlSizeDefinition)args.NewValue));
    }

    private static void OnMediumIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonTextBox.MediumIconProperty,
            args.NewValue);
    }

    private static void OnIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonTextBox.IconProperty,
            args.NewValue);
}

public partial class ComboBox
{
    /// <summary>Identifies the object-typed compatibility icon property.</summary>
    public new static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(ComboBox),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>Gets or sets icon content while retaining unsupported WinUI content on the facade.</summary>
    public new object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(ComboBox),
            new PropertyMetadata(default(RibbonControlSizeDefinition), OnSizeDefinitionChanged));

    /// <summary>Gets or sets the WPF-compatible size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="SimplifiedSizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(ComboBox),
            new PropertyMetadata(default(RibbonControlSizeDefinition)));

    /// <summary>Gets or sets the WPF-compatible simplified size definition.</summary>
    public RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the WPF-compatible <see cref="MediumIcon"/> dependency property.</summary>
    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(ComboBox),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the medium icon using the WPF-compatible object type.</summary>
    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="CanAddToQuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Gets or sets whether the control may be offered for quick access.</summary>
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    private static void OnSizeDefinitionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        RibbonProperties.SetSizeDefinition(
            sender,
            CompatibilityValueConverter.ToSizeDefinitionString((RibbonControlSizeDefinition)args.NewValue));
    }

    private static void OnMediumIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonComboBox.MediumIconProperty,
            args.NewValue);
    }

    private static void OnIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonComboBox.IconProperty,
            args.NewValue);
}

internal static class CompatibilityValueConverter
{
    internal static string ToSizeDefinitionString(RibbonControlSizeDefinition value) => value.ToString();
}

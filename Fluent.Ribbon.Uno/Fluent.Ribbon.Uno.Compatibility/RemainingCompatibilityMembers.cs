using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Fluent;

public partial class SplitButton
{
    /// <summary>Identifies the descriptive command-target property.</summary>
    public static readonly DependencyProperty CommandTargetProperty =
        DependencyProperty.Register(
            nameof(CommandTarget),
            typeof(UIElement),
            typeof(SplitButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the WinUI element associated with command invocation.
    /// Portable <see cref="System.Windows.Input.ICommand"/> does not route through this target.
    /// </summary>
    public UIElement? CommandTarget
    {
        get => (UIElement?)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }

    /// <summary>Identifies the primary-action key-tip postfix property.</summary>
    public static readonly DependencyProperty PrimaryActionKeyTipPostfixProperty =
        DependencyProperty.Register(
            nameof(PrimaryActionKeyTipPostfix),
            typeof(string),
            typeof(SplitButton),
            new PropertyMetadata("A"));

    /// <summary>Gets or sets the primary-action key-tip postfix.</summary>
    public string PrimaryActionKeyTipPostfix
    {
        get => (string)GetValue(PrimaryActionKeyTipPostfixProperty);
        set => SetValue(PrimaryActionKeyTipPostfixProperty, value);
    }

    /// <summary>Identifies the secondary-action key-tip postfix property.</summary>
    public static readonly DependencyProperty SecondaryActionKeyTipPostfixProperty =
        DependencyProperty.Register(
            nameof(SecondaryActionKeyTipPostfix),
            typeof(string),
            typeof(SplitButton),
            new PropertyMetadata("B"));

    /// <summary>Gets or sets the secondary-action key-tip postfix.</summary>
    public string SecondaryActionKeyTipPostfix
    {
        get => (string)GetValue(SecondaryActionKeyTipPostfixProperty);
        set => SetValue(SecondaryActionKeyTipPostfixProperty, value);
    }

    /// <summary>Identifies the secondary key-tip property.</summary>
    public static readonly DependencyProperty SecondaryKeyTipProperty =
        DependencyProperty.Register(
            nameof(SecondaryKeyTip),
            typeof(string),
            typeof(SplitButton),
            new PropertyMetadata(string.Empty));

    /// <summary>Gets or sets the explicit secondary key tip.</summary>
    public string SecondaryKeyTip
    {
        get => (string)GetValue(SecondaryKeyTipProperty);
        set => SetValue(SecondaryKeyTipProperty, value);
    }

    /// <summary>Identifies whether the primary action can be added to quick access.</summary>
    public static readonly DependencyProperty CanAddButtonToQuickAccessToolBarProperty =
        DependencyProperty.Register(
            nameof(CanAddButtonToQuickAccessToolBar),
            typeof(bool),
            typeof(SplitButton),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether the primary action can be added to quick access.</summary>
    public bool CanAddButtonToQuickAccessToolBar
    {
        get => (bool)GetValue(CanAddButtonToQuickAccessToolBarProperty);
        set => SetValue(CanAddButtonToQuickAccessToolBarProperty, value);
    }

}

public partial class Gallery
{
    /// <summary>Identifies the selected filter groups property.</summary>
    public static readonly DependencyProperty SelectedFilterGroupsProperty =
        DependencyProperty.Register(
            nameof(SelectedFilterGroups),
            typeof(string),
            typeof(Gallery),
            new PropertyMetadata(null));

    /// <summary>Gets the group expression for the selected filter.</summary>
    public string? SelectedFilterGroups => (string?)GetValue(SelectedFilterGroupsProperty);

    /// <summary>Initializes a gallery compatibility facade.</summary>
    public Gallery()
    {
        Items.CollectionChanged += OnFinalGalleryItemsChanged;
        Loaded += (_, _) => UpdateFinalIsLastItem();
        RegisterPropertyChangedCallback(
            RibbonGallery.SelectedFilterProperty,
            static (sender, _) =>
            {
                var gallery = (Gallery)sender;
                gallery.SetValue(
                    SelectedFilterGroupsProperty,
                    ((RibbonGallery)gallery).SelectedFilter?.Groups);
            });
    }
}

public partial class GalleryItem
{
    /// <summary>Identifies whether activation is definitive.</summary>
    public static readonly DependencyProperty IsDefinitiveProperty =
        DependencyProperty.Register(
            nameof(IsDefinitive),
            typeof(bool),
            typeof(GalleryItem),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether activation closes an ancestor drop-down.</summary>
    public bool IsDefinitive
    {
        get => (bool)GetValue(IsDefinitiveProperty);
        set => SetValue(IsDefinitiveProperty, value);
    }

    /// <summary>Identifies the descriptive command-target property.</summary>
    public static readonly DependencyProperty CommandTargetProperty =
        DependencyProperty.Register(
            nameof(CommandTarget),
            typeof(UIElement),
            typeof(GalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the WinUI element associated with command invocation.
    /// Portable commands do not route through this target.
    /// </summary>
    public UIElement? CommandTarget
    {
        get => (UIElement?)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }
}

public partial class Spinner
{
    /// <summary>Identifies the typed size-definition property.</summary>
    public new static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(Spinner),
            new PropertyMetadata(default(RibbonControlSizeDefinition)));

    /// <summary>Gets or sets the typed size definition.</summary>
    public new RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the typed simplified size-definition property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(Spinner),
            new PropertyMetadata(default(RibbonControlSizeDefinition)));

    /// <summary>Gets or sets the typed simplified size definition.</summary>
    public RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the object-typed medium icon property.</summary>
    public new static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(Spinner),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the medium icon using the WPF-compatible object type.</summary>
    public new object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the object-typed icon property inherited from WPF RibbonControl.</summary>
    public new static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(Spinner),
            new PropertyMetadata(null, OnMediumIconChanged));

    /// <summary>Gets or sets the primary icon.</summary>
    public new object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the quick-access capability property.</summary>
    public new static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Gets or sets whether the spinner can be added to quick access.</summary>
    public new bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    private static void OnMediumIconChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        CompatibilityIconAdapter.ApplyToImageSourceProperty(
            sender,
            RibbonSpinner.MediumIconProperty,
            args.NewValue);
}

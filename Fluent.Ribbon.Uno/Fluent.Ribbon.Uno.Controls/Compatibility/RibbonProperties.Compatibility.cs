namespace Fluent;

using Fluent.Extensibility;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

/// <summary>
/// WPF-compatible attached-property surface for ribbon controls.
/// </summary>
public partial class RibbonProperties
{
    private static readonly RibbonControlSizeDefinition DefaultSizeDefinition =
        new(RibbonControlSize.Large, RibbonControlSize.Middle, RibbonControlSize.Small);

    /// <summary>Identifies the SimplifiedSizeDefinition attached property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.RegisterAttached(
            "SimplifiedSizeDefinition",
            typeof(RibbonControlSizeDefinition),
            typeof(RibbonProperties),
            new PropertyMetadata(DefaultSizeDefinition, OnSimplifiedSizeDefinitionChanged));

    /// <summary>Identifies the MouseOverBackground attached property.</summary>
    public static readonly DependencyProperty MouseOverBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "MouseOverBackground",
            typeof(Brush),
            typeof(RibbonProperties),
            new PropertyMetadata(null));

    /// <summary>Identifies the PressedBackground attached property.</summary>
    public static readonly DependencyProperty PressedBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "PressedBackground",
            typeof(Brush),
            typeof(RibbonProperties),
            new PropertyMetadata(null));

    /// <summary>Identifies the MouseOverForeground attached property.</summary>
    public static readonly DependencyProperty MouseOverForegroundProperty =
        DependencyProperty.RegisterAttached(
            "MouseOverForeground",
            typeof(Brush),
            typeof(RibbonProperties),
            new PropertyMetadata(null));

    /// <summary>Identifies the IsSelectedBackground attached property.</summary>
    public static readonly DependencyProperty IsSelectedBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "IsSelectedBackground",
            typeof(Brush),
            typeof(RibbonProperties),
            new PropertyMetadata(null));

    /// <summary>Identifies the LastVisibleWidth attached property.</summary>
    public static readonly DependencyProperty LastVisibleWidthProperty =
        DependencyProperty.RegisterAttached(
            "LastVisibleWidth",
            typeof(double),
            typeof(RibbonProperties),
            new PropertyMetadata(0D));

    /// <summary>Identifies the IsElementInQuickAccessToolBar attached property.</summary>
    public static readonly DependencyProperty IsElementInQuickAccessToolBarProperty =
        DependencyProperty.RegisterAttached(
            "IsElementInQuickAccessToolBar",
            typeof(bool),
            typeof(RibbonProperties),
            new PropertyMetadata(false));

    /// <summary>Identifies the IconSize attached property.</summary>
    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.RegisterAttached(
            "IconSize",
            typeof(IconSize),
            typeof(RibbonProperties),
            new PropertyMetadata(IconSize.Small));

    /// <summary>Identifies the CustomIconSize attached property.</summary>
    public static readonly DependencyProperty CustomIconSizeProperty =
        DependencyProperty.RegisterAttached(
            "CustomIconSize",
            typeof(Size),
            typeof(RibbonProperties),
            new PropertyMetadata(default(Size)));

    /// <summary>Identifies the CornerRadius attached property.</summary>
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(RibbonProperties),
            new PropertyMetadata(default(CornerRadius)));

    /// <summary>Gets the simplified size definition.</summary>
    public static RibbonControlSizeDefinition GetSimplifiedSizeDefinition(DependencyObject element) =>
        (RibbonControlSizeDefinition)element.GetValue(SimplifiedSizeDefinitionProperty);

    /// <summary>Sets the simplified size definition.</summary>
    public static void SetSimplifiedSizeDefinition(
        DependencyObject element,
        RibbonControlSizeDefinition value) =>
        element.SetValue(SimplifiedSizeDefinitionProperty, value);

    /// <summary>Gets the pointer-over background.</summary>
    public static Brush? GetMouseOverBackground(DependencyObject element) =>
        (Brush?)element.GetValue(MouseOverBackgroundProperty);

    /// <summary>Sets the pointer-over background.</summary>
    public static void SetMouseOverBackground(DependencyObject element, Brush? value) =>
        element.SetValue(MouseOverBackgroundProperty, value);

    /// <summary>Gets the pressed background.</summary>
    public static Brush? GetPressedBackground(DependencyObject element) =>
        (Brush?)element.GetValue(PressedBackgroundProperty);

    /// <summary>Sets the pressed background.</summary>
    public static void SetPressedBackground(DependencyObject element, Brush? value) =>
        element.SetValue(PressedBackgroundProperty, value);

    /// <summary>Gets the pointer-over foreground.</summary>
    public static Brush? GetMouseOverForeground(DependencyObject element) =>
        (Brush?)element.GetValue(MouseOverForegroundProperty);

    /// <summary>Sets the pointer-over foreground.</summary>
    public static void SetMouseOverForeground(DependencyObject element, Brush? value) =>
        element.SetValue(MouseOverForegroundProperty, value);

    /// <summary>Gets the selected background.</summary>
    public static Brush? GetIsSelectedBackground(DependencyObject element) =>
        (Brush?)element.GetValue(IsSelectedBackgroundProperty);

    /// <summary>Sets the selected background.</summary>
    public static void SetIsSelectedBackground(DependencyObject element, Brush? value) =>
        element.SetValue(IsSelectedBackgroundProperty, value);

    /// <summary>Gets the last visible width.</summary>
    public static double GetLastVisibleWidth(DependencyObject? element) =>
        element is null ? 0D : (double)element.GetValue(LastVisibleWidthProperty);

    /// <summary>Sets the last visible width.</summary>
    public static void SetLastVisibleWidth(DependencyObject element, double value) =>
        element.SetValue(LastVisibleWidthProperty, value);

    /// <summary>Gets whether an element is in the quick access toolbar.</summary>
    public static bool GetIsElementInQuickAccessToolBar(DependencyObject element) =>
        (bool)element.GetValue(IsElementInQuickAccessToolBarProperty);

    /// <summary>Sets whether an element is in the quick access toolbar.</summary>
    public static void SetIsElementInQuickAccessToolBar(DependencyObject element, bool value) =>
        element.SetValue(IsElementInQuickAccessToolBarProperty, value);

    /// <summary>Gets the desired icon size.</summary>
    public static IconSize GetIconSize(DependencyObject element) =>
        (IconSize)element.GetValue(IconSizeProperty);

    /// <summary>Sets the desired icon size.</summary>
    public static void SetIconSize(DependencyObject element, IconSize value) =>
        element.SetValue(IconSizeProperty, value);

    /// <summary>Gets the custom icon size.</summary>
    public static Size GetCustomIconSize(DependencyObject element) =>
        (Size)element.GetValue(CustomIconSizeProperty);

    /// <summary>Sets the custom icon size.</summary>
    public static void SetCustomIconSize(DependencyObject element, Size value) =>
        element.SetValue(CustomIconSizeProperty, value);

    /// <summary>Gets the template corner radius.</summary>
    public static CornerRadius GetCornerRadius(DependencyObject element) =>
        (CornerRadius)element.GetValue(CornerRadiusProperty);

    /// <summary>Sets the template corner radius.</summary>
    public static void SetCornerRadius(DependencyObject element, CornerRadius value) =>
        element.SetValue(CornerRadiusProperty, value);

    /// <summary>Applies the size definition for a group state.</summary>
    public static void SetAppropriateSize(
        DependencyObject element,
        RibbonGroupBoxState state,
        bool isSimplified)
    {
        var definition = isSimplified
            ? GetSimplifiedSizeDefinition(element)
            : GetSizeDefinition(element);
        SetSize(element, definition.GetSize(state));
    }

    /// <summary>Applies the size definition for a control size.</summary>
    public static void SetAppropriateSize(DependencyObject element, RibbonControlSize size) =>
        SetSize(element, GetSizeDefinition(element).GetSize(size));

    private static void OnSimplifiedSizeDefinitionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var group = FindParentRibbonGroupBox(sender);
        if (group?.IsSimplified == true)
        {
            SetAppropriateSize(sender, group.State, true);
        }
    }

    private static void OnSizeDefinitionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var group = FindParentRibbonGroupBox(sender);
        if (group?.IsSimplified != true)
        {
            SetAppropriateSize(
                sender,
                group?.State ?? RibbonGroupBoxState.Large,
                false);
        }
    }

    internal static RibbonGroupBox? FindParentRibbonGroupBox(DependencyObject element)
    {
        var current = element;
        while (current is not null)
        {
            if (current is RibbonGroupBox group)
            {
                return group;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}

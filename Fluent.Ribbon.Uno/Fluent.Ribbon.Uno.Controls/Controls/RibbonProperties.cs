namespace Fluent;

/// <summary>
/// Provides attached properties and static helper methods used by ribbon controls.
/// This class holds common attached properties like Size, SizeDefinition, KeyTip,
/// and binding helpers for Quick Access Toolbar integration.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF version inherits from Control and serves as a base class; Uno version
/// provides attached properties only (controls use their own base classes).
/// </remarks>
public static class RibbonProperties
{
    #region Size

    /// <summary>Identifies the Size attached property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.RegisterAttached(
            "Size",
            typeof(RibbonControlSize),
            typeof(RibbonProperties),
            new PropertyMetadata(RibbonControlSize.Large));

    /// <summary>Gets the ribbon control size for the element.</summary>
    public static RibbonControlSize GetSize(DependencyObject element)
    {
        return (RibbonControlSize)element.GetValue(SizeProperty);
    }

    /// <summary>Sets the ribbon control size for the element.</summary>
    public static void SetSize(DependencyObject element, RibbonControlSize value)
    {
        element.SetValue(SizeProperty, value);
    }

    #endregion

    #region SizeDefinition

    /// <summary>Identifies the SizeDefinition attached property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.RegisterAttached(
            "SizeDefinition",
            typeof(string),
            typeof(RibbonProperties),
            new PropertyMetadata(null));

    /// <summary>Gets the size definition string for the element.</summary>
    public static string? GetSizeDefinition(DependencyObject element)
    {
        return (string?)element.GetValue(SizeDefinitionProperty);
    }

    /// <summary>Sets the size definition string for the element.</summary>
    public static void SetSizeDefinition(DependencyObject element, string? value)
    {
        element.SetValue(SizeDefinitionProperty, value);
    }

    #endregion

    #region CanAddToQuickAccessToolBar

    /// <summary>Identifies the CanAddToQuickAccessToolBar attached property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        DependencyProperty.RegisterAttached(
            "CanAddToQuickAccessToolBar",
            typeof(bool),
            typeof(RibbonProperties),
            new PropertyMetadata(true));

    /// <summary>Gets whether the element can be added to the Quick Access Toolbar.</summary>
    public static bool GetCanAddToQuickAccessToolBar(DependencyObject element)
    {
        return (bool)element.GetValue(CanAddToQuickAccessToolBarProperty);
    }

    /// <summary>Sets whether the element can be added to the Quick Access Toolbar.</summary>
    public static void SetCanAddToQuickAccessToolBar(DependencyObject element, bool value)
    {
        element.SetValue(CanAddToQuickAccessToolBarProperty, value);
    }

    #endregion

    #region AppBarButtonLabel

    /// <summary>Identifies the AppBarButtonLabel attached property.</summary>
    public static readonly DependencyProperty AppBarButtonLabelProperty =
        DependencyProperty.RegisterAttached(
            "AppBarButtonLabel",
            typeof(string),
            typeof(RibbonProperties),
            new PropertyMetadata(null));

    /// <summary>Gets the app bar button label for the element.</summary>
    public static string? GetAppBarButtonLabel(DependencyObject element)
    {
        return (string?)element.GetValue(AppBarButtonLabelProperty);
    }

    /// <summary>Sets the app bar button label for the element.</summary>
    public static void SetAppBarButtonLabel(DependencyObject element, string? value)
    {
        element.SetValue(AppBarButtonLabelProperty, value);
    }

    #endregion

    #region Binding Helpers

    /// <summary>
    /// Creates a one-way binding from source to target for the given property.
    /// </summary>
    /// <param name="target">The target element.</param>
    /// <param name="targetProperty">The target dependency property.</param>
    /// <param name="source">The source object.</param>
    /// <param name="sourcePath">The path to the source property.</param>
    public static void Bind(
        FrameworkElement target,
        DependencyProperty targetProperty,
        object source,
        string sourcePath)
    {
        target.SetBinding(targetProperty, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = source,
            Path = new PropertyPath(sourcePath),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay,
        });
    }

    /// <summary>
    /// Creates a two-way binding from source to target for the given property.
    /// </summary>
    /// <param name="target">The target element.</param>
    /// <param name="targetProperty">The target dependency property.</param>
    /// <param name="source">The source object.</param>
    /// <param name="sourcePath">The path to the source property.</param>
    public static void BindTwoWay(
        FrameworkElement target,
        DependencyProperty targetProperty,
        object source,
        string sourcePath)
    {
        target.SetBinding(targetProperty, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = source,
            Path = new PropertyPath(sourcePath),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
        });
    }

    #endregion
}

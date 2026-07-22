namespace Fluent;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Fluent.Extensibility;

/// <summary>
/// WPF-compatible definition metadata for toolbar controls.
/// </summary>
public sealed partial class RibbonToolBarControlDefinition :
    INotifyPropertyChanged,
    IRibbonSizeChangedSink
{
    private static readonly RibbonControlSizeDefinition DefaultSizeDefinition =
        new(RibbonControlSize.Large, RibbonControlSize.Middle, RibbonControlSize.Small);

    /// <summary>Identifies the size-definition property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(RibbonToolBarControlDefinition),
            new PropertyMetadata(DefaultSizeDefinition, OnDefinitionPropertyChanged));

    /// <summary>Gets or sets the size definition for the target control.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Occurs when definition metadata changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Initializes a toolbar control definition.</summary>
    public RibbonToolBarControlDefinition()
    {
        Size = RibbonControlSize.Small;
        RegisterPropertyChangedCallback(TargetProperty, OnRegisteredPropertyChanged);
        RegisterPropertyChangedCallback(SizeProperty, OnRegisteredPropertyChanged);
        RegisterPropertyChangedCallback(WidthProperty, OnRegisteredPropertyChanged);
    }

    private static void OnDefinitionPropertyChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
        => ((RibbonToolBarControlDefinition)sender).RaisePropertyChanged(
            nameof(SizeDefinition));

    private void OnRegisteredPropertyChanged(
        DependencyObject sender,
        DependencyProperty property)
    {
        var propertyName =
            property == TargetProperty ? nameof(Target) :
            property == WidthProperty ? nameof(Width) :
            nameof(Size);
        RaisePropertyChanged(propertyName);
    }

    /// <inheritdoc />
    public void OnSizePropertyChanged(
        RibbonControlSize previous,
        RibbonControlSize current)
        => RaisePropertyChanged(nameof(Size));

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// WPF-compatible size metadata for a toolbar layout definition.
/// </summary>
public partial class RibbonToolBarLayoutDefinition
{
    private static readonly RibbonControlSizeDefinition DefaultSizeDefinition =
        new(RibbonControlSize.Large, RibbonControlSize.Middle, RibbonControlSize.Small);

    /// <summary>Identifies the size-definition property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(RibbonToolBarLayoutDefinition),
            new PropertyMetadata(DefaultSizeDefinition));

    /// <summary>Gets or sets the layout size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }
}

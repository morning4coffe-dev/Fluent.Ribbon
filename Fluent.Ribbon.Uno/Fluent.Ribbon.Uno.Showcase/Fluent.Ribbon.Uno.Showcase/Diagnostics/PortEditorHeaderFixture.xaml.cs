namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

public sealed partial class PortEditorHeaderFixture : UserControl
{
    public PortEditorHeaderFixture()
    {
        InitializeComponent();
    }

    internal PortEditorHeaderState State => (PortEditorHeaderState)DataContext;
    internal Control[] Editors => [CoreCombo, FacadeCombo, CoreText, FacadeText, CoreSpinner, FacadeSpinner];
    internal Button FocusSink => FocusTarget;
    internal DataTemplate PrimaryTemplate => (DataTemplate)Resources["PrimaryHeaderTemplate"];
    internal DataTemplate AlternateTemplate => (DataTemplate)Resources["AlternateHeaderTemplate"];
    internal DataTemplate ExplicitTemplate => (DataTemplate)Resources["ExplicitHeaderTemplate"];
    internal ControlTemplate ConsumerTextTemplate => (ControlTemplate)Resources["ConsumerTextTemplate"];
    internal ControlTemplate ConsumerCoreTextTemplate => (ControlTemplate)Resources["ConsumerCoreTextTemplate"];
    internal ControlTemplate NativeTextTemplate => (ControlTemplate)Resources["NativeTextTemplate"];
    internal PortEditorHeaderTemplateSelector Selector => (PortEditorHeaderTemplateSelector)Resources["HeaderSelector"];
    internal PortEditorHeaderTemplateSelector ReplacementSelector =>
        (PortEditorHeaderTemplateSelector)Resources["ReplacementHeaderSelector"];
}

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class PortEditorHeaderModel : INotifyPropertyChanged
{
    private string title = string.Empty;

    public string Title
    {
        get => title;
        set
        {
            title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    public bool UseAlternate { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => Title;
}

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class PortEditorHeaderState : INotifyPropertyChanged
{
    private object? header;
    private DataTemplate? headerTemplate;
    private DataTemplateSelector? headerTemplateSelector;

    public object? Header
    {
        get => header;
        set
        {
            header = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Header)));
        }
    }

    public DataTemplate? HeaderTemplate
    {
        get => headerTemplate;
        set
        {
            headerTemplate = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HeaderTemplate)));
        }
    }

    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => headerTemplateSelector;
        set
        {
            headerTemplateSelector = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HeaderTemplateSelector)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed partial class PortEditorHeaderTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PrimaryTemplate { get; set; }
    public DataTemplate? AlternateTemplate { get; set; }
    public bool ReturnNull { get; set; }

    internal List<(object Item, DependencyObject? Container)> Calls { get; } = [];

    protected override DataTemplate SelectTemplateCore(object item) => Select(item, null);

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => Select(item, container);

    private DataTemplate Select(object item, DependencyObject? container)
    {
        Calls.Add((item, container));
        if (ReturnNull)
        {
            return null!;
        }

        return (item is PortEditorHeaderModel { UseAlternate: true } ? AlternateTemplate : PrimaryTemplate)
               ?? throw new InvalidOperationException("The editor header selector has no marked template.");
    }
}

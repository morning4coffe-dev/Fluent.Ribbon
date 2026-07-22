using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Fluent;

public partial class ToggleButton
{
    /// <summary>Identifies the WPF-compatible header template property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(ToggleButton),
            new PropertyMetadata(null, OnFinalHeaderPresentationChanged));

    /// <summary>Gets or sets the template used to display the header.</summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the WPF-compatible header template selector property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(ToggleButton),
            new PropertyMetadata(null, OnFinalHeaderPresentationChanged));

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
            typeof(ToggleButton),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether invocation closes an ancestor drop-down.</summary>
    public bool IsDefinitive
    {
        get => (bool)GetValue(IsDefinitiveProperty);
        set => SetValue(IsDefinitiveProperty, value);
    }

    /// <summary>Initializes a WPF-compatible toggle-button facade.</summary>
    public ToggleButton()
    {
        Click += (_, _) => OnClick();
        Checked += (_, args) => OnChecked(args);
        RegisterPropertyChangedCallback(
            RibbonToggleButton.HeaderProperty,
            static (sender, _) => ((ToggleButton)sender).ApplyFinalHeaderPresentation());
    }

    private static void OnFinalHeaderPresentationChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((ToggleButton)sender).ApplyFinalHeaderPresentation();

    private void ApplyFinalHeaderPresentation() =>
        ContentTemplate = CompatibilityHeaderTemplateAdapter.Select(
            HeaderTemplateSelector,
            Header,
            this,
            HeaderTemplate);
}

public partial class CheckBox
{
    /// <summary>Identifies the WPF-compatible header template property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(CheckBox),
            new PropertyMetadata(null, OnFinalHeaderPresentationChanged));

    /// <summary>Gets or sets the template used to display the header.</summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the WPF-compatible header template selector property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(CheckBox),
            new PropertyMetadata(null, OnFinalHeaderPresentationChanged));

    /// <summary>Gets or sets the selector used to choose a header template.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Initializes a WPF-compatible check-box facade.</summary>
    public CheckBox()
    {
        RegisterPropertyChangedCallback(
            RibbonCheckBox.HeaderProperty,
            static (sender, _) => ((CheckBox)sender).ApplyFinalHeaderPresentation());
    }

    private static void OnFinalHeaderPresentationChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((CheckBox)sender).ApplyFinalHeaderPresentation();

    private void ApplyFinalHeaderPresentation() =>
        ContentTemplate = CompatibilityHeaderTemplateAdapter.Select(
            HeaderTemplateSelector,
            Header,
            this,
            HeaderTemplate);
}

public partial class RadioButton
{
    /// <summary>Identifies the WPF-compatible header template property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(RadioButton),
            new PropertyMetadata(null, OnFinalHeaderPresentationChanged));

    /// <summary>Gets or sets the template used to display the header.</summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the WPF-compatible header template selector property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(RadioButton),
            new PropertyMetadata(null, OnFinalHeaderPresentationChanged));

    /// <summary>Gets or sets the selector used to choose a header template.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Initializes a WPF-compatible radio-button facade.</summary>
    public RadioButton()
    {
        RegisterPropertyChangedCallback(
            RibbonRadioButton.HeaderProperty,
            static (sender, _) => ((RadioButton)sender).ApplyFinalHeaderPresentation());
    }

    private static void OnFinalHeaderPresentationChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((RadioButton)sender).ApplyFinalHeaderPresentation();

    private void ApplyFinalHeaderPresentation() =>
        ContentTemplate = CompatibilityHeaderTemplateAdapter.Select(
            HeaderTemplateSelector,
            Header,
            this,
            HeaderTemplate);
}

public partial class TextBox
{
    private static readonly Func<DependencyProperty> NativeHeaderTemplatePropertyAccessor =
        static () => Microsoft.UI.Xaml.Controls.TextBox.HeaderTemplateProperty;

    /// <summary>Identifies the WPF-compatible header template property.</summary>
    public new static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(TextBox),
            new PropertyMetadata(null, OnFinalHeaderTemplateChanged));

    /// <summary>Gets or sets the template used to display the header.</summary>
    public new DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the WPF-compatible header template selector property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(TextBox),
            new PropertyMetadata(null, OnFinalHeaderTemplateSelectorChanged));

    /// <summary>Gets or sets the selector used to choose a header template.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Initializes a WPF-compatible text-box facade.</summary>
    public TextBox()
    {
        RegisterPropertyChangedCallback(
            RibbonTextBox.HeaderProperty,
            static (sender, _) => ((TextBox)sender).ApplyFinalHeaderTemplateSelector());
    }

    private static void OnFinalHeaderTemplateSelectorChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((TextBox)sender).ApplyFinalHeaderTemplateSelector();

    private static void OnFinalHeaderTemplateChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((TextBox)sender).ApplyFinalHeaderTemplateSelector();

    private void ApplyFinalHeaderTemplateSelector() =>
        SetValue(
            NativeHeaderTemplatePropertyAccessor(),
            CompatibilityHeaderTemplateAdapter.Select(
                HeaderTemplateSelector,
                Header,
                this,
                HeaderTemplate));
}

public partial class ComboBox
{
    /// <summary>Identifies the WPF-compatible header template property.</summary>
    public new static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(ComboBox),
            new PropertyMetadata(null, OnFinalHeaderTemplateChanged));

    /// <summary>Gets or sets the template used to display the header.</summary>
    public new DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the WPF-compatible header template selector property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(ComboBox),
            new PropertyMetadata(null, OnFinalHeaderTemplateSelectorChanged));

    /// <summary>Gets or sets the selector used to choose a header template.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the top-popup template selector property.</summary>
    public static readonly DependencyProperty TopPopupContentTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(TopPopupContentTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(ComboBox),
            new PropertyMetadata(null, OnFinalTopPopupPresentationChanged));

    /// <summary>Gets or sets the selector used for top-popup content.</summary>
    public DataTemplateSelector? TopPopupContentTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(TopPopupContentTemplateSelectorProperty);
        set => SetValue(TopPopupContentTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the top-popup string-format property.</summary>
    public static readonly DependencyProperty TopPopupContentStringFormatProperty =
        DependencyProperty.Register(
            nameof(TopPopupContentStringFormat),
            typeof(string),
            typeof(ComboBox),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the composite format used for top-popup content.</summary>
    public string? TopPopupContentStringFormat
    {
        get => (string?)GetValue(TopPopupContentStringFormatProperty);
        set => SetValue(TopPopupContentStringFormatProperty, value);
    }

    /// <summary>Identifies the WPF-compatible typed menu property.</summary>
    public new static readonly DependencyProperty MenuProperty =
        DependencyProperty.Register(
            nameof(Menu),
            typeof(RibbonMenu),
            typeof(ComboBox),
            new PropertyMetadata(null, OnFinalMenuChanged));

    /// <summary>Gets or sets the ribbon menu displayed below the drop-down items.</summary>
    public new RibbonMenu? Menu
    {
        get => (RibbonMenu?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Gets or sets whether a compatibility context menu is open.</summary>
    public bool IsContextMenuOpened { get; set; }

    /// <summary>Initializes a WPF-compatible combo-box facade.</summary>
    public ComboBox()
    {
        RegisterPropertyChangedCallback(
            RibbonComboBox.HeaderProperty,
            static (sender, _) => ((ComboBox)sender).ApplyFinalHeaderTemplateSelector());
        RegisterPropertyChangedCallback(
            RibbonComboBox.TopPopupContentProperty,
            static (sender, _) => ((ComboBox)sender).ApplyFinalTopPopupPresentation());
    }

    private static void OnFinalHeaderTemplateSelectorChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((ComboBox)sender).ApplyFinalHeaderTemplateSelector();

    private static void OnFinalHeaderTemplateChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((ComboBox)sender).ApplyFinalHeaderTemplateSelector();

    private static void OnFinalTopPopupPresentationChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((ComboBox)sender).ApplyFinalTopPopupPresentation();

    private static void OnFinalMenuChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((RibbonComboBox)sender).Menu = (RibbonMenu?)args.NewValue;

    private void ApplyFinalHeaderTemplateSelector() =>
        base.HeaderTemplate = CompatibilityHeaderTemplateAdapter.Select(
            HeaderTemplateSelector,
            Header,
            this,
            HeaderTemplate);

    private void ApplyFinalTopPopupPresentation()
    {
        if (TopPopupContentTemplateSelector is not null)
        {
            base.TopPopupContentTemplate =
                TopPopupContentTemplateSelector.SelectTemplate(TopPopupContent, this);
        }
    }
}

public partial class Gallery
{
    /// <summary>Identifies the delegate-based grouping property.</summary>
    public static readonly DependencyProperty GroupByAdvancedProperty =
        DependencyProperty.Register(
            nameof(GroupByAdvanced),
            typeof(Func<object, string>),
            typeof(Gallery),
            new PropertyMetadata(null, OnFinalGroupByAdvancedChanged));

    /// <summary>Gets or sets a delegate that computes an item's group name.</summary>
    public Func<object, string>? GroupByAdvanced
    {
        get => (Func<object, string>?)GetValue(GroupByAdvancedProperty);
        set => SetValue(GroupByAdvancedProperty, value);
    }

    /// <summary>Identifies whether this gallery is the final item in its items host.</summary>
    public static readonly DependencyProperty IsLastItemProperty =
        DependencyProperty.Register(
            nameof(IsLastItem),
            typeof(bool),
            typeof(Gallery),
            new PropertyMetadata(false));

    /// <summary>Gets whether this gallery is the final item in its items host.</summary>
    public bool IsLastItem
    {
        get => (bool)GetValue(IsLastItemProperty);
    }

    private static void OnFinalGroupByAdvancedChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((Gallery)sender).ApplyFinalAdvancedGroups();

    private void OnFinalGalleryItemsChanged(object? sender, NotifyCollectionChangedEventArgs args) =>
        ApplyFinalAdvancedGroups();

    private void ApplyFinalAdvancedGroups()
    {
        if (GroupByAdvanced is null)
        {
            return;
        }

        IsGrouped = true;
        foreach (var item in Items.OfType<RibbonGalleryItem>())
        {
            item.Group = GroupByAdvanced(item.DataContext ?? item);
        }
    }

    private void UpdateFinalIsLastItem()
    {
        for (DependencyObject? current = VisualTreeHelper.GetParent(this);
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is ItemsControl itemsControl)
            {
                var index = itemsControl.Items.IndexOf(this);
                if (index >= 0)
                {
                    SetValue(IsLastItemProperty, index == itemsControl.Items.Count - 1);
                    return;
                }
            }
        }

        SetValue(IsLastItemProperty, false);
    }
}

public partial class Spinner
{
    /// <summary>Identifies the text/value converter property.</summary>
    public static readonly DependencyProperty TextToValueConverterProperty =
        DependencyProperty.Register(
            nameof(TextToValueConverter),
            typeof(IValueConverter),
            typeof(Spinner),
            new PropertyMetadata(
                global::Fluent.Converters.SpinnerTextToValueConverter.DefaultInstance,
                OnFinalTextToValueConverterChanged));

    /// <summary>Gets or sets the converter used between editor text and numeric values.</summary>
    public IValueConverter TextToValueConverter
    {
        get => (IValueConverter)GetValue(TextToValueConverterProperty);
        set => SetValue(TextToValueConverterProperty, value);
    }

    /// <summary>Initializes a WPF-compatible spinner facade.</summary>
    public Spinner()
    {
        ValueChanged += (_, _) => ApplyFinalValueToEditor();
    }

    private static void OnFinalTextToValueConverterChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((Spinner)sender).ApplyFinalValueToEditor();
}

internal static class CompatibilityHeaderTemplateAdapter
{
    internal static DataTemplate? Select(
        DataTemplateSelector? selector,
        object? header,
        DependencyObject container,
        DataTemplate? fallback) =>
        selector?.SelectTemplate(header, container) ?? fallback;
}

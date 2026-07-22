namespace Fluent;

public partial class DropDownButton
{
    public static readonly DependencyProperty DismissOnClickOutsideProperty =
        DependencyProperty.Register(
            nameof(DismissOnClickOutside),
            typeof(bool),
            typeof(DropDownButton),
            new PropertyMetadata(true));

    public bool DismissOnClickOutside
    {
        get => (bool)GetValue(DismissOnClickOutsideProperty);
        set => SetValue(DismissOnClickOutsideProperty, value);
    }

    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(DropDownButton),
            new PropertyMetadata(null, OnHeaderTemplateSelectorChanged));

    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    public bool IsContextMenuOpened { get; set; }

    public DropDownButton()
    {
        RegisterPropertyChangedCallback(
            RibbonDropDownButton.HeaderProperty,
            static (sender, _) => ((DropDownButton)sender).ApplyHeaderTemplateSelector());
        RegisterPropertyChangedCallback(
            RibbonDropDownButton.IsSimplifiedProperty,
            static (sender, _) => ((DropDownButton)sender).OnSimplifiedPropertyChanged());
        DropDownOpened += (_, _) => OnDropDownOpened();
        DropDownClosed += (_, _) => OnDropDownClosed();
    }

    private static void OnHeaderTemplateSelectorChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((DropDownButton)sender).ApplyHeaderTemplateSelector();

    private void ApplyHeaderTemplateSelector()
    {
        if (HeaderTemplateSelector is not null)
        {
            base.HeaderTemplate =
                HeaderTemplateSelector.SelectTemplate(Header, this);
        }
    }
}

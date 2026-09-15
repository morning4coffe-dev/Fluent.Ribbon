namespace Fluent;

public partial class RibbonTextBox
{
    [ThreadStatic]
    private static DataTemplate? defaultHeaderContentTemplate;

    private DataTemplate? _headerContentFallback;
    private DataTemplate? _projectedHeaderTemplate;
    private bool _updatingHeaderProjection;

    /// <summary>Identifies the requested header template dependency property.</summary>
    public new static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(RibbonTextBox),
            new PropertyMetadata(null, OnHeaderTemplateChanged));

    /// <summary>Gets or sets the requested template used to display the header.</summary>
    /// <remarks>
    /// Use this property for nullable template bindings. Native WinUI TextBox cannot
    /// inspect an ordinary CLR Header model when its own HeaderTemplate is null.
    /// The inherited native template slot therefore contains an explicit template
    /// or a real content-presenting fallback, without changing Header or its binding.
    /// Direct bindings to the native HeaderTemplateProperty remain caller-owned;
    /// a null native template together with a CLR model is not supported by WinUI.
    /// Custom presenters should bind to the requested HeaderTemplate and
    /// HeaderTemplateSelector properties; a native HeaderTemplate TemplateBinding
    /// observes the protective projection, which is intentionally never null.
    /// </remarks>
    public new DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the requested header template selector dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(RibbonTextBox),
            new PropertyMetadata(null, OnHeaderTemplateChanged));

    /// <summary>Gets or sets the selector used when no explicit header template is supplied.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    private void InitializeHeaderPresentation()
    {
        _headerContentFallback = GetDefaultHeaderContentTemplate();
        RegisterPropertyChangedCallback(
            HeaderProperty,
            static (sender, _) => ((RibbonTextBox)sender).ApplyHeaderPresentation());
        RegisterPropertyChangedCallback(
            TextBox.HeaderTemplateProperty,
            static (sender, _) => ((RibbonTextBox)sender).OnNativeHeaderTemplateChanged());
        ApplyHeaderPresentation();
    }

    private static DataTemplate GetDefaultHeaderContentTemplate()
    {
        if (defaultHeaderContentTemplate is null)
        {
            var resources = new ResourceDictionary
            {
                Source = new Uri("ms-appx:///Fluent.Ribbon.Uno/Themes/RibbonTextBox.xaml"),
            };
            defaultHeaderContentTemplate = resources["Fluent.Ribbon.EditorHeaderContentTemplate"] as DataTemplate
                ?? throw new InvalidOperationException("The ribbon text editor has no fallback header content template.");
        }
        return defaultHeaderContentTemplate;
    }

    private static void OnHeaderTemplateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        => ((RibbonTextBox)sender).ApplyHeaderPresentation();

    private void OnNativeHeaderTemplateChanged()
    {
        if (!_updatingHeaderProjection)
        {
            ApplyHeaderPresentation();
        }
    }

    private bool OwnsNativeHeaderTemplate
        => _projectedHeaderTemplate is not null
           && GetBindingExpression(TextBox.HeaderTemplateProperty) is null
           && ReferenceEquals(ReadLocalValue(TextBox.HeaderTemplateProperty), _projectedHeaderTemplate)
           && ReferenceEquals(base.HeaderTemplate, _projectedHeaderTemplate);

    private void ApplyHeaderPresentation()
    {
        if (_headerContentFallback is null || _updatingHeaderProjection)
        {
            return;
        }

        var ownsProjection = OwnsNativeHeaderTemplate;
        var canInitializeProjection = GetBindingExpression(TextBox.HeaderTemplateProperty) is null
                                      && ReferenceEquals(ReadLocalValue(TextBox.HeaderTemplateProperty), DependencyProperty.UnsetValue)
                                      && base.HeaderTemplate is null;
        if (ownsProjection || canInitializeProjection)
        {
            var projected = HeaderTemplate ?? _headerContentFallback;
            if (!ownsProjection || !ReferenceEquals(projected, _projectedHeaderTemplate))
            {
                _updatingHeaderProjection = true;
                try
                {
                    _projectedHeaderTemplate = projected;
                    // Native visibility runs synchronously. Replace one non-null
                    // template with another; never clear/rebind this slot in between.
                    base.HeaderTemplate = projected;
                }
                finally
                {
                    _updatingHeaderProjection = false;
                }
            }
        }

        if (_headerPresenter is ContentPresenter { Tag: "Fluent.EditorHeader" } presenter)
        {
            var consumerNativeTemplate = OwnsNativeHeaderTemplate ? null : base.HeaderTemplate;
            // Native presenters can reuse a selector result for a new model of
            // the same type. Select again when Header or its selector changes.
            var template = HeaderTemplate ?? consumerNativeTemplate ?? HeaderTemplateSelector?.SelectTemplate(Header, this);
            EditorHeaderTemplateBinding.Apply(presenter, this, template);
        }
    }
}

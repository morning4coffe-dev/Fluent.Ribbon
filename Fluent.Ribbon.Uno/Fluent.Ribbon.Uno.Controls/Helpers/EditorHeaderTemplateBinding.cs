namespace Fluent;

internal static class EditorHeaderTemplateBinding
{
    private static readonly IValueConverter TemplateValueConverter = new SelectedTemplateConverter();

    internal static void Apply(ContentPresenter presenter, DependencyObject owner, DataTemplate? template)
    {
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding
        {
            Source = owner,
            Mode = BindingMode.OneWay,
            Converter = TemplateValueConverter,
            ConverterParameter = template,
        });
    }

    private sealed class SelectedTemplateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) => parameter;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}

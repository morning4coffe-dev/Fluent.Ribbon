namespace Fluent;

internal static class QuickAccessPresentationValue
{
    internal static object? Create(object? value)
    {
        if (value is not UIElement)
        {
            return value;
        }

        FrameworkElement copy;
        DependencyProperty[] properties;
        switch (value)
        {
            case Image:
                copy = new Image();
                properties = [Image.SourceProperty, Image.StretchProperty];
                break;
            case FontIcon:
                copy = new FontIcon();
                properties =
                [
                    FontIcon.GlyphProperty, FontIcon.FontFamilyProperty, FontIcon.FontSizeProperty,
                    FontIcon.FontStyleProperty, FontIcon.FontWeightProperty, FontIcon.IsTextScaleFactorEnabledProperty,
                    IconElement.ForegroundProperty,
                ];
                break;
            case SymbolIcon:
                copy = new SymbolIcon();
                properties = [SymbolIcon.SymbolProperty, IconElement.ForegroundProperty];
                break;
            case BitmapIcon:
                copy = new BitmapIcon();
                properties = [BitmapIcon.UriSourceProperty, BitmapIcon.ShowAsMonochromeProperty, IconElement.ForegroundProperty];
                break;
            case PathIcon:
                copy = new PathIcon();
                properties = [PathIcon.DataProperty, IconElement.ForegroundProperty];
                break;
            case IconSourceElement:
                copy = new IconSourceElement();
                properties = [IconSourceElement.IconSourceProperty, IconElement.ForegroundProperty];
                break;
            case TextBlock text when text.Inlines.Count == 0
                                     || text.Inlines.Count == 1 && text.Inlines[0] is Microsoft.UI.Xaml.Documents.Run run
                                     && run.Text == text.Text:
                copy = new TextBlock();
                properties =
                [
                    TextBlock.TextProperty, TextBlock.FontFamilyProperty, TextBlock.FontSizeProperty,
                    TextBlock.FontStyleProperty, TextBlock.FontWeightProperty, TextBlock.ForegroundProperty,
                    TextBlock.TextWrappingProperty, TextBlock.TextTrimmingProperty, TextBlock.TextAlignmentProperty,
                ];
                break;
            default:
                throw new NotSupportedException(
                    $"Quick access presentation cannot duplicate {value.GetType().FullName}. " +
                    "Use a data value with a header template, an ImageSource/IconSource, or a supported icon element. " +
                    "Interactive drop-down children are transferred live instead of being duplicated.");
        }

        var source = (FrameworkElement)value;
        var bindings = QuickAccessBindingSession.For(source, copy);
        bindings.BindCommon(enabled: false);
        bindings.Bind(FrameworkElement.WidthProperty);
        bindings.Bind(FrameworkElement.HeightProperty);
        bindings.Bind(FrameworkElement.MarginProperty);
        bindings.Bind(FrameworkElement.HorizontalAlignmentProperty);
        bindings.Bind(FrameworkElement.VerticalAlignmentProperty);
        foreach (var property in properties)
        {
            bindings.Bind(property);
        }

        return copy;
    }
}

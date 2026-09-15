#if WINDOWS
namespace Fluent;

internal static class NativePopupTemplateHelper
{
    internal static void Ensure(
        Control owner, object defaultStyleKey, Uri? defaultStyleResourceUri,
        XamlRoot root, ref Binding? initialization)
    {
        if (owner.XamlRoot is null)
        {
            owner.XamlRoot = root;
        }
        if (owner.IsLoaded || owner.Template is not null
            || owner.ReadLocalValue(Control.TemplateProperty) != DependencyProperty.UnsetValue
            || owner.GetBindingExpression(Control.TemplateProperty) is not null)
        {
            return;
        }

        // Native default styles are initialized on live Enter, not ApplyTemplate.
        // Own only this temporary value; caller bindings and first Loading win.
        var template = owner.Style is null
            ? FindTemplate(owner.Resources, owner.GetType())
              ?? FindTemplate(Application.Current.Resources, owner.GetType())
            : null;
        template ??= FindTemplate(owner.Resources, defaultStyleKey)
                     ?? FindTemplate(Application.Current.Resources, defaultStyleKey);
        if (template is null)
        {
            var defaults = new ResourceDictionary
            {
                Source = defaultStyleResourceUri ?? new Uri("ms-appx:///Fluent.Ribbon.Uno/Themes/Generic.xaml"),
            };
            template = FindTemplate(defaults, defaultStyleKey)
                       ?? throw new InvalidOperationException("The popup owner has no default ControlTemplate.");
        }

        initialization = new Binding { Source = template, Mode = BindingMode.OneWay };
        owner.SetBinding(Control.TemplateProperty, initialization);
    }

    internal static void Release(Control owner, ref Binding? initialization)
    {
        var binding = initialization;
        initialization = null;
        if (binding is not null
            && ReferenceEquals(owner.GetBindingExpression(Control.TemplateProperty)?.ParentBinding, binding))
        {
            owner.ClearValue(Control.TemplateProperty);
        }
    }

    private static ControlTemplate? FindTemplate(ResourceDictionary resources, object key)
    {
        if (!resources.TryGetValue(key, out var value))
        {
            return null;
        }

        for (var style = value as Style; style is not null; style = style.BasedOn)
        {
            if (style.Setters.OfType<Setter>().LastOrDefault(setter => setter.Property == Control.TemplateProperty)
                is { Value: ControlTemplate template })
            {
                return template;
            }
        }
        return null;
    }
}
#endif

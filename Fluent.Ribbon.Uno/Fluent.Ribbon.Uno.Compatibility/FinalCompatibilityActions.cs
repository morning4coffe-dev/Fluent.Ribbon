using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Fluent;

public partial class ToggleButton
{
    /// <inheritdoc />
    protected virtual void OnClick()
    {
        if (IsDefinitive)
        {
            PopupService.RaiseDismissPopupEvent(this, DismissPopupMode.Always, DismissPopupReason.Undefined);
        }

    }

    /// <inheritdoc />
    protected virtual void OnChecked(RoutedEventArgs e)
    {
    }

    /// <summary>Invokes the primary click action.</summary>
    public void InvokeClick()
    {
        base.OnKeyTipPressed();
        OnClick();
    }
}

public partial class TextBox
{
    /// <summary>Binds the portable quick-access surface to another facade instance.</summary>
    protected virtual void BindQuickAccessItem(FrameworkElement element)
    {
        RibbonControl.BindQuickAccessItem(this, element);
        CompatibilityQuickAccessBindings.Apply(this, element);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
    }
}

public partial class ComboBox
{
    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        DropDownPopup =
            GetTemplateChild("Popup") as Microsoft.UI.Xaml.Controls.Primitives.Popup
            ?? GetTemplateChild("PART_Popup") as Microsoft.UI.Xaml.Controls.Primitives.Popup;
        finalHeaderPresenter = CompatibilityHeaderTemplateAdapter.AttachEditorHeader(GetTemplateChild("HeaderText"));
        ApplyFinalHeaderTemplateSelector();
        ApplyFinalTopPopupPresentation();
    }

    /// <summary>Called after the core combo box opens its drop-down.</summary>
    protected virtual void OnDropDownOpened(EventArgs e)
    {
    }

    /// <summary>Called after the core combo box closes its drop-down.</summary>
    protected virtual void OnDropDownClosed(EventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnDropDownOpened(object e)
    {
        base.OnDropDownOpened(e);
        OnDropDownOpened(EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnDropDownClosed(object e)
    {
        base.OnDropDownClosed(e);
        OnDropDownClosed(EventArgs.Empty);
    }
}

public partial class SplitButton
{
    /// <summary>Called after the simplified state changes.</summary>
    protected override void OnIsSimplifiedChanged(bool oldValue, bool newValue)
    {
        base.OnIsSimplifiedChanged(oldValue, newValue);
    }
}

public partial class GalleryItem
{
    /// <summary>Initializes click forwarding for the gallery-item facade.</summary>
    public GalleryItem()
    {
        Click += OnClick;
    }

    /// <summary>Handles gallery-item activation.</summary>
    protected virtual void OnClick(object sender, RoutedEventArgs e)
    {
        if (IsDefinitive)
        {
            PopupService.RaiseDismissPopupEvent(this, DismissPopupMode.Always);
        }
    }
}

public partial class Spinner
{
    /// <inheritdoc />
    protected override void OnApplyTemplate() => base.OnApplyTemplate();

    /// <inheritdoc />
    protected override bool TryConvertTextToValue(string text, out double value)
    {
        if (ReferenceEquals(TextToValueConverter, global::Fluent.Converters.SpinnerTextToValueConverter.DefaultInstance))
        {
            return base.TryConvertTextToValue(text, out value);
        }

        var parameter = Tuple.Create(Format, Value);
        var converted = TextToValueConverter is global::Fluent.Converters.SpinnerTextToValueConverter spinnerConverter
            ? spinnerConverter.Convert(
                text,
                typeof(double),
                parameter,
                CultureInfo.CurrentCulture)
            : TextToValueConverter.Convert(
                text,
                typeof(double),
                parameter,
                CultureInfo.CurrentCulture.Name);
        value = converted is double number ? number : Value;
        return converted is double && double.IsFinite(value);
    }

    /// <inheritdoc />
    protected override string FormatValue(double value)
    {
        var converted = TextToValueConverter is global::Fluent.Converters.SpinnerTextToValueConverter spinnerConverter
            ? spinnerConverter.ConvertBack(value, typeof(string), Format, CultureInfo.CurrentCulture)
            : TextToValueConverter.ConvertBack(
                value,
                typeof(string),
                Format,
                CultureInfo.CurrentCulture.Name);
        return converted as string
               ?? throw new InvalidOperationException("The spinner value converter must format values as strings.");
    }
}

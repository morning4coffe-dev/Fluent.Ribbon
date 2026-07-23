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
            CompatibilityVisualTree.CloseAncestorDropDown(this);
        }
    }
}

public partial class Spinner
{
    private Microsoft.UI.Xaml.Controls.TextBox? finalEditor;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (finalEditor is not null)
        {
            finalEditor.LostFocus -= OnFinalEditorLostFocus;
        }

        base.OnApplyTemplate();
        finalEditor = GetTemplateChild("PART_TextBox") as Microsoft.UI.Xaml.Controls.TextBox;
        if (finalEditor is not null)
        {
            finalEditor.LostFocus += OnFinalEditorLostFocus;
            ApplyFinalValueToEditor();
        }
    }

    private void OnFinalEditorLostFocus(object sender, RoutedEventArgs e)
    {
        if (finalEditor is null)
        {
            return;
        }

        var parameter = Tuple.Create(Format, Value);
        var converted = TextToValueConverter is global::Fluent.Converters.SpinnerTextToValueConverter spinnerConverter
            ? spinnerConverter.TextToDouble(
                finalEditor.Text,
                Format,
                Value,
                CultureInfo.CurrentCulture)
            : TextToValueConverter.Convert(
                finalEditor.Text,
                typeof(double),
                parameter,
                CultureInfo.CurrentCulture.Name);
        if (converted is double value)
        {
            Value = value;
        }
    }

    private void ApplyFinalValueToEditor()
    {
        if (finalEditor is null)
        {
            return;
        }

        var converted = TextToValueConverter is global::Fluent.Converters.SpinnerTextToValueConverter spinnerConverter
            ? spinnerConverter.DoubleToText(Value, Format, CultureInfo.CurrentCulture)
            : TextToValueConverter.ConvertBack(
                Value,
                typeof(string),
                Format,
                CultureInfo.CurrentCulture.Name);
        if (converted is string text)
        {
            finalEditor.Text = text;
        }
    }
}

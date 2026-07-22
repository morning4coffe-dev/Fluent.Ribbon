namespace Fluent;

/// <summary>
/// Defines a control that provides a medium icon.
/// </summary>
public interface IMediumIconProvider
{
    /// <summary>
    /// Gets or sets the medium icon (16x16).
    /// </summary>
    object? MediumIcon
    {
        get => this switch
        {
            RibbonButton control => control.MediumIcon,
            RibbonCheckBox control => control.MediumIcon,
            RibbonComboBox control => control.MediumIcon,
            RibbonDropDownButton control => control.MediumIcon,
            RibbonRadioButton control => control.MediumIcon,
            RibbonSpinner control => control.MediumIcon,
            RibbonTextBox control => control.MediumIcon,
            RibbonToggleButton control => control.MediumIcon,
            _ => null,
        };
        set
        {
            var imageSource = value switch
            {
                null => null,
                ImageSource source => source,
                _ => throw new ArgumentException(
                    "Core Ribbon controls require an ImageSource medium icon.",
                    nameof(value)),
            };

            switch (this)
            {
                case RibbonButton control:
                    control.MediumIcon = imageSource;
                    break;
                case RibbonCheckBox control:
                    control.MediumIcon = imageSource;
                    break;
                case RibbonComboBox control:
                    control.MediumIcon = imageSource;
                    break;
                case RibbonDropDownButton control:
                    control.MediumIcon = imageSource;
                    break;
                case RibbonRadioButton control:
                    control.MediumIcon = imageSource;
                    break;
                case RibbonSpinner control:
                    control.MediumIcon = imageSource;
                    break;
                case RibbonTextBox control:
                    control.MediumIcon = imageSource;
                    break;
                case RibbonToggleButton control:
                    control.MediumIcon = imageSource;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{GetType().FullName} must implement MediumIcon explicitly.");
            }
        }
    }
}

/// <summary>
/// Provides the WPF-compatible medium-icon dependency property.
/// </summary>
public partial class MediumIconProviderProperties : DependencyObject
{
    private MediumIconProviderProperties()
    {
    }

    /// <summary>
    /// Identifies the medium-icon dependency property.
    /// </summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(IMediumIconProvider.MediumIcon),
            typeof(object),
            typeof(MediumIconProviderProperties),
            new PropertyMetadata(null));
}

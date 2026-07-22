namespace Fluent;

/// <summary>
/// Defines a control that provides a large icon.
/// </summary>
public interface ILargeIconProvider
{
    /// <summary>
    /// Gets or sets the large icon (32x32).
    /// </summary>
    object? LargeIcon
    {
        get => this switch
        {
            RibbonButton control => control.LargeIcon,
            RibbonDropDownButton control => control.LargeIcon,
            RibbonToggleButton control => control.LargeIcon,
            _ => null,
        };
        set
        {
            var imageSource = value switch
            {
                null => null,
                ImageSource source => source,
                _ => throw new ArgumentException(
                    "Core Ribbon controls require an ImageSource large icon.",
                    nameof(value)),
            };

            switch (this)
            {
                case RibbonButton control:
                    control.LargeIcon = imageSource;
                    break;
                case RibbonDropDownButton control:
                    control.LargeIcon = imageSource;
                    break;
                case RibbonToggleButton control:
                    control.LargeIcon = imageSource;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{GetType().FullName} must implement LargeIcon explicitly.");
            }
        }
    }
}

/// <summary>
/// Provides the WPF-compatible large-icon dependency property.
/// </summary>
public partial class LargeIconProviderProperties : DependencyObject
{
    private LargeIconProviderProperties()
    {
    }

    /// <summary>
    /// Identifies the large-icon dependency property.
    /// </summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(ILargeIconProvider.LargeIcon),
            typeof(object),
            typeof(LargeIconProviderProperties),
            new PropertyMetadata(null));
}

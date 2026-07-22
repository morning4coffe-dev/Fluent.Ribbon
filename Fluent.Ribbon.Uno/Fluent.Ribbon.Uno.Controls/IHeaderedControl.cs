namespace Fluent;

/// <summary>
/// Defines a control that provides a header.
/// </summary>
public interface IHeaderedControl
{
    /// <summary>
    /// Gets or sets the header content.
    /// </summary>
    object? Header { get; set; }

    /// <summary>
    /// Gets or sets the template used to display the header.
    /// </summary>
    DataTemplate? HeaderTemplate
    {
        get => this switch
        {
            InRibbonGallery control => control.HeaderTemplate,
            RibbonDropDownButton control => control.HeaderTemplate,
            RibbonGroupBox control => control.HeaderTemplate,
            _ => null,
        };
        set
        {
            switch (this)
            {
                case InRibbonGallery control:
                    control.HeaderTemplate = value;
                    break;
                case RibbonDropDownButton control:
                    control.HeaderTemplate = value;
                    break;
                case RibbonGroupBox control:
                    control.HeaderTemplate = value;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{GetType().FullName} must implement HeaderTemplate explicitly.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the selector used to choose a header template.
    /// </summary>
    DataTemplateSelector? HeaderTemplateSelector
    {
        get => this switch
        {
            InRibbonGallery control => control.HeaderTemplateSelector,
            RibbonGroupBox control => control.HeaderTemplateSelector,
            RibbonTab control => control.HeaderTemplateSelector,
            _ => null,
        };
        set
        {
            switch (this)
            {
                case InRibbonGallery control:
                    control.HeaderTemplateSelector = value;
                    break;
                case RibbonGroupBox control:
                    control.HeaderTemplateSelector = value;
                    break;
                case RibbonTab control:
                    control.HeaderTemplateSelector = value;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{GetType().FullName} must implement HeaderTemplateSelector explicitly.");
            }
        }
    }
}

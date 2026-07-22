namespace Fluent;

/// <summary>WPF-compatible drop-down button backed by the Uno ribbon implementation.</summary>
public partial class DropDownButton :
    RibbonDropDownButton,
    IQuickAccessItemProvider,
    IRibbonControl,
    IDropDownControl,
    ILargeIconProvider,
    IMediumIconProvider,
    ISimplifiedRibbonControl
{
}

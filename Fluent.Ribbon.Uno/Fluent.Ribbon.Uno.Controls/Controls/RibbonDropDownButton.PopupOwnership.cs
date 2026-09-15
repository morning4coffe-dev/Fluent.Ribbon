namespace Fluent;

public partial class RibbonDropDownButton
{
    internal virtual DependencyObject? OpenFlyoutContentRoot
    {
        get
        {
#if WINDOWS
            return OpenNativePopup?.Child;
#else
            var source = _sharedQuickAccessSource ?? this;
            return IsDropDownOpen
                   && ReferenceEquals(source._activePopupAnchor, this)
                   && source._flyout?.IsOpen == true
                ? source._flyout.Content
                : null;
#endif
        }
    }
}

public partial class ApplicationMenu
{
    internal override DependencyObject? OpenFlyoutContentRoot => _flyout?.Content;
}

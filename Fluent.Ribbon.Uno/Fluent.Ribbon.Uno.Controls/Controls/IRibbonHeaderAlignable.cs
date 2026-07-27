namespace Fluent;

/// <summary>
/// Internal contract implemented by ribbon input controls (spinner, text box, combo box)
/// that exposes the control's header element so that a <see cref="RibbonGroupBox"/> can size
/// the headers of every such control in the group to a common width.
/// <para>
/// This emulates WPF's <c>Grid.IsSharedSizeScope</c> / <c>SharedSizeGroup</c> mechanism (used by
/// the WPF Spinner/TextBox/ComboBox templates for their header column), which WinUI / Uno Platform
/// does not provide.
/// </para>
/// </summary>
internal interface IRibbonHeaderAlignable
{
    /// <summary>
    /// Gets the resolved header element (the template's <c>HeaderText</c>), or
    /// <see langword="null"/> when the template has not been applied yet.
    /// </summary>
    FrameworkElement? HeaderPresenter { get; }
}

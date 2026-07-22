namespace Fluent.Helpers;

/// <summary>
/// Helper functions for classes implementing <see cref="ILogicalChildSupport"/>.
/// </summary>
public static class LogicalChildSupportHelper
{
    /// <summary>
    /// Updates logical ownership after a dependency property changes.
    /// </summary>
    public static void OnLogicalChildPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var logicalChildSupport = d as ILogicalChildSupport
            ?? throw new ArgumentException(
                "Argument must be of type ILogicalChildSupport.",
                nameof(d));

        if (e.OldValue is DependencyObject oldValue)
        {
            logicalChildSupport.RemoveLogicalChild(oldValue);
        }

        if (e.NewValue is DependencyObject newValue
            && VisualTreeHelper.GetParent(newValue) is null)
        {
            logicalChildSupport.AddLogicalChild(newValue);
        }
    }
}

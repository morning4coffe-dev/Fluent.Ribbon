namespace Fluent;

/// <summary>
/// Receives the logical owner that presents an item inside a detached drop-down surface.
/// </summary>
public interface IDropDownItemOwner
{
    /// <summary>Sets the logical drop-down owner.</summary>
    void SetDropDownOwner(DependencyObject owner);
}

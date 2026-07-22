namespace Fluent;

/// <summary>
/// Provides the portable command-source contract used by ribbon controls.
/// </summary>
public interface ICommandSource
{
    /// <summary>Gets the command.</summary>
    System.Windows.Input.ICommand? Command { get; }

    /// <summary>Gets the command parameter.</summary>
    object? CommandParameter { get; }

    /// <summary>Gets the portable command target.</summary>
    UIElement? CommandTarget { get; }
}

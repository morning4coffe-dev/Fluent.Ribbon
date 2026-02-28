namespace Fluent.Internal;

/// <summary>
/// Helper for executing commands from controls that act as command sources.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, simplified for Uno/WinUI.
/// WinUI does not have RoutedCommand, so this only handles ICommand.
/// </remarks>
public static class CommandHelper
{
    /// <summary>
    /// Determines whether the command can execute with the given parameter.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="commandParameter">The command parameter.</param>
    /// <returns><c>true</c> if the command can execute; otherwise <c>false</c>.</returns>
    public static bool CanExecute(ICommand? command, object? commandParameter)
    {
        if (command is null)
        {
            return false;
        }

        return command.CanExecute(commandParameter);
    }

    /// <summary>
    /// Executes the command with the given parameter.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="commandParameter">The command parameter.</param>
    public static void Execute(ICommand? command, object? commandParameter)
    {
        if (command is null)
        {
            return;
        }

        if (command.CanExecute(commandParameter))
        {
            command.Execute(commandParameter);
        }
    }
}

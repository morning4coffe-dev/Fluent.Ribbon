namespace Fluent.Extensions;

/// <summary>
/// Extension methods for <see cref="ICommand"/> sources.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
public static class ICommandSourceExtensions
{
    /// <summary>Executes the command exposed by a portable command source.</summary>
    public static void ExecuteCommand(this global::Fluent.ICommandSource commandSource)
    {
        ArgumentNullException.ThrowIfNull(commandSource);
        Internal.CommandHelper.Execute(
            commandSource.Command,
            commandSource.CommandParameter,
            commandSource.CommandTarget);
    }

    /// <summary>Gets whether the command exposed by a portable command source can execute.</summary>
    public static bool CanExecuteCommand(this global::Fluent.ICommandSource commandSource)
    {
        ArgumentNullException.ThrowIfNull(commandSource);
        return Internal.CommandHelper.CanExecute(
            commandSource.Command,
            commandSource.CommandParameter,
            commandSource.CommandTarget);
    }

    /// <summary>
    /// Executes the command from a command source if it can execute.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="commandParameter">The parameter to pass.</param>
    public static void ExecuteCommandSource(ICommand? command, object? commandParameter)
    {
        Internal.CommandHelper.Execute(command, commandParameter);
    }

    /// <summary>
    /// Determines whether the command from a command source can execute.
    /// </summary>
    /// <param name="command">The command to check.</param>
    /// <param name="commandParameter">The parameter to pass.</param>
    /// <returns><c>true</c> if the command can execute.</returns>
    public static bool CanExecuteCommandSource(ICommand? command, object? commandParameter)
    {
        return Internal.CommandHelper.CanExecute(command, commandParameter);
    }
}

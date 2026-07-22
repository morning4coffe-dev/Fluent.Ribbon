namespace Fluent;

/// <summary>
/// Provides commands belonging to the Ribbon.
/// </summary>
public static class RibbonCommands
{
    /// <summary>
    /// Opens the Backstage or application menu for the Ribbon supplied as the command parameter.
    /// </summary>
    public static readonly XamlUICommand OpenBackstage = CreateOpenBackstageCommand();

    private static XamlUICommand CreateOpenBackstageCommand()
    {
        var command = new XamlUICommand
        {
            Label = "Open backstage",
        };

        command.ExecuteRequested += OnOpenBackstageExecuteRequested;
        return command;
    }

    private static void OnOpenBackstageExecuteRequested(
        XamlUICommand sender,
        ExecuteRequestedEventArgs args)
    {
        var menu = args.Parameter switch
        {
            Ribbon ribbon => ribbon.Menu,
            UIElement element => element,
            _ => null,
        };

        switch (menu)
        {
            case Backstage backstage:
                backstage.IsOpen = true;
                break;

            case ApplicationMenu applicationMenu:
                applicationMenu.Open();
                break;
        }
    }
}

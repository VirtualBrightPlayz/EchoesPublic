public abstract class SimpleGameCommandBase : IConsoleCommand
{
    public abstract string Command { get; }
    public abstract string[] Alias { get; }
    public abstract string CommandDescription { get; }
    public abstract bool Execute(string[] args, out string response);

    public bool Execute(ICommandSender sender, string[] args, out string response)
    {
        if (sender is GameConsole console)
        {
            return Execute(args, out response);
        }
        if (sender is PlayerConsole player && player.IsMultiplayerAuthority())
        {
            return Execute(args, out response);
        }

        response = "This command must be run from the console.";
        return false;
    }
}
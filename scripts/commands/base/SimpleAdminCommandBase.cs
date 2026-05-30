using System;
using Godot;

public abstract class SimpleAdminCommandBase : IConsoleCommand
{
    public abstract string Command { get; }
    public abstract string[] Alias { get; }
    public abstract string CommandDescription { get; }
    public abstract bool Execute(PlayerConsole console, string[] args, out string response);

    public bool Execute(ICommandSender sender, string[] args, out string response)
    {
        if (sender is not PlayerConsole console || !console.IsAdmin)
        {
            response = "You do not have permission to execute this command.";
            return false;
        }

        return Execute(console, args, out response);
    }
}
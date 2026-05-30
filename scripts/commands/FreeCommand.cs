using System;

public class FreeCommand : IConsoleCommand
{
    public string Command { get; } = "Free";

    public string[] Alias { get; } = new string[] { "freecam" };

    public string CommandDescription => "Opens the free-camera";

    public bool Execute(ICommandSender sender, string[] args, out string response)
    {
        if (!(sender is PlayerConsole console) || !console.IsAdmin)
        {
            response = "You do not have permission to execute this command.";
            return false;
        }

        FreeCam.Instance?.OpenWindow();
        response = "Opened free-camera";
        return true;
    }
}

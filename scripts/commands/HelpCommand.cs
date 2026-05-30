using System;
using System.Linq;
using System.Reflection;

public class HelpCommand : IConsoleCommand
{
    public string Command { get; } = "Help";

    public string[] Alias { get; } = new string[] { "commands", "cmds", "?" };

    public string CommandDescription => "Prints out a list of avalible commands.";

    public bool Execute(ICommandSender sender, string[] args, out string response)
    {
        string commands = string.Empty;
        commands += '\n';
        foreach (IConsoleCommand consoleCommand in GameConsole.Instance.RegisteredCommands.Where(cmd => cmd.GetType().GetCustomAttribute<HiddenCommand>() == null))
        {
            commands += consoleCommand.Command + ": " + consoleCommand.CommandDescription + "\n";
        }
        response = commands;
        return true;
    }
}

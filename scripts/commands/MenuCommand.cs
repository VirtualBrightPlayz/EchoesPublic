using System;
using Godot;

public class MenuCommand : SimpleGameCommandBase
{
    public override string Command { get; } = "Menu";

    public override string[] Alias { get; } = Array.Empty<string>();

    public override string CommandDescription { get; } = "Returns to the menu.";

    public override bool Execute(string[] args, out string response)
    {
        if (IInitScript.IsServerOnly)
        {
            response = "Cannot return to menu on dedicated servers.";
            return false;
        }

        response = "Killing network instance and returning to the menu...";
        NetworkManager.Instance.Shutdown();

        return true;
    }
}

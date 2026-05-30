using System;
using Godot;

public class QuitCommand : SimpleGameCommandBase
{
    public override string Command => "Quit";

    public override string[] Alias { get; } = new string[] { "exit" };

    public override string CommandDescription { get; } = "Quits the game.";

    public override bool Execute(string[] args, out string response)
    {
        response = "Quitting the game...";
        IInitScript.SceneTree.Quit();
        return true;
    }
}

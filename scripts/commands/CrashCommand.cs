using System;
using Godot;

public class CrashCommand : SimpleGameCommandBase
{
    public override string Command => "Crash";

    public override string[] Alias { get; } = Array.Empty<string>();

    public override string CommandDescription { get; } = "Crashes the game.";

    public override bool Execute(string[] args, out string response)
    {
        response = "Crashing the game...";
        // unsafe
        {
            // int i = *((int*)GD.Randi());
        }
        OS.Crash("Debug Crash!");
        return true;
    }
}

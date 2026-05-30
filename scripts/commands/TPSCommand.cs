using System;
using System.Linq;
using Godot;

public class TPSCommand : SimpleGameCommandBase
{
    public override string Command => "TPS";
    public override string[] Alias => Array.Empty<string>();
    public override string CommandDescription => "Shows the TPS.";

    public override bool Execute(string[] args, out string response)
    {
        if (args.Length > 0)
        {
            if (int.TryParse(args[0], out int fps))
            {
                Engine.MaxFps = fps;
            }
        }
        response = Engine.GetFramesPerSecond().ToString();
        return true;
    }
}
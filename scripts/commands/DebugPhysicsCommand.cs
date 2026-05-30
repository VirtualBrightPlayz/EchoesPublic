using System;
using Godot;

public class DebugPhysicsCommand : SimpleGameCommandBase
{
    public override string Command => "DebugPhysics";
    public override string[] Alias => Array.Empty<string>();
    public override string CommandDescription => "Controls physics debugging settings.";

    public override bool Execute(string[] args, out string response)
    {
        if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "step":
                    if (args.Length > 1 && int.TryParse(args[1], out int tps))
                    {
                        Engine.PhysicsTicksPerSecond = tps;
                        response = $"Set physics TPS to {tps}";
                        return true;
                    }
                    break;
                case "lerp":
                    if (args.Length > 1 && bool.TryParse(args[1], out bool val))
                    {
                        ((SceneTree)Engine.GetMainLoop()).PhysicsInterpolation = val;
                        response = $"Set physics interpolation to {val}";
                        return true;
                    }
                    break;
                case "info":
                    response = $"Physics\nTarget TPS: {Engine.PhysicsTicksPerSecond}\nInterpolation: {((SceneTree)Engine.GetMainLoop()).PhysicsInterpolation}";
                    return true;
            }
        }
        response = $"DebugPhysics <step|lerp|info> <tps|true/false>\n";
        return false;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EffectCommand : SimpleAdminPlayerCommandBase
{
    public override string Command => "effect";

    public override string[] Alias => new string[] { "curse" };

    public override string CommandDescription => "Effects a player. Syntax: effect <playerId?> <type> <duration> <intensity>";

    public override PlayerMode Target => PlayerMode.SelfTarget;

    public override bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response)
    {
        if(!player.Multiplayer.IsServer())
        {
            response = "This command must be ran on the server.";
            return false;
        }
        string typeStr = args[0];
        string durationStr = args[1];
        string intensityStr = args[2];
        EffectType type = EffectType.Invalid;
        float duration = 1f;
        float intensity = 1f;
        if(!Enum.TryParse(typeStr, out type))
        {
            response = $"Invalid type '{typeStr}'";
            return false;
        }
        if(!float.TryParse(durationStr, out duration))
        {
            response = $"Invalid duration '{durationStr}'";
            return false;
        }
        if (!float.TryParse(intensityStr, out intensity))
        {
            response = $"Invalid duration '{intensityStr}'";
            return false;
        }
        if (Godot.Mathf.IsZeroApprox(intensity) && Godot.Mathf.IsZeroApprox(duration))
        {
            player.statusEffectManager.DisableEffect(type);
        }
        else
        {
            player.statusEffectManager.EnableEffect(type, duration, intensity);
        }
        response = "Done.";
        return true;
    }
}

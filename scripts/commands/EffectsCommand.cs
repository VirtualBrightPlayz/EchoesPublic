using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EffectsCommand : SimpleAdminPlayerCommandBase
{
    public override string Command => "effects";

    public override string[] Alias => new string[] { };

    public override string CommandDescription => "Prints all effects";

    public override PlayerMode Target => PlayerMode.SelfTarget;

    public override bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response)
    {
        string resp = "Effects: \n";
        foreach(StatusEffectBase effect in player.statusEffectManager.GetEffects())
        {
            resp += $"Class Name: {effect.GetType().FullName}, Type: {effect.Type}, Active?: {effect.Active}, Duration: {effect.Duration}, Intensity: {effect.Intensity}\n";
        }
        response = resp;
        return true;
    }
}

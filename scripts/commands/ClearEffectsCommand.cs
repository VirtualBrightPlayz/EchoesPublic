public class ClearEffectsCommand : SimpleAdminPlayerCommandBase
{
    public override string Command => "cleareffects";
    public override string[] Alias => new string[] { "ceffect" };
    public override string CommandDescription => "Clears the effects of the player";
    public override PlayerMode Target => PlayerMode.SelfTarget;
    public override bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response)
    {
        player.statusEffectManager.DisableEffects();
        response = "Done.";
        return true;
    }
}
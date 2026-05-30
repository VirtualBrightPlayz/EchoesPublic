#if false
public class IgnorePropWeight : SimpleAdminPlayerCommandBase
{
    public override string Command => "ignorepropweight";
    public override string[] Alias => new string[] { "steroids" };
    public override string CommandDescription => "Makes the player ignore the weight of props.";
    public override PlayerMode Target => PlayerMode.SelfTarget;
    public override bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response)
    {
        if (player.Controller is not FPController controller)
        {
            response = "You need to be using a first person role to use this command.";
            return false;
        }
        controller.IgnorePropWeight = !controller.IgnorePropWeight;
        response = "Okay, you {?} ignore prop weight.";
        response = response.Replace("{?}", controller.IgnorePropWeight ? "WILL" : "WILL NOT");
        return true;
    }
}
#endif
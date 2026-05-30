#if false
public class DeleteProp : SimpleAdminPlayerCommandBase
{
    public override string Command => "deleteprops";
    public override string[] Alias => new string[0];
    public override string CommandDescription => "Delete props by grabbing them";
    public override PlayerMode Target => PlayerMode.SelfTarget;
    public override bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response)
    {
        if (player.Controller is not FPController controller)
        {
            response = "You need to be using a first person role to use this command.";
            return false;
        }
        controller.DeleteItemsOnGrab = !controller.DeleteItemsOnGrab;
        response = "Okay, you {?} delete props.";
        response = response.Replace("{?}", controller.DeleteItemsOnGrab ? "WILL" : "WILL NOT");
        return true;
    }
}
#endif
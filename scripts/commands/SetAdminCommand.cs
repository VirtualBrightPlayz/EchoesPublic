public class SetAdminCommand : SimpleAdminPlayerCommandBase
{
    public override string Command { get; } = "SetAdmin";

    public override string[] Alias { get; } = new string[] { "admin", "adm" };

    public override string CommandDescription => "Gives or removes a player's admin permissions";

    public override PlayerMode Target { get; } = PlayerMode.Required;

    public override bool Execute(PlayerConsole console, NetworkPlayer targetPlayer, string[] args, out string response)
    {
        if (targetPlayer.console.GetMultiplayerAuthority() == 1)
        {
            response = "The host must always be an admin.";
            return false;
        }

        if (args.Length > 0 && bool.TryParse(args[0], out bool state))
            targetPlayer.console.IsAdmin = state;
        else
            targetPlayer.console.IsAdmin = !targetPlayer.console.IsAdmin;
        if (targetPlayer.console.IsAdmin)
            response = $"{targetPlayer.username} is now an admin.";
        else
            response = $"{targetPlayer.username} is no longer an admin.";
        return true;
    }
}
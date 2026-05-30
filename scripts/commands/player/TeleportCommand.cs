using System;
using Godot;

public class TeleportCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "Teleport";

    public override string[] Alias { get; } = ["tp", "tpto", "teleportto"];

    public override string CommandDescription => "Teleports to a player.";

    public override string ButtonLabel => "Goto";
    
    public override Type Category => typeof(AdminCategoryPlayer);
    
    public override string Container => nameof(AdminCategoryPlayer.Container.Teleporting);

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        if (console.Player == null)
        {
            response = "You must be a player to run this command!";
            return false;
        }
        
        if (args.Length == 0 || !CommandHelper.TryParsePlayer(args[0], out NetworkPlayer player))
        {
            response = "No suitable player.";
            return false;
        }

        console.Player?.ForceTeleport(player.PlayerPosition, player.PlayerRotation);

        response = $"Teleported to player ({player.username})";
        return true;
    }
}
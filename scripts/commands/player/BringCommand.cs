using System;
using System.Collections.Generic;
using System.Linq;

public class BringCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "Bring";

    public override string[] Alias { get; } = ["tphere", "teleporthere"];

    public override string CommandDescription { get; } = "Brings a player to you.";

    public override string ButtonLabel => "Bring";

    public override Type Category => typeof(AdminCategoryPlayer);

    public override string Container => nameof(AdminCategoryPlayer.Container.Teleporting);
    

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        if (console.Player == null)
        {
            response = "You must be a player to run this command!";
            return false;
        }
        
        if (args.Length == 0)
        {
            response = "You need to pass a player ID or Name.";
            return false;
        }
        
        HashSet<NetworkPlayer> players = CommandHelper.ParsePlayersArgument(args[0]);

        if (players.Count == 0)
        {
            response = "No suitable players.";
            return false;
        }

        foreach (NetworkPlayer player in players)
        {
            player?.ForceTeleport(console.Player.PlayerPosition, console.Player.PlayerRotation);
        }

        response = $"Brought player(s) {string.Join(", ", players.Select(p => p.username))}";
        return true;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class KillCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "Kill";

    public override string[] Alias { get; } = [];
    
    public override string CommandDescription { get; } = "Kills a specified player.";

    public override string ButtonLabel => "Kill";
    
    public override Type Category => typeof(AdminCategoryPlayer);
    
    public override string Container => nameof(AdminCategoryPlayer.Container.Stats);

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        HashSet<NetworkPlayer> players = args.Length > 0  ? CommandHelper.ParsePlayersArgument(args[0]) : [console.Player];

        if (players.Count == 0)
        {
            response = "No suitable players.";
            return false;
        }
        
        foreach (NetworkPlayer player in players)
        {
            player?.Kill(new DamageInfo());
        }
        
        response = $"Killed player(s) {string.Join(", ", players.Select(p => p.username))}";
        return true;
    }
}
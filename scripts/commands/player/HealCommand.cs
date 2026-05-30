using System;
using System.Collections.Generic;
using System.Linq;

public class HealCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "Heal";

    public override string[] Alias { get; } = [];

    public override string CommandDescription { get; } = "Heals a specified player to full HP.";
    
    public override string ButtonLabel => "Heal";
    
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
            player.Health = player.MaxHealth;
        }
        
        response = $"Healed player(s) {string.Join(", ", players.Select(p => p.username))}";
        return true;
    }
}
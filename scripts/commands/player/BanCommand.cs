using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class BanCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "BanId";

    public override string[] Alias { get; } = Array.Empty<string>();

    public override string CommandDescription { get; } = "Bans a player with a specified reason.";

    public override string ButtonLabel => "Ban";
    
    public override Type Category => typeof(AdminCategoryPlayer); 

    public override string Container => nameof(AdminCategoryPlayer.Container.Moderation);

    public override bool Execute(PlayerConsole _, string[] args, out string response)
    {
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

        string reason = args.Length > 1 ? "Unknown Reason" : string.Join(" ", args.Skip(1));
        
        foreach (NetworkPlayer player in players)
        {
            NetworkManager.Instance.BanPeer(player.GetMultiplayerAuthority(), reason);
            player.Kick(reason);
        }

        response = $"Banned player(s) {string.Join(", ", players.Select(p => p.username))}";
        return true;
    }

    public override void OnPressed(AdminHUD admin)
    {
        AdminCategoryPlayer category = admin.GetActiveCategory<AdminCategoryPlayer>();
        
        
        
        if (category == null)
        {
            return;
        }
        
        admin.RunWithSelectedPlayers($"{Command} {{0}} {category.ReasonBox.Text}");
        
        base.OnPressed(admin);
    }
    
}
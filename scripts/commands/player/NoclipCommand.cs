using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class NoclipCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "Noclip";

    public override string[] Alias { get; } = ["nc"];

    public override string CommandDescription => "Toggles noclip";

    public override string ButtonLabel => "Toggle Noclip";
    
    public override Type Category => typeof(AdminCategoryPlayer);
    
    public override string Container => nameof(AdminCategoryPlayer.Container.Movement);

    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        HashSet<NetworkPlayer> players = args.Length > 0  ? CommandHelper.ParsePlayersArgument(args[0]) : [console.Player];

        if (players.Count == 0)
        {
            response = "No suitable players.";
            return false;
        }
        
        HashSet<NetworkPlayer> enabledNoclip = [];
        HashSet<NetworkPlayer> disabledNoclip = [];

        foreach (NetworkPlayer player in players)
        {
            player.CanNoclip = !player.CanNoclip;
            if (player.CanNoclip)
            {
                enabledNoclip.Add(player);
            }
            else
            {
                disabledNoclip.Add(player);
            }
        }

        response = string.Empty;
        
        if (enabledNoclip.Count > 0)
        {
            response = $"Noclip enabled for {string.Join(", ", enabledNoclip.Select(p => p.username))}";
        }
        
        if (disabledNoclip.Count > 0)
        {
            response += $"\nNoclip disabled for {string.Join(", ", disabledNoclip.Select(p => p.username))}";
        }
        
        return true;
    }
}
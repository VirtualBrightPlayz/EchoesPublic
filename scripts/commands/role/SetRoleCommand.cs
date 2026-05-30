using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static System.Boolean;

public class SetRoleCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "SetRole";

    public override string[] Alias { get; } = new string[] { "ForceClass", "fc" };

    public override string CommandDescription { get; } = "Sets the role of the specified player";

    public override string ButtonLabel => "Set Role";

    public override Type Category => typeof(AdminCategoryRoles);

    public override string Container => nameof(AdminCategoryRoles.Container.Commands);

    public override bool Execute(PlayerConsole console, string[] args, out string response) // setrole [players] [role] [shouldUseSpawn]
    {
        if (args.Length == 0)
        {
            response = $"No role id specified\nValid roles:\n{string.Join(", ", IInitScript.Instance.Data.RoleLookup.Keys)}";
            return false;
        }

        HashSet<NetworkPlayer> players = CommandHelper.ParsePlayersArgument(args[0]);
        PlayerRole role = null;
        bool shouldUseSpawn = false;

        if (IInitScript.Instance.Data.RoleLookup.TryGetValue(args[0], out role) && args.Length <= 2)
        {
            players = [console.Player];
            if (args.Length > 1)
                TryParse(args[1], out shouldUseSpawn);
        }

        if (players.Count == 0)
        {
            response = "No suitable players.";
            return false;
        }

        if (role == null && !IInitScript.Instance.Data.RoleLookup.TryGetValue(args[1], out role))
        {
            response = "Argument is not a valid Role";
            return false;
        }
        else
        {
            if (args.Length > 2)
                TryParse(args[2], out shouldUseSpawn);
        }
        
        foreach (NetworkPlayer player in players)
        {
            Vector3? pos = shouldUseSpawn ? null : player.PlayerPosition;
            player.SV_Spawn(role.ResourceName, pos);
        }

        response = $"Role {role.RichDisplayName} applied to player(s) {string.Join(", ", players.Select(p => p.username))}";
        return true;
    }

    public override void OnPressed(AdminHUD admin)
    {
        AdminCategoryRoles category = admin.GetActiveCategory<AdminCategoryRoles>();

        if (category == null)
        {
            return;
        }

        int[] selectedItem = category.RoleList.GetSelectedItems();

        if (selectedItem.Length == 0)
        {
            admin.RunWithSelectedPlayers($"{Command} {{0}}");
        }

        string role = category.PlayerRoles[selectedItem[0]].ResourceName;

        admin.RunWithSelectedPlayers($"{Command} {{0}} {role} {category.UseSpawnpoint.ButtonPressed}");
    }
}
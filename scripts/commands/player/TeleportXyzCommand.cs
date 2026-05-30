using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class TeleportXyzCommand : AdminConsoleCommandButton
{
    public override string Command { get; } = "teleport_xyz";

    public override string[] Alias { get; } = new string[] { "tpxyz", "tpx", "teleportxyz" };

    public override string CommandDescription => "Teleports a player to an X Y Z coordinate.";
    
    public override string ButtonLabel => "Teleport to Cords";
    
    public override Type Category => typeof(AdminCategoryPlayer);
    
    public override string Container => nameof(AdminCategoryPlayer.Container.Teleporting);
    
    public override bool Execute(PlayerConsole console, string[] args, out string response)
    {
        HashSet<NetworkPlayer> players = args.Length > 0  ? CommandHelper.ParsePlayersArgument(args[0]) : [console.Player];
        
        if (args.Length < 3)
        {
            response = "Wrong syntax";
            return false; 
        }

        foreach (NetworkPlayer player in players)
        { 
            float? x = args[1] == "~" ? player.PlayerPosition.X : (float.TryParse(args[1], out float xcord) ? xcord : null);
            float? y = args[2] == "~" ? player.PlayerPosition.Y : (float.TryParse(args[2], out float ycord) ? ycord : null);
            float? z = args[3] == "~" ? player.PlayerPosition.Z : (float.TryParse(args[3], out float zcord) ? zcord : null);

            if (x == null || y == null || z == null)
            {
                response = "Unable to teleport player.";
                return false;
            }
            
            player.ForceTeleport(new Godot.Vector3(x.Value, y.Value, z.Value), player.PlayerRotation);
        }
        
        
        response = $"Teleported ({args[1]}, {args[2]}, {args[3]}) players {string.Join(", ", players.Select(p => p.username))}.";
        return true;
    }

    public override Control GenerateUi(AdminHUD admin)
    {
        HBoxContainer container = new HBoxContainer();

        LineEdit x = new LineEdit()
        {
            PlaceholderText = "X",
        };
        
        LineEdit y = new LineEdit()
        {
            PlaceholderText = "Y",
        };
        
        LineEdit z = new LineEdit()
        {
            PlaceholderText = "Z",
        };

        Button button = base.GenerateUi(admin) as Button;

        button!.Pressed += () =>
        {
            admin.RunWithSelectedPlayers($"{Command} {{0}} {x.Text} {y.Text} {z.Text}");
        };
        
        container.AddChild(x);
        container.AddChild(y);
        container.AddChild(z);
        container.AddChild(button);
        
        return container;
    }
}
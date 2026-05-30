using System;
using Godot;

public class SpawnNpcCommand : SimpleAdminPlayerCommandBase
{
    public override string Command { get; } = "SpawnNPC";
    public override string[] Alias { get; } = new string[] { "npc" };
    public override string CommandDescription => "Spawn an NPC at the specified player.";
    public override PlayerMode Target => PlayerMode.SelfTarget;

    public override bool Execute(PlayerConsole console, NetworkPlayer player, string[] args, out string response)
    {
        // PackedScene scn = GD.Load<PackedScene>("res://scenes/npcs/npc_173.tscn");
        PackedScene scn = GD.Load<PackedScene>("res://scenes/npcs/zombie_npc.tscn");
        // PackedScene scn = GD.Load<PackedScene>("res://scenes/npcs/forest_monster_npc.tscn");
        Node3D npc = scn.Instantiate<Node3D>();
        ItemManager.Instance.SpawnNode.AddChild(npc, true);
        npc.GlobalPosition = player.PlayerPosition;
        response = "Zombie Spawned on player.";
        return true;
    }

}
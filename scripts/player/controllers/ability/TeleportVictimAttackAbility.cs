using System;
using Godot;

[GlobalClass]
public partial class TeleportVictimAttackAbility : AttackAbility
{
    protected override void _DamagePlayer(IPlayerController victim)
    {
        base._DamagePlayer(victim);
        ZoneArea.Zone zone = ZoneArea.GetZone(victim.Player.PlayerPosition);
        var pdSpawn = (Node3D)GetTree().GetFirstNodeInGroup("106_pd");
        victim.Player.Rpc(NetworkPlayer.MethodName.Teleport, pdSpawn.GlobalPosition, pdSpawn.GlobalRotation);
        PDLogic.instance.OnEnter(victim.Player, zone);
    }
}
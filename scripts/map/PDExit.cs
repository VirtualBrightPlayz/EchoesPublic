using System;
using System.Linq;
using Godot;

public partial class PDExit : Area3D, IDamageSource
{
    [Export]
    public bool isExit = false;

    public string AttackerDisplayName => "Pocket Dimension";
    public NodePath AbsolutePath => GetPath();
    public DamageType TypeOfDamage => DamageType.Scp106;

    public override void _Ready()
    {
        BodyEntered += Enter;
    }

    public override void _ExitTree()
    {
        BodyEntered -= Enter;
    }

    private void Enter(Node3D body)
    {
        if (!IsMultiplayerAuthority())
            return;
        if (body is IPlayerController plr)
        {
            bool kill = !isExit;
            PDLogic.instance.OnExit(plr.Player, this, ref kill, out ZoneArea.Zone zone);
            if (kill || zone == ZoneArea.Zone.Unknown)
            {
                plr.Player.Kill(new DamageInfo(0f, this, DamageType.Scp106));
                var spawn = (PlayerSpawnpoint)GetTree().GetNodesInGroup("spawn").Where(x => x is PlayerSpawnpoint sp && sp.CanSpawn(plr.Player.Role.team)).FirstOrDefault();
                if (IsInstanceValid(spawn))
                {
                    plr.Player.ForceTeleport(spawn.GlobalPosition, spawn.GlobalRotation);
                }
                return;
            }
            var targets = GetTree().GetNodesInGroup("pd_exit").Where(x => x is Node3D n3d && ZoneArea.GetZone(n3d.GlobalPosition) == zone).ToArray();
            if (RoundManager.Instance.nukeDetonated || RoundManager.Instance.nukeActive || targets.Length == 0)
            {
                targets = GetTree().GetNodesInGroup("pd_exit_sz").ToArray();
            }
            var target = (Node3D)targets[(int)(GD.Randi() % targets.Length)];
            plr.Player.ForceTeleport(target.GlobalPosition, target.GlobalRotation);
        }
    }
}

using Godot;
using System;

[GlobalClass]
public partial class KillArea : Area3D, IDamageSource
{
    public string AttackerDisplayName => string.Empty;
    public NodePath AbsolutePath => GetPath();

    [Export]
    public DamageType TypeOfDamage { get; set; } = DamageType.Unknown;

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer())
            return;
        foreach (var body in GetOverlappingBodies())
        {
            IHealth health = IHealth.GetHealth(body);
            if (body is IPlayerController player && player.Player.Role.team != TeamID.Dead)
            {
                player.Player.Kill(new DamageInfo(default, this, TypeOfDamage));
            }
            else if (health != null)
            {
                health.Kill(new DamageInfo(default, this, TypeOfDamage));
            }
        }
    }
}

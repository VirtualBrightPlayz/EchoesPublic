using Godot;
using System;

[GlobalClass]
public partial class DamageArea : Area3D, IDamageSource
{
    [Export]
    public float amount = 10f;
    private float amountFinal;
    [Export]
    public bool everyFrame = false;
    [Export]
    public DamageType Type = DamageType.Unknown;

    public string AttackerDisplayName => Name;

    public NodePath AbsolutePath => GetPath();

    public void SV_SetEveryFrame(bool value)
    {
        everyFrame = value;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (everyFrame && IsVisibleInTree())
        {
            amountFinal = amount * (float)delta;
            Tick();
        }
        else
            amountFinal = amount;
    }

    public void Tick()
    {
        if (Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
            return;
        foreach (var body in GetOverlappingBodies())
        {
            IHealth health = IHealth.GetHealth(body);
            if (body is IPlayerController player && player.Player.Role.team != TeamID.Dead)
            {
                player.Player.Damage(new DamageInfo(amountFinal, this, Type));
            }
            else if (health != null)
            {
                health.Damage(new DamageInfo(amountFinal));
            }
        }
    }
}

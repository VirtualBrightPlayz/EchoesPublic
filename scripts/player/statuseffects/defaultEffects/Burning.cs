using System;
using Godot;

[GlobalClass]
public partial class Burning : TickingStatusEffect
{
    public override EffectType Type => EffectType.Burn;
    
    public override EffectClassification Classification => EffectClassification.Negative;

    protected override void ClientEnabled()
    {
        base.ClientEnabled();
        if (!IsInstanceValid(OwnerPlayer.FireEffect))
        {
            return;
        }
        OwnerPlayer.FireEffect.Enabled = true;
        OwnerPlayer.FireEffect.FireAmount = 1f;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!IsInstanceValid(OwnerPlayer.FireEffect))
        {
            return;
        }
        OwnerPlayer.FireEffect.FireAmount = (float)Mathf.Clamp(Duration, 0d, 1d);
    }

    protected override void ClientDisabled()
    {
        base.ClientDisabled();
        if (!IsInstanceValid(OwnerPlayer.FireEffect))
        {
            return;
        }
        OwnerPlayer.FireEffect.Enabled = false;
        OwnerPlayer.FireEffect.FireAmount = 0f;
    }

    protected override void ServerTick()
    {
        base.ServerTick();
        DamageInfo damageInfo = new DamageInfo(1f * Intensity, Source, DamageType.Fire);
        OwnerPlayer.Damage(damageInfo);
    }
}

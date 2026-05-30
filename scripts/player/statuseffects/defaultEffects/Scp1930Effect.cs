using System;
using Godot;

[GlobalClass]
public partial class Scp1930Effect : StatusEffectBase
{
    public override EffectType Type => EffectType.Scp1930;
    
    public override EffectClassification Classification => EffectClassification.Negative;

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!Active)
        {
            return;
        }
        float max = OwnerPlayer.MaxHealth;
        float targetHealth = max - max * Intensity;
        if (OwnerPlayer.Health > targetHealth)
        {
            DamageInfo damageInfo = new DamageInfo(OwnerPlayer.Health - targetHealth, Source, DamageType.Generic);
            OwnerPlayer.Damage(damageInfo);
        }
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Scp207Effect : TickingStatusEffect, IMovementSpeedModifier, IStaminaIncreaseModifier, IStaminaDecreaseModifier
{
    public override EffectType Type => EffectType.Scp207;

    public override EffectClassification Classification => EffectClassification.Mixed;

    public bool SpeedModifierActive => Active;

    public float SpeedModifier => Mathf.Max(1.5f * Intensity, 1.5f);

    public bool StaminaIncreaseModifierActive => Active;

    public float StaminaIncreaseModifier => 5f;

    public bool StaminaDecreaseModifierActive => Active;

    public float StaminaDecreaseModifier => 0.1f;

    protected override void ServerTick()
    {
        base.ServerTick();
        DamageInfo damage = new DamageInfo(2.5f, Source, DamageType.Scp207);
        OwnerPlayer.Damage(damage);
    }
}

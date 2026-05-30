using Godot;

[GlobalClass]
public partial class SlimeEffect : TickingStatusEffect, IGravityModifier, IMovementSpeedModifier, IJumpModifier
{
    public override EffectType Type => EffectType.SlimeEffect;
    public override EffectClassification Classification => EffectClassification.Negative;
    
    public bool GravityModifierActive => Active;

    public float GravityModifier => 0.025f * Intensity;
    
    public bool SpeedModifierActive => Active;

    public float SpeedModifier => 0.25f * Intensity;
    
    public bool JumpModifierActive => Active;
    
    public float JumpModifier => 0.25f * Intensity;

    public bool AllowJumping => true;
}
using Godot;

[GlobalClass]
public partial class BoneBreak : TickingStatusEffect, IMovementSpeedModifier
{
    public override EffectType Type => EffectType.BoneBreak;
    public override EffectClassification Classification => EffectClassification.Negative;
    public bool SpeedModifierActive => Active;
    public float SpeedModifier => 0.75f * (1/Intensity);
}
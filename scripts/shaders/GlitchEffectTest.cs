using Godot;
using System;

public partial class GlitchEffectTest : ColorRect
{
    public float GlitchEffectAmount
    {
        get => ((ShaderMaterial)Material).GetShaderParameter("amount").AsSingle();
        set => ((ShaderMaterial)Material).SetShaderParameter("amount", value);
    }
    [Export]
    public float speed = 1f;
    [Export]
    public float maxValue = 1f;

    public override void _Ready()
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsKeyPressed(Key.Space))
        {
            GlitchEffectAmount = maxValue;
        }
        GlitchEffectAmount = Mathf.Lerp(GlitchEffectAmount, 0f, (float)delta * speed);
    }
}

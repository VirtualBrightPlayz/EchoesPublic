using Godot;
using System;

[GlobalClass]
public partial class VelocityAudio : Node
{
    [Export]
    public RigidBody3D rb;
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public float maxVolume = 1f;
    [Export]
    public float linear = 0f;
    [Export]
    public float angular = 90f;
    [Export]
    public bool lerp = false;
    [Export]
    public float lerpSpeed = 10f;
    [Export]
    public bool pitch = false;

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(rb) && IsInstanceValid(audio))
        {
            float target = Mathf.Clamp(rb.LinearVelocity.Length() * linear + rb.AngularVelocity.Length() * Mathf.DegToRad(angular), 0f, maxVolume);
            if (pitch)
            {
                if (lerp)
                    audio.PitchScale = Mathf.Max(Mathf.Lerp(audio.PitchScale, target, (float)delta * lerpSpeed), 0.01f);
                else
                    audio.PitchScale = target;
            }
            else
            {
                if (lerp)
                    audio.VolumeLinear = Mathf.Lerp(audio.VolumeLinear, target, (float)delta * lerpSpeed);
                else
                    audio.VolumeLinear = target;
            }
        }
    }
}

using Godot;
using System;

[GlobalClass]
[Tool]
public partial class LightFlicker : Node
{
    [Export]
    public float MinEnergy = 0f;
    [Export]
    public float MaxEnergy = 1f;
    [Export]
    public float Frequency = 0f;

    public Light3D Light => GetParent<Light3D>();

    private float timer;

    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            Light.LightEnergy = MaxEnergy;
        }
    }

    public override void _ExitTree()
    {
        Light.LightEnergy = MaxEnergy;
    }

    public override void _Process(double delta)
    {
        if (IsInstanceValid(Light))
        {
            timer -= (float)delta;
            if (timer <= 0f || timer > Frequency)
            {
                timer = Frequency;
                Light.LightEnergy = (float)GD.RandRange(MinEnergy, MaxEnergy);
            }
        }
    }
}

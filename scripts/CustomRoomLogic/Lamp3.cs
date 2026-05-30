using Godot;
using System;

[Tool]
public partial class Lamp3 : Node3D
{
    [Export]
    public Light3D light;
    [Export]
    public MeshInstance3D glowMesh;
    [Export]
    public float multiplier = 1f;

    public override void _Process(double delta)
    {
        if (IsInstanceValid(glowMesh) && IsInstanceValid(light))
            glowMesh.SetInstanceShaderParameter("emission", new Color(light.LightColor, light.LightEnergy * multiplier));
    }
}

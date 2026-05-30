using Godot;
using System;

public partial class LookAtMainCamera : Node
{
    public override void _Process(double delta)
    {
        var cam = GetViewport().GetCamera3D();
        GetParent<Node3D>().LookAt(cam.GlobalPosition);
    }
}

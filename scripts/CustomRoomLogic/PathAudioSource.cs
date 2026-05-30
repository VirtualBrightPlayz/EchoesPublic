using Godot;
using System;

[GlobalClass]
public partial class PathAudioSource : Node3D
{
    [Export]
    public Path3D path;

    public override void _Process(double delta)
    {
        if (IsInstanceValid(path) && IsInstanceValid(NetworkPlayer.LocalInstance) && NetworkPlayer.LocalInstance.ActiveController != null)
        {
            Vector3 pos = NetworkPlayer.LocalInstance.ActiveController.Camera.GlobalPosition;
            pos = path.ToLocal(pos);
            pos = path.Curve.GetClosestPoint(pos);
            pos = path.ToGlobal(pos);
            GlobalPosition = pos;
        }
    }
}

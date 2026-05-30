using Godot;

public partial class FollowLocalPlayer : Node
{
    public Node3D Target => GetParent<Node3D>();

    public override void _Process(double delta)
    {
        if (IsInstanceValid(NetworkPlayer.LocalInstance))
            Target.GlobalPosition = NetworkPlayer.LocalInstance.ActiveController.Camera.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(NetworkPlayer.LocalInstance))
            Target.GlobalPosition = NetworkPlayer.LocalInstance.ActiveController.Camera.GlobalPosition;
    }
}
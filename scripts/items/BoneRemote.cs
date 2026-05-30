using Godot;

[Tool]
public partial class BoneRemote : Node3D
{
    [Export]
    public NodePath remote;
    [Export]
    public bool copyPosition;
    [Export]
    public bool copyRotation;
    [Export]
    public bool useScale;

    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            var other = GetNodeOrNull<Node3D>(remote);
            if (other == null)
                return;
            // other.Position = Vector3.Zero;
            // other.RotationDegrees = Vector3.Zero;
            // other.Scale = Vector3.One;
        }
    }

    public override void _Process(double delta)
    {
        var other = GetNodeOrNull<Node3D>(remote);
        if (other == null)
            return;
        if (copyPosition)
            other.GlobalPosition = GlobalPosition;
        if (copyRotation)
            other.GlobalBasis = new Basis(GlobalBasis.GetRotationQuaternion());
    }
}

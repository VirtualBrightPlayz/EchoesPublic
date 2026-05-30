using Godot;

[Tool]
public partial class FollowBone : Node3D
{
    [Export]
    public string bone;
    [Export]
    public Skeleton3D skeleton;
    [Export]
    public bool copyPosition;
    // [Export]
    // public Vector3 positionOffset = Vector3.Zero;
    [Export]
    public bool copyRotation;
    // [Export]
    // public Vector3 rotationOffset = Vector3.Zero;

    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            Position = Vector3.Zero;
            RotationDegrees = Vector3.Zero;
            Scale = Vector3.One;
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;
        int idx = skeleton.FindBone(bone);
        if (idx != -1)
        {
            var xform = skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(idx);
            if (copyPosition)
            {
                GlobalPosition = xform.Origin;
                // Position += positionOffset;
            }
            if (copyRotation)
            {
                GlobalBasis = xform.Basis;
                // RotationDegrees += rotationOffset;
            }
            Scale = Vector3.One;
        }
    }
}

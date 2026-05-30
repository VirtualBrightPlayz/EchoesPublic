using Godot;

[GlobalClass]
public partial class CameraShake : Node
{
    public static StringName MetaName = "cam_shake";

    [Export(PropertyHint.Range, "0,1")]
    public float shake;
    [Export]
    public float maxShake = 5f;
    [Export]
    public float speed = 1f;
    [Export]
    public Node3D camera;

    public override void _EnterTree()
    {
        if (!IsInstanceValid(camera))
            camera = GetParent<Node3D>();
        camera.SetMeta(MetaName, this);
    }

    public override void _ExitTree()
    {
        camera.RemoveMeta(MetaName);
    }

    public override void _Process(double delta)
    {
        return;
        float theta = Mathf.DegToRad((float)GD.RandRange(-maxShake, maxShake) * shake);
        float theta2 = Mathf.DegToRad((float)GD.RandRange(-maxShake, maxShake) * shake);
        camera.Quaternion = camera.Quaternion.Slerp(Basis.LookingAt(new Vector3(Mathf.Sin(theta2), Mathf.Sin(theta), -Mathf.Cos(theta))).GetRotationQuaternion(), (float)delta * speed).Normalized();
    }
}
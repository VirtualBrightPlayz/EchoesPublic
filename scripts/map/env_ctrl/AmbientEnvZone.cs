using Godot;
using System;
using System.Linq;

[GlobalClass]
[Tool]
public partial class AmbientEnvZone : CollisionShape3D
{
    public Camera3D MainCamera => GetViewport().GetCamera3D();

    public static StringName GroupName = "ambient_env";

    [Export]
    public AmbientEnv env;
    [Export]
    public AmbientProfile profile;
    [Export]
    public float edgeSize = 5f;
    [Export]
    public int priority = 0;

    public override void _EnterTree()
    {
        AddToGroup(GroupName);
        // AmbientEnvCtrl.zones.Add(this);
    }

    public override void _ExitTree()
    {
        RemoveFromGroup(GroupName);
        // AmbientEnvCtrl.zones.Remove(this);
    }

    public float GetAmount()
    {
        if (IsInstanceValid(Shape) && IsInstanceValid(MainCamera) && Visible && !Disabled)
        {
            if (Shape is BoxShape3D box)
            {
                Aabb ab = new Aabb(-box.Size / 2f, box.Size);
                Vector3 size = box.Size + Vector3.One * edgeSize;
                Aabb abEdge = new Aabb(-size / 2f, size);
                Vector3 localPoint = ToLocal(MainCamera.GlobalPosition);
                if (ab.HasPoint(localPoint))
                {
                    return 1f;
                }
                if (abEdge.HasPoint(localPoint))
                {
                    var planes = Geometry3D.BuildBoxPlanes(box.Size / 2f);
                    foreach (var pl in planes)
                    // var pl = planes.OrderByDescending(x => Mathf.Abs(x.DistanceTo(localPoint))).First();
                    {
                        var pt = (pl.DistanceTo(localPoint));
                        if (pt > 0f)
                        {
                            return 1f - (pt / edgeSize);
                        }
                    }
                    // GD.PrintErr("AHHHHH");
                    return 1f;
                }
            }
        }
        return 0f;
    }
}

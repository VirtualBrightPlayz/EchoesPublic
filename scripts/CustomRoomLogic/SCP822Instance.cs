using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class SCP822Instance : MultiMeshInstance3D, IDamageSource
{
    [Export]
    public int gridSize = 16;
    [Export]
    public float gridInterval = 1f;
    [Export]
    public float gridVerticalInterval = 1f;
    [Export]
    public float maxDistance = 10f;
    [Export]
    public Shape3D shape;
    [Export]
    public int growthArea = 8;
    [Export]
    public double growthSpeed = 1d;
    [Export(PropertyHint.Range, "0,1")]
    public double GrowthRatio
    {
        get => growth / Multimesh.InstanceCount;
        set
        {
            growth = value * Multimesh.InstanceCount;
            UpdateGrowth();
        }
    }
    [Export]
    public double GrowthTotal
    {
        get => growth;
        set
        {
            growth = Mathf.Clamp(value, 0, Multimesh.InstanceCount);
            UpdateGrowth();
        }
    }
    [Export]
    public Curve growthCurve;

    [Export]
    public Area3D area;
    [Export]
    public float damageDistance = 3f;

    [Export]
    public Array<Transform3D> transforms = new Array<Transform3D>();
    private List<Transform3D> listTransforms = new List<Transform3D>();
    private double growth = 0f;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint mask = 0xFFFFFFFF;

    [ExportToolButton("Populate")]
    public Callable PopulateTool => Callable.From(PopulateInstances);

    public string AttackerDisplayName => "SCP-822";

    public NodePath AbsolutePath => GetPath();

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;
        listTransforms.Clear();
        listTransforms.AddRange(transforms);
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
            return;
        GrowthTotal += delta * growthSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(area) && IsMultiplayerAuthority())
        {
            foreach (var obj in area.GetOverlappingBodies())
            {
                if (obj is IHealth hp)
                {
                    if (IsTouching(ToLocal(obj.GlobalPosition)))
                    {
                        hp.Damage(new DamageInfo((float)(100f * GrowthRatio), this, DamageType.Nuke));
                        GrowthTotal = 0d;
                        break;
                    }
                }
            }
        }
    }

    public bool IsTouching(Vector3 point)
    {
        for (int i = 0; i < listTransforms.Count; i++)
        {
            if (i + growthArea > growth)
                break;
            if (listTransforms[i].Origin.DistanceSquaredTo(point) < damageDistance * damageDistance)
            {
                return true;
            }
        }
        return false;
    }

    public void UpdateGrowth()
    {
        // Multimesh.VisibleInstanceCount = Mathf.FloorToInt(growth);
        SetInstanceShaderParameter("max_instances", Multimesh.InstanceCount);
        SetInstanceShaderParameter("min_instances", Mathf.FloorToInt(growth));
        SetInstanceShaderParameter("ratio", GrowthTotal);
        return;
        if (!IsInstanceValid(growthCurve))
            return;
        int end = Multimesh.VisibleInstanceCount;
        if (end == -1)
            end = Multimesh.InstanceCount;
        float offset = (float)(growth - Multimesh.VisibleInstanceCount) / growthArea;
        for (int i = 0; i < transforms.Count - growthArea; i++)
        {
            Multimesh.SetInstanceTransform(i, transforms[i]);
        }
        for (int i = 0; i < growthArea; i++)
        {
            float f = growthCurve.Sample((float)i / growthArea + offset);
            int index = end - i - 1;
            if (index < 0)
                continue;
            Multimesh.SetInstanceTransform(index, transforms[index].ScaledLocal(Vector3.One * f));
        }
    }

    public void PopulateInstances()
    {
        List<Transform3D> list = new List<Transform3D>();
        PopulateRing(list, gridSize, 1f);
        PopulateRing(list, gridSize, 0.5f);
        list.Sort((x, y) => Mathf.Sign(x.Origin.LengthSquared() - y.Origin.LengthSquared()));
        for (int i = 0; i < gridSize; i++)
        {
            // PopulateRing(list, i);
        }
        Multimesh.InstanceCount = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            Multimesh.SetInstanceTransform(i, list[i]);
        }
        transforms.Clear();
        transforms.AddRange(list);
        UpdateGrowth();
        // Multimesh.InstanceCount = gridSize;// * gridSize;
        // Multimesh.CustomAabb = new Aabb(Vector3.Zero, Vector3.One * gridInterval * gridSize);
        return;
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                var offset = new Vector3(x, 0f, z) * gridInterval;
                offset -= new Vector3(gridSize, 0f, gridSize) * gridInterval / 2f;
                var pos = GetFloor(ToGlobal(offset), Vector3.One, Vector3.Down);
                pos = GlobalTransform.AffineInverse() * pos;
                Multimesh.SetInstanceTransform(x + z * gridSize, pos);
            }
        }
    }

    // https://stackoverflow.com/questions/969798/plotting-a-point-on-the-edge-of-a-sphere#969880
    public void PopulateRing(List<Transform3D> list, int ring, float scl)
    {
        int len = Mathf.CeilToInt(Mathf.Tau / gridInterval * ring);
        for (int i = 0; i < len; i++)
        {
            float s = (float)i / len * Mathf.Tau;
            for (int k = 0; k < len; k++)
            {
                float t = (float)k / len * Mathf.Tau;
                float x = Mathf.Cos(s) * Mathf.Sin(t);
                float z = Mathf.Sin(s) * Mathf.Sin(t);
                float y = Mathf.Cos(t);
                var offset = new Vector3(x * gridInterval * ring, y * gridVerticalInterval * ring, z * gridInterval * ring);
                var gOffset = ToGlobal(offset);
                {
                    var pos = GetFloor(GlobalPosition, Scale * scl, GlobalPosition.DirectionTo(gOffset));
                    if (!pos.Origin.IsEqualApprox(GlobalPosition))
                    {
                        pos = GlobalTransform.AffineInverse() * pos;
                        if (list.FindIndex(x => x.Origin.DistanceTo(pos.Origin) < gridInterval * scl) == -1)
                        {
                            list.Add(pos);
                        }
                    }
                }
            }
        }
    }

    public Transform3D GetFloor(Vector3 pos, Vector3 size, Vector3 dir)
    {
        Vector3[] dirs = new Vector3[] { /*Vector3.Down,*/ /*GlobalPosition.DirectionTo(pos)*/ dir };
        // Vector3[] dirs = new Vector3[] { Vector3.Down, Vector3.Down, Vector3.Forward, Vector3.Back, Vector3.Left, Vector3.Right };
        Vector3 final = pos;
        Vector3 finalNormal = Vector3.Up;
        bool hasNormal = false;
        for (int i = 0; i < dirs.Length; i++)
        {
            var args = PhysicsRayQueryParameters3D.Create(final, final + dirs[i] * maxDistance, mask);
            args.HitBackFaces = false;
            args.HitFromInside = false;
            var results = GetWorld3D().DirectSpaceState.IntersectRay(args);
            {
                if (results.Count > 0)
                {
                    var n = results["normal"].AsVector3().Normalized();
                    // if (n.Dot(dir) > 0f)
                    //     n = -n;
                    final = results["position"].AsVector3();
                    // final = final.Lerp(results["position"].AsVector3(), 0.5f);
                    // final /= 2f;
                    if (!hasNormal)
                        finalNormal = n;
                    hasNormal = true;
                    if (i == 0)
                        break;
                    // return new Transform3D(Basis.LookingAt(n, up).Rotated(cr, Mathf.DegToRad(-90f)).Scaled(size), results["position"].AsVector3());
                }
            }
            // break;
        }
        var args2 = new PhysicsShapeQueryParameters3D()
        {
            Transform = new Transform3D(Basis.Identity, final),
            Shape = shape,
            Margin = shape.Margin,
            CollisionMask = mask,
        };
        var results2 = GetWorld3D().DirectSpaceState.GetRestInfo(args2);
        if (results2.Count > 0)
        {
            // finalNormal = results2["normal"].AsVector3().Normalized();
            final = results2["point"].AsVector3();
        }
        else
        {
            return new Transform3D(Basis.Identity, pos);
        }
        var up = Vector3.Up;
        if (Mathf.IsEqualApprox(Mathf.Abs(finalNormal.Dot(up)), 1f))
        {
            up = Vector3.Forward;
        }
        var cr = finalNormal.Cross(up).Normalized();
        return new Transform3D(Basis.LookingAt(finalNormal, up).Rotated(cr, Mathf.DegToRad(-90f)).Scaled(size), final);
    }
}
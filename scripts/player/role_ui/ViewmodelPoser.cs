using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class ViewmodelPoser : SkeletonModifier3D
{
    public Skeleton3D skeleton => GetSkeleton();
    [Export]
    public string boneName;
    [Export]
    public string excludeName;
    [Export]
    public Vector3 axis = new Vector3(1f, 0f, 0f);
    [Export]
    public Vector3 rayAxis = new Vector3(0f, 1f, 0f);
    [Export]
    public bool PoseNow
    {
        get => false;
        set => PoseAll();
    }
    [Export(PropertyHint.Layers3DPhysics)]
    public uint layers;
    [Export]
    public int MinPoseDeg = -45;
    [Export]
    public int MaxPoseDeg = 45;
    [Export]
    public bool poseRoot = false;
    [Export]
    public float radius = 0.1f;

    public Dictionary<int, Quaternion> rotations = new Dictionary<int, Quaternion>();

    public override void _Ready()
    {
        PoseAll();
    }

    public override void _ProcessModification()
    {
        if (!IsVisibleInTree())
            return;
        PoseAll();
        foreach (var rot in rotations)
        {
            skeleton.SetBonePoseRotation(rot.Key, rot.Value);
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsString() == nameof(boneName))
        {
            var skel = GetSkeleton();
            if (IsInstanceValid(skel))
            {
                property["hint"] = (long)PropertyHint.Enum;
                property["hint_string"] = skel.GetConcatenatedBoneNames();
            }
        }
    }

    public void PoseAll()
    {
        rotations.Clear();
        if (!IsInstanceValid(skeleton))
            return;
        int boneIdx = skeleton.FindBone(boneName);
        if (boneIdx == -1)
            return;
        if (poseRoot)
        {
            PoseBoneChildren(boneIdx);
        }
        else
        {
            int[] childs = skeleton.GetBoneChildren(boneIdx);
            for (int i = 0; i < childs.Length; i++)
            {
                PoseBoneChildren(childs[i]);
            }
        }
    }

    public void PoseBoneChildren(int boneIdx)
    {
        if (!string.IsNullOrWhiteSpace(excludeName) && skeleton.GetBoneName(boneIdx).Contains(excludeName))
            return;
        int[] childs = skeleton.GetBoneChildren(boneIdx);
        for (int j = 0; j < childs.Length; j++)
        {
            PoseBone(childs[j]);
            PoseBoneChildren(childs[j]);
        }
    }

    public void PoseBone(int boneIdx)
    {
        int parentBoneIdx = skeleton.GetBoneParent(boneIdx);
        skeleton.ResetBonePose(parentBoneIdx);
        skeleton.ForceUpdateBoneChildTransform(parentBoneIdx);
        Quaternion rotStart = skeleton.GetBonePoseRotation(parentBoneIdx);
        for (int i = MinPoseDeg; i < MaxPoseDeg; i++)
        {
            break;
            Quaternion rot = skeleton.GetBonePoseRotation(parentBoneIdx);
            skeleton.SetBonePoseRotation(parentBoneIdx, rotStart * Quaternion.FromEuler(axis * Mathf.DegToRad(i)));
            skeleton.ForceUpdateBoneChildTransform(parentBoneIdx);
            // CollideItem(boneIdx);
            continue;
            // if (IsHittingItems(boneIdx))
            {
                // skeleton.SetBonePoseRotation(parentBoneIdx, rot);
                // break;
            }
        }
        // skeleton.SetBonePoseRotation(parentBoneIdx, rotStart);
        skeleton.SetBonePoseRotation(parentBoneIdx, rotStart * Quaternion.FromEuler(axis * Mathf.DegToRad(MaxPoseDeg)));
        rotations[parentBoneIdx] = skeleton.GetBonePoseRotation(parentBoneIdx);
        skeleton.ResetBonePose(parentBoneIdx);
    }

    public bool IsHittingItems(int boneIdx)
    {
        return IsHittingItem(boneIdx) || skeleton.GetBoneChildren(boneIdx).Any(IsHittingItems);
    }

    public bool IsHittingItem(int boneIdx)
    {
        return IsHittingItem(skeleton.ToGlobal(skeleton.GetBoneGlobalPose(boneIdx).Origin));
    }

    public void CollideItem(int boneIdx)
    {
        int parentBoneIdx = skeleton.GetBoneParent(boneIdx);
        Vector3 v = skeleton.GetBoneGlobalPose(boneIdx).Origin;
        Vector3 v2 = skeleton.ToLocal(CollideItem(skeleton.ToGlobal(v)));
        Transform3D xform = skeleton.GetBoneGlobalPose(parentBoneIdx);
        xform.Basis *= Basis.LookingAt((v - v2).Normalized());
        skeleton.SetBoneGlobalPose(boneIdx, xform);
    }

    public Vector3 CollideItem(Vector3 pos)
    {
        PhysicsShapeQueryParameters3D args = new PhysicsShapeQueryParameters3D()
        {
            CollideWithAreas = false,
            CollideWithBodies = true,
            CollisionMask = layers,
            Shape = new SphereShape3D()
            {
                Radius = radius,
            },
            Transform = new Transform3D(Basis.Identity, pos),
        };
        var results = GetWorld3D().DirectSpaceState.IntersectShape(args);
        foreach (var res in results)
        {
            return res["position"].AsVector3();
        }
        // if (results.TryGetValue("position", out var item))
        {
            // return item.AsVector3();
        }
        return pos;
    }

    public bool IsHittingItem(Vector3 pos)
    {
        PhysicsPointQueryParameters3D args = new PhysicsPointQueryParameters3D()
        {
            CollideWithAreas = false,
            CollideWithBodies = true,
            CollisionMask = layers,
            Position = pos,
            Exclude = new Array<Rid>(),
        };
        var results = GetWorld3D().DirectSpaceState.IntersectPoint(args);
        return results.Count > 0;
    }
}

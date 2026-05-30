using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

[Tool]
[GlobalClass]
public partial class HitboxMaker : Node3D
{
    public string[] HeadBones = new string[]
    {
        "Head",
        "Neck",
    };

    public string[] LimbBones = new string[]
    {
        "LeftUpperArm",
        "RightUpperArm",
        "LeftUpperLeg",
        "RightUpperLeg",
        "LeftLowerArm",
        "RightLowerArm",
        "LeftLowerLeg",
        "RightLowerLeg",
        "LeftFoot",
        "RightFoot",
        "LeftHand",
        "RightHand",
    };

    public string[] HipsBones = new string[]
    {
        "Hips",
        "Spine",
        "Chest",
        "UpperChest",
        "LeftShoulder",
        "RightShoulder",
    };

    [Export]
    public Skeleton3D skeleton;
    [Export]
    public Node3D headNode;
    [Export]
    public Node playerController;
    [Export(PropertyHint.Range, "0,1,or_greater")]
    public float collisionSize = 0.2f;
    [Export(PropertyHint.Range, "0,1,or_greater")]
    public float headSize = 0.2f;
    [Export(PropertyHint.Range, "0,2,or_greater")]
    public float headDamageMulti = 1f;
    [Export(PropertyHint.Range, "0,2,or_greater")]
    public float limbDamageMulti = 1f;
    [Export(PropertyHint.Range, "0,2,or_greater")]
    public float bodyDamageMulti = 1f;
    [Export(PropertyHint.Layers3DPhysics)]
    public uint layer;

    [ExportToolButton("Make Hitboxes")]
    public Callable MakeHitboxCall => Callable.From(MakeHitboxes);

    public Skeleton3D GetSkeleton() => skeleton;

    public void MakeHitboxes()
    {
        Skeleton3D skel = GetSkeleton();
        foreach (var ch in skel.GetChildren().ToArray())
        {
            if (ch.HasMeta("hitbox"))
                ch.QueueFreeNow();
        }
        foreach (var ch in GetChildren().ToArray())
        {
            // if (ch.HasMeta("hitbox"))
                ch.QueueFreeNow();
        }
        List<int> used = new List<int>();
        for (int i = 0; i < skel.GetBoneCount(); i++)
        {
            int parent = skel.GetBoneParent(i);
            if (parent != -1)
            {
                SetupForBone(parent, used);
            }
            else
            {
                // SetupForBone(i);
            }
        }
    }

    private void SetupForBone(int i, List<int> used)
    {
        Skeleton3D skel = GetSkeleton();
        if (used.Contains(i))
            return;
        if (Array.IndexOf(HeadBones, skel.GetBoneName(i)) != -1)
            SetupPhysbone(i, headDamageMulti, true);
        else if (Array.IndexOf(LimbBones, skel.GetBoneName(i)) != -1)
            SetupPhysbone(i, limbDamageMulti);
        else if (Array.IndexOf(HipsBones, skel.GetBoneName(i)) != -1)
            SetupPhysbone(i, bodyDamageMulti);
        else
            return;
        used.Add(i);
    }

    public BoneAttachment3D SetupPhysbone(int idx, float multi = 1f, bool head = false)
    {
        Skeleton3D skel = GetSkeleton();
        // int chId = skel.GetBoneParent(idx);
        int[] ch = skel.GetBoneChildren(idx);
        foreach (var chId in ch)
        // if (chId != -1)
        {
            Transform3D childRest = skel.GetBonePose(chId);
            if (head && IsInstanceValid(headNode))
            {
                // shape.Radius = headRadius;
                childRest = skel.GlobalTransform.AffineInverse() * skel.GetBoneGlobalRest(idx).AffineInverse() * headNode.GlobalTransform;
            }
            float halfHeight = childRest.Origin.Length() * 0.5f;

            BoneAttachment3D newBone = new BoneAttachment3D();
            
            newBone.Name = skel.GetBoneName(idx);
            skel.AddChild(newBone, true);
            newBone.Owner = Owner;
            newBone.SetMeta("hitbox", true);

            {
                // newBone.Set("use_external_skeleton", true);
                // newBone.Set("external_skeleton", newBone.GetPathTo(skel));
                newBone.BoneName = skel.GetBoneName(idx);
                newBone.SetMeta("bone_idx", idx);
                newBone.SetMeta("child_bone_idx", chId);
            }

            StaticBody3D body = new StaticBody3D();
            body.Name = "Body";
            body.CollisionLayer = layer;
            body.CollisionMask = layer;
            Node health = new Node();
            health.Name = "Hitbox";
            string path = typeof(PlayerHitbox).GetCustomAttribute<ScriptPathAttribute>().Path;
            health.SetScript(GD.Load(path));
            body.AddChild(health, true);
            newBone.AddChild(body, true);
            body.Owner = Owner;
            health.Owner = Owner;
            health.Set(PlayerHitbox.PropertyName.HealthNode, health.GetPathTo(playerController));
            health.Set(PlayerHitbox.PropertyName.DamageMultiplier, multi);

            CapsuleShape3D shape = new CapsuleShape3D();
            shape.Height = halfHeight * 2f;
            shape.Radius = collisionSize;
            if (head)
            {
                shape.Height = headSize * 2f;
                shape.Radius = headSize;
            }

            CollisionShape3D shape3D = new CollisionShape3D();
            shape3D.Name = "Shape";
            body.AddChild(shape3D, true);
            shape3D.Owner = Owner;
            shape3D.Shape = shape;
            shape3D.Transform = new Transform3D(new Basis(
                1f, 0f, 0f,
                0f, 0f, 1f,
                0f, -1f, 0f
            ), Vector3.Zero);

            Vector3 up = Vector3.Up;
            if (up.Cross(childRest.Origin).IsZeroApprox())
            {
                up = Vector3.Back;
            }

            Transform3D bodyTransform = Transform3D.Identity;
            bodyTransform.Basis = Basis.LookingAt(childRest.Origin, up);
            bodyTransform.Origin = bodyTransform.Basis * new Vector3(0f, 0f, -halfHeight);

            Transform3D jointTransform = Transform3D.Identity;
            jointTransform.Origin = new Vector3(0f, 0f, halfHeight);

            // newBone.BodyOffset = bodyTransform;
            // newBone.JointOffset = jointTransform;
            body.Transform = bodyTransform;

            // newBone.Visible = false;

            return newBone;
        }
        return null;
    }

    public void SetupNonePhysbone(int idx, float radius = 0.1f, float multi = 1f)
    {
        Skeleton3D skel = GetSkeleton();
        int chId = skel.GetBoneParent(idx);
        // if (chId != -1)
        {
            BoneAttachment3D newBone = new BoneAttachment3D();
            newBone.Name = skel.GetBoneName(idx);
            skel.AddChild(newBone, true);
            newBone.Owner = Owner;
            newBone.SetMeta("hitbox", true);
            // newBone.SetUseExternalSkeleton(true);
            // newBone.SetExternalSkeleton(newBone.GetPathTo(skel));
            // newBone.Set("use_external_skeleton", true);
            // newBone.Set("external_skeleton", newBone.GetPathTo(skel));
            newBone.BoneIdx = chId == -1 ? idx : chId;

            CapsuleShape3D shape = new CapsuleShape3D();
            // BoxShape3D shape = new BoxShape3D();

            StaticBody3D animBody3D = new StaticBody3D();
            animBody3D.Name = "Body";
            newBone.AddChild(animBody3D, true);
            animBody3D.Set(PlayerHitbox.PropertyName.HealthNode, animBody3D.GetPathTo(playerController));
            animBody3D.Set(PlayerHitbox.PropertyName.DamageMultiplier, multi);
            // animBody3D.HealthNode = animBody3D.GetPathTo(playerController);
            animBody3D.Owner = Owner;
            animBody3D.CollisionLayer = layer;
            animBody3D.CollisionMask = layer;

            CollisionShape3D shape3D = new CollisionShape3D();
            shape3D.Name = "Shape";
            animBody3D.AddChild(shape3D, true);
            shape3D.Owner = Owner;
            shape3D.Shape = shape;

            if (chId != -1)
            {
                Transform3D fromXform = skel.GlobalTransform * skel.GetBoneGlobalPose(idx);
                Transform3D toXform = skel.GlobalTransform * skel.GetBoneGlobalPose(chId);
                Vector3 from = skel.ToGlobal(skel.GetBoneGlobalPose(idx).Origin);
                Vector3 to = skel.ToGlobal(skel.GetBoneGlobalPose(chId).Origin);
                Vector3 localFrom = skel.GetBoneGlobalPose(idx).Origin;
                Vector3 localTo = skel.GetBoneGlobalPose(chId).Origin;

                shape.Height = from.DistanceTo(to) * 1.1f;
                shape.Radius = radius;
                // shape.Size = new Vector3(radius, from.DistanceTo(to), radius);

                Vector3 up = (localFrom - localTo).Normalized();
                shape3D.Basis = toXform.AffineInverse().Basis * new Basis(new Quaternion(up, Vector3.Up));
                shape3D.Position = (shape3D.Quaternion * Vector3.Up) * localFrom.DistanceTo(localTo) / 2f;
            }
            else
            {
                shape.Height = radius;
                shape.Radius = radius;
                // shape.Size = new Vector3(radius, radius, radius);
            }
            // EditorInterface.Singleton.MarkSceneAsUnsaved();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

[Tool]
[GlobalClass]
public partial class RagdollMaker : PhysicalBoneSimulator3D
{
    [Export(PropertyHint.Range, "0,1,or_greater")]
    public float collisionSize = 0.2f;
    [Export]
    public float dampLinear = 0f;
    [Export]
    public float dampAngular = 0f;
    [Export]
    public int solverIterations = 10;
    [Export]
    public bool selfCollide = false;
    [Export]
    public float mass = 1f;

    [ExportToolButton("Make Ragdoll")]
    public Callable MakeRagdollCall => Callable.From(MakeRagdoll);

    public static string[] HumanBones = new string[]
    {
        "Head",
        "Hips",
        "Chest",
        "UpperChest",
        "LeftFoot",
        "RightFoot",
        "LeftHand",
        "RightHand",        
        "LeftUpperArm",
        "RightUpperArm",
        "LeftUpperLeg",
        "RightUpperLeg",
        "LeftLowerArm",
        "RightLowerArm",
        "LeftLowerLeg",
        "RightLowerLeg",
    };
    
    public string[] PinBones = new string[]
    {
        "Chest",
        "UpperChest",
        // "Head",
        // "LeftFoot",
        // "RightFoot",
        // "LeftHand",
        // "RightHand",
        // "Neck",
    };

    public string[] ConeBones = new string[]
    {
        "LeftUpperArm",
        "RightUpperArm",
        "LeftUpperLeg",
        "RightUpperLeg",
    };

    public string[] HingeBones = new string[]
    {
        "LeftLowerArm",
        "RightLowerArm",
        "LeftLowerLeg",
        "RightLowerLeg",
    };

    public void MakeRagdoll()
    {
        foreach (var ch in GetChildren().ToArray())
        {
            // if (ch is PhysicalBone3D)
                ch.Free();
        }
        Skeleton3D skel = GetSkeleton();
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
        foreach (var ch in GetChildren().ToArray())
        {
            if (ch is PhysicalBone3D bone)
            {
                string mainBoneScript = typeof(BoneSync).GetCustomAttribute<Godot.ScriptPathAttribute>()?.Path;
                string proxyBoneScript = typeof(BoneProxySync).GetCustomAttribute<Godot.ScriptPathAttribute>()?.Path;
                if (IsInstanceValid(bone.GetScript().AsGodotObject()))
                {
                    if (bone.GetScript().As<Script>().ResourcePath == mainBoneScript)
                    {
                        bone.Set(nameof(BoneSync.sync), bone.FindChild("Sync"));
                    }
                    else if (bone.GetScript().As<Script>().ResourcePath == proxyBoneScript)
                    {
                        NodePath pathToMain = bone.GetPathTo(FindChild("Hips"));
                        bone.Set(nameof(BoneProxySync.MainBone), FindChild("Hips"));
                        bone.Set(nameof(BoneProxySync.sync), bone.FindChild("Sync"));
                    }
                }
                string name = bone.Get("bone_name").AsString();
                if (Array.IndexOf(PinBones, name) != -1)
                {
                    SetupConePhysbone(bone, 2f, 20f);
                    // SetupPinPhysbone(bone);
                }
                else if (Array.IndexOf(ConeBones, name) != -1)
                {
                    SetupConePhysbone(bone, 10f, 5f);
                    // SetupPinPhysbone(bone);
                }
                else if (Array.IndexOf(HingeBones, name) != -1)
                {
                    SetupConePhysbone(bone, 5f, 2f);
                    // SetupHingePhysbone(bone, -30f, 30f);
                    // SetupPinPhysbone(bone);
                }
            }
        }
        #if TOOLS
        EditorInterface.Singleton.MarkSceneAsUnsaved();
        #endif
    }

    private void SetupForBone(int i, List<int> used)
    {
        Skeleton3D skel = GetSkeleton();
        if (used.Contains(i))
            return;
        if (skel.GetBoneName(i) == "Hips")
            SetupPhysbone(i);
        else if (Array.IndexOf(PinBones, skel.GetBoneName(i)) != -1)
            SetupPhysbone(i);
            // SetupPinPhysbone(i);
        else if (Array.IndexOf(ConeBones, skel.GetBoneName(i)) != -1)
            SetupPhysbone(i);
            // SetupConePhysbone(i, 10f, 5f);
        else if (Array.IndexOf(HingeBones, skel.GetBoneName(i)) != -1)
            SetupPhysbone(i);
            // SetupHingePhysbone(i, -30f, 0f);
        else
            return;
        used.Add(i);
    }

    public void MakeHitboxes()
    {
        foreach (var ch in GetChildren().ToArray())
        {
            // if (ch is PhysicalBone3D)
                ch.Free();
        }
        Skeleton3D skel = GetSkeleton();
        for (int i = 0; i < skel.GetBoneCount(); i++)
        {
            if (skel.GetBoneName(i) == "Hips")
                SetupNonePhysbone(i);
            else if (Array.IndexOf(PinBones, skel.GetBoneName(i)) != -1)
                SetupNonePhysbone(i);
            else if (Array.IndexOf(ConeBones, skel.GetBoneName(i)) != -1)
                SetupNonePhysbone(i);
            else if (Array.IndexOf(HingeBones, skel.GetBoneName(i)) != -1)
                SetupNonePhysbone(i);
        }
    }

    public void SetupNonePhysbone(int idx)
    {
        Skeleton3D skel = GetSkeleton();
        int chId = skel.GetBoneParent(idx);
        if (chId != -1)
        {
            BoneAttachment3D newBone = new BoneAttachment3D();
            newBone.Name = skel.GetBoneName(idx);
            AddChild(newBone, true);
            newBone.Owner = Owner;
            newBone.BoneIdx = idx;

            Vector3 from = skel.ToGlobal(skel.GetBoneGlobalPose(idx).Origin);
            Vector3 to = skel.ToGlobal(skel.GetBoneGlobalPose(chId).Origin);

            CapsuleShape3D shape = new CapsuleShape3D();
            shape.Height = from.DistanceTo(to);
            shape.Radius = collisionSize;

            CollisionShape3D shape3D = new CollisionShape3D();
            shape3D.Name = "Shape";
            newBone.AddChild(shape3D, true);
            shape3D.Owner = Owner;
            shape3D.Shape = shape;
            shape3D.GlobalPosition = from.Lerp(to, 0.5f);
            shape3D.Quaternion *= new Quaternion(newBone.GlobalBasis.Y.Normalized(), (to - from).Normalized());

            newBone.Visible = false;
        }
    }

    public PhysicalBone3D SetupPhysbone(int idx)
    {
        Skeleton3D skel = GetSkeleton();
        // int chId = skel.GetBoneParent(idx);
        int[] ch = skel.GetBoneChildren(idx);
        foreach (var chId in ch)
        // if (chId != -1)
        {
            Transform3D childRest = skel.GetBonePose(chId);
            Transform3D rest = skel.GetBoneGlobalRest(idx);
            float halfHeight = childRest.Origin.Length() * 0.5f;

            PhysicalBone3D newBone = new PhysicalBone3D();

            if (skel.GetBoneName(idx) == "Hips")
            {
                string path = typeof(BoneSync).GetCustomAttribute<Godot.ScriptPathAttribute>().Path;
                // newBone.SetScript(GD.Load(path));
            }
            else
            {
                string path = typeof(BoneProxySync).GetCustomAttribute<Godot.ScriptPathAttribute>().Path;
                // newBone.SetScript(GD.Load(path));
            }
            
            newBone.Name = skel.GetBoneName(idx);
            AddChild(newBone, true);
            newBone.Owner = Owner;
            newBone.Mass = mass;
            newBone.LinearDamp = dampLinear;
            newBone.AngularDamp = dampAngular;
            // if (chId == -1)
            // {
            //     halfHeight = collisionSize * 0.5f;
            //     newBone.Set("bone_name", skel.GetBoneName(idx));
            // }
            // else
            {
                newBone.Set("bone_name", skel.GetBoneName(idx));
                newBone.SetMeta("bone_idx", idx);
                newBone.SetMeta("child_bone_idx", chId);
            }
            newBone.JointType = PhysicalBone3D.JointTypeEnum.None;

            CapsuleShape3D shape = new CapsuleShape3D();
            shape.Height = halfHeight * 2f;
            shape.Radius = collisionSize;

            CollisionShape3D shape3D = new CollisionShape3D();
            shape3D.Name = "Shape";
            newBone.AddChild(shape3D, true);
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
            if (!childRest.Origin.IsZeroApprox())
            {
                bodyTransform.Basis = Basis.LookingAt(childRest.Origin, up);
            }
            bodyTransform.Origin = bodyTransform.Basis * new Vector3(0f, 0f, -halfHeight);

            Transform3D jointTransform = Transform3D.Identity;
            jointTransform.Origin = new Vector3(0f, 0f, halfHeight);
            // jointTransform.Basis = rest.Basis;
            // jointTransform = skel.GlobalTransform * rest;

            newBone.BodyOffset = bodyTransform;
            // newBone.JointOffset = newBone.GlobalTransform.AffineInverse() * jointTransform;
            newBone.JointOffset = jointTransform;
            
            // newBone.Visible = false;

            Node sync = new Node();//ClassDB.Instantiate("TransformSyncNative").As<Node>();
            sync.Name = "Sync";
            newBone.AddChild(sync, true);
            sync.Owner = Owner;
            sync.SetScript(GD.Load(typeof(TransformSync).GetCustomAttribute<ScriptPathAttribute>().Path));
            sync.Set(nameof(TransformSync.targetNode), sync.GetPathTo(newBone));
            return newBone;
        }
        return null;
    }

    public Node FindParentBone(int child)
    {
        Skeleton3D skel = GetSkeleton();
        int parent = skel.GetBoneParent(child);
        Node chNode = GetNodeOrNull(skel.GetBoneName(parent));
        if (IsInstanceValid(chNode))
        {
            return chNode;
        }
        else if (parent != -1)
        {
            return FindParentBone(parent);
        }
        else
        {
            return null;
        }
    }

    public void SetupPinPhysbone(PhysicalBone3D bone)
    {
        if (IsInstanceValid(bone))
        {
            int idx = bone.GetMeta("bone_idx").AsInt32();
            Node chNode = FindParentBone(idx);
            if (IsInstanceValid(chNode))
            {
                Node3D joint = ClassDB.Instantiate("PinJoint").As<Node3D>();
                bone.AddChild(joint, true);
                joint.Owner = Owner;
                joint.Set("exclude_nodes_from_collision", !selfCollide);
                joint.Transform = bone.JointOffset;
                joint.Set("node_b", joint.GetPathTo(bone));
                joint.Set("node_a", joint.GetPathTo(chNode));
                // joint.Set("solver_velocity_iterations", solverIterations);
                // joint.Set("solver_position_iterations", solverIterations);
            }
        }
    }

    public void SetupConePhysbone(PhysicalBone3D bone, float swing_span = 25f, float twist_span = 20f)
    {
        // SetupPinPhysbone(idx);
        // return;
        Skeleton3D skel = GetSkeleton();
        // PhysicalBone3D bone = SetupPhysbone(idx);
        if (IsInstanceValid(bone))
        {
            int idx = bone.GetMeta("bone_idx").AsInt32();
            Node chNode = FindParentBone(idx);
            if (IsInstanceValid(chNode))
            {
                Node3D joint = ClassDB.Instantiate("ConeTwistJoint3D").As<Node3D>();
                bone.AddChild(joint, true);
                joint.Owner = Owner;
                joint.Set("exclude_nodes_from_collision", !selfCollide);
                joint.Transform = bone.JointOffset;
                // joint.RotateY(Mathf.DegToRad(90f));
                joint.Set("node_b", joint.GetPathTo(bone));
                joint.Set("node_a", joint.GetPathTo(chNode));
                // joint.Set("solver_velocity_iterations", solverIterations);
                // joint.Set("solver_position_iterations", solverIterations);

                // joint.Set("swing_limit_enabled", true);
                joint.Set("swing_span", Mathf.DegToRad(swing_span));
                // joint.Set("twist_limit_enabled", true);
                joint.Set("twist_span", Mathf.DegToRad(twist_span));
            }
            // joint.Set("node_a", joint.GetPathTo(bone));
            // bone.JointType = PhysicalBone3D.JointTypeEnum.Cone;
            // bone.Set("joint_constraints/swing_span", swing_span);
            // bone.Set("joint_constraints/twist_span", twist_span);
        }
    }

    public void SetupHingePhysbone(PhysicalBone3D bone, float lower, float upper)
    {
        if (IsInstanceValid(bone))
        {
            int idx = bone.GetMeta("bone_idx").AsInt32();
            Node chNode = FindParentBone(idx);
            if (IsInstanceValid(chNode))
            {
                Node3D joint = ClassDB.Instantiate("HingeJoint3D").As<Node3D>();
                bone.AddChild(joint, true);
                joint.Owner = Owner;
                joint.Set("exclude_nodes_from_collision", !selfCollide);
                joint.Transform = bone.JointOffset;
                joint.RotateX(Mathf.DegToRad(-90f));
                joint.RotateY(Mathf.DegToRad(90f));
                joint.GlobalTransform = GetSkeleton().GlobalTransform * GetSkeleton().GetBoneGlobalRest(idx);
                // joint.RotateObjectLocal(new Vector3(0f, 0f, 1f), Mathf.DegToRad(-90f));
                // joint.RotateY(Mathf.DegToRad(-90f));
                joint.Set("node_b", joint.GetPathTo(bone));
                joint.Set("node_a", joint.GetPathTo(chNode));
                // joint.Set("solver_velocity_iterations", solverIterations);
                // joint.Set("solver_position_iterations", solverIterations);

                joint.Set("angular_limit/enable", true);
                joint.Set("angular_limit/lower", Mathf.DegToRad(lower));
                joint.Set("angular_limit/upper", Mathf.DegToRad(upper));
            }

            // bone.JointType = PhysicalBone3D.JointTypeEnum.Hinge;
            // bone.Set("joint_constraints/angular_limit_enabled", true);
            // bone.Set("joint_constraints/angular_limit_upper", upper);
            // bone.Set("joint_constraints/angular_limit_lower", lower);
        }
    }
}
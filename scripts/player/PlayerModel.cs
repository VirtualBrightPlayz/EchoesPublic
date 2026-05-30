using DitzelGames.FastIK;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Tomlyn.Model;

[GlobalClass]
public partial class PlayerModel : Node3D
{
    public enum BoneName : int
    {
        Other = -1,
        Left = 0,
        Right = 1,
        Head = 2,
        LeftFoot = 3,
        RightFoot = 4,
        Hips = 5,
    }

    public class IKData
    {
        public Node3D target;
        public Node3D pole;
        public FastIKFabric2 ik;
        public Basis offset;
    }

    public BasePlayer Player;

    [Export]
    public Node3D[] disableLocal = [];
    [Export]
    public LookAtModifier3D[] lookForwards = [];
    [Export]
    public PlayerAnims anims;
    [Export]
    public Skeleton3D skeleton;
    [Export]
    public Godot.Collections.Dictionary<BoneName, string> boneNames = new Godot.Collections.Dictionary<BoneName, string>();
    [Export]
    public Godot.Collections.Dictionary<BoneName, int> boneChainLengths = new Godot.Collections.Dictionary<BoneName, int>();
    [Export]
    public Godot.Collections.Dictionary<BoneName, Quaternion> boneOffsets = new Godot.Collections.Dictionary<BoneName, Quaternion>();
    [Export]
    public Node3D rightHandBone;
    [Export]
    public string hipsName;
    [Export]
    public float headAngle = 0f;
    [Export]
    public Vector3 headAxis = Vector3.Forward;
    [Export]
    public Vector3 headAxisFwd = Vector3.Forward;
    [Export]
    public Vector3 headAxisUp = Vector3.Up;
    [Export]
    public Vector3 handAxis = Vector3.Up;
    [Export]
    public Vector3 handPalmAxis = Vector3.Forward;
    [Export]
    public float handRotation = 0f;
    [Export]
    public SkeletonModifier3D ikSys;
    [Export]
    public Vector3 headOffset = Vector3.Zero;
    [Export]
    public Vector3 hipsOffset = Vector3.Zero;
    [Export]
    public Quaternion hipsRotation = Quaternion.Identity;
    [Export]
    public float ikWalkSpeed = 1f;
    [Export]
    public IterateIK3D iterateIk;
    [Export]
    public BoneConstraint3D copyIk;
    [Export]
    public BoneTwistDisperser3D twistIk;
    [Export]
    public Godot.Collections.Dictionary<BoneName, Node3D> ikNodes = new Godot.Collections.Dictionary<BoneName, Node3D>();
    [Export]
    public float ikRadius = 1f;
    [Export]
    public Godot.Collections.Dictionary<BoneName, ShapeCast3D> ikCasts = new Godot.Collections.Dictionary<BoneName, ShapeCast3D>();
    [Export]
    public Godot.Collections.Dictionary<BoneName, float> ikCastLengths = new Godot.Collections.Dictionary<BoneName, float>();

    public Dictionary<BoneName, IKData> ikData = new Dictionary<BoneName, IKData>();

    public Node3D fwd;

    public float walkCycle;
    [Export]
    public Curve walkCycleCurve;
    [Export]
    public float walkCycleMaxDistance = 1f;
    [Export]
    public float walkCycleMaxAngle = 1f;
    [Export]
    public float lerpSpeed = 10f;

    public Dictionary<BoneName, Vector3> ikCastPositions = new Dictionary<BoneName, Vector3>();
    public Dictionary<BoneName, Basis> ikCastRotations = new Dictionary<BoneName, Basis>();
    public Dictionary<BoneName, float> ikCastWalkCycles = new Dictionary<BoneName, float>();

    [Export]
    public Vector3 velocity = Vector3.Zero;

    public static Node CreateFromToml(ModModelInfo info, Node node)
    {
        if (info.Type == "Player")
        {
            PlayerModel mdl = new PlayerModel();
            mdl.Name = info.ModelId;
            mdl.AddChild(node, true);
            mdl.disableLocal = [(Node3D)node];
            mdl.skeleton = node.FindChildren("*", owned: false).OfType<Skeleton3D>().FirstOrDefault();
            foreach (var physBody in node.FindChildren("*", owned: false).OfType<PhysicsBody3D>())
            {
                physBody.CollisionLayer = 16;
                physBody.CollisionMask = 16;
                PlayerHitbox hitbox = new PlayerHitbox();
                physBody.AddChild(hitbox, true);
                hitbox.HealthNode = hitbox.GetPathTo(mdl);
            }
            AnimationPlayer animPlayer = node.FindChildren("*", owned: false).OfType<AnimationPlayer>().FirstOrDefault();
            if (IsInstanceValid(animPlayer) && IsInstanceValid(mdl.skeleton))
            {
                PlayerAnims anims = new PlayerAnims();
                mdl.AddChild(anims, true);
                anims.AnimPlayer = anims.GetPathTo(animPlayer);
                mdl.anims = anims;
                MultiplayerSynchronizer sync = new MultiplayerSynchronizer();
                sync.ReplicationInterval = 0.3f;
                sync.ReplicationConfig = new SceneReplicationConfig();
                mdl.AddChild(sync, true);
                if (TomlExtensions.TryGetValue(info.dataCfg, "anim_idle", out string idleLoop) && TomlExtensions.TryGetValue(info.dataCfg, "anim_walk", out string walkLoop))
                {
                    PlayerAnimTool tool = new PlayerAnimTool();
                    anims.AddChild(tool, true);
                    tool.animTree = anims;
                    tool.skeleton = mdl.skeleton;
                    tool.CreateSimple(idleLoop, walkLoop);
                    tool.QueueFree();
                    anims.walkSpeedPath = "parameters/MainBlend/blend_amount";
                    NodePath prop = mdl.GetPathTo(anims) + ":" + PlayerAnims.PropertyName.walking;
                    sync.ReplicationConfig.AddProperty(prop);
                    sync.ReplicationConfig.PropertySetReplicationMode(prop, SceneReplicationConfig.ReplicationMode.Always);
                    sync.ReplicationConfig.PropertySetSpawn(prop, true);
                }
            }
            return mdl;
        }
        return null;
    }

    public override void _Ready()
    {
        fwd = new Node3D();
        AddChild(fwd);
        fwd.GlobalPosition = Player.RoleController.View.GlobalPosition - Player.RoleController.View.GlobalBasis.Z.Normalized();
        foreach (var item in lookForwards)
        {
            item.TargetNode = item.GetPathTo(fwd);
            item.OriginExternalNode = item.GetPathTo(Player.RoleController.View);
        }
        if (IsMultiplayerAuthority())
        {
            foreach (var item in disableLocal)
            {
                foreach (var inst in item.FindChildren("*", nameof(VisualInstance3D), owned: false))
                {
                    if (inst is VisualInstance3D vis)
                    {
                        vis.Layers = Player.modelLocalCullFlags;
                    }
                }
            }
        }
        foreach (var kvp in ikData)
        {
            kvp.Value.target?.QueueFree();
            kvp.Value.pole?.QueueFree();
            kvp.Value.ik?.QueueFree();
        }
        ikData.Clear();
        if (IsInstanceValid(ikSys))
            ikSys.Active = false;
        foreach (var kvp in boneNames)
        {
            if (!boneChainLengths.TryGetValue(kvp.Key, out int chain))
                chain = GetChainCount(kvp.Value, hipsName);
            AddIkChain(kvp.Value, chain, kvp.Key);
        }
    }

    public void SetWorldModelVisible(bool visible)
    {
        if (visible)
        {
            Visible = true;
        }
        else
        {
            Visible = false;
        }
    }

    public Aabb? GetAabb()
    {
        Aabb? aabb = null;
        if (disableLocal != null)
        {
            foreach (var item in disableLocal)
            {
                foreach (MeshInstance3D node in item.FindChildren("*", nameof(MeshInstance3D), owned: false))
                {
                    if (aabb.HasValue)
                        aabb = aabb.Value.Merge(node.GlobalTransform * node.GetAabb());
                    else
                        aabb = node.GlobalTransform * node.GetAabb();
                }
            }
        }
        return aabb;
    }

    public Transform3D GetGlobalRest(BoneName bone)
    {
        if (bone == BoneName.Hips && skeleton.FindBone(hipsName) != -1)
        {
            return (skeleton.GlobalTransform * skeleton.GetBoneGlobalRest(skeleton.FindBone(hipsName)));
        }
        if (boneNames.TryGetValue(bone, out var boneName) && skeleton.FindBone(boneName) != -1)
        {
            return (skeleton.GlobalTransform * skeleton.GetBoneGlobalRest(skeleton.FindBone(boneName)));
        }
        return Transform3D.Identity;
    }

    public Transform3D GetGlobalPose(BoneName bone)
    {
        if (bone == BoneName.Hips && skeleton.FindBone(hipsName) != -1)
        {
            return (skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(skeleton.FindBone(hipsName)));
        }
        if (boneNames.TryGetValue(bone, out var boneName) && skeleton.FindBone(boneName) != -1)
        {
            return (skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(skeleton.FindBone(boneName)));
        }
        return Transform3D.Identity;
    }

    public Quaternion GetOffsetPose(BoneName bone)
    {
        if (boneOffsets.TryGetValue(bone, out var pose))
        {
            return pose;
        }
        return Quaternion.Identity;
    }

    public int GetChainCount(string boneName, string rootBoneName)
    {
        int cur = skeleton.FindBone(boneName);
        int target = skeleton.FindBone(rootBoneName);
        int count = 0;
        while (skeleton.GetBoneParent(cur) != -1 && cur != target)
        {
            cur = skeleton.GetBoneParent(cur);
            count++;
        }
        return count;
    }

    public Node3D AddIkChain(string boneName, int length, BoneName id)
    {
        if (!IsInstanceValid(ikSys))
            return null;
        if (id == BoneName.LeftFoot || id == BoneName.RightFoot)
        {
            // return null;
        }

        // GD.PrintS(GetPath(), id, length);
        Transform3D boneRestPose = skeleton.GetBoneGlobalRest(skeleton.FindBone(boneName));
        Node3D target = ClassDB.Instantiate("GodotIKEffector").As<Node3D>();
        target.Name = $"IkTarget_{boneName}";
        target.Set("bone_idx", skeleton.FindBone(boneName));
        target.Set("chain_length", length);
        if (id == BoneName.LeftFoot || id == BoneName.RightFoot)
            target.Set("transform_mode", 1);
        else
            target.Set("transform_mode", 3);
        ikSys.AddChild(target);
        /*
        Node3D target = new Node3D();
        target.Name = $"IkTarget_{boneName}";
        skeleton.AddChild(target);

        FastIKFabric2 ik = new FastIKFabric2();
        ik.Name = $"Ik_{boneName}";
        ik.Active = false;
        ik.ChainLength = length;
        ik.TargetBone = boneName;
        ik.Target = target;
        skeleton.AddChild(ik);
        ik.RequestReady();
        */

        Basis pose = boneRestPose.Basis;
        ikData.Add(id, new IKData()
        {
            target = target,
            pole = null,
            ik = null,
            offset = pose,
        });

        return target;
    }

    public Transform3D GetTargetPose(BoneName id)
    {
        if (ikData.ContainsKey(id))
            return ikData[id].target.GlobalTransform;
        return Transform3D.Identity;
    }

    public void SetPositionRotationForIk(BoneName index, double delta, Vector3 pos, Vector3 rot, bool useOffset = true)
    {
        if (IsInstanceValid(ikSys))
            ikSys.Active = true;
        if (index == BoneName.Hips)
        {
            skeleton.LerpNodePosition(pos, 10f * (float)delta);
            // skeleton.GlobalPosition = pos;
            // skeleton.GlobalRotation = rot;
            return;
        }
        if (ikData.TryGetValue(index, out var target))
        {
            Basis fwdHand = Basis.FromEuler(rot);
            target.target.LerpNodePosition(pos, 10f * (float)delta);
            Basis offset = Basis.Identity;
            if (boneOffsets.ContainsKey(index))
                offset = new Basis(boneOffsets[index]).Inverse();
            Basis b = fwdHand * offset * new Basis(hipsRotation) * target.offset;
            // b = b.Scaled(Vector3.One / b.Scale);
            if (useOffset)
                target.target.LerpNodeRotation(b, 10f * (float)delta);
            else
                target.target.LerpNodeRotation(fwdHand, 10f * (float)delta);
                // target.target.GlobalBasis = fwdHand;
            target.target.Scale = Vector3.One;

            if (index == BoneName.Head)
            {
                // return;
                Vector3 hipPos = pos - headOffset + hipsOffset;
                skeleton.LerpNodePosition(hipPos, 10f * (float)delta);
                // skeleton.GlobalPosition = hipPos;
            }
        }
    }

    public override void _Process(double delta)
    {
        UpdateModel(delta);
        if (IsMultiplayerAuthority())
        {
            HandleAnimations(delta);
        }
        if (IsInstanceValid(skeleton) && IsInstanceValid(anims))
        {
            Vector3 pos = anims.GetRootMotionPosition();
            Quaternion rot = anims.GetRootMotionRotation();
            Vector3 scl = anims.GetRootMotionScale();
            // pos.X = 0f;
            // pos.Z = 0f;
            // pos.Y = 0f;
            // var root = anims.GetNode<Node3D>(anims.RootNode);
            // root.Position -= (root.Quaternion * pos);
            // root.Quaternion = root.Quaternion * rot;
            // anims.GetNode(anims.RootNode);
        }
        if (IsInstanceValid(skeleton) && boneNames.ContainsKey(BoneName.Head))
        {
            int head = skeleton.FindBone(boneNames[BoneName.Head]);
            if (head != -1 && (!ikData.ContainsKey(BoneName.Head) || !IsInstanceValid(ikSys) || !ikSys.Active))
            {
                var pose = skeleton.GetBoneGlobalPoseNoOverride(head);
                pose = pose.RotatedLocal(headAxis.Normalized(), headAngle);
                skeleton.SetBoneGlobalPoseOverride(head, pose, 1f);
            }
        }
    }

    public static BoneName GetOpposingBone(BoneName name)
    {
        switch (name)
        {
            default:
                return BoneName.Other;
            case BoneName.Left:
                return BoneName.Right;
            case BoneName.Right:
                return BoneName.Left;
            case BoneName.LeftFoot:
                return BoneName.RightFoot;
            case BoneName.RightFoot:
                return BoneName.LeftFoot;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateModelPhysics(delta);
        if (IsMultiplayerAuthority() && Player.RoleController is FPController fp && IsInstanceValid(anims))
        {
            float targetWalk;
            if (fp.MovementDirection.IsZeroApprox() || !Player.RoleController.IsControllerActive)
            {
                targetWalk = 0f;
            }
            else
            {
                targetWalk = fp.Velocity.Length() / fp.EffectiveSpeed;
            }
            anims.walkingDirection = fp.MovementDirection * Mathf.Clamp(targetWalk, 0f, 1f);
            anims.walking = targetWalk;
        }
        if (IsMultiplayerAuthority() && Player.ActiveController is FPController ctrl1)
        {
            velocity = ctrl1.ToLocal(ctrl1.GlobalPosition + ctrl1.Velocity);
        }
        if (IsInstanceValid(iterateIk))
        {
            // Vector3 velocity = ctrl.ToLocal(ctrl.GlobalPosition + ctrl.GetLastMotion());
            // iterateIk.Influence = Mathf.Clamp(velocity.Length() / ctrl.EffectiveSpeed, 0f, 1f);
            if (velocity.IsZeroApprox())
            {
                foreach (var kvp in ikNodes)
                {
                    kvp.Value.Quaternion = Quaternion.Identity;
                }
            }
            else
            {
                walkCycle = (walkCycle + velocity.Length() * (float)delta * ikWalkSpeed) % 1f;
                walkCycle = 0f;

                Vector3 direction = velocity.Normalized();

                float angle = Mathf.Atan2(direction.X, -direction.Z);
                // GD.PrintErr(angle);
                foreach (var kvp in ikNodes)
                {
                    if (IsInstanceValid(walkCycleCurve))
                    {
                        kvp.Value.Rotation = new Vector3(Mathf.DegToRad(walkCycleCurve.Sample(walkCycle)), 0f, angle);
                    }
                    else
                    {
                        kvp.Value.Rotation = new Vector3(walkCycle * Mathf.Tau, 0f, angle);
                    }
                }
            }
            foreach (var kvp in ikCastWalkCycles)
            {
                ikCastWalkCycles[kvp.Key] = kvp.Value - (float)delta * velocity.Length();
            }
            foreach (var kvp in ikCasts)
            {
                float length = ikCastLengths.GetValueOrDefault(kvp.Key, ikRadius);
                Node3D otherTarget = ikNodes[kvp.Key].GetChildOrNull<Node3D>(0);
                kvp.Value.GlobalBasis = Basis.Identity;
                kvp.Value.TargetPosition = kvp.Value.ToLocal(otherTarget.GlobalPosition - GlobalBasis.Y.Normalized() * ikRadius);
                kvp.Value.ForceShapecastUpdate();
                Node3D target = kvp.Value.GetChildOrNull<Node3D>(0);
                if (IsInstanceValid(target))
                {
                    if (kvp.Value.IsColliding())
                    {
                        int index = 0;
                        Vector3 globalTarget = kvp.Value.ToGlobal(kvp.Value.TargetPosition);
                        for (int i = 0 ; i < kvp.Value.GetCollisionCount(); i++)
                        {
                            if (kvp.Value.GetCollisionPoint(i).DistanceTo(globalTarget) < kvp.Value.GetCollisionPoint(index).DistanceTo(globalTarget))
                            {
                                index = i;
                            }
                        }
                        float ratio = kvp.Value.GetClosestCollisionUnsafeFraction();
                        Vector3 pos = kvp.Value.GlobalPosition.Lerp(kvp.Value.ToGlobal(kvp.Value.TargetPosition), ratio);
                        Basis basis = Basis.LookingAt(-GlobalBasis.Z.Normalized().Slide(kvp.Value.GetCollisionNormal(index)), kvp.Value.GetCollisionNormal(index)) * otherTarget.Basis;
                        bool timeRunOut = (!ikCastWalkCycles.TryGetValue(kvp.Key, out float value) || value <= 0f);
                        bool timeRunOutOther = (!ikCastWalkCycles.TryGetValue(GetOpposingBone(kvp.Key), out float value2) || value2 <= 0f);
                        bool angleTooBig = (ikCastRotations.TryGetValue(kvp.Key, out var b) && b.GetRotationQuaternion().AngleTo(basis.GetRotationQuaternion()) > Mathf.DegToRad(walkCycleMaxAngle));

                        if (((ikCastPositions.ContainsKey(kvp.Key) && ikCastPositions[kvp.Key].DistanceTo(pos) > walkCycleMaxDistance) && timeRunOutOther) || (angleTooBig && velocity.IsZeroApprox()))
                        {
                            ikCastPositions.Remove(kvp.Key);
                            ikCastRotations.Remove(kvp.Key);
                            ikCastWalkCycles[kvp.Key] = ikWalkSpeed;
                            // if (ikCastWalkCycles.TryGetValue(kvp.Key, out float value))
                            //     walkCycle = (value + 0.5f) % 1f;
                        }

                        bool added = ikCastPositions.TryAdd(kvp.Key, pos);
                        if (added || !ikCastRotations.ContainsKey(kvp.Key))
                        {
                            ikCastRotations[kvp.Key] = basis;
                        }
                        if (added || !ikCastWalkCycles.ContainsKey(kvp.Key))
                        {
                            // ikCastWalkCycles[kvp.Key] = walkCycle;
                        }
                        // target.GlobalPosition = kvp.Value.GetCollisionPoint(index) + kvp.Value.GetCollisionNormal(index) * length;
                        // target.GlobalPosition = pos;
                        target.GlobalPosition = target.GlobalPosition.Lerp(ikCastPositions[kvp.Key], (float)delta * lerpSpeed);
                        // target.GlobalPosition = ikCastPositions[kvp.Key];
                        // target.GlobalBasis = GlobalBasis * otherTarget.Basis;
                        target.GlobalBasis = target.GlobalBasis.Slerp(ikCastRotations[kvp.Key].Orthonormalized(), (float)delta * lerpSpeed).Orthonormalized();
                    }
                    else// if (false)
                    {
                        bool removed = ikCastPositions.Remove(kvp.Key);
                        target.GlobalPosition = target.GlobalPosition.Lerp(kvp.Value.ToGlobal(kvp.Value.TargetPosition), (float)delta * lerpSpeed);
                        target.GlobalBasis = target.GlobalBasis.Slerp((GlobalBasis * otherTarget.Basis).Orthonormalized(), (float)delta * lerpSpeed).Orthonormalized();
                    }
                }
            }
        }
    }

    public virtual void UpdatePosition(double delta)
    {
        if (Player.ActiveController != null)
        {
            if (!Player.TryGetAbility(out StatueAbility _))
            {
                GlobalPosition = Player.ActiveController.Floor.GlobalPosition;
                GlobalRotation = Player.ActiveController.Floor.GlobalRotation;
            }
            fwd.GlobalPosition = Player.ActiveController.View.GlobalPosition - Player.ActiveController.View.GlobalBasis.Z.Normalized();
        }
        // TODO: maybe look into not doing this every frame?
        foreach (var item in lookForwards)
        {
            item.OriginExternalNode = item.GetPathTo(Player.ActiveController.View);
        }
    }

    public virtual void UpdateModel(double delta)
    {
        UpdatePosition(delta);
        {
            headAngle = Player.ActiveController.View.Rotation.X;
            if (Player.IsVR)
            {
                for (int i = 0; i < Player.HandSyncTargets.Length; i++)
                {
                    SetPositionRotationForIk((PlayerModel.BoneName)i, delta, Player.HandSyncTargets[i].GlobalPosition, Player.HandSyncTargets[i].GlobalRotation);
                }
            }
#if false
            else
            {
                Transform3D left = GetGlobalPose(PlayerModel.BoneName.Left);
                Transform3D right = GetGlobalPose(PlayerModel.BoneName.Right);
                // Transform3D head = model.GetGlobalPose(PlayerModel.BoneName.Head);
                SetPositionRotationForIk(PlayerModel.BoneName.Left, delta, left.Origin, left.Basis.GetEuler(), false);
                SetPositionRotationForIk(PlayerModel.BoneName.Right, delta, right.Origin, right.Basis.GetEuler(), false);
                SetPositionRotationForIk(PlayerModel.BoneName.Head, delta, GlobalPosition + (View.GlobalPosition - GlobalPosition), View.GlobalRotation);
            }
#endif
        }
    }

    public virtual void UpdateModelPhysics(double delta)
    {
        return;
        // the 41 is layers 1, 4, and 6 as a mask.
        if (Player.IsVR)
        {
            {
                Transform3D hipsRest = GetGlobalRest(PlayerModel.BoneName.Hips);
                Transform3D hips = GetGlobalPose(PlayerModel.BoneName.Hips);
                Transform3D footRest = GetGlobalRest(PlayerModel.BoneName.LeftFoot);
                Transform3D footPos = GetGlobalPose(PlayerModel.BoneName.LeftFoot);
                var args = PhysicsRayQueryParameters3D.Create(hips.Origin, footPos.Origin, 41);
                var results = Player.GetWorld3D().DirectSpaceState.IntersectRay(args);
                Transform3D pos = footPos;
                if (results.ContainsKey("position"))
                {
                    Vector3 offset = (hipsRest.Origin - hipsOffset) - footRest.Origin;
                    pos.Origin = results["position"].AsVector3() - offset;
                    // pos.Basis = Basis.LookingAt(Vector3.Forward, results["normal"].AsVector3());
                }
                SetPositionRotationForIk(PlayerModel.BoneName.LeftFoot, delta, pos.Origin, pos.Basis.GetEuler());
            }
            {
                Transform3D hipsRest = GetGlobalRest(PlayerModel.BoneName.Hips);
                Transform3D hips = GetGlobalPose(PlayerModel.BoneName.Hips);
                Transform3D footRest = GetGlobalRest(PlayerModel.BoneName.RightFoot);
                Transform3D footPos = GetGlobalPose(PlayerModel.BoneName.RightFoot);
                var args = PhysicsRayQueryParameters3D.Create(hips.Origin, footPos.Origin, 41);
                var results = Player.GetWorld3D().DirectSpaceState.IntersectRay(args);
                Transform3D pos = footPos;
                if (results.ContainsKey("position"))
                {
                    Vector3 offset = (hipsRest.Origin - hipsOffset) - footRest.Origin;
                    pos.Origin = results["position"].AsVector3() - offset;
                    // pos.Basis = Basis.LookingAt(Vector3.Forward, results["normal"].AsVector3());
                }
                SetPositionRotationForIk(PlayerModel.BoneName.RightFoot, delta, pos.Origin, pos.Basis.GetEuler());
            }
        }
    }

    public virtual void HandleAnimations(double delta)
    {
        if (IsInstanceValid(anims) && Player.TryGetAbility(out InventoryAbility inventory))
        {
            if (inventory.InventoryEquipped.Any(x => x.model is Pistol))
            {
                anims.heldItem = inventory.InventoryEquipped.Where(x => x.model is Pistol).Select(x => (Pistol)x.model).FirstOrDefault().holdType;
            }
            else if (inventory.InventoryEquipped.Any(/*x => x.model is Flashlight*/))
            {
                anims.heldItem = PlayerAnims.HeldItemType.SmallItem;
            }
            else
            {
                anims.heldItem = PlayerAnims.HeldItemType.None;
            }
        }
    }
}

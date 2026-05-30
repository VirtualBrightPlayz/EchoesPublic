using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Vosk;

public partial class Ragdoll : Node, IInteractable
{
    public static StringName META_NAME = "ragdoll";

    [Export]
    public Skeleton3D skeleton = null;
    [Export]
    public PhysicalBoneSimulator3D boneSim;
    [Export]
    public PhysicalBone3D[] include = new PhysicalBone3D[0];
    [Export]
    public int roleId;
    [Export]
    public string playerName;
    [Export]
    public int TypeId;
    [Export]
    public bool AllowAnimationAdaptation = true;
    [Export]
    public PackedScene boneScene;
    [Export]
    public PackedScene bloodScene;
    
    public DamageType Type => (DamageType)TypeId;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint layer;
    [Export(PropertyHint.Layers3DPhysics)]
    public uint mask;

    [Export]
    public CollisionShape3D shape;

    public PlayerRole Role => roleId >= 0 && roleId < IInitScript.Instance.Data.Roles.Length ? IInitScript.Instance.Data.Roles[roleId] : IInitScript.Instance.Data.Roles[0];

    public Vector3 WorldInteractPosition => skeleton.GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;

    public Dictionary<string, Array<PhysicsBody3D>> hits = new Dictionary<string, Array<PhysicsBody3D>>();

    public override void _EnterTree()
    {
        GetParent().SetMeta(META_NAME, this);
        foreach (PhysicalBone3D physBone in skeleton.FindChildren("*", nameof(PhysicalBone3D), owned: false))
        {
            physBone.SetMeta(META_NAME, this);
            physBone.SetMeta(IInteractable.META_NAME, this);
            physBone.CollisionLayer = layer;
            physBone.CollisionMask = mask;
        }
    }

    public Dictionary<string, Transform3D> boneLocations = new Dictionary<string, Transform3D>();
    
    public DamageInfo Info { get; set; }

    public void ApplyDamageInfo(DamageInfo damageInfo)
    {
        foreach (PhysicalBone3D physBone in skeleton.FindChildren("*", nameof(PhysicalBone3D), owned: false))
        {
            foreach (var vModifier in damageInfo.Modifiers)
            {
                vModifier.ApplyPostMortem(physBone);
            }
        }
    }

    public override void _ExitTree()
    {
        GetParent().RemoveMeta(META_NAME);
        foreach (PhysicalBone3D physBone in skeleton.FindChildren("*", nameof(PhysicalBone3D), owned: false))
        {
            physBone.RemoveMeta(META_NAME);
        }
    }

    public override void _Ready()
    {
        Spawned();
    }

    public void SpawnBlood(Vector3 pos, Vector3 normal)
    {
        if (!IsInstanceValid(bloodScene))
            return;
        BloodGib blood = bloodScene.Instantiate<BloodGib>();
        ItemManager.Instance.SpawnNode.AddChild(blood, true);
        blood.GlobalPosition = pos;
        blood.GlobalBasis = Basis.LookingAt(Vector3.Forward, normal);
    }

    public void ResetHits()
    {
        hits.Clear();
    }

    public void SpawnBones()
    {
        if (IsMultiplayerAuthority())
        {
            GetParent().QueueFree();
            if (IsInstanceValid(boneScene))
            {
                var bones = skeleton.FindChildren("*", nameof(PhysicalBone3D), owned: false);
                for (int i = 0; i < 3; i++)
                {
                    var physBone = (PhysicalBone3D)bones.PickRandom();
                    Node3D bone = boneScene.Instantiate<Node3D>();
                    ItemManager.Instance.SpawnNode.AddChild(bone, true);
                    bone.GlobalTransform = physBone.GlobalTransform;
                    bone.Scale = Vector3.One;
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (boneSim.IsSimulatingPhysics())
        {
            int found = 0;
            foreach (PhysicalBone3D physBone in skeleton.FindChildren("*", nameof(PhysicalBone3D), owned: false))
            {
                if (!IsMultiplayerAuthority())
                {
                    // physBone.LinearVelocity = Vector3.Zero;
                    // physBone.AngularVelocity = Vector3.Zero;
                    // skeleton.SetBoneGlobalPose(physBone.GetBoneId(), physBone.Transform);
                    continue;
                }
                PhysicsDirectBodyState3D state = PhysicsServer3D.BodyGetDirectState(physBone.GetRid());
                {
                    for (int i = 0; i < state.GetContactCount(); i++)
                    {
                        GodotObject obj = state.GetContactColliderObject(i);
                        if (obj is not PhysicalBone3D)
                        if (obj is PhysicsBody3D body)
                        {
                            if (!hits.ContainsKey(physBone.Name))
                                hits.Add(physBone.Name, new Array<PhysicsBody3D>());
                            if (!hits[physBone.Name].Contains(body))
                            {
                                hits[physBone.Name].Add(body);
                                SpawnBlood(state.GetContactLocalPosition(i), state.GetContactLocalNormal(i));
                            }
                        }
                        // if (obj is not PhysicalBone3D)
                        {
                            // found++;
                            // break;
                        }
                    }
                }
            }
            if (found >= 8)
            {
                if (IsInstanceValid(shape))
                {
                    var head = skeleton.FindChild("Head", owned: false) as PhysicalBone3D;
                    var hips = skeleton.FindChild("Hips", owned: false) as PhysicalBone3D;
                    if (IsInstanceValid(head) && IsInstanceValid(hips))
                    {
                        shape.GlobalTransform = head.GlobalTransform.InterpolateWith(hips.GlobalTransform, 0.5f);
                        shape.Scale = Vector3.One;
                        shape.Disabled = false;
                    }
                    else if (IsInstanceValid(hips))
                    {
                        shape.GlobalTransform = hips.GlobalTransform;
                        shape.Scale = Vector3.One;
                        shape.Disabled = false;
                    }
                }
                // boneSim.PhysicalBonesStopSimulation();
            }
        }
    }

    public async void Spawned()
    {
        // await ToSignal(GetTree().CreateTimer(10d), SceneTreeTimer.SignalName.Timeout);
        Skeleton3D skel = skeleton;
        if (AllowAnimationAdaptation)
        {
            foreach (var name in RagdollMaker.HumanBones)
            {
                int id = skeleton.FindBone(name);
                // BoneSync sync = skeleton.FindChild(name, owned: false) as BoneSync;
                // BoneProxySync proxySync = skeleton.FindChild(name, owned: false) as BoneProxySync;
                if (id == -1 || !boneLocations.ContainsKey(name))
                {
                    Log.PrintWarn("Can't find bone by name: " + name);
                    continue;
                }
                Transform3D location = boneLocations[name];
                skeleton.SetBonePose(id, location);
            }
        }
        Array<StringName> names = new Array<StringName>();
        names.AddRange(include.Select(x => new StringName(skel.GetBoneName(x.GetBoneId()))));
        boneSim.PhysicalBonesStartSimulation(names);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        if (Info != null)
        {
            ApplyDamageInfo(Info);
        }
        foreach (PhysicalBone3D physBone in skeleton.FindChildren("*", nameof(PhysicalBone3D), owned: false))
        {
            PhysicsServer3D.BodySetMaxContactsReported(physBone.GetRid(), 4);
            if (!IsMultiplayerAuthority())
                PhysicsServer3D.BodySetMode(physBone.GetRid(), PhysicsServer3D.BodyMode.Kinematic);
        }
        if (IsMultiplayerAuthority())
        {
            await ToSignal(GetTree().CreateTimer(60d), SceneTreeTimer.SignalName.Timeout);
            SpawnBones();
        }
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        if (holder.GetPlayer().TryGetAbility(out ReviveAbility revive))
        {
            revive.RpcId(MultiplayerPeer.TargetPeerServer, ReviveAbility.MethodName.RpcRevive, GetPath());
        }
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return holder.GetPlayer().TryGetAbility(out ReviveAbility _);
    }

}

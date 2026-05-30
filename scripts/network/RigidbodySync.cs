using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class RigidbodySync : RigidBody3D, IPhysicsProp, IDamageSource
{
    [Export]
    [Obsolete("Use xformSync")]
    public Node sync;
    [Export]
    public TransformSync xformSync;
    [Export]
    public double timeout = 1d;
    [Export]
    public bool updateSync = true;
    [Export]
    public bool updateFreeze = true;
    [Export]
    public bool canGrab = true;

    public double timer;
    public bool timerRunning = false;
    public List<IPlayerController> grabbingPeers = [];
    public Dictionary<int, Vector3> grabPoints = new();
    public Dictionary<int, Basis> grabRotations = new();
    public IPlayerList list;

    public bool hasImpl;
    public Vector3 impl;
    public Vector3 offset;

    public double syncTimer;

    public override void _Ready()
    {
        // if (!IsInstanceValid(sync))
        //     Log.PrintErr($"No TransformSync found! Path is: {GetPath()}");
        // FreezeMode = FreezeModeEnum.Kinematic;
        ContactMonitor = true;
        MaxContactsReported = 4;
        SleepingStateChanged += _SleepChange;
        list = IPlayerList.List(this);
        UpdateFreezeState();
        xformSync.OnClaimantChanged += UpdateFreezeState;
        xformSync.OnLastPositionSet += UpdateFreezeState;
    }

    public const float MINIMUM_WEIGHT = 0.35f;

    public override void _EnterTree()
    {
        base._EnterTree();
        _lastRotation = GlobalRotation;
        _lastPosition = GlobalPosition;
        if (Mass < MINIMUM_WEIGHT)
        {
            //Log.Print($"Mass must be greater than {MINIMUM_WEIGHT}kg, It has been set to {MINIMUM_WEIGHT}kg. Path: " + GetPath());
            Mass = MINIMUM_WEIGHT;
        }
        // NetworkManager.Instance.OnProcess += Tick;
        Multiplayer.PeerConnected += _Joined;
        if (IsInstanceValid(sync))
        {
            sync.QueueFreeNow();
            sync = null;
        }
        if (!IsInstanceValid(xformSync))
        {
            //Log.Print($"\"xformSync\" not set, creating default TransformSync. Path: {GetPath()}");
            xformSync = new TransformSync()
            {
                Name = "Sync",
                targetNode = new NodePath(".."),
                CanClaim = true,
            };
            AddChild(xformSync, true);
        }
        xformSync.OnSync += _OnSync;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DestroyJoint();
        // NetworkManager.Instance.OnProcess -= Tick;
        Multiplayer.PeerConnected -= _Joined;
        xformSync.OnSync -= _OnSync;
    }

    private Vector3 _lastRotation = Vector3.Zero;
    private Vector3 _lastPosition = Vector3.Zero;
    private Generic6DofJoint3D joint;
    private PhysicsDirectBodyState3D bodyState;
    private HashSet<RigidbodySync> connectedSyncs = new HashSet<RigidbodySync>();

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!IsNodeReady())
            return;
        if (!IsInstanceValid(xformSync))
            return;
        syncTimer += delta;
        if (syncTimer > xformSync.syncIntervalTicks / 1000d)
        {
            syncTimer = 0d;
            if (hasImpl)
            {
                hasImpl = false;
                Rpc(MethodName.RpcImpulse, impl, offset);
            }
        }
    }

    public virtual void DamageOther(IHealth health, DamageInfo info)
    {
        health.Damage(info);
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        UpdateState(state);
    }

    public virtual void UpdateState(PhysicsDirectBodyState3D state)
    {
        if (!IsMultiplayerAuthority())
            return;
        if (!xformSync.CanClaim || xformSync.IsClaimantServer())
        {
            foreach (var sync in connectedSyncs)
            {
                if (IsInstanceValid(sync) && IsInstanceValid(sync.xformSync))
                {
                    sync.xformSync.SetClaimant((int)MultiplayerPeer.TargetPeerBroadcast);
                }
            }
            connectedSyncs.Clear();
            return;
        }
        for (int i = 0; i < state.GetContactCount(); i++)
        {
            GodotObject obj = state.GetContactColliderObject(i);
            if (obj is RigidbodySync rb && rb.xformSync.CanClaim && rb.xformSync.ClaimantId != xformSync.ClaimantId)
            {
                rb.xformSync.SetClaimant(xformSync.ClaimantId);
                // connectedSyncs.Add(rb);
            }
        }
    }

    private void _OnSync()
    {
        return;
        if (xformSync.CanClaim && xformSync.IsClaimant())
            Rpc(MethodName.RpcVelocity, LinearVelocity, AngularVelocity);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RpcVelocity(Vector3 linear, Vector3 angular)
    {
        if (!xformSync.SenderIsClaimantOrServer())
            return;
        LinearVelocity = linear;
        AngularVelocity = angular;
        SetDeferred(PropertyName.LinearVelocity, linear);
        SetDeferred(PropertyName.AngularVelocity, angular);
    }

    public void CreateJoint(IPlayerController player, Vector3 pos, Basis rot)
    {
        DestroyJoint();
        if (player is PhysicsBody3D pb)
        {
            if (!IsInstanceValid(joint))
            {
                joint = new Generic6DofJoint3D();
                // disable limits
                joint.SetFlagX(Generic6DofJoint3D.Flag.EnableLinearLimit, false);
                joint.SetFlagY(Generic6DofJoint3D.Flag.EnableLinearLimit, false);
                joint.SetFlagZ(Generic6DofJoint3D.Flag.EnableLinearLimit, false);
                // disable limits
                joint.SetFlagX(Generic6DofJoint3D.Flag.EnableAngularLimit, false);
                joint.SetFlagY(Generic6DofJoint3D.Flag.EnableAngularLimit, false);
                joint.SetFlagZ(Generic6DofJoint3D.Flag.EnableAngularLimit, false);
                // enable springs
                joint.SetFlagX(Generic6DofJoint3D.Flag.EnableLinearSpring, true);
                joint.SetFlagY(Generic6DofJoint3D.Flag.EnableLinearSpring, true);
                joint.SetFlagZ(Generic6DofJoint3D.Flag.EnableLinearSpring, true);
                // enable springs
                joint.SetFlagX(Generic6DofJoint3D.Flag.EnableAngularSpring, true);
                joint.SetFlagY(Generic6DofJoint3D.Flag.EnableAngularSpring, true);
                joint.SetFlagZ(Generic6DofJoint3D.Flag.EnableAngularSpring, true);
                // setup springs
                float stiffness = 120f;
                joint.SetParamX(Generic6DofJoint3D.Param.LinearSpringStiffness, stiffness);
                joint.SetParamY(Generic6DofJoint3D.Param.LinearSpringStiffness, stiffness);
                joint.SetParamZ(Generic6DofJoint3D.Param.LinearSpringStiffness, stiffness);
                // setup springs
                joint.SetParamX(Generic6DofJoint3D.Param.AngularSpringStiffness, stiffness);
                joint.SetParamY(Generic6DofJoint3D.Param.AngularSpringStiffness, stiffness);
                joint.SetParamZ(Generic6DofJoint3D.Param.AngularSpringStiffness, stiffness);
                // setup damps
                float damp = 1f;
                joint.SetParamX(Generic6DofJoint3D.Param.LinearSpringDamping, damp);
                joint.SetParamY(Generic6DofJoint3D.Param.LinearSpringDamping, damp);
                joint.SetParamZ(Generic6DofJoint3D.Param.LinearSpringDamping, damp);
                // setup damps
                joint.SetParamX(Generic6DofJoint3D.Param.AngularSpringDamping, damp);
                joint.SetParamY(Generic6DofJoint3D.Param.AngularSpringDamping, damp);
                joint.SetParamZ(Generic6DofJoint3D.Param.AngularSpringDamping, damp);
                joint.Position = pos;
                joint.Basis = rot;
                AddChild(joint);
                joint.NodeA = joint.GetPathTo(this);
            }
            joint.NodeB = joint.GetPathTo(player.View);
        }
    }

    public void DestroyJoint()
    {
        if (IsInstanceValid(joint))
        {
            joint.QueueFreeNow();
        }
    }
    
    public virtual void _Joined(long id)
    {
    }

    private void _SleepChange()
    {
        if (IsMultiplayerAuthority() && xformSync.CanClaim && Sleeping)
        {
            xformSync.SetClaimant((int)MultiplayerPeer.TargetPeerBroadcast);
        }
    }

    public virtual void UpdateFreezeState()
    {
        Freeze = !xformSync.IsClaimant() && !IsMultiplayerAuthority();// && xformSync.lastPosition.HasValue;
    }

    public void Grab(bool grab, Vector3 localPos = default, Vector3 releaseForce = default)
    {
        if (releaseForce == default)
        {
            releaseForce = Vector3.Zero;
        }
        if (grab)
            xformSync.SendTryClaim();
        else
        {
            Rpc(MethodName.RpcGrab, grab, GlobalPosition, releaseForce);
        }
        if (Sleeping)
        {
            Sleeping = false;
        }
    }
    
    public new float Mass
    {
        get
        {
            return base.Mass;
        }
        set
        {
            base.Mass = value;
        }
    }

    private bool _isGrabbed => xformSync.IsClaimant();
    
    public bool CurrentlyGrabbed => _isGrabbed;

    public bool AllowGrab
    {
        get => canGrab;
        set => canGrab = value;
    }

    public Node Sync => xformSync;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcGrab(bool add, Vector3 worldPos, Vector3 releaseForce)
    {
        if (!add && xformSync.SenderIsClaimantOrServer())
        {
            // xformSync.SetClaimant((int)MultiplayerPeer.TargetPeerBroadcast);
            GlobalPosition = worldPos;
            ApplyCentralImpulse(releaseForce);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RpcImpulse(Vector3 vel, Vector3 pos)
    {
        if (!xformSync.IsClaimantOrIsClaimantServer())
        {
            return;
        }
        ApplyImpulse(vel, pos);
    }

    public void OnImpulse(Vector3 vel, Vector3 pos)
    {
        if (xformSync.IsClaimantOrIsClaimantServer())
        {
            ApplyImpulse(vel, pos);
            return;
        }
        Rpc(MethodName.RpcImpulse, vel, pos);
    }
    
    [Export]
    public IPhysicsProp.AffectedBy Affected { get; set; } = IPhysicsProp.AffectedBy.PlayerPush |
                                                            IPhysicsProp.AffectedBy.Bullet |
                                                            IPhysicsProp.AffectedBy.Explosion |
                                                            IPhysicsProp.AffectedBy.Environment;

    public virtual string AttackerDisplayName => "Physics body";

    public NodePath AbsolutePath => GetPath();

}
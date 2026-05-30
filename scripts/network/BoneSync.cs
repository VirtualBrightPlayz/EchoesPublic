using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class BoneSync : PhysicalBone3D, IGrabbable
{
    [Export]
    public Node sync;
    [Export]
    public double timeout = 1d;
    [Export]
    public BoneProxySync[] ProxyBones = new BoneProxySync[0];

    public void RegisterBone(BoneProxySync bone)
    {
        ProxyBones = ProxyBones.Append(bone).ToArray();
    }
    
    public double timer;
    public bool timerRunning = false;
    public List<IPlayerController> grabbingPeers = new List<IPlayerController>();
    public Dictionary<int, Vector3> grabPoints = new Dictionary<int, Vector3>();
    public Dictionary<int, Basis> grabRotations = new Dictionary<int, Basis>();
    public IPlayerList list;

    public bool hasImpl = false;
    public Vector3 impl;
    public Vector3 offset;

    public double syncTimer;

    public Vector3 spawnPos;

    public override void _EnterTree()
    {
        base._EnterTree();
        spawnPos = Position;
    }

    public override void _Ready()
    {
        Multiplayer.PeerConnected += _Joined;
        list = IPlayerList.List(this);
    }

    /*
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(sync))
            return;
        syncTimer += delta;
        if (syncTimer > sync.syncIntervalTicks / 1000d)
        {
            syncTimer = 0d;
            if (hasImpl)
            {
                hasImpl = false;
                Rpc(MethodName.RpcImpulse, impl, offset);
            }
        }
        if (sync.IsAuthority || (sync.IsAuthorityServer && sync.IsMultiplayerAuthority()))
        {
            // Freeze = false;
            sync.SyncPosition = grabbingPeers.Count != 0;
            sync.SyncRotation = grabbingPeers.Count != 0;
        }
        else
        {
            // Freeze = true;
            sync.SyncPosition = true;
            sync.SyncRotation = true;
        }
        if (timer <= timeout)
            timer += delta;
        if (timer > timeout)
        {
            if (timerRunning)
            {
                timerRunning = false;
                if (sync.CanClaim)
                    sync.Rpc(TransformSync.MethodName.RpcReleaseClaim);
            }
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!IsInstanceValid(sync))
            return;
        if (sync.IsAuthority || (sync.IsAuthorityServer && sync.IsMultiplayerAuthority()))
        {
            // Freeze = false;
            // sync.SyncPosition = !state.Sleeping;
            // sync.SyncRotation = !state.Sleeping;
        }
        else
        {
            // Freeze = true;
            // sync.SyncPosition = true;
            // sync.SyncRotation = true;
            // Vector3 impl = Position - sync.targetPosition.value;
            // state.ApplyCentralForce(impl);
        }
        if (!sync.CanClaim && sync.IsMultiplayerAuthority())
        {
            for (int i = 0; i < grabbingPeers.Count; i++)
            {
                var plr = grabbingPeers[i];
                if (!IsInstanceValid(plr as GodotObject))
                {
                    grabbingPeers.RemoveAt(i);
                    i--;
                    continue;
                }
                float plrMass = 10f;
                float plrInertia = 45f;
                float plrAccel = 80f;
                var head = plr.View;
                Vector3 grabbedPosLocal = Vector3.Zero;
                if (grabPoints.ContainsKey(plr.Player.GetMultiplayerAuthority()))
                {
                    grabbedPosLocal = grabPoints[plr.Player.GetMultiplayerAuthority()];
                }
                Vector3 origin = ToGlobal(grabbedPosLocal);
                Vector3 impl = (head.GlobalPosition - head.GlobalBasis.Z.Normalized() * 1.5f) - origin;
                state.LinearVelocity = state.LinearVelocity.MoveToward(impl * plrMass * state.InverseMass, state.Step * plrAccel);

                Basis grabRot = Basis.Identity;
                if (grabRotations.ContainsKey(plr.Player.GetMultiplayerAuthority()))
                {
                    grabRot = grabRotations[plr.Player.GetMultiplayerAuthority()];
                }
                Quaternion quat = (head.GlobalBasis * (state.Transform.Basis * grabRot).Inverse()).GetRotationQuaternion();
                Vector3 rot = quat.GetAxis() * quat.GetAngle();
                state.AngularVelocity = state.AngularVelocity.MoveToward(rot * Mathf.DegToRad(plrInertia) * state.InverseInertia, state.Step * plrAccel);
            }
        }
    }
    */

    private void _Joined(long id)
    {
    }

    public void Grab(bool grab, Vector3 localPos = default, Vector3 releaseForce = default)
    {
        Rpc(MethodName.RpcGrab, grab, localPos, releaseForce);
        _isGrabbed = grab;
    }

    public new double Mass => base.Mass;

    private bool _isGrabbed = false;
    
    public bool CurrentlyGrabbed => _isGrabbed;
    
    public bool AllowGrab { get; set; } = true;
    
    public Node Sync => sync;

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcGrabbed(bool state)
    {
        _isGrabbed = state;
        foreach (BoneProxySync bone in ProxyBones)
        {
            if (IsInstanceValid(bone))
            {
                bone.OnGrabbed(state);
            }
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcGrab(bool add, Vector3 localPos, Vector3 releaseForce = default)
    {
        // if (!sync.IsMultiplayerAuthority() || sync.CanClaim)
        {
            // return;
        }
        int id = Multiplayer.GetRemoteSenderId();
        NetworkPlayer player = list.PlayerList.FirstOrDefault(x => x.AuthorityId == id);
        if (add)
        {
            if (!grabbingPeers.Contains(player.ActiveController))
                grabbingPeers.Add(player.ActiveController);
            grabPoints[Multiplayer.GetRemoteSenderId()] = localPos;
            //Sleeping = false;
            ApplyCentralImpulse(Vector3.Up * 0.01f);
        }
        else
        {
            grabbingPeers.Remove(player.ActiveController);
            OnImpulse(releaseForce, default);
        }
        Rpc(MethodName.RpcGrabbed, add);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    public void RpcImpulse(Vector3 vel, Vector3 pos)
    {
        // if (!sync.IsMultiplayerAuthority() || sync.CanClaim)
        {
            // return;
        }
        ApplyImpulse(vel, pos);
    }

    public void OnImpulse(Vector3 vel, Vector3 pos)
    {
        // if (sync.IsMultiplayerAuthority() && !sync.CanClaim)
        {
            // ApplyImpulse(vel, pos);
            // return;
        }
        if (hasImpl)
        {
            impl = impl.Lerp(vel, 0.5f);
            offset = offset.Lerp(pos, 0.5f);
        }
        else
        {
            hasImpl = true;
            impl = vel;
            offset = pos;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

[GlobalClass]
public partial class TransformSync : Node
{
    public static bool DebugDraw = false;

    [Signal]
    public delegate void OnSyncEventHandler();
    [Signal]
    public delegate void OnClaimantChangedEventHandler();
    [Signal]
    public delegate void OnLastPositionSetEventHandler();

    [Export]
    public bool SyncActive
    {
        get => _syncActive;
        set
        {
            if (_syncActive != value)
            {
                _syncActive = value;
                if (value)
                {
                    SendToAll();
                }
            }
        }
    }
    
    private bool _syncActive = true;
    
    [Export]
    public bool SyncPosition = true;
    [Export]
    public ulong displayDelayTicks = 300;
    [Export]
    public ulong syncIntervalTicks = 250;
    [Export]
    public NodePath targetNode;
    [Export]
    public int ClaimantId = (int)MultiplayerPeer.TargetPeerServer;
    [Export]
    public bool CanClaim = false;
    [Export]
    public bool CanStealClaim = true;
    [Export]
    public Node3D relativeTo;

    public struct PositionData<T>
    {
        public ulong localTick;
        public ulong remoteTick;
        public int claimantId;
        public T value;
    }

    public struct PosRotPair
    {
        public Vector3 position;
        public Quaternion rotation;
        public NodePath relation;
        public bool teleport;
    }

    public Node3D parent;
    public double syncTimer;
    public bool isSpawned = false;

    public Queue<PositionData<PosRotPair>> positions = new Queue<PositionData<PosRotPair>>();
    public PositionData<PosRotPair>? lastPosition = null;
    public bool lastPositionShown = false;
    public PosRotPair? lastSentPosition = null;
    public ulong? sentPositionLastTick = null;

    public int localId = (int)MultiplayerPeer.TargetPeerBroadcast;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClaimant() => localId == ClaimantId;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClaimantOrIsClaimantServer() => IsClaimant() || (IsMultiplayerAuthority() && IsClaimantServer());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClaimantServer() => MultiplayerPeer.TargetPeerBroadcast == ClaimantId;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SenderIsClaimantOrServer() => Multiplayer.GetRemoteSenderId() == ClaimantId || Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority();

    public bool IsNearMainCamera(Vector3 position)
    {
        return true;
        if (IsInstanceValid(NetworkPlayer.LocalInstance) && IsInstanceValid(ItemManager.Instance) && NetworkPlayer.LocalInstance.ActiveController != null && IsInstanceValid(NetworkPlayer.LocalInstance.ActiveController.Camera))
        {
            return NetworkPlayer.LocalInstance.PlayerPosition.DistanceSquaredTo(position) < NetworkPlayer.LocalInstance.ActiveController.Camera.Far * NetworkPlayer.LocalInstance.ActiveController.Camera.Far;
        }
        return true;
    }

    public override void _EnterTree()
    {
        ClaimantId = CanClaim ? (int)MultiplayerPeer.TargetPeerBroadcast : GetMultiplayerAuthority();
    }

    public override void _ExitTree()
    {
    }

    public override void _Ready()
    {
        SetTargetNode(GetNodeOrNull<Node3D>(targetNode));
        localId = Multiplayer.GetUniqueId();
        Multiplayer.PeerConnected += _Joined;
        syncTimer = 0d;
        positions.Clear();
        lastSentPosition = null;
        if (IsMultiplayerAuthority())
        {
            // CallDeferred(MethodName.Rpc, MethodName.RpcChangeOwner, ClaimantId);
            CallDeferred(MethodName.SendToAll);
        }
        SetDeferred(PropertyName.isSpawned, true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer() || !isSpawned)
            return;
        if (!IsInstanceValid(parent))
        {
            return;
        }
        if (IsClaimantOrIsClaimantServer())
        {
            // send
            _ProcessSending(delta);
        }
        if (DebugDraw || !IsClaimantOrIsClaimantServer())
        {
            // incoming
            _ProcessIncoming(delta);
        }
    }

    public Vector3 GetVelocity()
    {
        if (!SyncPosition || !lastPosition.HasValue || positions.Count == 0)
            return Vector3.Zero;
        PositionData<PosRotPair> targetPosition = positions.Peek();
        ulong tickDelta = targetPosition.remoteTick - lastPosition.Value.remoteTick;
        double timeDelta = tickDelta / 1000d;
        return (targetPosition.value.position - lastPosition.Value.value.position) * (float)timeDelta;
    }

    private void _ProcessIncoming(double delta)
    {
        if (!SyncActive)
        {
            return;
        }
        float speed = (float)delta * 10f;
        speed = 1f;
        // speed = Mathf.Clamp(speed, 0f, 1f);
        Transform3D offsets = Transform3D.Identity;
        if (lastPosition.HasValue)
        {
            // relativeTo = GetNodeOrNull<Node3D>(lastPosition.Value.value.relation);
        }
        if (IsInstanceValid(relativeTo))
        {
            // offsets = relativeTo.GlobalTransform;
        }
        if (SyncPosition && positions.Count != 0)
        {
            PositionData<PosRotPair> targetPosition = positions.Peek();
            /*
            if (lastPosition.HasValue)
            {
                PositionData<PosRotPair> v = lastPosition.Value;
                v.value.position = parent.Position;
                v.value.rotation = parent.Quaternion;
                v.value.relation = IsInstanceValid(relativeTo) ? GetPathTo(relativeTo) : new NodePath();
                lastPosition = v;
            }
            */
            var relation = GetNodeOrNull<Node3D>(targetPosition.value.relation);
            if (IsInstanceValid(relation))
            {
                offsets = relation.GlobalTransform;
            }
            ulong lastTick = lastPosition.Value.localTick;//targetPosition.localTick + (lastPosition.Value.remoteTick - targetPosition.remoteTick);
            Vector3? resultPosition = null;
            Quaternion? resultQuaternion = null;
            // prevent NaN
            if (lastTick == targetPosition.localTick)
            {
                resultPosition = offsets * lastPosition.Value.value.position;
                resultQuaternion = lastPosition.Value.value.rotation;
                lastPosition = positions.Dequeue();
                lastPositionShown = false;
                EmitSignalOnLastPositionSet();
            }
            else
            {
                ulong curTick = Time.GetTicksMsec() - displayDelayTicks;
                float lerpAmount = Mathf.InverseLerp(lastTick, targetPosition.localTick, curTick);
                lerpAmount = Mathf.Clamp(lerpAmount, 0f, 1f);
                if (targetPosition.value.teleport || lastPosition.Value.value.teleport)
                {
                    lerpAmount = 1f;
                }
                // prevent NaN
                if (lastPosition.Value.value.position.IsEqualApprox(targetPosition.value.position))
                {
                    resultPosition = offsets * targetPosition.value.position;
                }
                else
                {
                    resultPosition = offsets * lastPosition.Value.value.position.Lerp(targetPosition.value.position, lerpAmount);
                }
                if (lastPosition.Value.value.rotation.IsEqualApprox(targetPosition.value.rotation))
                {
                    resultQuaternion = targetPosition.value.rotation;
                }
                else
                {
                    resultQuaternion = lastPosition.Value.value.rotation.Slerp(targetPosition.value.rotation, lerpAmount);
                }
                if (curTick >= targetPosition.localTick)
                {
                    lastPosition = positions.Dequeue();
                    lastPositionShown = false;
                    EmitSignalOnLastPositionSet();
                }
            }
            if (resultPosition.HasValue && (IsNearMainCamera(offsets.AffineInverse() * resultPosition.Value) || IsNearMainCamera(parent.GlobalPosition)))
            {
                if (DebugDraw && IsInstanceValid(DebugDrawManager.Instance))
                {
                    DebugDrawManager.Instance.DrawDebugPoint(offsets.AffineInverse() * resultPosition.Value, Colors.Green, delta);
                }
                if (!IsClaimantOrIsClaimantServer())
                {
                    if (CanClaim)
                    {
                        parent.Position = parent.Position.Lerp(resultPosition.Value, speed);
                    }
                    else
                    {
                        parent.Position = resultPosition.Value;
                    }
                    if (resultQuaternion.HasValue)
                    {
                        if (CanClaim)
                        {
                            parent.Quaternion = parent.Quaternion.Slerp(resultQuaternion.Value, speed);
                        }
                        else
                        {
                            parent.Quaternion = resultQuaternion.Value;
                        }
                    }
                }
            }
            else if (resultPosition.HasValue && DebugDraw && IsInstanceValid(DebugDrawManager.Instance))
            {
                DebugDrawManager.Instance.DrawDebugPoint(offsets.AffineInverse() * resultPosition.Value, Colors.Red, delta);
            }
        }
        else if (SyncPosition && lastPosition.HasValue && !lastPositionShown)
        {
            var relation = GetNodeOrNull<Node3D>(lastPosition.Value.value.relation);
            if (IsInstanceValid(relation))
            {
                offsets = relation.GlobalTransform;
            }
            if (!IsClaimantOrIsClaimantServer())
            {
                if (CanClaim)
                {
                    parent.Position = parent.Position.Lerp(offsets * lastPosition.Value.value.position, speed);
                    parent.Quaternion = parent.Quaternion.Slerp(lastPosition.Value.value.rotation, speed);
                }
                else
                {
                    parent.Position = offsets * lastPosition.Value.value.position;
                    parent.Quaternion = lastPosition.Value.value.rotation;
                }
            }
            if (!IsInstanceValid(relation))
                lastPositionShown = true;

            if (DebugDraw && IsInstanceValid(DebugDrawManager.Instance))
            {
                DebugDrawManager.Instance.DrawDebugPoint(offsets.AffineInverse() * lastPosition.Value.value.position, Colors.Yellow, delta);
            }
        }
    }

    private void _ProcessSending(double delta)
    {
        if (!SyncActive)
        {
            return;
        }
        if (!DebugDraw)
        {
            positions.Clear();
            lastPosition = null;
        }
        syncTimer += delta;
        if (syncTimer > syncIntervalTicks / 1000d)
        {
            syncTimer = 0d;
            Transform3D offsets = Transform3D.Identity;
            NodePath path = new NodePath();
            if (IsInstanceValid(relativeTo))
            {
                offsets = relativeTo.GlobalTransform;
                path = GetPathTo(relativeTo);
            }
            if (SyncPosition)
            {
                Vector3 pos = offsets.AffineInverse() * parent.Position;
                Quaternion rot = parent.Quaternion;
                if (!lastSentPosition.HasValue || lastSentPosition.Value.position.DistanceTo(pos) > 0.05f || lastSentPosition.Value.rotation.AngleTo(rot) > Mathf.DegToRad(2.5f) || !sentPositionLastTick.HasValue)
                {
                    bool tp = !sentPositionLastTick.HasValue && lastSentPosition.HasValue;
                    // if (!sentPositionLastTick && lastSentPosition.HasValue)
                    //     Rpc(MethodName.RpcPositionRotation, Time.GetTicksMsec(), lastSentPosition.Value.position, lastSentPosition.Value.rotation, lastSentPosition.Value.relation);
                    lastSentPosition = new PosRotPair()
                    {
                        position = pos,
                        rotation = rot,
                        relation = path,
                        teleport = tp,
                    };
                    Rpc(MethodName.RpcPositionRotation, Time.GetTicksMsec(), pos, rot, path, tp);
                    sentPositionLastTick = Time.GetTicksMsec();

                    if (DebugDraw && IsInstanceValid(DebugDrawManager.Instance))
                    {
                        DebugDrawManager.Instance.DrawDebugPoint(parent.GlobalPosition, Colors.White, syncIntervalTicks / 1000d);
                    }
                }
                // else
                {
                    // sentPositionLastTick = false;
                }
            }
            EmitSignalOnSync();
        }
    }
    
    private void _Joined(long id)
    {
        if (!isSpawned)
            return;
        if (IsMultiplayerAuthority())
        {
            RpcId(id, MethodName.RpcChangeOwner, ClaimantId);
        }
        Transform3D offsets = Transform3D.Identity;
        NodePath path = new();
        if (IsInstanceValid(relativeTo))
        {
            offsets = relativeTo.GlobalTransform;
            path = GetPathTo(relativeTo);
        }
        else
            relativeTo = null; // set to null just in case
        if (IsInstanceValid(parent) && (IsMultiplayerAuthority() || IsClaimantOrIsClaimantServer()))
        {
            RpcId(id, MethodName.RpcPositionRotationReliable, Time.GetTicksMsec(), offsets.AffineInverse() * parent.Position, parent.Quaternion, path);
        }
    }

    public void SetTargetNode(Node3D path)
    {
        if (!IsInstanceValid(path))
        {
            targetNode = new NodePath();
            return;
        }
        targetNode = GetPathTo(path);
        if (targetNode == null || targetNode.IsEmpty)
            parent = null;
        else
            parent = GetNode<Node3D>(targetNode);
    }

    public void SetRelativeTo(Node3D node)
    {
        relativeTo = node;
        // SendToAll();
        AfterTeleport();
    }

    public void SendToAll()
    {
        if (IsMultiplayerAuthority())
        {
            Rpc(MethodName.RpcChangeOwner, ClaimantId);
        }
        Transform3D offsets = Transform3D.Identity;
        NodePath path = new();
        if (IsInstanceValid(relativeTo))
        {
            offsets = relativeTo.GlobalTransform;
            path = GetPathTo(relativeTo);
        }
        else
            relativeTo = null; // set to null just in case
        if (IsInstanceValid(parent) && (IsMultiplayerAuthority() || IsClaimantOrIsClaimantServer()))
        {
            Rpc(MethodName.RpcPositionRotationReliable, Time.GetTicksMsec(), offsets.AffineInverse() * parent.Position, parent.Quaternion, path);
        }
    }

    public void AfterTeleport()
    {
        sentPositionLastTick = null;
        /*
        if (lastPosition.HasValue)
        {
            positions.Clear();
            var data = lastPosition.Value;
            data.value = new PosRotPair()
            {
                position = parent.Position,
                rotation = parent.Quaternion,
                relation = new NodePath(),
                teleport = true,
            };
            data.localTick = Time.GetTicksMsec();
            positions.Enqueue(data);
        }
        else
        {
            positions.Clear();
        }
        lastPosition = null;
        lastPositionShown = true;
        */
    }

    public void SendTryClaim()
    {
        Rpc(MethodName.RpcTryClaim);
    }

    public void SendReleaseClaim()
    {
        Rpc(MethodName.RpcReleaseClaim);
    }

    public void SetClaimant(int id)
    {
        if (!IsMultiplayerAuthority())
            return;
        if (id == ClaimantId)
            return;
        Rpc(MethodName.RpcChangeOwner, id);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RpcPositionRotation(ulong ticksMsec, Vector3 position, Quaternion rotation, NodePath relation, bool teleport)
    {
        if (!SenderIsClaimantOrServer())
            return;
        ulong localTick = Time.GetTicksMsec();
        if (positions.Count != 0 && positions.Peek().remoteTick > ticksMsec)
        {
            Log.Print($"Position from {Multiplayer.GetRemoteSenderId()} rejected on {GetPath()}. Reason: remoteTick is greater than localTick {localTick}.");
            return;
        }
        if (positions.Count != 0 && positions.Peek().remoteTick == ticksMsec)
        {
            Log.Print($"Position from {Multiplayer.GetRemoteSenderId()} on {GetPath()} warning. Reason: remoteTick is the same as localTick {localTick}.");
        }
        var data = new PositionData<PosRotPair>()
        {
            value = new PosRotPair()
            {
                position = position,
                rotation = rotation,
                relation = relation,
                teleport = teleport,
            },
            localTick = localTick,
            remoteTick = ticksMsec,
            claimantId = Multiplayer.GetRemoteSenderId(),
        };
        positions.Enqueue(data);
        if (!lastPosition.HasValue)
        {
            lastPosition = positions.Dequeue();
            lastPositionShown = false;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcPositionRotationReliable(ulong ticksMsec, Vector3 position, Quaternion rotation, NodePath relation)
    {
        if (!SenderIsClaimantOrServer())
            return;
        // positions.Clear();
        // lastSentPosition = null;
        var data = new PositionData<PosRotPair>()
        {
            value = new PosRotPair()
            {
                position = position,
                rotation = rotation,
                relation = relation,
                teleport = true,
            },
            localTick = Time.GetTicksMsec(),
            remoteTick = ticksMsec,
            claimantId = Multiplayer.GetRemoteSenderId(),
        };
        positions.Enqueue(data);
        if (!lastPosition.HasValue)
        {
            lastPosition = positions.Dequeue();
            lastPositionShown = false;
        }
        /*
        lastPosition = positions.Peek();
        lastPositionShown = false;
        if (relation.IsEmpty)
            relativeTo = null;
        else
            relativeTo = GetNodeOrNull<Node3D>(relation);
        */
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcChangeOwner(int id)
    {
        syncTimer = 0d;
        if (ClaimantId != id)
        {
            ClaimantId = id;
            EmitSignalOnClaimantChanged();
        }
        if (SyncActive && IsInstanceValid(parent))
        {
            Transform3D offsets = Transform3D.Identity;
            if (IsInstanceValid(relativeTo))
            {
                offsets = relativeTo.GlobalTransform;
            }
            // while (positions.TryDequeue(out var position))
            {
                // parent.Position = offsets * position.value.position;
                // parent.Quaternion = position.value.rotation;
            }
        }
        // AfterTeleport();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcTryClaim()
    {
        if (IsMultiplayerAuthority() && CanClaim && (CanStealClaim || ClaimantId == MultiplayerPeer.TargetPeerBroadcast))
        {
            int id = Multiplayer.GetRemoteSenderId();
            Rpc(MethodName.RpcChangeOwner, id);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcReleaseClaim()
    {
        if (IsMultiplayerAuthority() && CanClaim && Multiplayer.GetRemoteSenderId() == ClaimantId)
        {
            Rpc(MethodName.RpcChangeOwner, MultiplayerPeer.TargetPeerBroadcast);
        }
    }
}
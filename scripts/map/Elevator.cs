using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class Elevator : Node3D, IInteractable, IDoorStatus
{
    public static Dictionary<string, List<Elevator>> All { get; private set; } = new Dictionary<string, List<Elevator>>();

    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Touch;

    public DoorStatus Status
    {
        get
        {
            if (isLocked)
                return DoorStatus.Err;
            if (isMoving)
                return DoorStatus.Warn;
            return DoorStatus.Ok;
        }
    }
    public float StatusPulse => Status == DoorStatus.Warn ? 0.2f : 0f;
    public Action StatusChanged { get; set; }

    [Export]
    public Node3D marker;
    [Export]
    public Area3D area;
    [Export]
    public Door door;
    [Export]
    public string elevatorId;
    [Export]
    public Elevator linked;
    [Export]
    public AudioStream moveSound;
    [Export]
    public AudioStream beepSound;
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public float preMoveTime = 1f;
    [Export]
    public float moveTime = 7f;
    [Export]
    public bool isMoving
    {
        get => _isMovingInternal;
        set
        {
            if (_isMovingInternal != value)
            {
                _isMovingInternal = value;
                StatusChanged?.Invoke();
            }
        }
    }
    private bool _isMovingInternal = false;
    [Export]
    public bool isLocked = false;
    [Export]
    public NavigationLink3D navLink;

    public override void _EnterTree()
    {
        All.TryAdd(elevatorId, new List<Elevator>());
        if (All.TryGetValue(elevatorId, out List<Elevator> list))
        {
            list.Add(this);
        }
    }

    public override void _ExitTree()
    {
        if (All.TryGetValue(elevatorId, out List<Elevator> list))
        {
            list.RemoveAll(x => x == this);
        }
    }

    public override void _Ready()
    {
        if (linked == null && All.TryGetValue(elevatorId, out List<Elevator> list))
        {
            linked = list.FirstOrDefault(x => x != this);
            if (linked != null && door != null && door.IsMultiplayerAuthority())
            {
                door.SV_SetState(linked.linked != this);
            }
            if (linked != null)
            {
                linked.linked = this;
            }
        }
        if (IsInstanceValid(linked))
        {
            navLink.SetGlobalEndPosition(linked.GlobalPosition);
            navLink.Enabled = true;
        }
        else
        {
            navLink.Enabled = false;
        }
        StatusChanged?.Invoke();
    }

    public async void Toggle()
    {
        _Ready();
        if (linked == null || linked.isMoving || isMoving /*|| door.isOpen*/)
            return;
        if (isLocked || linked.isLocked)
            return;
        if (!door.isOpen)
        {
            linked.Toggle();
            return;
        }
        isMoving = true;
        linked.isMoving = true;
        door.SV_SetState(false);
        linked.door.SV_SetState(false);
        await ToSignal(GetTree().CreateTimer(preMoveTime), "timeout");
        SV_MovingSound(false);
        linked.SV_MovingSound(false);
        await ToSignal(GetTree().CreateTimer(moveTime), "timeout");
        foreach (var body in area.GetOverlappingBodies())
        {
            if (body is IElevatorTeleport tp)
            {
                var outPos = GlobalTransform.Inverse().TranslatedLocal(body.GlobalPosition).Origin;
                outPos = linked.GlobalTransform.TranslatedLocal(outPos).Origin;
                var outRot = GlobalRotation - body.GlobalRotation;
                if (body is IPlayerController ctrl)
                    outRot = GlobalRotation - ctrl.Player.PlayerRotation;
                outRot = linked.GlobalRotation - outRot;
                // var outRot = GlobalRotation.Y - body.GlobalRotation.Y;
                // outRot = linked.GlobalRotation.Y - outRot;
                // outRot = linked.GlobalRotation.Y - (GlobalRotation.Y - body.GlobalRotation.Y);
                body.CallDeferred(nameof(IElevatorTeleport.ElevatorTeleport), outPos, outRot);
                // tp.ElevatorTeleport(outPos, new Vector3(0f, outRot, 0f));
            }
        }
        linked.door.SV_SetState(true);
        // SV_MovingSound(true);
        linked.SV_MovingSound(true);
        await ToSignal(GetTree().CreateTimer(preMoveTime), "timeout");
        isMoving = false;
        linked.isMoving = false;
    }

    public void SV_MovingSound(bool isBeep)
    {
        Rpc(nameof(CL_MovingSound), isBeep);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_MovingSound(bool isBeep)
    {
        if (Multiplayer.GetRemoteSenderId() != GetMultiplayerAuthority())
            return;
        audio.Stream = isBeep ? beepSound : moveSound;
        audio.Play();
        if (!isBeep)
        {
            foreach (var body in area.GetOverlappingBodies())
            {
                if (body is IPlayerController player && player.Player.IsLocalPlayer)
                {
                    if (player.Camera.HasMeta(CameraShake.MetaName))
                    {
                        ElevatorShake elv = (ElevatorShake)player.Camera.GetMeta(CameraShake.MetaName).AsGodotObject();
                        elv.ShakeElevator(1f, moveTime - preMoveTime);
                    }
                }
                else if (IsInstanceValid(NetworkPlayer.LocalInstance) && NetworkPlayer.LocalInstance.IsAncestorOf(body))
                {
                    if (NetworkPlayer.LocalInstance.ActiveController.Camera.HasMeta(CameraShake.MetaName))
                    {
                        ElevatorShake elv = (ElevatorShake)NetworkPlayer.LocalInstance.ActiveController.Camera.GetMeta(CameraShake.MetaName).AsGodotObject();
                        elv.ShakeElevator(1f, moveTime - preMoveTime);
                    }
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse()
    {
        // TODO: anti-cheat checks
        Toggle();
    }

    public bool CanUse(IItemHolder holder, ItemObject item) => true;

    public void Use(IItemHolder holder, ItemObject item)
    {
        RpcId(1, nameof(RpcUse));
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}

public interface IElevatorTeleport
{
    void ElevatorTeleport(Vector3 position, Vector3 rotation);
}
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class AnimElevator : Node3D, IInteractable, IDoorStatus
{
    [Signal]
    public delegate void OnFloorChangeEventHandler();

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
    public AnimationPlayer anim;
    [Export]
    public Node3D marker;
    [Export]
    public Area3D area;
    [Export]
    public float preMoveTime = 1f;
    [Export]
    public float moveTime = 10f;
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
    public int floor = 0;
    [Export]
    public int floorCount = 2;
    [Export]
    public Door[] doors = Array.Empty<Door>();
    [Export]
    public StringName[] idleAnims = Array.Empty<StringName>();
    [Export]
    public Godot.Collections.Dictionary<string, StringName> moveAnimLookup = new Godot.Collections.Dictionary<string, StringName>();

    public override void _Ready()
    {
        IdleAnim(floor);
        area.BodyEntered += _Entered;
        area.BodyExited += _Exited;
        StatusChanged?.Invoke();
    }

    private void _Entered(Node3D body)
    {
        if (body is StaticBody3D)
            return;
        if (body is RigidbodySync sync && sync.IsMultiplayerAuthority())
        {
            sync.xformSync.SetRelativeTo(area);
        }
        if (body is IPlayerController plr && plr.Player.xformSync.IsClaimantOrIsClaimantServer())
        {
            plr.Player.xformSync.SetRelativeTo(area);
        }
    }

    private void _Exited(Node3D body)
    {
        if (body is RigidbodySync sync && sync.IsMultiplayerAuthority())
        {
            sync.xformSync.SetRelativeTo(null);
        }
        if (body is IPlayerController plr && plr.Player.xformSync.IsClaimantOrIsClaimantServer())
        {
            plr.Player.xformSync.SetRelativeTo(null);
        }
    }

    public async void Toggle(int newFloor)
    {
        if (newFloor == floor)
            return;
        if (isMoving)
            return;
        if (isLocked)
            return;
        isMoving = true;
        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].SV_SetState(false);
        }
        await ToSignal(GetTree().CreateTimer(preMoveTime), SceneTreeTimer.SignalName.Timeout);
        foreach (var obj in area.GetOverlappingBodies())
        {
            if (obj is RigidBody3D rb)
            {
                rb.Sleeping = false;
                rb.ApplyImpulse(Vector3.Zero);
            }
        }
        Rpc(MethodName.MoveAnim, floor, newFloor);
        await ToSignal(GetTree().CreateTimer(moveTime), SceneTreeTimer.SignalName.Timeout);
        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].SV_SetState(i == newFloor);
        }
        floor = newFloor;
        await ToSignal(GetTree().CreateTimer(preMoveTime), SceneTreeTimer.SignalName.Timeout);
        Rpc(MethodName.IdleAnim, newFloor);
        isMoving = false;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void MoveAnim(int fromFloor, int toFloor)
    {
        string str = fromFloor + "_" + toFloor;
        if (moveAnimLookup.TryGetValue(str, out var animName))
        {
            anim.Play(animName);
            EmitSignalOnFloorChange();
        }
        else
        {
            str = toFloor + "_" + fromFloor;
            if (moveAnimLookup.TryGetValue(str, out animName))
            {
                anim.PlayBackwards(animName);
                EmitSignalOnFloorChange();
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void IdleAnim(int toFloor)
    {
        anim.Play(idleAnims[toFloor]);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse(int newFloor)
    {
        // TODO: anti-cheat checks
        Toggle(newFloor);
    }

    public bool CanUse(IItemHolder holder, ItemObject item) => true;

    public void Use(IItemHolder holder, ItemObject item)
    {
        RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcUse, (floor + 1) % floorCount);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}
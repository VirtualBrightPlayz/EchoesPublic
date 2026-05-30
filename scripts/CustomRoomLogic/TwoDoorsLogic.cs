using System;
using System.Linq;
using Godot;

public partial class TwoDoorsLogic : Node3D, IInteractable, IDoorStatus
{
    [Export]
    public bool isOpen = false;
    public bool isMoving => doors.Any(x => x.isMoving);
    [Export]
    public Door[] doors = Array.Empty<Door>();

    public Vector3 WorldInteractPosition => GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Touch;

    public DoorStatus Status
    {
        get
        {
            if (isMoving)
                return DoorStatus.Warn;
            return DoorStatus.Ok;
        }
    }
    public float StatusPulse => isMoving ? 0.25f : 0f;
    public Action StatusChanged { get; set; }

    public override void _Ready()
    {
        if (IsMultiplayerAuthority())
        {
            SV_Toggle();
            SV_Toggle();
        }
        foreach (var door in doors)
        {
            door.StatusChanged += StatusChanged;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse()
    {
        if (isMoving)
            return;

        var senderId = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AuthorityId == senderId);
        // TODO: anit-cheat checks
        if (!IsInstanceValid(plr))
            return;
        SV_Toggle();
    }

    public void SV_SetState(bool state)
    {
        if (state != isOpen)
        {
            SV_Toggle();
        }
    }

    public void SV_Toggle()
    {
        isOpen = !isOpen;
        int i = isOpen ? 1 : 0;
        foreach (var door in doors)
        {
            door.SV_SetState(i % 2 == 0);
            i++;
        }
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return !isMoving;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        RpcId(GetMultiplayerAuthority(), nameof(RpcUse));
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}
using Godot;
using System;
using System.Linq;

public partial class TimedDoor : Node3D, IInteractable, IDoorStatus, ISpecificEventSource<IDoorEvent>
{
    public enum TimerStateEnum : int
    {
        Idle,
        NotWarned,
        Warned,
        Failed,
    }

    [Signal]
    public delegate void OnOpenedEventHandler();
    [Signal]
    public delegate void OnWarnedEventHandler();
    [Signal]
    public delegate void OnClosedEventHandler();

    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    public DoorStatus Status
    {
        get
        {
            if (IsInstanceValid(audioError) && audioError.Playing)
                return DoorStatus.Invalid;
            if (isLocked)
                return DoorStatus.Err;
            if (!isMoving && timerState == TimerStateEnum.NotWarned)
                return DoorStatus.Ok;
            if (isMoving || timerState == TimerStateEnum.Warned)
                return DoorStatus.Warn;
            return DoorStatus.Idle;
        }
    }
    public float StatusPulse
    {
        get
        {
            switch (timerState)
            {
                default:
                    return 0f;
                case TimerStateEnum.NotWarned:
                    return 1f;
                case TimerStateEnum.Warned:
                    return 0.1f;
            }
        }
    }
    public Action StatusChanged { get; set; }

    [Export]
    public Node3D marker;

    [Export]
    public Door[] doors = new Door[0];
    [Export]
    public AudioStreamPlayer3D audioToggle;
    [Export]
    public AudioStreamPlayer3D audioWarn;
    [Export]
    public AudioStreamPlayer3D audioError;
    [Export]
    public float timeToWarn = 5f;
    [Export]
    public float timeAfterWarn = 2f;
    [Export]
    public bool isLocked = false;
    [Export]
    public bool canClose = false;
    [Export]
    public Godot.Collections.Array<KeycardAccess> validCardTypes = new();
    [Export]
    public Godot.Collections.Array<PlayerRole> autoRoleTypes = new();

    public bool isMoving => doors.Any(x => x.isMoving);

    public float timer;
    public TimerStateEnum timerState;

    public override void _PhysicsProcess(double delta)
    {
        StatusChanged?.Invoke(); // TODO: move this out of physics update and make it event based
        if (timerState != TimerStateEnum.Idle && !isLocked && IsMultiplayerAuthority())
        {
            timer -= (float)delta;
            if (timer <= 0f)
            {
                switch (timerState)
                {
                    case TimerStateEnum.NotWarned:
                        timer = timeAfterWarn;
                        timerState = TimerStateEnum.Warned;
                        Rpc(nameof(CL_Toggle), (int)timerState);
                        EmitSignal(SignalName.OnWarned);
                        break;
                    case TimerStateEnum.Warned:
                        timerState = TimerStateEnum.Idle;
                        Rpc(nameof(CL_Toggle), (int)timerState);
                        foreach (var door in doors)
                        {
                            door.SV_SetState(false);
                        }
                        EmitSignal(SignalName.OnClosed);
                        break;
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse(int itemSerial)
    {
        // TODO: anti-cheat checks
        if (timerState != TimerStateEnum.Idle)
        {
            EventDoorUseFailed failed = EventManager.GetInstance<EventDoorUseFailed>();
            Emit(failed);
            return;
        }
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AuthorityId == Multiplayer.GetRemoteSenderId());
        if (!IsInstanceValid(plr))
        {
            return;
        }
        if (validCardTypes.Count == 0 || autoRoleTypes.Any(x => x.ResourceName == plr.Role.ResourceName || x.team == plr.Role.team))
        {
            SV_Toggle();
        }
        else if (ItemManager.Instance.Items.TryGetValue(itemSerial, out ItemObject item) && item.Player == plr && item.model is Keycard card)
        {
            TryUseCard(card);
        }
        else
        {
            Rpc(nameof(CL_Toggle), (int)TimerStateEnum.Failed);
        }
    }

    public void TryUseCard(Keycard card)
    {
        EventDoorTryUsing tryUsing = EventManager.GetInstance<EventDoorTryUsing>();
        tryUsing.Keycard = card;
        if (!Emit(tryUsing) || timerState != TimerStateEnum.Idle)
        {
            return;
        }
        if (timerState != TimerStateEnum.Idle && !canClose)
        {
            return;
        }
        if (card.CanAccess(validCardTypes))
        {
            SV_Toggle();
        }
        else
        {
            Rpc(nameof(CL_Toggle), (int)TimerStateEnum.Failed);
        }
        card.OnUsed(validCardTypes);
        EventDoorUsed used = EventManager.GetInstance<EventDoorUsed>();
        used.Keycard = card;
        Emit(used);
    }

    public void SV_Toggle()
    {
        if (timerState != TimerStateEnum.Idle)
        {
            EventDoorToggling toggling = EventManager.GetInstance<EventDoorToggling>();
            toggling.NewState = false;
            toggling.CurrentState = true;
            if (!Emit(toggling))
            {
                return;
            }
            timer = 0f;
            timerState = TimerStateEnum.Idle;
            Rpc(nameof(CL_Toggle), (int)timerState);
            foreach (var door in doors)
            {
                door.SV_SetState(false);
            }
            EventDoorToggled toggled = EventManager.GetInstance<EventDoorToggled>();
            toggled.CurrentState = false;
            Emit(toggled);
            return;
        }
        EventDoorToggling toggling2 = EventManager.GetInstance<EventDoorToggling>();
        toggling2.NewState = true;
        toggling2.CurrentState = false;
        if (!Emit(toggling2))
        {
            return;
        }
        timer = timeToWarn;
        timerState = TimerStateEnum.NotWarned;
        Rpc(nameof(CL_Toggle), (int)timerState);
        foreach (var door in doors)
        {
            door.SV_SetState(true);
        }
        EmitSignal(SignalName.OnOpened);
        EventDoorToggled toggled3 = EventManager.GetInstance<EventDoorToggled>();
        toggled3.CurrentState = true;
        Emit(toggled3);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void CL_Toggle(int state)
    {
        if (Multiplayer.GetRemoteSenderId() != GetMultiplayerAuthority())
            return;
        timerState = (TimerStateEnum)state;
        switch ((TimerStateEnum)state)
        {
            default:
                audioToggle?.Play();
                break;
            case TimerStateEnum.Warned:
                audioWarn?.Play();
                break;
            case TimerStateEnum.Failed:
                audioError?.Play();
                timerState = TimerStateEnum.Idle;
                break;
        }
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        EventCanUse canUse = EventManager.GetInstance<EventCanUse>();
        canUse.Holder = holder;
        canUse.Item = item;
        canUse.Result = true;
        Emit(canUse);
        return canUse.Result;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        EventUsing evtUsing = EventManager.GetInstance<EventUsing>();
        evtUsing.Holder = holder;
        evtUsing.Item = item;
        RpcId(GetMultiplayerAuthority(), nameof(RpcUse), IsInstanceValid(item) ? item.Serial : -1);
        EventUsed evtUsed = EventManager.GetInstance<EventUsed>();
        evtUsed.Item = item;
        evtUsed.Holder = holder;
        Emit(evtUsed);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        EventUseEnded evtEnded = EventManager.GetInstance<EventUseEnded>();
        evtEnded.Holder = holder;
        evtEnded.Item = item;
        Emit(evtEnded);
    }

    public bool Emit(IDoorEvent evt)
    {
        evt.DoorStatus = this;
        evt.Interactable = this;
        return Emit(evt as IEvent);
    }

    public bool Emit(IEvent evt)
    {
        return EventManager.Emit(evt, this);
    }
}

using Godot;
using System;
using System.Linq;

public partial class Door : Node3D, IInteractable, IDoorStatus, ISpecificEventSource<IDoorEvent>
{
    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    public DoorStatus Status
    {
        get
        {
            if (hasAudioCached)
                return DoorStatus.Invalid;
            if (isLocked)
                return DoorStatus.Err;
            if (isMoving)
                return DoorStatus.Warn;
            if (isOpen)
                return DoorStatus.Ok;
            return DoorStatus.Idle;
        }
    }
    public float StatusPulse => 0f;
    public Action StatusChanged { get; set; }

    [Export]
    public Node3D marker;
    [Export]
    public NodePath targetParent;
    [Export]
    public Node3D[] doors = new Node3D[0];
    [Export]
    public AnimationPlayer[] animPlayers = new AnimationPlayer[0];
    [Export]
    public StringName openAnim = "open";
    [Export]
    public StringName closeAnim = "close";
    [Export]
    public Node3D[] startPoints = new Node3D[0];
    [Export]
    public Node3D[] endPoints = new Node3D[0];
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public AudioStreamPlayer3D audioNear;
    [Export]
    public AudioStream[] openSounds = new AudioStream[0];
    [Export]
    public AudioStream[] closeSounds = new AudioStream[0];
    [Export]
    public AudioStream[] failSounds = new AudioStream[0];
    [Export]
    public float speed = 1f;
    [Export]
    public Tween.TransitionType transition = Tween.TransitionType.Sine;
    [Export]
    public Tween.EaseType ease = Tween.EaseType.InOut;
    [Export]
    public float distance = 2f;
    public float timer = 0f;
    [Export]
    public bool isOpen
    {
        get => _isOpenInternal;
        set => UpdateState(value, false);
    }
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
    [Export]
    public bool isLocked = false;
    [Export]
    public OccluderInstance3D occluder;
    [Export]
    public Godot.Collections.Array<KeycardAccess> validCardTypes = new();
    [Export]
    public Godot.Collections.Array<PlayerRole> autoRoleTypes = new();
    public Action OnToggled = () => { };
    public Tween tween = null;
    public bool wasMoving = false;
    public IInteractable targetInteract => GetNodeOrNull<IInteractable>(targetParent);
    private bool hasAudioCached = false;
    private bool wasOccluderVisible = false;

    private bool _isOpenInternal = false;
    private bool _isMovingInternal = true;

    public void SetProgress(float amount)
    {
        if (!IsNodeReady())
            return;
        for (int i = 0; i < doors.Length; i++)
        {
            if (i >= startPoints.Length || i >= endPoints.Length || !IsInstanceValid(doors[i]))
            {
                continue;
            }
            doors[i].Position = startPoints[i].Position.Lerp(endPoints[i].Position, amount);
        }
        timer = amount;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    public override void _Ready()
    {
        // hasAudioCached = IsInstanceValid(audioNear);
        hasAudioCached = false;
        if (IsInstanceValid(occluder))
            wasOccluderVisible = occluder.Visible;
        UpdateState(isOpen, true);
    }

    private void EndMoving()
    {
        isMoving = false;
    }

    public void UpdateState(bool targetOpenState, bool force)
    {
        EventDoorUpdatingState uState = EventManager.GetInstance<EventDoorUpdatingState>();
        uState.TargetState = targetOpenState;
        uState.Force = force;
        if (!Emit(uState))
        {
            return;
        }
        targetOpenState = uState.TargetState;
        force = uState.Force;
        if (_isOpenInternal == targetOpenState && !force)
        {
            return;
        }
        _isOpenInternal = targetOpenState;
        if (!IsNodeReady())
        {
            return;
        }
        StatusChanged?.Invoke();
        bool shouldMove = (Mathf.IsEqualApprox(timer, 0f) && targetOpenState) || (Mathf.IsEqualApprox(timer, 1f) && !targetOpenState);
        isMoving = true;
        foreach (var anim in animPlayers)
        {
            if (targetOpenState)
            {
                anim.Play(openAnim);
            }
            else
            {
                anim.Play(closeAnim);
            }
        }
        if (IsInstanceValid(tween))
        {
            tween.Kill();
        }
        tween = CreateTween();
        tween.SetProcessMode(Tween.TweenProcessMode.Physics);
        tween.Finished += EndMoving;
        tween.SetTrans(transition);
        tween.SetEase(ease);
        {
            if (targetOpenState)
            {
                tween.TweenMethod(Callable.From<float>(SetProgress), 0f, 1f, 1d / speed);
            }
            else
            {
                tween.TweenMethod(Callable.From<float>(SetProgress), 1f, 0f, 1d / speed);
            }
        }
        wasMoving = isMoving;
        bool isVisible = !targetOpenState && !isMoving;
        if (IsInstanceValid(occluder) && wasOccluderVisible != isVisible)
        {
            occluder.Visible = isVisible;
            wasOccluderVisible = isVisible;
        }
        EventDoorUpdatedState udState = EventManager.GetInstance<EventDoorUpdatedState>();
        udState.TargetState = targetOpenState;
        udState.Force = force;
        Emit(udState);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse(int itemSerial)
    {
        if (isMoving)
            return;
        if (isLocked)
        {
            Rpc(nameof(RpcToggleFailed));
            EventDoorUseFailed failed = EventManager.GetInstance<EventDoorUseFailed>();
            Emit(failed);
            return;
        }

        var senderId = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AuthorityId == senderId);
        // TODO: anit-cheat checks
        if (!IsInstanceValid(plr))
            return;
        if (validCardTypes.Count == 0 || autoRoleTypes.Any(x => x.ResourceName == plr.Role.ResourceName || x.team == plr.Role.team))
            SV_Toggle();
        else if (ItemManager.Instance.Items.TryGetValue(itemSerial, out ItemObject item) && item.Player == plr && item.model is Keycard card)
        {
            if (card.CanAccess(validCardTypes))
            {
                SV_Toggle();
            }
            else
            {
                Rpc(nameof(RpcToggleFailed));
                EventDoorUseFailed failed = EventManager.GetInstance<EventDoorUseFailed>();
                failed.Player = plr;
                Emit(failed);
            }
            card.OnUsed(validCardTypes);
        }
        else
        {
            Rpc(nameof(RpcToggleFailed));
            EventDoorUseFailed failed = EventManager.GetInstance<EventDoorUseFailed>();
            failed.Player = plr;
            Emit(failed);
        }

        /*
        if (validCardTypes.Count == 0 || (plr!.EquippedItem is Keycard card && card.CanAccess(validCardTypes)) || autoRoleTypes.Contains(plr.Role))
            SV_Toggle();
        else
            Rpc(nameof(RpcToggleFailed));
        if (plr.EquippedItem is Keycard card2 && validCardTypes.Count != 0)
            card2.Use(plr, validCardTypes);
        */
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcToggled(bool isOpen2)
    {
        if (Multiplayer.GetRemoteSenderId() != GetMultiplayerAuthority())
            return;
        if (audio == null)
            return;
        if (!audio.HasStreamPlayback())
            audio.Stream = new AudioStreamPolyphonic();
        if (!audio.Playing)
            audio.Play();
        if (audio.HasStreamPlayback() && audio.GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
        {
            if (isOpen2)
                playback.PlayStream(openSounds[GD.Randi() % openSounds.Length]);
            else
                playback.PlayStream(closeSounds[GD.Randi() % closeSounds.Length]);
        }
        EventDoorToggled toggled = EventManager.GetInstance<EventDoorToggled>();
        toggled.CurrentState = isOpen2;
        Emit(toggled);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public async void RpcToggleFailed()
    {
        if (Multiplayer.GetRemoteSenderId() != GetMultiplayerAuthority())
            return;
        if (failSounds.Length == 0)
            return;
        if (audioNear == null)
            return;
        audioNear.Stream = failSounds[GD.Randi() % failSounds.Length];
        audioNear.Play();
        hasAudioCached = true;
        StatusChanged?.Invoke();
        await ToSignal(audioNear, AudioStreamPlayer3D.SignalName.Finished);
        hasAudioCached = false;
        StatusChanged?.Invoke();
        EventDoorUseFailed failed = EventManager.GetInstance<EventDoorUseFailed>();
        Emit(failed);
    }

    public void TryUseCard(Keycard card)
    {
        EventDoorTryUsing tryUsing = EventManager.GetInstance<EventDoorTryUsing>();
        tryUsing.Keycard = card;
        if (!Emit(tryUsing))
        {
            return;
        }
        if (isMoving)
            return;
        if (isLocked)
        {
            Rpc(nameof(RpcToggleFailed));
            return;
        }
        if (card.CanAccess(validCardTypes))
        {
            SV_Toggle();
        }
        else
            Rpc(nameof(RpcToggleFailed));
        card.OnUsed(validCardTypes);
        EventDoorUsed used = EventManager.GetInstance<EventDoorUsed>();
        used.Keycard = card;
        Emit(used);
    }

    public void SV_SetState(bool state)
    {
        if (state != isOpen)
        {
            SV_Toggle();
        }
    }

    public void SV_SetLocked(bool state)
    {
        EventDoorLockedStateUpdating lUpdating = EventManager.GetInstance<EventDoorLockedStateUpdating>();
        lUpdating.LockedCurrent = isLocked;
        lUpdating.LockedNext = state;
        if (!Emit(lUpdating))
        {
            return;
        }
        isLocked = lUpdating.LockedNext;
        EventDoorLockedStateUpdated lUpdated = EventManager.GetInstance<EventDoorLockedStateUpdated>();
        lUpdated.Locked = state;
        Emit(lUpdated);
    }

    public void SV_Toggle()
    {
        EventDoorToggling toggling = EventManager.GetInstance<EventDoorToggling>();
        toggling.NewState = !isOpen;
        toggling.CurrentState = isOpen;
        if (!Emit(toggling))
        {
            return;
        }
        isOpen = !isOpen;
        isMoving = true;
        OnToggled?.Invoke();
        Rpc(nameof(RpcToggled), isOpen);
        EventDoorToggled toggled = EventManager.GetInstance<EventDoorToggled>();
        toggled.CurrentState = isOpen;
        Emit(toggled);
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        bool result = !isLocked;
        if (targetInteract != null)
            result = targetInteract.CanUse(holder, item);
        if (isMoving)
            result = false;
        EventCanUse canUse = EventManager.GetInstance<EventCanUse>();
        canUse.Holder = holder;
        canUse.Item = item;
        canUse.Result = result;
        Emit(canUse);
        return canUse.Result;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        EventUsing evtUsing = EventManager.GetInstance<EventUsing>();
        evtUsing.Holder = holder;
        evtUsing.Item = item;
        if (!Emit(evtUsing))
        {
            return;
        }
        if (targetInteract != null)
            targetInteract.Use(holder, item);
        else
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

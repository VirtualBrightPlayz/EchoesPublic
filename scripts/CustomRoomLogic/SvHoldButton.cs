using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class SvHoldButton : StaticBody3D, IInteractable
{
    public enum State : byte
    {
        Idle = 0,
        Using = 1,
    }

    [Signal]
    public delegate void OnUsedEventHandler();
    [Signal]
    public delegate void OnPlayerUsedEventHandler(NetworkPlayer player);

    [Export]
    public double timeToUse = 0d;
    [Export]
    public NodePath target;
    [Export]
    public State state = State.Idle;

    public double useTimer;
    public NetworkPlayer usePlayer;

    public Vector3 WorldInteractPosition => GlobalPosition;

    public IInteractable.InteractType ActionType => IInteractable.InteractType.Touch;

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return true;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        Rpc(MethodName.RpcUse);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        Rpc(MethodName.RpcUseEnd);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsMultiplayerAuthority())
        {
            switch (state)
            {
                case State.Idle:
                    break;
                case State.Using:
                {
                    if (IsInstanceValid(usePlayer))
                    {
                        useTimer -= delta;
                        if (useTimer <= 0d)
                        {
                            SV_Use();
                        }
                    }
                    else
                    {
                        usePlayer = null;
                        state = State.Idle;
                    }
                    break;
                }
            }
        }
    }

    private void SV_Use()
    {
        Node interact = GetNodeOrNull(target);
        if (IsInstanceValid(interact))
        {
            switch (interact)
            {
                case Door door:
                    door.SV_Toggle();
                    break;
                case TwoDoorsLogic door:
                    door.SV_Toggle();
                    break;
            }
        }
        EmitSignalOnUsed();
        EmitSignalOnPlayerUsed(usePlayer);
        usePlayer = null;
        state = State.Idle;
    }    

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse()
    {
        if (!IsMultiplayerAuthority())
            return;
        // TODO: check distance to player
        int remoteSender = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => remoteSender == x.GetMultiplayerAuthority());
        if (state == State.Idle)
        {
            useTimer = timeToUse;
            usePlayer = plr;
            if (timeToUse <= 0d)
                SV_Use();
            else
                state = State.Using;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUseEnd()
    {
        if (!IsMultiplayerAuthority())
            return;
        int remoteSender = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => remoteSender == x.GetMultiplayerAuthority());
        if (usePlayer == plr && state == State.Using)
        {
            usePlayer = null;
        }
    }
}

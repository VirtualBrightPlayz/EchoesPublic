using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ControlPoint : StaticBody3D, IInteractable
{
    public enum State : byte
    {
        Idle = 0,
        Using = 1,
        Capturing = 2,
    }

    public enum Status : byte
    {
        Idle = 0,
        Locked = 1,
        Contested = 2,
    }

    [Export]
    public ZoneArea.Zone zone = ZoneArea.Zone.Unknown;
    [Export]
    public Area3D area;
    [Export]
    public double timeToUse = 1d;
    public double timeToCapture => IsInstanceValid(RoundManager.Instance) ? RoundManager.Instance.Logic.Config.ControlPointCaptureTime : 90d;
    [Export]
    public State state = State.Idle;
    [Export]
    public TeamID owningTeam = TeamID.Dead;
    [Export]
    public TeamID capturingTeam = TeamID.Dead;
    [Export]
    public bool areaLocked = false;
    [Export]
    public bool ableToCapture = false;
    public bool wasAbleToCapture = false;
    [Export]
    public AudioStreamPlayer3D audioAlarm;

    public double useTimer;
    public NetworkPlayer usePlayer;

    public double captureTimer;

    public Vector3 WorldInteractPosition => GlobalPosition;

    public IInteractable.InteractType ActionType => IInteractable.InteractType.Touch;

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return holder.GetPlayer().Role.CanCapturePoints;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        Rpc(MethodName.RpcUse);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        Rpc(MethodName.RpcUseEnd);
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (IsInstanceValid(ControlPointManager.Instance))
        {
            ControlPointManager.Instance.Points.Add(this);
        }
        if (IsInstanceValid(area))
        {
            area.BodyEntered += _BodyEntered;
            area.BodyExited += _BodyExited;
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (IsInstanceValid(ControlPointManager.Instance))
        {
            ControlPointManager.Instance.Points.Remove(this);
        }
        if (IsInstanceValid(area))
        {
            area.BodyEntered -= _BodyEntered;
            area.BodyExited -= _BodyExited;
        }
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
                    if (IsInstanceValid(usePlayer) && usePlayer.Role.CanCapturePoints)
                    {
                        useTimer -= delta;
                        if (useTimer <= 0d)
                        {
                            capturingTeam = usePlayer.Role.team;
                            captureTimer = timeToCapture;
                            usePlayer = null;
                            state = State.Capturing;
                            Rpc(MethodName.RpcAlarm);
                            BroadcastToAll($"The {ControlPointLabel.TranslateZone(zone)} zone is being captured!", 5d);
                        }
                    }
                    else
                    {
                        usePlayer = null;
                        state = State.Idle;
                    }
                    break;
                }
                case State.Capturing:
                {
                    wasAbleToCapture = ableToCapture;
                    if (ControlPointManager.Instance.IsAnyPlayerInZone(zone, capturingTeam))
                    {
                        ableToCapture = ControlPointManager.Instance.IsAbleToCapture(zone, capturingTeam);
                        if (ableToCapture)
                        {
                            captureTimer -= delta;
                        }
                        else if (wasAbleToCapture)
                        {
                            BroadcastToTeam(capturingTeam, $"Enemies detected in {ControlPointLabel.TranslateZone(zone)} zone!", 5d);
                        }
                        if (captureTimer <= 0d)
                        {
                            owningTeam = capturingTeam;
                            capturingTeam = TeamID.Dead;
                            state = State.Idle;
                            areaLocked = true;
                            BroadcastToAll($"The {ControlPointLabel.TranslateZone(zone)} has been captured by {ControlPointLabel.TranslateTeam(owningTeam)}", 5d);
                            ControlPointManager.Instance.CheckForEnd();
                        }
                    }
                    else
                    {
                        owningTeam = TeamID.Dead;
                        capturingTeam = TeamID.Dead;
                        state = State.Idle;
                        areaLocked = false;
                    }
                    break;
                }
            }
        }
    }

    public static void BroadcastToTeam(TeamID team, string message, double time)
    {
        if (IsInstanceValid(RoundManager.Instance))
        {
            var players = RoundManager.Instance.players.Players;
            foreach (var plr in players)
            {
                if (plr.Role.team == team && plr.Role.CanCapturePoints)
                {
                    plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ShowHint, message, time);
                    // RoundManager.Instance.RpcId(plr.GetMultiplayerAuthority(), RoundManager.MethodName.RpcBroadcast, message, time);
                }
            }
        }
    }

    public static void BroadcastToAll(string message, double time)
    {
        if (IsInstanceValid(RoundManager.Instance))
        {
            var players = RoundManager.Instance.players.Players;
            foreach (var plr in players)
            {
                plr.RpcId(plr.GetMultiplayerAuthority(), BasePlayer.MethodName.CL_ShowHint, message, time);
            }
            // RoundManager.Instance.Rpc(RoundManager.MethodName.RpcBroadcast, message, time);
        }
    }

    private void _BodyEntered(Node3D body)
    {
    }

    private void _BodyExited(Node3D body)
    {
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcAlarm()
    {
        if (IsInstanceValid(audioAlarm))
        {
            audioAlarm.Play();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcCaptured(TeamID team)
    {
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse()
    {
        if (!IsMultiplayerAuthority())
            return;
        // TODO: check distance to player
        int remoteSender = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => remoteSender == x.GetMultiplayerAuthority());
        if (plr.Role.CanCapturePoints && state != State.Using && capturingTeam != plr.Role.team && !areaLocked)
        {
            useTimer = timeToUse;
            usePlayer = plr;
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

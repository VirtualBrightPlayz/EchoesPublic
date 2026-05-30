using System;
using Godot;

[GlobalClass]
public partial class PlayerSeat : StaticBody3D, IInteractable, IPlayerController
{
    [Export]
    public VehicleSync Vehicle;
    [Export]
    public Camera3D camera;
    [Export]
    public Node3D exitPoint;
    [Export]
    public NodePath PlayerPath
    {
        get => IsInstanceValid(Player) ? this.GetMultiplayerRoot().GetPathTo(Player) : new NodePath();
        set => Player = this.GetMultiplayerRoot().GetNodeOrNull<BasePlayer>(value);
    }

    public Node3D Floor => this;
    public Node3D View => camera;
    public Camera3D Camera => camera;
    public BasePlayer Player { get; set; }

    private bool justEntered;
    private ButtonInputFlags ButtonUse;
    private Vector2 MouseMotion => Player.GetStickPadActionData(LocalPlayerInput.PlayerCamera) * Settings.User.MouseSensitivity;

    public Vector3 WorldInteractPosition => GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return holder.GetPlayer().TryGetAbility(out UseAbility _) && holder.GetPlayer().TryGetAbility(out GrabbingAbility _);
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        SendEnterSeat(holder.GetPlayer());
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        SetMeta(IInteractable.META_NAME, this);
        if (IsInstanceValid(Vehicle))
        {
            Vehicle.AddCollisionExceptionWith(this);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        IPlayerController controller = this;
        if (controller.IsControllerActive && Player.IsLocalPlayer)
        {
            camera.Rotation = new Vector3(Mathf.Clamp(camera.Rotation.X + MouseMotion.Y, -Mathf.Pi / 2f, Mathf.Pi / 2f), Mathf.Clamp(camera.Rotation.Y + MouseMotion.X, -Mathf.Pi / 2f, Mathf.Pi / 2f), 0f);
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        IPlayerController controller = this;
        if (controller.IsControllerActive && Player.IsLocalPlayer)
        {
            InputManager.UpdateInput(Player, LocalPlayerInput.PlayerUse, ref ButtonUse);
            if (!justEntered)
            {
                if (ButtonUse.HasFlag(ButtonInputFlags.JustPressed))
                {
                    SendExitSeat(Player, Vehicle.EngineForce, Vehicle.Steering);
                }
            }
            justEntered = false;
        }
        else
        {
            camera.Rotation = Vector3.Zero;
            justEntered = true;
        }
    }

    public void SendEnterSeat(BasePlayer player)
    {
        Rpc(MethodName.Rpc_EnterSeat, GetPathTo(player));
    }

    public void SendExitSeat(BasePlayer player, float engine, float steering)
    {
        Rpc(MethodName.Rpc_ExitSeat, GetPathTo(player), engine, steering);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Rpc_EnterSeat(NodePath playerPath)
    {
        BasePlayer player = GetNodeOrNull<BasePlayer>(playerPath);
        if (IsInstanceValid(player) && player.CallerHasAuthority && !IsInstanceValid(Player))
        {
            SV_EnterSeat(player);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void Rpc_ExitSeat(NodePath playerPath, float engine, float steering)
    {
        BasePlayer player = GetNodeOrNull<BasePlayer>(playerPath);
        if (IsInstanceValid(player) && player.CallerHasAuthority && player == Player)
        {
            SV_ExitSeat(player, engine, steering);
        }
    }

    // target is NOT optional
    public void SV_EnterSeat(BasePlayer target)
    {
        if (IsInstanceValid(target))
        {
            Player = target;
            target.SV_SetActiveController(this);
            if (Vehicle.driverSeat == this)
            {
                Vehicle.xformSync.SetClaimant(target.GetMultiplayerAuthority());
            }
        }
    }

    // target is optional
    public void SV_ExitSeat(BasePlayer target, float engine, float steering)
    {
        if (Vehicle.driverSeat == this)
        {
            Vehicle.xformSync.SetClaimant((int)MultiplayerPeer.TargetPeerBroadcast);
            Vehicle.EngineForce = engine;
            Vehicle.Steering = steering;
        }
        if (IsInstanceValid(target))
        {
            target.SV_SetActiveController(null);
        }
        Player = null;
        target.ForceTeleportPosition(exitPoint.GlobalPosition);
    }
}

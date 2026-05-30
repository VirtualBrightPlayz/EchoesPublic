using System;
using Godot;

public partial class SCP914Key : StaticBody3D//, IInteractable
{
    public Vector3 WorldInteractPosition => GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Drag;

    [Export]
    public SCP914Knob knob;
    [Export]
    public Lever knobLever;
    [Export]
    public Lever keyLever;
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public Area3D intakeArea;
    [Export]
    public Node3D outputArea;
    [Export]
    public double timeToCloseDoors;
    [Export]
    public double timeToUpgrade;
    [Export]
    public double timeAfterUpgrade;
    public bool isMoving = false;
    [Export]
    public Door[] doors = Array.Empty<Door>();

    public async void Next()
    {
        if (!isMoving)
        {
            Rpc(nameof(RpcSound));
            isMoving = true;
            Rpc(MethodName.RpcLocked, true);
            keyLever.SendSetValue(1f);
            await ToSignal(GetTree().CreateTimer(timeToCloseDoors), SceneTreeTimer.SignalName.Timeout);
            foreach (var door in doors)
            {
                door.SV_SetState(false);
            }
            await ToSignal(GetTree().CreateTimer(timeToUpgrade - timeToCloseDoors), SceneTreeTimer.SignalName.Timeout);
            foreach (var body in intakeArea.GetOverlappingBodies())
            {
                try
                {
                    if (body is WorldItem wi)
                    {
                        ItemObject outputItem = ItemManager.Instance.UpgradeItem(wi, knob.Setting, out bool destroy);
                        if (destroy)
                            wi.Item.QueueFreeNow();
                        else if (!IsInstanceValid(outputItem))
                        {
                            wi.GlobalPosition -= intakeArea.GlobalPosition - outputArea.GlobalPosition;
                        }
                        else
                        {
                            Node3D output = outputItem.model;
                            output.GlobalPosition = wi.GlobalPosition;
                            output.GlobalRotation = wi.GlobalRotation;
                            outputItem.ModelEnabled = true;
                            wi.Item.QueueFreeNow();
                            output.GlobalPosition -= intakeArea.GlobalPosition - outputArea.GlobalPosition;
                        }
                    }
                    if (body is IPlayerController ctrl)
                    {
                        ctrl.Player.Rpc(nameof(NetworkPlayer.Teleport), ctrl.Player.PlayerPosition - (intakeArea.GlobalPosition - outputArea.GlobalPosition), ctrl.Player.PlayerRotation);
                    }
                }
                catch (Exception e)
                {
                    Log.PrintErr(e);
                }
            }
            foreach (var door in doors)
            {
                door.SV_SetState(true);
            }
            await ToSignal(GetTree().CreateTimer(timeAfterUpgrade), SceneTreeTimer.SignalName.Timeout);
            isMoving = false;
            Rpc(MethodName.RpcLocked, false);
            keyLever.SendSetValue(0f);
        }
    }

    public void StartUpgrades(int val)
    {
        if (IsMultiplayerAuthority() && val > 0)
        {
            Next();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcNext()
    {
        // TODO: AC checks
        if (IsMultiplayerAuthority())
        {
            // Next();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcSound()
    {
        audio.Play();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RpcLocked(bool state)
    {
        knobLever.SetLocked(state);
        keyLever.SetLocked(state);
    }

    public bool CanUse(IItemHolder holder, ItemObject item) => !knobLever.Locked;

    public void Use(IItemHolder holder, ItemObject item)
    {
        // RpcId(1, nameof(RpcNext));
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}
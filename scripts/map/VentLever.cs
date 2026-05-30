using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class VentLever : StaticBody3D, IInteractable
{
    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;

    [Export]
    public Node3D marker;

    [Export]
    public string VentKey;
    [Export]
    public Vector3 unlockedRot;
    [Export]
    public Vector3 lockedRot;
    [Export]
    public Node3D leverHandle;
    [Export]
    public float speed = 1f;
    public Vent connected;
    [Export]
    public GameSound sound;
    [Export]
    public Label3D label;
    [Export]
    public bool startLocked = false;

    public override void _Process(double delta)
    {
        Find();
        if (!IsInstanceValid(connected))
            return;
        if (IsInstanceValid(label))
        {
            label.Text = $"{VentKey} Vent {(connected.locked ? "Locked" : "Unlocked")}";
            label.Modulate = label.Modulate.Lerp(connected.locked ? Colors.Red : Colors.Green, (float)delta * speed);
        }
        leverHandle.RotationDegrees = leverHandle.RotationDegrees.Lerp(connected.locked ? lockedRot : unlockedRot, (float)delta * speed);
    }

    public void Find()
    {
        if (IsInstanceValid(connected))
            return;
        connected = Vent.All.FirstOrDefault(x => x.VentKey == VentKey);
        if (IsInstanceValid(connected))
        {
            connected.locked = startLocked;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_Toggle()
    {
        Find();
        if (!IsInstanceValid(connected))
            return;
        var multiplayerRemoteSenderId = Multiplayer.GetRemoteSenderId();
        var player = IPlayerList.List(this).PlayerList.FirstOrDefault(x => multiplayerRemoteSenderId == x.GetMultiplayerAuthority());
        if (player == null)
            return;
        connected.locked = !connected.locked;
        player.Rpc(nameof(NetworkPlayer.CL_PlaySoundAt3D), player.Data.Sounds.IndexOf(sound), GlobalPosition);
    }

    public bool CanUse(IItemHolder holder, ItemObject item) => true;

    public void Use(IItemHolder holder, ItemObject item)
    {
        RpcId(1, nameof(SV_Toggle));
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}

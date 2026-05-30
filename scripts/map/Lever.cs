using System;
using Godot;

[GlobalClass]
public partial class Lever : Node3D, IInteractable
{
    [Signal]
    public delegate void ValueSnappedEventHandler(int value);
    [Signal]
    public delegate void OnUseEndEventHandler();

    [Export]
    public StaticBody3D collision;
    [Export]
    public Node3D visuals;
    [Export]
    public bool Locked = false;
    [Export]
    public Node3D[] Presets = [];
    [Export]
    public Vector2 MouseAxis = Vector2.Down;
    [Export]
    public float Value = 0f;
    [Export]
    public float SnapSpeed = 10f;
    [Export]
    public bool UseView = false;

    public bool InUse = false;
    public bool InUseLocal = false;
    public float sign = 1f;

    public Vector2 MouseMotion => InputManager.Instance.GetStickPadActionData(LocalPlayerInput.PlayerCamera);

    public Vector3 WorldInteractPosition => GlobalPosition;

    public IInteractable.InteractType ActionType => IInteractable.InteractType.DragCapture;

    public override void _Ready()
    {
        base._Ready();
        collision.SetMeta(IInteractable.META_NAME, this);
        Multiplayer.PeerConnected += _Joined;
        Rpc(MethodName.RpcEndUse, Value);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Multiplayer.PeerConnected -= _Joined;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (InUse && !Locked)
        {
            // var valueDelta = (MouseAxis * MouseMotion);
            if (InUseLocal)
            {
                int oldTargetIndex = Mathf.RoundToInt(Value);
                float valueSign = MouseAxis.Dot(MouseMotion.Normalized()) * MouseMotion.Length();
                if (UseView)
                {
                    valueSign *= sign;
                }
                float newValue = Mathf.Clamp(Value + valueSign, 0, Presets.Length - 1);
                int targetIndex = Mathf.RoundToInt(newValue);
                // if (targetIndex != oldTargetIndex)
                {
                    Rpc(MethodName.RpcValue, newValue);
                }
                Value = newValue;
            }
            var target1 = Presets[Mathf.FloorToInt(Value)];
            var target2 = Presets[Mathf.CeilToInt(Value)];
            float fract = Value % 1f;
            visuals.Position = target1.Position.Lerp(target2.Position, fract);
            visuals.Quaternion = target1.Quaternion.Slerp(target2.Quaternion, fract);
        }
        else
        {
            var target = Presets[Mathf.RoundToInt(Value)];
            visuals.Position = visuals.Position.Lerp(target.Position, (float)delta * SnapSpeed);
            visuals.Quaternion = visuals.Quaternion.Slerp(target.Quaternion, (float)delta * SnapSpeed);
        }
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        if (holder.GetPlayer().IsLocalPlayer && !InUse && !Locked)
        {
            InUse = true;
            InUseLocal = true;
            sign = Mathf.Sign(visuals.GlobalPosition.DirectionTo(holder.HolderTransform.Origin).Dot(GlobalBasis.X));
        }
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        if (holder.GetPlayer().IsLocalPlayer && InUse)
        {
            // Value = Mathf.Round(Value);
            Rpc(MethodName.RpcEndUse, Value);
            InUse = false;
            InUseLocal = false;
        }
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return !InUse && !Locked;
    }

    private void _Joined(long id)
    {
        RpcId(id, MethodName.RpcEndUse, Value);
    }

    public void SendSetValue(float val)
    {
        Rpc(MethodName.RpcSetValue, val);
    }

    public void SetLocked(bool val)
    {
        Locked = val;
        if (val)
        {
            InUse = false;
            InUseLocal = false;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RpcValue(float value)
    {
        int oldTargetIndex = Mathf.RoundToInt(Value);
        Value = value;
        int targetIndex = Mathf.RoundToInt(Value);
        if (targetIndex != oldTargetIndex)
        {
            EmitSignalValueSnapped(targetIndex);
        }
        InUse = true;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcEndUse(float value)
    {
        int oldTargetIndex = Mathf.RoundToInt(Value);
        Value = Mathf.Round(value);
        int targetIndex = Mathf.RoundToInt(Value);
        if (targetIndex != oldTargetIndex)
        {
            EmitSignalValueSnapped(targetIndex);
        }
        InUse = false;
        EmitSignalOnUseEnd();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcSetValue(float value)
    {
        Value = Mathf.Round(value);
    }
}
using System;
using Godot;

public partial class SCP914Knob : StaticBody3D//, IInteractable
{
    public enum KnobSetting : byte
    {
        Min = 0,
        Rough = Min,
        Coarse,
        OneToOne,
        Fine,
        VeryFine,
        Max = VeryFine,
    }

    public Vector3 WorldInteractPosition => GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;

    [Export]
    public byte knob;
    [Export]
    public Node3D root;
    [Export]
    public float[] Rotations = Array.Empty<float>();
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public SCP914Key key;

    public KnobSetting Setting
    {
        get => (KnobSetting)knob;
        set => knob = (byte)value;
    }

    public override void _PhysicsProcess(double delta)
    {
        return;
        if (knob < 0 || knob >= Rotations.Length)
            return;
        root.RotationDegrees = root.RotationDegrees.Lerp(new Vector3(0f, 0f, Rotations[knob]), (float)delta * 10f);
    }

    public void OnValue(int val)
    {
        if (IsMultiplayerAuthority())
        {
            knob = (byte)val;
        }
    }

    public void Next()
    {
        if (key.isMoving)
            return;
        if (knob + 1 > (int)KnobSetting.Max)
        {
            knob = (int)KnobSetting.Min;
        }
        else
        {
            knob++;
        }
        Rpc(nameof(RpcSound));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcNext()
    {
        // TODO: AC checks
        if (IsMultiplayerAuthority())
        {
            // Next();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcSound()
    {
        // audio.Play();
    }

    public bool CanUse(IItemHolder holder, ItemObject item) => true;

    public void Use(IItemHolder holder, ItemObject item)
    {
        // RpcId(1, nameof(RpcNext));
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}
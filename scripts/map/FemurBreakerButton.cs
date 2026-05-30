using Godot;
using System;
using System.Collections.Generic;

public partial class FemurBreakerButton : StaticBody3D, IInteractable
{
    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    [Export]
    public Node3D marker;
    [Export]
    public FemurBreaker breaker;

    public bool CanUse(IItemHolder holder, ItemObject item) => true;

    public void Use(IItemHolder holder, ItemObject item)
    {
        breaker.Rpc(nameof(FemurBreaker.RpcRun));
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}

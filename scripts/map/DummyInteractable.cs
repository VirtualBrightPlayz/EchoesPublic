using Godot;

[GlobalClass]
public partial class DummyInteractable : Node3D, IInteractable
{
    public Vector3 WorldInteractPosition => GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return false;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}
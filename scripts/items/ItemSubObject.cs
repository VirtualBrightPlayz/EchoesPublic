using Godot;

[GlobalClass]
public partial class ItemSubObject : RigidBody3D, IInteractable
{
    public Vector3 WorldInteractPosition => GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;
    
    [Export]
    public Marker3D marker;
    [Export]
    public Node3D leftMarker;
    [Export]
    public Node3D rightMarker;

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return holder is VRPhysicsHand;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        if (holder is VRPhysicsHand hand)
        {
            // hand.GrabSubItem(this);
        }
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}
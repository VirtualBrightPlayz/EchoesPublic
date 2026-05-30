using System;
using Godot;

public partial class InventorySocket : StaticBody3D, IItemHolder, IInteractable
{
    public NetworkPlayer Player => NetworkPlayer.LocalInstance;
    public bool IsSocket => true;
    public ButtonInputFlags InputPrimary => ButtonInputFlags.None;
    public CollisionObject3D MainCollider => this;
    public Transform3D HolderTransform => GlobalTransform;
    public Transform3D AimTransform => GlobalTransform;
    public bool IsAimingDown => false;
    public Vector3 WorldInteractPosition => GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;
    public BasePlayer GetPlayer() => Player;

    [Export]
    public float size = 1f;

    public ItemObject grabbedItem;

    public override void _Ready()
    {
        VisibilityChanged += OnVisChanged;
    }

    public override void _PhysicsProcess(double delta)
    {
        SetPhysics(IsVisibleInTree());
    }

    private void OnVisChanged()
    {
        // SetPhysics(Visible);
        if (IsInstanceValid(grabbedItem))
            grabbedItem.CL_SetModelState(IsVisibleInTree());
    }

    public void SetPhysics(bool value)
    {
        foreach (var ownerId in GetShapeOwners())
        {
            if (ShapeOwnerGetOwner((uint)ownerId) is CollisionShape3D shape3D)
            {
                shape3D.Disabled = !value;
            }
        }
    }

    public void PickupWorldItem(int serial, bool grabbing)
    {
        if (IsInstanceValid(grabbedItem))
        {
            grabbedItem.GripRelease(this);
            grabbedItem.model.Scale = Vector3.One;
            RemoveCollisionExceptionWith(grabbedItem.model);
            if (!grabbing)
                grabbedItem.Release(GlobalPosition, GlobalRotation);
        }
        grabbedItem = null;
        if (ItemManager.Instance.Items.TryGetValue(serial, out ItemObject item))
        {
            item.Grab(true);
            item.GripGrab(this);
            item.CL_SetModelState(true);
            if (item.model is WorldItem world)
            {
                Aabb aabb = world.GetAabb();
                float s = size / aabb.GetLongestAxisSize();
                world.Scale = Vector3.One * Mathf.Abs(s);
            }
            AddCollisionExceptionWith(item.model);
            grabbedItem = item;
            /*
            PackedScene scn = item.ItemScene;
            if (!IsInstanceValid(scn))
                return;
            grabbedItem = scn.Instantiate<WorldItem>();
            grabbedItem.Item = item;
            grabbedItem.Freeze = true;
            grabbedItem.DoSync = false;
            grabbedItem.Position = Vector3.Zero;
            grabbedItem.Rotation = Vector3.Zero;
            AddChild(grabbedItem);
            Aabb aabb = grabbedItem.GetAabb();
            float s = size / aabb.GetLongestAxisSize();
            grabbedItem.Scale = Vector3.One * Mathf.Abs(s);
            */
        }
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        if (holder is VRPhysicsHand hand && IsInstanceValid(hand) && IsInstanceValid(grabbedItem))
        {
            // PickupWorldItem(-1, true);
            grabbedItem.GripRelease(this);
            grabbedItem.model.Scale = Vector3.One;
            RemoveCollisionExceptionWith(grabbedItem.model);
            hand.PickupWorldItem(grabbedItem.Serial);
            grabbedItem = null;
        }
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return true;
    }
}
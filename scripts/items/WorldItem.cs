using Godot;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class WorldItem : RigidbodySync, IInteractable, IElevatorTeleport, ISpecificEventSource<IWorldItemEvent>, ISpecificEventSource<IInteractableEvent>
{
    public const float UpdateInterval = 0.1f;

    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;

    [Export]
    public Node3D marker;
    [Export]
    public ItemType type = ItemType.Generic;
    // [Export]
    // public TransformSync sync;

    private ItemObject _itemCached = null;
    public ItemObject Item
    {
        get
        {
            _itemCached ??= GetParent<ItemObject>();
            return _itemCached;
        }
    }

    [ExportGroup("Sync")]
    // [Export]
    // public MultiplayerSynchronizer Synchronizer;
    [Export]
    public float lerpSpeed = 10f;
    [Export]
    public Vector3 ItemPosition;
    [Export]
    public Vector3 ItemRotation;

    private bool lastSleeping = false;

    public bool ShouldFreeze = false; // TODO: remove this hacky workaround

    protected Godot.Collections.Array<Node> visuals = new Godot.Collections.Array<Node>();
    private uint lastLayers = 0;
    private bool lastCollisionPhys = false;

    public void SetCollisionPhysics(bool value)
    {
        if (lastCollisionPhys != value)
        {
            lastCollisionPhys = value;
            foreach (var ownerId in GetShapeOwners())
            {
                if (ShapeOwnerGetOwner((uint)ownerId) is CollisionShape3D shape3D && shape3D.Disabled == value)
                {
                    shape3D.Disabled = !value;
                    // shape3D.SetDeferred(CollisionShape3D.PropertyName.Disabled, !value);
                }
            }
        }
    }

    public void SetFreezePhysics(bool value)
    {
        if (Freeze != value)
        {
            Freeze = value;
            foreach (var ownerId in GetShapeOwners())
            {
                if (ShapeOwnerGetOwner((uint)ownerId) is CollisionShape3D shape3D)
                {
                    // shape3D.Disabled = !value;
                    shape3D.DebugFill = !value;
                }
            }
        }
    }

    public void SetVisualLayers(uint value)
    {
        if (lastLayers == value)
            return;
        foreach (var item in visuals)
        {
            if (item is VisualInstance3D vis)
            {
                vis.Layers = value;
            }
        }
        lastLayers = value;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        _itemCached = GetParent<ItemObject>();
        xformSync.CanClaim = false; // HACK
        canGrab = false; // HACK
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    public override void _Ready()
    {
        base._Ready();
        visuals = FindChildren("*", nameof(VisualInstance3D));
        VisibilityChanged += VisChange;
        VisChange();
        if (IsInstanceValid(Item))
            foreach (var pb in Item.physicsBodies)
                AddCollisionExceptionWith(pb);
        // clear caches
        lastCollisionPhys = false;
        SetCollisionPhysics(true);
    }

    private void VisChange()
    {
        // SetCollisionPhysics(Visible);
    }

    public override void UpdateFreezeState()
    {
        if (ShouldFreeze)
        {
            Freeze = true;
        }
        else if (IsInstanceValid(Item.Player))
        {
            Freeze = true;
        }
        else
        {
            base.UpdateFreezeState();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateFreezeState();
        // DebugDrawManager.Instance.DrawDebugSphere(GlobalPosition, 1f, Freeze ? Colors.Blue : Colors.Red, delta);
        // DebugDrawManager.Instance.DrawDebugLine(GlobalPosition, GlobalPosition + LinearVelocity, Freeze ? Colors.Blue : Colors.Red, delta);
        SetCollisionPhysics(Visible && !Item.ViewModelEnabledCached);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!IsNodeReady())
            return;
        UpdateModelState();
        // SetFreezePhysics(!Item.IsAuthorityOrServer || (!Visible || Item.ViewModelEnabledCached));
    }

    public void UpdateModelState()
    {
        if (Item.IsAuthority)
        {
            SetVisualLayers(Item.ViewModelEnabledCached ? 32u : 1u);
        }
        else
        {
            SetVisualLayers(1u);
        }
    }

    public void SendVelocity()
    {
        if (!IsInstanceValid(Item.Player))
        {
            Rpc(MethodName.RpcAddForces, LinearVelocity, AngularVelocity);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void RpcAddForces(Vector3 linear, Vector3 angular)
    {
        if (IsMultiplayerAuthority() && !IsInstanceValid(Item.Player))
        {
            LinearVelocity = linear;
            AngularVelocity = angular;
            // ApplyCentralImpulse(linear);
            // ApplyTorqueImpulse(angular);
        }
    }

    // [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void RpcSetTransform(Vector3 position, Vector3 rotation, Vector3 linearVel, Vector3 angularVel, bool physState)
    {
        if (Item.SenderIsPlayer || Item.SenderIsServer)
        {
            ItemPosition = position;
            ItemRotation = rotation;
            LinearVelocity = linearVel;
            AngularVelocity = angularVel;
            // SetPhysics(physState);
        }
    }

    public Aabb GetAabb()
    {
        Aabb? aabb = null;
        foreach (MeshInstance3D node in FindChildren("*", nameof(MeshInstance3D), owned: false))
        {
            if (aabb.HasValue)
                aabb = aabb.Value.Merge(node.GlobalTransform * node.GetAabb());
            else
                aabb = node.GlobalTransform * node.GetAabb();
        }
        return aabb.GetValueOrDefault();
    }

    public virtual bool CanUse(IItemHolder holder, ItemObject item)
    {
        bool result = holder.GetPlayer().TryGetAbility(out InventoryAbility _);
        EventCanUse evt = EventManager.GetInstance<EventCanUse>();
        evt.Result = result;
        evt.Item = item;
        Emit(evt);
        return evt.Result;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        EventUsing evt = EventManager.GetInstance<EventUsing>();
        evt.Item = item;
        evt.Holder = holder;
        if (!Emit(evt))
        {
            return;
        }
        if (holder is VRPhysicsHand hand)
        {
            hand.PickupWorldItem(Item.Serial);
        }
        else if (holder.GetPlayer().TryGetAbility(out InventoryAbility _))
        {
            Item.Grab(false);
        }
        EventUsed used = EventManager.GetInstance<EventUsed>();
        used.Item = item;
        used.Holder = holder;
        Emit(used);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        EventUseEnding ending = EventManager.GetInstance<EventUseEnding>();
        ending.Holder = holder;
        ending.Item = item;
        Emit(ending);
        if (ending.Canceled)
        {
            return;
        }
        EventUseEnded ended = EventManager.GetInstance<EventUseEnded>();
        ended.Holder = holder;
        ended.Item = item;
        Emit(ended);
    }

    public void ElevatorTeleport(Vector3 position, Vector3 rotation)
    {
        CallDeferred(MethodName.SetGlobalPosition, position);
        CallDeferred(MethodName.SetGlobalRotation, rotation);
        GlobalPosition = position;
        GlobalRotation = rotation;
    }

    public override string ToString()
    {
        return Item.Preset.ResourceName + '\n' + Item.Preset.Description;
    }

    public bool Emit(IWorldItemEvent evt)
    {
        evt.WorldItem = this;
        return EventManager.Emit(evt, this);
    }

    public bool Emit(IEvent evt)
    {
        if (evt is IWorldItemEvent wevt)
        {
            return Emit(wevt);
        }
        if (evt is IInteractableEvent ievt)
        {
            return Emit(ievt);
        }
        return EventManager.Emit(evt, this);
    }

    public bool Emit(IInteractableEvent evt)
    {
        evt.Interactable = this;
        return EventManager.Emit(evt, this);
    }
}

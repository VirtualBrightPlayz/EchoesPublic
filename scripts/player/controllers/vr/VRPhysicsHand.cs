using System;
using Godot;

public partial class VRPhysicsHand : AnimatableBody3D, IItemHolder
{
    public NetworkPlayer Player => NetworkPlayer.LocalInstance;

    [Export]
    public XRController3D hand;
    [Export]
    public bool isLeftHand = true;
    [Export]
    public int maxSteps = 4;
    [Export]
    public bool pushBodies = true;
    [Export]
    public float stiffness = 10f;
    [Export]
    public float maxForce = 1f;
    [Export]
    public float rotateLerpWeight = 1f;
    [Export] public float rotateLerpTimerSpeed = 1f;
    public float rotateLerpTimer = 1f;
    [Export] public PIDController pidController;
    [Export] public Node3D handMesh;

    public Vector3 velocity;
    public Vector3 lastPosition;
    public Node3D lastCollision;
    public BodyCollision collision;
    public BodyCollision prevCollision;
    public ItemSubObject grabbedSubItem;
    public ItemObject grabbedItem;
    public IGrabbable grabbable;
    private bool wasTriggerPressed = false;

    public bool IsSocket => false;
    public ButtonInputFlags InputPrimary { get; set; } = ButtonInputFlags.None;
    public CollisionObject3D MainCollider => this;
    public Transform3D HolderTransform => GlobalTransform;
    public Transform3D AimTransform => GlobalTransform;
    public bool IsAimingDown => true;

    public BasePlayer GetPlayer() => Player;

    public void Recoil(Vector3 rotation)
    {
        Rotation += rotation * Mathf.DegToRad(360f);
        rotateLerpTimer = 0f;
        if (IsInstanceValid(grabbedItem))
        {
            ItemGrip grip = grabbedItem.FindGrip(this);
            RigidBody3D rb = null;
            if (grabbedItem.PrimaryHolder == this)
            {
                rb = grabbedItem.model as RigidBody3D;
            }
            else if (IsInstanceValid(grip) && IsInstanceValid(grip.target))
            {
                rb = grip.target;
            }
            if (IsInstanceValid(rb) && IsInstanceValid(grip))
            {
                Node3D marker = grip.marker;
                if (isLeftHand && IsInstanceValid(grip.leftMarker))
                {
                    marker = grip.leftMarker;
                }
                else if (!isLeftHand && IsInstanceValid(grip.rightMarker))
                {
                    marker = grip.rightMarker;
                }
                rb.GlobalBasis = GlobalBasis.Orthonormalized() * marker.Basis.Inverse().Orthonormalized();
            }
        }
    }

    public void MoveTick(bool physics, double delta)
    {
        handMesh.Transform = Transform3D.Identity;
        handMesh.Scale = Vector3.One * (float)XRServer.WorldScale;
        if (IsInstanceValid(grabbedItem))
        {
            ItemGrip grip = grabbedItem.FindGrip(this);
            RigidBody3D rb = null;
            if (grabbedItem.PrimaryHolder == this)
            {
                rb = grabbedItem.model as RigidBody3D;
            }
            else if (IsInstanceValid(grip) && IsInstanceValid(grip.target))
            {
                rb = grip.target;
            }
            if (IsInstanceValid(rb))
            {
                if (IsInstanceValid(grip))
                {
                    Node3D marker = grip.marker;
                    if (isLeftHand && IsInstanceValid(grip.leftMarker))
                    {
                        marker = grip.leftMarker;
                    }
                    else if (!isLeftHand && IsInstanceValid(grip.rightMarker))
                    {
                        marker = grip.rightMarker;
                    }
                    rb.LinearVelocity = (hand.GlobalPosition - marker.GlobalPosition);
                    rb.AngularVelocity = (hand.GlobalBasis.Orthonormalized() * marker.Basis.Inverse().Orthonormalized()).GetEuler();
                    collision = rb.MoveAndSlideRigidBody(hand.GlobalPosition - marker.GlobalPosition, maxSteps, stiffness, maxForce, pushBodies);
                    if (delta > 0)
                        rb.GlobalBasis = rb.GlobalBasis.Orthonormalized().Slerp(hand.GlobalBasis.Orthonormalized() * marker.Basis.Inverse().Orthonormalized(), (float)delta * rotateLerpWeight * rotateLerpTimer);
                    if (physics)
                    {
                        MoveTowardsCallbacks();
                    }
                }
            }
            if (IsInstanceValid(grip))
            {
                if (isLeftHand && IsInstanceValid(grip.leftMarker))
                    GlobalTransform = grip.leftMarker.GlobalTransform;
                else if (!isLeftHand && IsInstanceValid(grip.rightMarker))
                    GlobalTransform = grip.rightMarker.GlobalTransform;
                else
                    GlobalTransform = grip.marker.GlobalTransform;
            }
            else
                MoveTowards(hand.GlobalPosition, hand.GlobalBasis, physics, delta);
        }
        else if (IsInstanceValid((GodotObject)grabbable))
        {
            Node3D target = (Node3D)grabbable;
            Vector3 localPos = pidController.grabPositionLocal;
            if (hand.GetFloat("grip") <= 0.5f || Player.Role.team == TeamID.Dead)
            {
                ReleaseGrab(velocity);
            }
            else
            {
                MoveTowards(hand.GlobalPosition, hand.GlobalBasis, physics, delta);
                handMesh.GlobalPosition = target.ToGlobal(localPos);
                handMesh.GlobalBasis = (target.GlobalBasis * pidController.grabRotation).Orthonormalized();
                handMesh.Scale = Vector3.One * (float)XRServer.WorldScale;
            }
        }
        else
            MoveTowards(hand.GlobalPosition, hand.GlobalBasis, physics, delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        InputPrimary = InputManager.UpdateInput(hand.GetFloat("trigger") >= 0.5f, InputPrimary);

        bool triggerPressed = hand.GetFloat("trigger") > 0.5f;

        MoveTick(true, delta);

        if (IsInstanceValid(collision?.collider) && collision.collider is IPlayerController ctrl)
        {
            if (ctrl.Player.HasAuthority && ctrl is PhysicsBody3D body)
            {
                AddCollisionExceptionWith(body);
            }
            if (!ctrl.Player.HasAuthority && Player.TryGetAbility(out AttackAbility scp) && triggerPressed)
            {
                scp.RpcId(MultiplayerPeer.TargetPeerServer, AttackAbility.MethodName.RpcTryAttack, collision.collider.GetPath());
            }
        }
        if (IsInstanceValid(grabbedItem))
        {
            if (grabbedItem.PrimaryHolder == this)
            {
                if (hand.GetFloat("grip") <= 0.5f)
                {
                    {
                        grabbedItem.GripRelease(this);
                        if (IsInstanceValid(collision?.collider) && collision.collider is InventorySocket socket && !IsInstanceValid(socket.grabbedItem))
                        {
                            socket.PickupWorldItem(grabbedItem.Serial, false);
                        }
                        else
                        {
                            if (grabbedItem.PrimaryHolder == null)
                                grabbedItem.Release(GlobalPosition, GlobalRotation, velocity / (float)delta, Vector3.Zero);
                        }
                    }
                    RemoveCollisionExceptionWith(grabbedItem.model);
                    foreach (var pb in grabbedItem.physicsBodies)
                        RemoveCollisionExceptionWith(pb);
                    grabbedItem = null;
                }
            }
            else
            {
                if (hand.GetFloat("grip") <= 0.5f)
                {
                    grabbedItem.GripRelease(this);
                    RemoveCollisionExceptionWith(grabbedItem.model);
                    foreach (var pb in grabbedItem.physicsBodies)
                        RemoveCollisionExceptionWith(pb);
                    grabbedItem = null;
                }
            }
        }
        wasTriggerPressed = triggerPressed;

        rotateLerpTimer = Mathf.Clamp(rotateLerpTimer + (float)delta * rotateLerpTimerSpeed, 0f, 1f);
    }

    public void PickupWorldItem(int serial)
    {
        if (IsInstanceValid(grabbedItem) || IsInstanceValid(grabbedSubItem))
        {
            RemoveCollisionExceptionWith(grabbedItem.model);
            foreach (var pb in grabbedItem.physicsBodies)
                RemoveCollisionExceptionWith(pb);
            grabbedItem.Release(GlobalPosition, GlobalRotation);
        }
        grabbedItem = null;
        if (ItemManager.Instance.Items.TryGetValue(serial, out ItemObject item))
        {
            item.Grab(true);
            if (!item.GripGrabNext(this))
            {
                return;
            }
            item.CL_SetModelState(true);
            AddCollisionExceptionWith(item.model);
            foreach (var pb in item.physicsBodies)
                AddCollisionExceptionWith(pb);
            grabbedItem = item;
            Log.Print($"{GetPath()} grabbed {item.Name}");
        }
    }

    public void MoveTowards(Vector3 targetPosition, Basis targetBasis, bool callbacks, double delta)
    {
        if (GlobalPosition.DistanceTo(targetPosition) > 1f)
        {
            GlobalPosition = targetPosition;
            lastPosition = GlobalPosition;
            velocity = Vector3.Zero;
        }
        if (delta > 0)
        {
            GlobalBasis = GlobalBasis.Orthonormalized().Slerp(targetBasis.Orthonormalized(), (float)delta * rotateLerpWeight * rotateLerpTimer);
        }
        collision = MoveAndSlide(targetPosition - GlobalPosition);
        ForceUpdateTransform();
        if (callbacks)
        {
            MoveTowardsCallbacks();
        }
    }

    public void MoveTowardsCallbacks()
    {
        velocity += collision?.travel ?? velocity;
        velocity /= 2f;
        lastPosition = GlobalPosition;
        if (!TryInteract(collision?.collider, collision?.position ?? Vector3.Zero, collision?.travel ?? Vector3.Zero))
        {
            TryGrab(collision?.collider, collision?.position ?? Vector3.Zero, collision?.travel ?? Vector3.Zero);
        }
        if (IsInstanceValid(Player))
        {
            Player.HandSyncTargets[hand.GetMeta("hand").AsInt32()].GlobalTransform = GlobalTransform;
        }
        if (!IsInstanceValid(prevCollision?.collider) && IsInstanceValid(collision?.collider))
        {
            hand.TriggerHapticPulse("haptic", 0, 0.25, 0.1, 0);
        }
        prevCollision = collision;
    }

    public bool CanUse(IInteractable interactable, Vector3 travel)
    {
        if (interactable is Node node)
        {
            if (node.HasMeta("no_vr_use"))
                return false;
        }
        switch (interactable.ActionType)
        {
            default:
            case IInteractable.InteractType.Touch:
                return true;
            case IInteractable.InteractType.Grab:
                return hand.GetFloat("grip") > 0.5f && !IsInstanceValid(grabbedItem) && !IsInstanceValid(grabbedSubItem);
            case IInteractable.InteractType.Hit:
                return (hand.GlobalPosition - GlobalPosition).Length() > 0.1f;
        }
    }

    public void ReleaseGrab(Vector3 travel)
    {
        if (IsInstanceValid((GodotObject)grabbable))
        {
            RemoveCollisionExceptionWith((Node)grabbable);
            if (Player.ActiveController is PhysicsBody3D body)
            {
                body.RemoveCollisionExceptionWith((Node)grabbable);
            }
            pidController.SetTarget(null, default, default);
            grabbable.Grab(false, default, travel);
            grabbable = null;
        }
    }

    private void TryGrab(Node3D other, Vector3 hitGlobal, Vector3 travel)
    {
        if (Player.Role.team == TeamID.Dead)
            return;
        if (!IsInstanceValid(other))
            return;
        if (IsInstanceValid(grabbedItem))
            return;
        if (other is IGrabbable grb)
        {
            bool gripState = hand.GetFloat("grip") > 0.5f;
            if (!IsInstanceValid((GodotObject)grabbable))
            {
                if (gripState)
                {
                    ReleaseGrab(travel);
                    grabbable = grb;
                    AddCollisionExceptionWith(other);
                    if (Player.ActiveController is PhysicsBody3D body)
                    {
                        body.AddCollisionExceptionWith(other);
                    }
                    grb.Grab(gripState, other.ToLocal(hitGlobal), travel);
                    pidController.SetTargetB((RigidBody3D)other, other.ToLocal(hitGlobal), GlobalBasis);
                }
            }
        }
    }

    private bool TryInteract(Node3D other, Vector3 hitGlobal, Vector3 travel)
    {
        if (Player.Role.team == TeamID.Dead)
            return false;
        bool found = false;
        if (IsInstanceValid(other) && other is IInteractable interactable)
        {
            found = true;
            if (interactable.CanUse(this, grabbedItem) && CanUse(interactable, travel))
            {
                if (!IsInstanceValid(lastCollision) || lastCollision != other)
                    interactable.Use(this, grabbedItem);
                lastCollision = other;
            }
            else
                other = null;
        }
        else if (IsInstanceValid(other) && other.HasMeta(IInteractable.META_NAME) && other.GetMeta(IInteractable.META_NAME).AsGodotObject() is IInteractable interactable1)
        {
            found = true;
            if (interactable1.CanUse(this, grabbedItem) && CanUse(interactable1, travel))
            {
                if (!IsInstanceValid(lastCollision) || lastCollision != other)
                    interactable1.Use(this, grabbedItem);
                lastCollision = other;
            }
            else
                other = null;
        }
        else
            other = null;
        if (IsInstanceValid(lastCollision) && lastCollision != other)
        {
            lastCollision = null;
        }
        return found;
    }

    public BodyCollision MoveAndSlide(Vector3 move)
    {
        SyncToPhysics = false;
        Vector3 stepMove = move;
        BodyCollision ret = null;
        for (int i = 0; i < maxSteps; i++)
        {
            KinematicCollision3D collision = MoveAndCollide(stepMove);
            if (ret == null)
                ret = new BodyCollision();
            if (collision == null)
            {
                ret.travel = move;
                break;
            }
            ret.collider = (Node3D)collision.GetCollider();
            ret.position = collision.GetPosition();
            ret.normal = collision.GetNormal();
            ret.travel = collision.GetTravel();
            Vector3 nextMove = collision.GetRemainder().Slide(collision.GetNormal());

            if (pushBodies && Player.Role.team != TeamID.Dead)
            {
                if (collision.GetCollider() is RigidBody3D rb)
                {
                    Vector3 lost = stepMove - nextMove;
                    if (rb is RigidbodySync sync)
                    {
                        sync.OnImpulse((lost * stiffness).LimitLength(maxForce), GlobalPosition - rb.GlobalPosition);
                    }
                    else
                    {
                        rb.ApplyImpulse((lost * stiffness).LimitLength(maxForce), GlobalPosition - rb.GlobalPosition);
                    }
                }
            }

            stepMove = nextMove;

            if (nextMove.Dot(move) <= 0f)
                break;
        }
        return ret;
    }
}
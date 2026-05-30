using Godot;

[GlobalClass]
public partial class PhysicsDoor : RigidbodySync, IInteractable
{
    [Export]
    public Node3D OpenPoint;
    [Export]
    public Node3D ClosePoint;
    [Export]
    public Vector3 axis = Vector3.Up;
    [Export]
    public float speed = 90f;
    [Export]
    public bool isOpen = false;
    [Export]
    public bool isMoving = false;
    [Export]
    public HingeJoint3D hinge;
    [Export]
    public AnimationPlayer handleAnims;
    [Export]
    public StringName handlePullAnim;
    [Export]
    public StringName handlePullShutAnim;

    public Vector3 WorldInteractPosition => GlobalPosition;

    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return true;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        Rpc(MethodName.RpcUse);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse()
    {
        if (!IsMultiplayerAuthority())
            return;
        if (!isMoving && !isOpen)
            Rpc(MethodName.RpcHandlePull, false);
        isOpen = !isOpen;
        isMoving = true;
        UpdateFreezeState();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcHandlePull(bool close)
    {
        if (IsInstanceValid(handleAnims))
        {
            handleAnims.Play(close ? handlePullShutAnim : handlePullAnim);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (IsMultiplayerAuthority())
        {
            if (isMoving)
            {
                if (IsNear(GlobalTransform, OpenPoint.GlobalTransform) && isOpen)
                {
                    isMoving = false;
                    UpdateFreezeState();
                    GlobalTransform = OpenPoint.GlobalTransform;
                }
                if (IsNear(GlobalTransform, ClosePoint.GlobalTransform) && !isOpen)
                {
                    isMoving = false;
                    Rpc(MethodName.RpcHandlePull, true);
                    UpdateFreezeState();
                    GlobalTransform = ClosePoint.GlobalTransform;
                }
            }
            else
            {
                if (IsNear(GlobalTransform, ClosePoint.GlobalTransform) && isOpen)
                {
                    isOpen = false;
                    isMoving = false;
                    Rpc(MethodName.RpcHandlePull, true);
                    UpdateFreezeState();
                    GlobalTransform = ClosePoint.GlobalTransform;
                }
            }
            float sign = 1f;
            if (isOpen)
            {
                sign = -1f;
            }
            hinge.SetParam(HingeJoint3D.Param.MotorTargetVelocity, Mathf.DegToRad(speed) * sign);
            hinge.SetFlag(HingeJoint3D.Flag.EnableMotor, isMoving || !isOpen);
        }
    }

    public override void UpdateFreezeState()
    {
        // if (isMoving && IsMultiplayerAuthority())
            base.UpdateFreezeState();
        // else
            // Freeze = true;
    }

    public static bool IsNear(Transform3D src, Transform3D dst)
    {
        return src.Origin.DistanceSquaredTo(dst.Origin) <= 0.05f * 0.05f && src.Basis.GetRotationQuaternion().AngleTo(dst.Basis.GetRotationQuaternion()) <= 1f;
    }
}

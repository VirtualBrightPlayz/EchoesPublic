using Godot;

[GlobalClass]
public partial class ClimbingController : FPController
{
    public enum ControlState : byte
    {
        Walking,
        Climbing,
    }

    public Vector3 Up = Vector3.Up;
    public Vector3 WallUp = Vector3.Up;
    public Basis ClimbBasis => Basis.LookingAt(Fwd.Slide(Up.Normalized()), Up.Normalized());
    public Basis WallBasis => Basis.LookingAt(Fwd.Slide(WallUp), WallUp);
    private Vector3 lastFwd = Vector3.Forward;
    public Vector3 CameraFwd
    {
        get
        {
            var fwd = -camera.GlobalBasis.Z.Normalized();
            if (Mathf.Abs(fwd.Dot(Up.Normalized())) > 0.99f)
            {
                fwd = camera.GlobalBasis.Y.Normalized();
            }
            return fwd;
        }
    }
    public Vector3 Fwd
    {
        get
        {
            var fwd = -head.GlobalBasis.Z.Normalized();
            if (Mathf.Abs(fwd.Dot(WallUp.Normalized())) > 0.99f)
            {
                // fwd = GlobalBasis.Y.Normalized();
                return lastFwd;
            }
            lastFwd = fwd;
            return fwd;
        }
    }

    public double wallSwitchCooldown;

    [Export]
    public ControlState state = ControlState.Walking;

    [Export]
    public float rotateFloorSpeed = 1f;
    [Export]
    public ShapeCast3D shapeCast;
    [Export]
    public float castMargin = 0.1f;
    [Export]
    public Node3D floorMarker;
    public override Node3D Floor => floorMarker;

    public override void ApplySprint(double delta)
    {
        if (CanSprint())
        {
            state = ControlState.Climbing;
        }
        else
        {
            state = ControlState.Walking;
            IsSprinting = false;
        }
        if (Player.TryGetAbility(out StaminaAbility ability))
        {
            ability.ApplySprint(this, delta, !UpDirection.IsEqualApprox(Vector3.Up));
        }
    }

    public override void UpdateToggles()
    {
        if (ButtonSprint.HasFlag(ButtonInputFlags.JustPressed))
        {
            IsSprinting = !IsSprinting;
        }
        IsCrouching = ButtonCrouch.HasFlag(ButtonInputFlags.Pressed);
        if (ButtonNoclip.HasFlag(ButtonInputFlags.JustPressed))
        {
            NoClipEnabled = !NoClipEnabled;
        }
    }

    public Vector3 GetUpVector()
    {
        shapeCast.TargetPosition = shapeCast.ToLocal(shapeCast.GlobalPosition + GetMovementDirection().Normalized() * castMargin);
        if (!shapeCast.IsColliding())
        {
            return Vector3.Up;
        }
        int closest = -1;
        for (int i = 0; i < shapeCast.GetCollisionCount(); i++)
        {
            Vector3 pt = shapeCast.GetCollisionPoint(i);
            if (closest == -1 || pt.DistanceTo(shapeCast.GlobalPosition) < shapeCast.GetCollisionPoint(closest).DistanceTo(shapeCast.GlobalPosition))
            {
                closest = i;
            }
        }
        if (closest == -1)
        {
            return Vector3.Up;
        }
        else
        {
            Vector3 v = shapeCast.GetCollisionPoint(closest).DirectionTo(shapeCast.GlobalPosition);
            if (v.IsZeroApprox())
            {
                return Vector3.Up;
            }
            return v;
        }
    }

    public override Vector3 GetMovementDirection()
    {
        if (state == ControlState.Walking)
            return base.GetMovementDirection();
        Vector2 inputDir = MovementDirection;
        Vector3 direction = (WallBasis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        foreach (var ability in Player.Abilities)
        {
            ability.ModifyMovementDirection(this, ref direction);
        }
        return direction;
    }

    public override Vector3 GetMovementVelocity(Vector3 vel, double delta)
    {
        if (state == ControlState.Walking)
        {
            WallUp = Vector3.Up;
            UpDirection = Vector3.Up;
            return base.GetMovementVelocity(vel, delta);
        }

        Vector3 velocity = vel;

        Vector3 direction = GetMovementDirection();

        shapeCast.ForceShapecastUpdate();
        WallUp = GetUpVector();
        Vector3 mask = WallUp.Abs();
        UpDirection = WallUp;

        Vector3 invMask = Vector3.One - mask;

        float multiplier = GetSpeedMultiplier();

        Vector3 targetVel = direction * EffectiveSpeed * multiplier;
        targetVel = velocity.MoveToward(targetVel, EffectiveAccel * (float)delta) * invMask + velocity * mask;
        if (IsOnFloor() || IsOnWall() || shapeCast.IsColliding())
        {
            targetVel -= UpDirection * GetGravity().Length() * (float)delta * EffectiveGravityMultiplier;
        }
        else
        {
            targetVel += GetGravity() * (float)delta * EffectiveGravityMultiplier;
        }

        // Handle Jump.
        if (ButtonJump.HasFlag(ButtonInputFlags.JustPressed) && IsOnFloor())
        {
            targetVel = UpDirection * EffectiveJumpVelocity + targetVel * invMask;
            footstepTimer = footstepInterval;
            if (IsInstanceValid(Player.model) && IsInstanceValid(Player.model.anims))
            {
                Player.model.anims.Rpc(PlayerAnims.MethodName.RpcJump, false);
            }
        }

        return targetVel;
    }

    public override void ApplyMouseMotion(double delta, Vector2 mouseMotion)
    {
        Basis = new Basis(Up.Normalized(), mouseMotion.X) * Basis;
        if (!InputManager.Instance.IsVR)
        {
            head.Rotation += new Vector3(mouseMotion.Y, 0, 0);
            head.Rotation = new Vector3(Mathf.Clamp(head.Rotation.X, Mathf.DegToRad(-75f), Mathf.DegToRad(75f)), head.Rotation.Y, head.Rotation.Z);
        }
    }

    public override void ViewBob(double delta)
    {
        if (state == ControlState.Walking)
        {
            head.Position = head.Position.Lerp(new Vector3(0f, Player.Role.CameraHeight, 0f), (float)delta * 10f);
            return;
        }
        head.Position = head.Position.Lerp(new Vector3(0f, 0f, 0f), (float)delta * 10f);
    }

    public override void Movement(double delta)
    {
        // Vector3 pos = camera.GlobalPosition - camera.GlobalBasis.Z;
        // DebugDrawManager.Instance.DrawDebugLine(pos, pos + Velocity, IsOnFloor() ? Colors.Green : Colors.Red);

        bool wasOnFloor = IsOnFloor();
        Vector3 velocity = Velocity;

        velocity = GetMovementVelocity(velocity, delta);

        Velocity = velocity;

        // apply rigidbody physics
        {
            KinematicCollision3D collision = GetLastSlideCollision();
            if (IsInstanceValid(collision))
            {
                GodotObject obj = collision.GetCollider();
                if (obj is RigidBody3D rb)
                {
                    var pushDir = -collision.GetNormal();
                    var velDotDiff = Velocity.Dot(pushDir) - rb.LinearVelocity.Dot(pushDir);
                    velDotDiff = Mathf.Max(0f, velDotDiff);
                    float mass = 80f;
                    float force = Mathf.Min(1f, mass / rb.Mass);
                    if (obj is RigidbodySync sync)
                    {
                        if (obj is IPhysicsProp prop && !prop.Affected.HasFlag(IPhysicsProp.AffectedBy.PlayerPush))
                        {
                        }
                        else
                        {
                            sync.OnImpulse(pushDir * velDotDiff * force, collision.GetPosition() - rb.GlobalPosition);
                        }
                    }
                    else
                    {
                    }
                }
            }
        }

        MoveAndSlide();

        if (!wasOnFloor && IsOnFloor())
        {
            if (IsInstanceValid(Player.model) && IsInstanceValid(Player.model.anims))
            {
                Player.model.anims.Rpc(PlayerAnims.MethodName.RpcJump, true);
            }
        }

        UpdateLastFootsteps();

        Vector3 n = WallUp;
        if (Up.IsEqualApprox(n) || !Up.IsNormalized())
            Up = n;
        else
            Up = Up.Slerp(n, (float)delta * rotateFloorSpeed);
        GlobalBasis = ClimbBasis;
    }
}

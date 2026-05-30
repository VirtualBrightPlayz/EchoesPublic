using System;
using Godot;
using Godot.Collections;

public partial class NpcScp173 : CharacterBody3D, IDamageSource, IElevatorTeleport
{
    [Export]
    public Label3D debug;
    [Export]
    public NavigationAgent3D agent;
    [Export]
    public Area3D playerFinder;
    [Export]
    public ShapeCast3D doorFinder;
    [Export]
    public float walkSpeed = 1f;
    [Export]
    public float accel = 10f;
    [Export]
    public float rotationLerpSpeed = 1f;
    [Export]
    public float footstepInterval = 0.1f;
    [Export]
    public GameSound footsteps;

    public string AttackerDisplayName => "SCP-173";
    public NodePath AbsolutePath => GetPath();

    public Node3D target;

    private float gravity = 1f;
    private int waypointCounter = 0;
    protected double footstepTimer;
    protected ulong lastPathTime = 0;
    protected Vector3 lastPosition;

    public override void _Ready()
    {
        // agent = new NpcPath(GetWorld3D().NavigationMap);
        gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        agent.LinkReached += OnLink;
        agent.WaypointReached += OnWaypoint;
        // agent.OnWaypoint = () => OnWaypoint(null);
        agent.VelocityComputed += OnVelocity;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority())
            return;

        Vector3 direction = Vector3.Zero;
        if (agent.IsNavigationFinished() || !IsInstanceValid(target) || lastPosition.IsEqualApprox(GlobalPosition))
        {
            UpdateTarget();
        }
        if (!agent.IsNavigationFinished())
        {
            Vector3 targetPosition = agent.GetNextPathPosition();
            direction = GlobalPosition.DirectionTo(targetPosition);
            debug.GlobalPosition = targetPosition;
        }
        if (IsSeen() && !RoundManager.Instance.IsBlinking)
        {
            direction = Vector3.Zero;
        }

        if (agent.AvoidanceEnabled)
            agent.Velocity = direction;
        else
            OnVelocity(direction);

        FootStep(delta);

        if (IsInstanceValid(doorFinder))
        {
            for (int i = 0; i < doorFinder.GetCollisionCount(); i++)
            {
                var body = doorFinder.GetCollider(i);
                if (body is DoorButton btn)
                {
                    InteractWith(btn);
                }
            }
        }

        lastPosition = GlobalPosition;

        // debug.Text = $"{agent.pathIndex}/{agent.path.Length}";
    }

    public void FootStep(double delta)
    {
        if (IsOnFloor())
        {
            footstepTimer += delta * Velocity.Length() / walkSpeed;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                Rpc(nameof(RpcFootstep));
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcFootstep()
    {
        if (IsInstanceValid(footsteps))
            footsteps.PlayOneShot3D(this);
    }

    public void InteractWith(DoorButton btn)
    {
        if (IsInstanceValid(btn.door) && btn.door.GetParent() is TwoDoorsLogic twoDoors)
        {
            twoDoors.SV_Toggle();
        }
        if (IsInstanceValid(btn.door) && btn.door.GetParent() is TimedDoor timed && !timed.isLocked && timed.timerState == TimedDoor.TimerStateEnum.Idle)
        {
            timed.SV_Toggle();
        }
        if (IsInstanceValid(btn.door) && btn.door.GetParent() is Elevator elevator && !elevator.isLocked && !elevator.isMoving)
        {
            elevator.Toggle();
        }
        if (IsInstanceValid(btn.door) && !btn.door.isLocked && !btn.door.isMoving)
        {
            btn.door.SV_SetState(true);
        }
        if (IsInstanceValid(btn.elevator) && !btn.elevator.isLocked && !btn.elevator.isMoving && !btn.elevator.door.isOpen)
        {
            btn.elevator.Toggle();
        }
        if (IsInstanceValid(btn.timedDoor) && !btn.timedDoor.isLocked && btn.timedDoor.timerState == TimedDoor.TimerStateEnum.Idle)
        {
            btn.timedDoor.SV_Toggle();
        }
    }

    private void UpdateTarget()
    {
        if (Time.GetTicksMsec() - lastPathTime < 1_000)
            return;
        target = null;
        foreach (var body in playerFinder.GetOverlappingBodies())
        {
            if (body is IPlayerController)
            {
                target = body;
                break;
            }
        }
        if (!IsInstanceValid(target))
            return;
        lastPathTime = Time.GetTicksMsec();
        Vector3 pt = NavigationServer3D.MapGetClosestPoint(agent.GetNavigationMap(), target.GlobalPosition);
        if (pt.DistanceTo(target.GlobalPosition) > agent.PathMaxDistance)
            return;
        agent.SetTargetPosition(target.GlobalPosition);
        waypointCounter = 0;
    }

    public bool IsSeen()
    {
        bool found = false;
        var bodies = playerFinder.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is IPlayerController plr)
                found = IsSeenBy(plr);
            if (found)
                break;
        }
        return found;
    }

    public bool IsSeenBy(IPlayerController target)
    {
        {
            Vector3 origin = target.Camera.GlobalPosition;
            Vector3 end = GlobalPosition;
            if (!target.Camera.IsPositionInFrustum(end))
            {
                return false;
            }
            PhysicsRayQueryParameters3D ray = new PhysicsRayQueryParameters3D()
            {
                From = origin,
                To = end,
                Exclude = new Godot.Collections.Array<Rid>(new Rid[] { GetRid() }),
                CollisionMask = ItemManager.Instance.playerLayer,
            };
            var result = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (result.Count > 0)
            {
                var collider = result["collider"].AsGodotObject();
                if (collider.Equals(this))
                {
                    return true;
                }
            }
            else
                return true;
        }
        return false;
    }

    public void Repath()
    {
        CallDeferred(MethodName.UpdateTarget);
    }

    private void OnVelocity(Vector3 safeVelocity)
    {
        Vector3 direction = safeVelocity;
        double delta = GetPhysicsProcessDeltaTime();
        Vector3 vel = Velocity;
        if (!IsOnFloor())
            vel.Y -= gravity * (float)delta;
        else
            vel.Y = 0f;
        float y = vel.Y;
        direction.Y = 0f;
        if (direction.IsZeroApprox())
            direction = Vector3.Zero;
        direction = direction.Normalized();
        vel = vel.MoveToward(direction * walkSpeed, accel * (float)delta);
        if (direction.IsZeroApprox())
            vel = Vector3.Zero;
        vel.Y = y;
        Velocity = vel;

        ApplyForces();

        Vector3 val = this.MoveAndStairs(delta, Velocity, Vector3.Up, FloorSnapLength);
        GlobalPosition += val;
        MoveAndSlide();

        if (!direction.IsZeroApprox())
            Quaternion = Quaternion.Slerp(Basis.LookingAt(direction).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);
        else if (IsInstanceValid(target))
        {
            // var playerDir = GlobalPosition.DirectionTo(target.GlobalPosition);
            // playerDir.Y = 0f;
            // playerDir = playerDir.Normalized();
            // Quaternion = Quaternion.Slerp(Basis.LookingAt(playerDir).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);
        }
    }

    private void ApplyForces()
    {
        // apply rigidbody physics
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D collision = GetSlideCollision(i);
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
                            return;
                        }
                        sync.OnImpulse(pushDir * velDotDiff * force, collision.GetPosition() - rb.GlobalPosition);
                    }
                    else
                    {
                        // rb.ApplyImpulse(pushDir * velDotDiff * force, collision.GetPosition() - rb.GlobalPosition);
                    }
                }
            }
        }
    }

    private void OnLink(Dictionary details)
    {
        if (details["owner"].AsGodotObject() is NavigationLink3D navLink)
        {
            if (navLink.GetParent() is Elevator elevator)
            {
                elevator.Toggle();
            }
        }
    }

    private void OnWaypoint(Dictionary details)
    {
        if (IsInstanceValid(target) && waypointCounter++ >= 2)
        {
            waypointCounter = 0;
            CallDeferred(MethodName.UpdateTarget);
        }
    }

    public void ElevatorTeleport(Vector3 position, Vector3 rotation)
    {
        GlobalPosition = position;
        GlobalRotation = rotation;
        agent.SetVelocityForced(Vector3.Zero);
    }
}
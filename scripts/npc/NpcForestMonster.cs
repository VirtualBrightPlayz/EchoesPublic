using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class NpcForestMonster : CharacterBody3D, IDamageSource, IElevatorTeleport, IHealth
{
    public enum NpcState : int
    {
        Idle = 0,
        Stalking = 1,
        FollowingTarget = 2,
        AvoidTarget = 3,
        Attack = 4,
    }

    [Export] public NpcPlanner planner;
    [Export] public NpcState state = NpcState.Idle;
    [Export] public Godot.Collections.Array<NpcState> stateOrders = new();
    [Export(PropertyHint.MultilineText)] public string[] stateConditions = Array.Empty<string>();
    [Export] public Node3D view;
    [Export(PropertyHint.Layers3DPhysics)] public uint visLayers;
    [Export]
    public NavigationAgent3D agent;
    [Export]
    public ShapeCast3D doorFinder;
    [Export]
    public ShapeCast3D attackFinder;
    [Export]
    public ShapeCast3D attackArea;
    [Export] public Vector2 walkSpeedRange = new Vector2(1f, 4f);
    [Export]
    public float walkSpeed = 1f;
    [Export]
    public float accel = 10f;
    [Export]
    public float rotationLerpSpeed = 1f;
    [Export]
    public float footstepInterval = 0.1f;
    [Export]
    public AudioStreamPlayer3D footstepsAudio;
    [Export]
    public float attackWarmupTime = 1f;
    [Export]
    public float attackCooldownTime = 1f;
    [Export]
    public GameEffect ragdollEffect;
    [Export] public AudioStreamPlayer3D attackAudio;
    [Export] public Node3D visualsRoot;
    [Export] public float damageAmount = 10f;
    [Export] public float minRange = 20f;
    [Export] public float maxRange = 50f;
    [Export] public double timeToStalk = 15d;
    [Export] public float stalkRange = 30f;

    [ExportGroup("State Configs")]
    [Export] public bool isHorde = false;
    [Export] public bool canAttack = false;

    [ExportGroup("Anims")]
    [Export]
    public AnimationTree animTree;
    [Export]
    public string animStatePath;
    [Export]
    public string walkAnimStatePath;
    [Export]
    public string animAttackPath;
    [Export]
    public float animLerpSpeed = 1f;
    [Export]
    public float walkAnimSpeed = 1f;
    [ExportSubgroup("Sync")]
    [Export]
    public Vector2 animWalkingDirection = Vector2.Zero;

    public string AttackerDisplayName => "SCP-049-2";
    public NodePath AbsolutePath => GetPath();

    public Node3D target;
    public double timeStalking;
    public int timesStalked = 0;
    public double timeInState;
    public NpcState lastState = NpcState.Idle;
    public List<NpcAction> plan = new List<NpcAction>();

    public float currentSpeed = 0f;
    private float gravity = 1f;
    private Vector3 pos;
    private int waypointCounter = 0;
    private bool isAttacking = false;
    protected double footstepTimer;
    protected ulong lastPathTime = 0;
    protected BodyCollision lastCollision;

    [ExportGroup("Health")]
    [Export]
    public float Health { get; set; }
    [Export]
    public float MaxHealth { get; set; }

    public void Spawn(HealInfo info)
    {
        if (IsMultiplayerAuthority())
        {
            Health = MaxHealth;
            walkSpeed = (float)GD.RandRange(walkSpeedRange.X, walkSpeedRange.Y);
            if (IsInstanceValid(planner))
            {
                planner.Setup();
            }
        }
    }

    public void Heal(HealInfo info)
    {
        Health = Mathf.Clamp(Health + info.Amount, 0f, MaxHealth);
    }

    public void Damage(DamageInfo info)
    {
        Rpc(nameof(CL_Damaged), DamageInfo.ToNetwork(info));
        Health -= info.Amount;
        if (Health <= 0)
            Kill(info);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void CL_Damaged(Godot.Collections.Dictionary dictInfo)
    {
        DamageInfo info = new DamageInfo(dictInfo);
        if (info.Source == NetworkPlayer.LocalInstance)
        {
            NetworkPlayer.LocalInstance.Hud.OnHit(info, null);
        }
    }

    public void Kill(DamageInfo info)
    {
        QueueFree();
        int idx = Array.IndexOf(IInitScript.Instance.Data.Effects, ragdollEffect);
        if (idx != -1)
        {
            var n = ragdollEffect.scene.Instantiate<Node3D>();
            // var n = ragdollEffect.SpawnAt3D(ItemManager.Instance.SpawnNode, Player.PlayerPosition, Player.PlayerRotation, true);
            n.Position = GlobalPosition;
            n.Rotation = GlobalRotation;
            ItemManager.Instance.SpawnNode.AddChild(n, true);
            var ragdoll = n.GetMeta(Ragdoll.META_NAME).As<Ragdoll>();
            ragdoll.playerName = AttackerDisplayName;
            ragdoll.roleId = (int)RoleID.SCP049_2;
            ragdoll.TypeId = info?.TypeId ?? (int)DamageType.Unknown;
            // Player.Rpc(nameof(NetworkPlayer.CL_PlayEffectAt3D), idx, Player.PlayerPosition, -GlobalBasis.Z);
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        SetMeta(IHealth.MetaName, this);
    }

    public override void _Ready()
    {
        gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        agent.WaypointReached += OnWaypoint;
        agent.PathChanged += Repath;
        // agent.NavigationFinished += UpdateTarget;
        agent.VelocityComputed += OnVelocity;
        Spawn(new HealInfo());
        if (IsInstanceValid(visualsRoot))
        {
            // visualsRoot.Visible = state != NpcState.Idle;
        }
    }

    private void Repath()
    {
        // if (waypointCounter++ >= 2)
        if (state == NpcState.FollowingTarget)
        {
            CallDeferred(MethodName.UpdatePath, false, target.GlobalPosition);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(visualsRoot))
        {
            // visualsRoot.Visible = state != NpcState.Idle;
        }
        if (IsMultiplayerAuthority())
        {
            timeInState += delta;
            UpdateTarget(false);
            if (IsInstanceValid(planner))
            {
                if (IsInstanceValid(target))
                {
                    UpdateState();
                    if (plan.Count > 0)
                    {
                        if (!planner.AreInputsValid(plan[0].PreConditions))
                        {
                            plan.RemoveAt(0);
                        }
                        // else
                        if (plan.Count > 0)
                        {
                            ChangeState(Enum.Parse<NpcState>(plan[0].ActionName));
                        }
                    }
                    else
                    {
                        ChangeState(NpcState.Idle);
                    }
                    // DebugDrawManager.Instance.DrawDebugString(GlobalPosition, $"{state}\n{plan.Count}\n{string.Join(',', plan.Select(x => x.ResourceName))}", Colors.White, delta);
                }
                else
                {
                    ChangeState(NpcState.Idle);
                }
            }
            else
            {
                ChangeState(GetState());
                // DebugDrawManager.Instance.DrawDebugString(GlobalPosition, $"{state}", Colors.White, 0.1d);
            }
            switch (state)
            {
                case NpcState.Idle:
                    State_Idle(delta);
                    break;
                case NpcState.Stalking:
                    State_Stalking(delta);
                    break;
                case NpcState.FollowingTarget:
                    State_FollowingTarget(delta);
                    break;
                case NpcState.AvoidTarget:
                    State_AvoidTarget(delta);
                    break;
                case NpcState.Attack:
                    State_Attack(delta);
                    break;
            }
        }
    }

    public override void _Process(double delta)
    {
        // animTree.Set($"parameters/{walkSpeedPath}", Mathf.Lerp(animTree.Get($"parameters/{walkSpeedPath}").AsSingle(), animWalking, (float)delta * animLerpSpeed));
        if (IsInstanceValid(animTree))
            animTree.Set(walkAnimStatePath, animTree.Get(walkAnimStatePath).AsVector2().Lerp(animWalkingDirection, (float)delta * animLerpSpeed));
    }

    public void ResetState()
    {
        // planner.SetState("near_target", false);
        // planner.SetState("target_dead", false);
        // planner.SetState("near_target", GlobalPosition.DistanceTo(target.GlobalPosition) < minRange);
    }

    public void UpdateState()
    {
        // planner.SetState("far_target", GlobalPosition.DistanceTo(target.GlobalPosition) < maxRange && GlobalPosition.DistanceTo(target.GlobalPosition) >= minRange);
        // if (planner.GetState("near_target"))
        planner.SetState("target_dead", false);
        planner.SetState("near_target", GlobalPosition.DistanceTo(target.GlobalPosition) < minRange, false);
        planner.SetState("is_seen", CanAnyoneSeeMe());
        planner.SetState("should_attack", CheckAttack());
        bool prevCanAttack = planner.GetState("can_attack");
        if (prevCanAttack != canAttack)
        {
            if (canAttack)
            {
                planner.DesiredWorldState["target_dead"] = true;
            }
            else
            {
                planner.DesiredWorldState.Remove("target_dead");
            }
        }
        planner.SetState("can_attack", canAttack);
        planner.SetState("health_low", Health <= MaxHealth * 0.4f);
        // planner.SetState("health_high", Health >= MaxHealth * 0.75f);
        if (plan.Count == 0 || planner.PlanDirty)
        {
            plan = planner.GetPlan().ToList();
            plan.Reverse();
        }
    }

    public NpcState GetState()
    {
        if (!IsInstanceValid(target))
            return NpcState.Idle;
        // TODO: reduce GC
        var expr = new Expression();
        string[] names = ["state", "last_state", "target_distance", "is_seen", "stalk_timer", "stalk_count", "is_horde"];
        Godot.Collections.Array values = [(int)state, (int)lastState, GlobalPosition.DistanceTo(target.GlobalPosition), CanAnyoneSeeMe(), timeStalking, timesStalked, isHorde];
        for (int i = 0; i < stateOrders.Count; i++)
        {
            Error err = expr.Parse(stateConditions[i], names);
            if (err != Error.Ok)
            {
                Log.PrintErr("GetState ", stateOrders[i], " Error: ", err);
                continue;
            }
            Variant ret = expr.Execute(values, null, false);
            if (expr.HasExecuteFailed())
            {
                Log.PrintErr("GetState Error: ", expr.GetErrorText());
                continue;
            }
            if (ret.AsBool())
            {
                return stateOrders[i];
            }
        }
        return NpcState.Idle;
    }

    public void ChangeState(NpcState newstate)
    {
        if (newstate == state)
            return;
        timeInState = 0d;
        switch (newstate)
        {
            case NpcState.Stalking:
            {
                var playerDir = GlobalPosition.DirectionTo(target.GlobalPosition);
                playerDir.Y = 0f;
                playerDir = playerDir.Normalized();
                Quaternion = Basis.LookingAt(playerDir).GetRotationQuaternion();

                timeStalking = 0d;
                timesStalked++;
                Rpc(MethodName.RpcFootstep);
                break;
            }
            case NpcState.FollowingTarget:
                UpdatePath(true, target.GlobalPosition);
                break;
            case NpcState.AvoidTarget:
                UpdatePath(true, GlobalPosition + target.GlobalPosition.DirectionTo(GlobalPosition) * minRange);
                break;
            case NpcState.Attack:
                Attack();
                break;
        }
        lastState = state;
        state = newstate;
    }

    public bool EndState()
    {
        if (IsInstanceValid(planner) && ((plan.Count > 1 && planner.AreInputsValid(plan[1].PreConditions)) || plan.Count == 1))
        {
            ChangeState(NpcState.Idle);
            bool done = planner.NextAction(plan[0]);
            plan.RemoveAt(0);
            // if (done)
            {
                UpdateState();
                ResetState();
            }
            return true;
        }
        return false;
    }

    public void TeleportBehindTarget()
    {
        Vector3 dir = new Vector3((float)GD.RandRange(-1d, 1d), 0f, (float)GD.RandRange(-1d, 0d)).Normalized() * stalkRange;
        dir = target.GlobalBasis.Inverse() * dir;
        Vector3 pt = NavigationServer3D.MapGetClosestPoint(agent.GetNavigationMap(), target.GlobalPosition + dir);
        Teleport(pt);

        var playerDir = GlobalPosition.DirectionTo(target.GlobalPosition);
        playerDir.Y = 0f;
        playerDir = playerDir.Normalized();
        Quaternion = Basis.LookingAt(playerDir).GetRotationQuaternion();
    }

    public void State_Idle(double delta)
    {
        agent.GetNextPathPosition();
        agent.Velocity = Vector3.Zero;
        OnVelocity(Vector3.Zero);
        animWalkingDirection = Vector2.Zero;
        EndState();
    }

    public void State_Stalking(double delta)
    {
        agent.GetNextPathPosition();
        agent.Velocity = Vector3.Zero;
        OnVelocity(Vector3.Zero);
        animWalkingDirection = Vector2.Zero;
        timeStalking += delta;

        var playerDir = GlobalPosition.DirectionTo(target.GlobalPosition);
        playerDir.Y = 0f;
        playerDir = playerDir.Normalized();
        Quaternion = Quaternion.Slerp(Basis.LookingAt(playerDir).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);
        if (timeStalking >= timeToStalk)
        {
            Heal(new HealInfo(MaxHealth));
            EndState();
        }
    }

    public void State_FollowingTarget(double delta)
    {
        Vector3 direction = Vector3.Zero;
        UpdatePath(false, target.GlobalPosition);
        if (!agent.IsNavigationFinished())
        {
            Vector3 targetPosition = agent.GetNextPathPosition();
            direction = GlobalPosition.DirectionTo(targetPosition) * walkSpeed;
        }

        if (agent.AvoidanceEnabled)
            agent.Velocity = direction;
        else
            OnVelocity(direction);

        var playerDir = GetRealVelocity();
        playerDir.Y = 0f;
        playerDir = playerDir.Normalized();
        if (!playerDir.IsZeroApprox())
            Quaternion = Quaternion.Slerp(Basis.LookingAt(playerDir).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);

        var animDir = GlobalBasis.Inverse() * direction * Velocity.Length() * walkAnimSpeed;
        animWalkingDirection = new Vector2(-animDir.X, animDir.Z);

        FootStep(delta);

        if (IsInstanceValid(doorFinder))
        {
            // doorFinder.ForceShapecastUpdate();
            for (int i = 0; i < doorFinder.GetCollisionCount(); i++)
            {
                var body = doorFinder.GetCollider(i);
                if (body is DoorButton btn)
                {
                    InteractWith(btn);
                }
            }
        }

        if (agent.IsNavigationFinished() && target.GlobalPosition.DistanceSquaredTo(agent.GetFinalPosition()) < agent.TargetDesiredDistance * agent.TargetDesiredDistance)
        {
            EndState();
        }
    }

    public void State_AvoidTarget(double delta)
    {
        Vector3 direction = Vector3.Zero;
        // UpdatePath(false, GlobalPosition + target.GlobalPosition.DirectionTo(GlobalPosition) * minRange);
        if (!agent.IsNavigationFinished())
        {
            Vector3 targetPosition = agent.GetNextPathPosition();
            direction = GlobalPosition.DirectionTo(targetPosition) * walkSpeed;
        }

        if (agent.AvoidanceEnabled)
            agent.Velocity = direction;
        else
            OnVelocity(direction);

        var rotDir = direction;
        rotDir.Y = 0f;
        rotDir = rotDir.Normalized();
        if (!rotDir.IsZeroApprox())
            Quaternion = Quaternion.Slerp(Basis.LookingAt(rotDir).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);

        var animDir = GlobalBasis.Inverse() * direction * Velocity.Length() * walkAnimSpeed;
        animWalkingDirection = new Vector2(-animDir.X, animDir.Z);

        FootStep(delta);

        if (IsInstanceValid(doorFinder))
        {
            // doorFinder.ForceShapecastUpdate();
            for (int i = 0; i < doorFinder.GetCollisionCount(); i++)
            {
                var body = doorFinder.GetCollider(i);
                if (body is DoorButton btn)
                {
                    InteractWith(btn);
                }
            }
        }

        if (agent.IsNavigationFinished())
        {
            EndState();
        }
    }

    public void State_Attack(double delta)
    {
        agent.GetNextPathPosition();
        agent.Velocity = Vector3.Zero;
        OnVelocity(Vector3.Zero);
        animWalkingDirection = Vector2.Zero;

        if (IsInstanceValid(target))
        {
            var playerDir = GlobalPosition.DirectionTo(target.GlobalPosition);
            playerDir.Y = 0f;
            playerDir = playerDir.Normalized();
            Quaternion = Quaternion.Slerp(Basis.LookingAt(playerDir).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);
        }

        if (!isAttacking)
        {
            EndState();
        }
    }

    public bool CheckAttack()
    {
        if (IsInstanceValid(attackFinder))
        {
            attackFinder.ForceShapecastUpdate();
            for (int i = 0; i < attackFinder.GetCollisionCount(); i++)
            {
                var body = attackFinder.GetCollider(i);
                if (body is IPlayerController ctrl && ctrl.Player.Role.team != TeamID.SCP)
                {
                    return true;
                    // Attack();
                }
            }
        }
        return false;
    }

    public async void Attack()
    {
        if (isAttacking)
            return;
        isAttacking = true;
        Rpc(MethodName.RpcAttack);
        await ToSignal(GetTree().CreateTimer(attackWarmupTime, processInPhysics: true), SceneTreeTimer.SignalName.Timeout);
        if (IsInstanceValid(attackArea))
        {
            attackArea.ForceShapecastUpdate();
            for (int i = 0; i < attackArea.GetCollisionCount(); i++)
            {
                var body = attackArea.GetCollider(i);
                IHealth hp = IHealth.GetHealth(body);
                if (body is IPlayerController ctrl && ctrl.Player.Role.team != TeamID.SCP && hp != null)
                {
                    hp.Damage(new DamageInfo(damageAmount, this, DamageType.Scp049_2));
                }
            }
            Rpc(MethodName.RpcAttackSound);
        }
        await ToSignal(GetTree().CreateTimer(attackCooldownTime, processInPhysics: true), SceneTreeTimer.SignalName.Timeout);
        isAttacking = false;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcAttack()
    {
        if (IsInstanceValid(animTree))
        {
            var playback = animTree.Get(animStatePath).As<AnimationNodeStateMachinePlayback>();
            if (IsInstanceValid(playback))
            {
                playback.Travel(animAttackPath);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcAttackSound()
    {
        if (IsInstanceValid(attackAudio))
        {
            attackAudio.Play();
        }
    }

    public void FootStep(double delta)
    {
        if (IsOnFloor())
        {
            footstepTimer += delta * Velocity.Length() / walkSpeed;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                Rpc(MethodName.RpcFootstep);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcFootstep()
    {
        if (IsInstanceValid(footstepsAudio))
        {
            footstepsAudio.Play();
        }
    }

    public void InteractWith(DoorButton btn)
    {
        if (IsInstanceValid(btn.door) && btn.door.targetInteract != null && btn.door.targetInteract is TimedDoor timed)
        {
            if (!timed.isLocked && timed.timerState == TimedDoor.TimerStateEnum.Idle)
            {
                timed.SV_Toggle();
            }
        }
        if (IsInstanceValid(btn.door) && !btn.door.isLocked && !btn.door.isMoving)
        {
            btn.door.SV_SetState(true);
        }
        if (IsInstanceValid(btn.timedDoor) && !btn.timedDoor.isLocked && btn.timedDoor.timerState == TimedDoor.TimerStateEnum.Idle)
        {
            btn.timedDoor.SV_Toggle();
        }
    }

    private bool CanAnyoneSeeMe()
    {
        foreach (var player in RoundManager.Instance.players.Players)
        {
            if (player.Role.team == TeamID.Dead || player.Role.team == TeamID.SCP)
                continue;
            if (player.PlayerPosition.DistanceSquaredTo(GlobalPosition) < maxRange * maxRange && player.ActiveController.Camera.IsPositionInFrustum(view.GlobalPosition) && CanSee(player.ActiveController.View.GlobalPosition))
            {
                return true;
            }
        }
        return false;
    }

    private bool CanSee(Vector3 pt, float maxError = 1f)
    {
        var rayQuery = PhysicsRayQueryParameters3D.Create(view.GlobalPosition, pt, visLayers);
        var result = GetViewport().FindWorld3D().DirectSpaceState.IntersectRay(rayQuery);
        if (result.Count > 0)
        {
            return result["position"].AsVector3().DistanceSquaredTo(pt) <= maxError * maxError;
        }
        else
        {
            return true;
        }
    }

    private void UpdatePath(bool force, Vector3 pt)
    {
        if (Time.GetTicksMsec() - lastPathTime < 1_000 && !force)
            return;
        lastPathTime = Time.GetTicksMsec();
        pos = pt;
        agent.TargetPosition = pt;
        waypointCounter = 0;
    }

    private void UpdateTarget(bool force)
    {
        // if (Time.GetTicksMsec() - lastPathTime < 1_000 && !force)
        //     return;
        NetworkPlayer closestPlayer = null;
        float distSqr = maxRange * maxRange;
        foreach (var player in RoundManager.Instance.players.Players)
        {
            if (player.Role.team == TeamID.Dead || player.Role.team == TeamID.SCP)
                continue;
            if (player.PlayerPosition.DistanceSquaredTo(GlobalPosition) < distSqr && CanSee(player.ActiveController.View.GlobalPosition))
            {
                closestPlayer = player;
                distSqr = player.PlayerPosition.DistanceSquaredTo(GlobalPosition);
            }
        }
        if ((!IsInstanceValid(target) && IsInstanceValid(closestPlayer)) || (IsInstanceValid(closestPlayer) && target.GlobalPosition.DistanceSquaredTo(GlobalPosition) > distSqr))
        {
            target = closestPlayer.ActiveController.Root;
        }
        /*
        lastPathTime = Time.GetTicksMsec();
        if (!IsInstanceValid(closestPlayer))
        {
            return;
            Vector3 pt2 = NavigationServer3D.MapGetClosestPoint(agent.GetNavigationMap(), GlobalPosition + GlobalBasis.Z.Normalized() * (float)GD.RandRange(-10d, 10d) + GlobalBasis.X.Normalized() * (float)GD.RandRange(-10d, 10d));
            pos = pt2;
            agent.TargetPosition = pt2;
            waypointCounter = 0;
            return;
        }
        if (!IsInstanceValid(target) || target.GlobalPosition.DistanceSquaredTo(GlobalPosition) > distSqr)
        {
            target = closestPlayer.controller;
        }
        Vector3 pt = NavigationServer3D.MapGetClosestPoint(agent.GetNavigationMap(), target.GlobalPosition);
        if (pt.DistanceTo(target.GlobalPosition) > agent.PathMaxDistance)
            return;
        // GD.Print("Target Updated");
        // DebugDrawManager.Instance.DrawDebugString(GlobalPosition, "Target Updated", Colors.White);
        pos = target.GlobalPosition;
        agent.TargetPosition = target.GlobalPosition;
        waypointCounter = 0;
        // CheckAttack();
        */
    }

    private void OnVelocity(Vector3 safeVelocity)
    {
        double delta = GetPhysicsProcessDeltaTime();
        Vector3 direction = safeVelocity;
        Vector3 vel = Velocity;
        if (!IsOnFloor())
            vel.Y -= gravity * (float)delta;
        else
            vel.Y = 0f;
        float y = vel.Y;
        direction.Y = 0f;
        float speed = walkSpeed;
        if (isAttacking)
            speed = 0f;
        vel = vel.MoveToward(direction.Normalized() * speed, accel * (float)delta);
        if (isAttacking || direction.IsZeroApprox())
            vel = Vector3.Zero;
        vel.Y = y;
        Velocity = vel;

        // DebugDrawManager.Instance.DrawDebugLine(GlobalPosition, GlobalPosition + Velocity, Colors.White);

        Vector3 val = this.MoveAndStairs(delta, Velocity.Normalized() * speed, UpDirection, FloorSnapLength);
        Position += val;

        MoveAndSlide();
    }

    private void OnWaypoint(Godot.Collections.Dictionary details)
    {
        /*if (details.TryGetValue("position", out var positionVar) && details.TryGetValue("type", out var typeVar))
        {
            var position = positionVar.AsVector3();
            var type = typeVar.As<NavigationPathQueryResult3D.PathSegmentType>();
            if (type == NavigationPathQueryResult3D.PathSegmentType.Region)
            {
                // return;
            }
        }*/
        if (IsInstanceValid(target) && waypointCounter++ >= 2 && state == NpcState.FollowingTarget)
        {
            CallDeferred(MethodName.UpdatePath, false, target.GlobalPosition);
        }
    }

    public void ElevatorTeleport(Vector3 position, Vector3 rotation)
    {
        GlobalPosition = position;
        GlobalRotation = rotation;
        agent.SetVelocityForced(Vector3.Zero);
    }

    public void Teleport(Vector3 position)
    {
        GlobalPosition = position;
        agent.SetVelocityForced(Vector3.Zero);
    }
}
using System;
using System.Collections.Generic;
using Godot;

public partial class NpcZombie : CharacterBody3D, IDamageSource, IElevatorTeleport, IHealth
{
    public enum NpcState : int
    {
        Idle = 0,
        Following = 1,
        FollowingSCP049 = 2,
    }

    [Export] public NpcState state = NpcState.Idle;
    [Export]
    public NavigationAgent3D agent;
    [Export]
    public ShapeCast3D doorFinder;
    [Export]
    public ShapeCast3D attackFinder;
    [Export]
    public ShapeCast3D attackArea;
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
    [Export] public float maxRange = 50f;
    [Export] public AudioStreamPlayer3D attackAudio;
    [Export] public Node3D visualsRoot;
    [Export] public float damageAmount = 10f;
    [Export] public Node3D view;
    [Export(PropertyHint.Layers3DPhysics)] public uint visLayers;

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
    public Node3D target049;
    public bool isTargetDirty = false;
    public bool isTarget049Dirty = false;
    public double timeStalking;

    private float gravity = 1f;
    private bool isAttacking = false;
    protected double footstepTimer;
    protected ulong lastPathTime = 0;
    protected BodyCollision lastCollision;

    public float speed => isAttacking ? 0f : walkSpeed;

    [ExportGroup("Health")]
    [Export]
    public float Health { get; set; }
    [Export]
    public float MaxHealth { get; set; }

    public void Spawn(HealInfo info)
    {
        Health = MaxHealth;
    }

    public void Heal(HealInfo info)
    {
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
        agent.VelocityComputed += OnVelocity;
        Spawn(new HealInfo());
        if (IsInstanceValid(visualsRoot))
        {
            visualsRoot.Visible = state != NpcState.Idle;
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
            UpdateTarget049();
            UpdateTargetHuman();
            switch (state)
            {
                case NpcState.Idle:
                    State_Idle(delta);
                    break;
                case NpcState.Following:
                    State_Following(delta);
                    break;
                case NpcState.FollowingSCP049:
                    State_FollowingSCP049(delta);
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

    public void ChangeState(NpcState newstate)
    {
        state = newstate;
    }

    public void State_Idle(double delta)
    {
        agent.GetNextPathPosition();
        agent.Velocity = Vector3.Zero;
        if (IsInstanceValid(target))
        {
            ChangeState(NpcState.Following);
            return;
        }
        else if (IsInstanceValid(target049))
        {
            ChangeState(NpcState.FollowingSCP049);
            return;
        }
    }

    public void State_Following(double delta)
    {
        if (!IsInstanceValid(target))
        {
            ChangeState(NpcState.Idle);
            return;
        }

        if (isTargetDirty)
        {
            isTargetDirty = false;
            agent.TargetPosition = target.GlobalPosition;
        }
        Vector3 direction = Vector3.Zero;
        if (agent.IsNavigationFinished() || agent.TargetPosition.DistanceSquaredTo(target.GlobalPosition) > agent.PathMaxDistance)
        {
            agent.TargetPosition = target.GlobalPosition;
        }
        else
        {
            Vector3 targetPosition = agent.GetNextPathPosition();
            direction = GlobalPosition.DirectionTo(targetPosition) * speed;
        }

        agent.TargetDesiredDistance = 1f;
        TryMove(direction);

        var animDir = GlobalBasis.Inverse() * direction * Velocity.Length() * walkAnimSpeed;
        animWalkingDirection = new Vector2(-animDir.X, animDir.Z);

        FootStep(delta);

        if (CheckAttack())
        {
            Attack();
        }
        FindDoor();
    }

    public void State_FollowingSCP049(double delta)
    {
        if (!IsInstanceValid(target049))
        {
            ChangeState(NpcState.Idle);
            return;
        }

        if (isTarget049Dirty)
        {
            isTarget049Dirty = false;
            agent.TargetPosition = target049.GlobalPosition;
        }
        Vector3 direction = Vector3.Zero;
        if (agent.IsNavigationFinished() || agent.TargetPosition.DistanceSquaredTo(target049.GlobalPosition) > agent.PathMaxDistance)
        {
            agent.TargetPosition = target049.GlobalPosition;
        }
        else
        {
            Vector3 targetPosition = agent.GetNextPathPosition();
            direction = GlobalPosition.DirectionTo(targetPosition) * speed;
        }

        agent.TargetDesiredDistance = 5f;
        TryMove(direction);

        var animDir = GlobalBasis.Inverse() * direction * Velocity.Length() * walkAnimSpeed;
        animWalkingDirection = new Vector2(-animDir.X, animDir.Z);

        FootStep(delta);

        FindDoor();
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
        await ToSignal(GetTree().CreateTimer(attackCooldownTime), SceneTreeTimer.SignalName.Timeout);
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

    public void FindDoor()
    {
        if (IsInstanceValid(doorFinder))
        {
            doorFinder.ForceShapecastUpdate();
            for (int i = 0; i < doorFinder.GetCollisionCount(); i++)
            {
                var body = doorFinder.GetCollider(i);
                if (body is DoorButton btn)
                {
                    InteractWith(btn);
                }
            }
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

    private bool IsValid049(Node3D node)
    {
        return IsInstanceValid(node) && node is IPlayerController plr && plr.Player.RoleIndex == (int)RoleID.SCP049;
    }

    private bool IsValidHuman(Node node)
    {
        return IsInstanceValid(node) && node is IPlayerController plr && plr.Player.Role.team != TeamID.Dead && plr.Player.Role.team != TeamID.SCP;
    }

    private void UpdateTarget049()
    {
        if (!IsValid049(target049))
        {
            // find new SCP-049
            foreach (var player in RoundManager.Instance.players.Players)
            {
                if (IsValid049(player.ActiveController.Root))
                {
                    target049 = player.ActiveController.Root;
                    isTarget049Dirty = true;
                    return;
                }
            }
        }
    }

    private void UpdateTargetHuman()
    {
        if (!IsValidHuman(target))
        {
            // find new Human Target
            Node3D closestPlayer = null;
            float distSqr = maxRange * maxRange;
            foreach (var player in RoundManager.Instance.players.Players)
            {
                if (!IsValidHuman(player.ActiveController.Root))
                {
                    continue;
                }
                if (player.PlayerPosition.DistanceSquaredTo(GlobalPosition) < distSqr)
                {
                    closestPlayer = player.ActiveController.Root;
                    distSqr = player.PlayerPosition.DistanceSquaredTo(GlobalPosition);
                }
            }
            if (IsValidHuman(closestPlayer))
            {
                target = closestPlayer;
                isTargetDirty = true;
            }
        }
    }

    private void TryMove(Vector3 direction)
    {
        double delta = GetPhysicsProcessDeltaTime();
        Vector3 dirRot = direction;
        dirRot.Y = 0f;
        if (isAttacking && IsInstanceValid(target))
        {
            var playerDir = GlobalPosition.DirectionTo(target.GlobalPosition);
            playerDir.Y = 0f;
            playerDir = playerDir.Normalized();
            if (!playerDir.IsZeroApprox())
            {
                Quaternion = Quaternion.Slerp(Basis.LookingAt(playerDir).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);
            }
        }
        else if (!dirRot.IsZeroApprox())
            Quaternion = Quaternion.Slerp(Basis.LookingAt(dirRot).GetRotationQuaternion(), rotationLerpSpeed * (float)delta);

        Vector3 fwd = -GlobalBasis.Z.Normalized() * direction.Length();

        if (agent.AvoidanceEnabled)
            agent.Velocity = fwd;
        else
            OnVelocity(fwd);

    }

    private void OnVelocity(Vector3 safeVelocity)
    {
        double delta = GetPhysicsProcessDeltaTime();
        /*
        float speed = 1f;
        if (isAttacking)
            speed = 0f;
        Position += safeVelocity * (float)delta;
        */
        // /*
        Vector3 direction = safeVelocity;
        Vector3 vel = Velocity;
        if (!IsOnFloor())
        // if (lastCollision != null && (!IsInstanceValid(lastCollision.collider) || lastCollision.normal.AngleTo(UpDirection) <= FloorMaxAngle))
            vel.Y -= gravity * (float)delta;
        else
            vel.Y = 0f;
        float y = vel.Y;
        direction.Y = 0f;
        // direction = direction.Normalized();
        // vel = vel.MoveToward(-GlobalBasis.Z.Normalized() * speed, accel * (float)delta);
        // vel = vel.MoveToward(direction, accel * (float)delta);
        vel = direction;
        if (isAttacking || direction.IsZeroApprox())
            vel = Vector3.Zero;
        vel.Y = y;
        Velocity = vel;

        // DebugDrawManager.Instance.DrawDebugLine(GlobalPosition, GlobalPosition + Velocity, Colors.White);

        Vector3 val = this.MoveAndStairs(delta, Velocity, UpDirection, FloorSnapLength);
        Position += val;

        MoveAndSlide();
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
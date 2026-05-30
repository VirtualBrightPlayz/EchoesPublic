using Godot;
using System;
using System.Linq;
using Array = System.Array;

[GlobalClass]
public partial class FPController : CharacterBody3D, IPlayerControllerExt, IElevatorTeleport, IHealth
{
    public Func<float[], float> ModifierCalculator => Player.ModifierCalculator;

    [Export]
    public float Speed = 5.0f;
    public float ExtraSpeed = 1f; // TODO: replace this with a status effect when that system is refactored.

    public float EffectiveSpeed
    {
        get
        {
            float mult = ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IMovementSpeedModifier && x.Active).Cast<IMovementSpeedModifier>().Where(x => x.SpeedModifierActive).Select(x => x.SpeedModifier).ToArray());
            return Player.Role.Speed * mult;
        }
    }

    [Export]
    public float Accel = 100.0f;

    public float EffectiveAccel
    {
        get
        {
            float mult = ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IAccelModifier && x.Active).Cast<IAccelModifier>().Where(x => x.AccelModifierActive).Select(x => x.AccelModifier).ToArray());
            return Player.Role.Acceleration * mult;
        }
    }

    [Export]
    public float SprintMultiplier = 2.0f;

    public float EffectiveSprintMultiplier
    {
        get
        {
            float mult = ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is ISprintModifier && x.Active).Cast<ISprintModifier>().Where(x => x.SprintModifierActive).Select(x => x.SprintModifier).ToArray());
            return Player.Role.SprintSpeedMultiplier * mult;
        }
    }

    [Export]
    public float SneakMultiplier = 0.25f;

    public float EffectiveSneakMultiplier
    {
        get
        {
            float mult = ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is ISneakModifier && x.Active).Cast<ISneakModifier>().Where(x => x.SneakModifierActive).Select(x => x.SneakModifier).ToArray());
            return Player.Role.CrouchSpeedMultiplier * mult;
        }
    }

    [Export]
    public float JumpVelocity = 5.0f;

    public float EffectiveJumpVelocity
    {
        get
        {
            float mult = ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IJumpModifier && x.Active).Cast<IJumpModifier>().Where(x => x.JumpModifierActive).Select(x => x.JumpModifier).ToArray());
            if (Player.TryGetAbility(out StatueAbility statue) && !RoundManager.Instance.IsBlinking && statue.seen)
            {
                return 0f;
            }
            return Player.Role.JumpVelocity * mult;
        }
    }

    [Export]
    public float GravityMulti = 1.0f;

    public float EffectiveGravityMultiplier
    {
        get
        {
            float mult = ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IGravityModifier && x.Active).Cast<IGravityModifier>().Where(x => x.GravityModifierActive).Select(x => x.GravityModifier).ToArray());
            return Player.Role.GravityMultiplier * mult;
        }
    }

    public float EffectiveStaminaIncreaseMultiplier
    {
        get
        {
            return ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IStaminaIncreaseModifier && x.Active).Cast<IStaminaIncreaseModifier>().Where(x => x.StaminaIncreaseModifierActive).Select(x => x.StaminaIncreaseModifier).ToArray());
        }
    }

    public float EffectiveStaminaDecreaseMultiplier
    {
        get
        {
            return ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IStaminaDecreaseModifier && x.Active).Cast<IStaminaDecreaseModifier>().Where(x => x.StaminaDecreaseModifierActive).Select(x => x.StaminaDecreaseModifier).ToArray());
        }
    }

    public float EffectiveMaxStaminaMultiplier
    {
        get
        {
            return ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IMaxStaminaModifier && x.Active).Cast<IMaxStaminaModifier>().Where(x => x.MaxStaminaModifierActive).Select(x => x.MaxStaminaModifier).ToArray());
        }
    }

    public float EffectiveStaminaPauseTimerMultiplier
    {
        get
        {
            return ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IStaminaPauseTimerModifier && x.Active).Cast<IStaminaPauseTimerModifier>().Where(x => x.StaminaPauseModifierActive).Select(x => x.StaminaPauseModifier).ToArray());
        }
    }

    public float EffectiveFOVMultiplier
    {
        get
        {
            return ModifierCalculator(Player.statusEffectManager.GetEffects(x => x is IFOVModifier && x.Active).Cast<IFOVModifier>().Where(x => x.FOVModifierActive).Select(x => x.FOVModifier).ToArray());
        }
    }

    [Export]
    public Node3D head;
    [Export]
    public Node3D speakingIndicator;
    [Export]
    public Camera3D camera;
    [Export]
    public FireWorldEffects FireEffect; // TODO: move this to the PlayerModel script
    // [Export]
    public string defaultFootsteps => Player.Role.DefaultFootsteps;
    public string lastFootsteps;
    // [Export]
    public bool forceDefaultFootsteps => Player.Role.ForceDefaultFootsteps;
    [Export]
    public AudioStream breaths;
    [Export]
    public AudioStream outOfBreath;
    [Export]
    public AudioStreamPlayer3D footstepsAudio;
    // [Export]
    public float footstepInterval => Player.Role.FootstepInterval;// = 0.1f;
    [Export]
    public bool Noclip = false;
    // [Export]
    public GameEffect ragdollEffect => Player.Role.RagdollEffect;
    [Export]
    public CollisionShape3D collider;

    public Node3D View => head;
    public virtual Node3D Floor => this;
    public Camera3D Camera => camera;
    public int Authority => GetMultiplayerAuthority();
    public BasePlayer Player { get; set; }

    public float Health { get => Player.Health; set => Player.Health = value; }
    public float MaxHealth { get => Player.MaxHealth; set => throw new InvalidOperationException(); }

    protected double footstepTimer;
    protected double lastFootstepTimer;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = 1f;

    public Vector2 MovementDirection => Player.GetStickPadActionData(LocalPlayerInput.PlayerMove);
    public Vector2 MouseMotion => Player.GetStickPadActionData(LocalPlayerInput.PlayerCamera) * Settings.User.MouseSensitivity;
    public ButtonInputFlags ButtonUse = ButtonInputFlags.None;
    public ButtonInputFlags ButtonPrimary = ButtonInputFlags.None;
    public ButtonInputFlags ButtonSecondary = ButtonInputFlags.None;
    public ButtonInputFlags ButtonJump = ButtonInputFlags.None;
    public ButtonInputFlags ButtonSprint = ButtonInputFlags.None;
    public ButtonInputFlags ButtonCrouch = ButtonInputFlags.None;
    public ButtonInputFlags ButtonNoclip = ButtonInputFlags.None;

    public bool IsSprinting = false;
    public bool IsCrouching = false;
    public bool NoClipEnabled = true;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(GetParent().GetMultiplayerAuthority());
        SetMeta(IHealth.MetaName, this);
    }

    public override void _Ready()
    {
        gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        if (Player.IsLocalPlayer)
        {
            camera.Current = true;
        }
    }

    public override void _Process(double delta)
    {
        IPlayerController controller = this;
        if (!controller.IsControllerActive)
            return;
        if (!Player.HasAuthority)
            return;
        if (IsInstanceValid(Player.model))
        {
            Player.model.GlobalTransform = Player.ActiveController.Root.GlobalTransform;
        }
        if (!Player.IsLocalPlayer)
            return;
        ViewBob(delta);
        ApplyRotation(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        IPlayerController controller = this;

        if (IsInstanceValid(Player) && IsInstanceValid(collider) && collider.Shape is CapsuleShape3D capsule)
        {
            capsule.Height = Player.Role.CollisionHeight;
            collider.Position = new Vector3(0f, Player.Role.CollisionHeight / 2f, 0f);
            collider.Disabled = !controller.IsControllerActive;
        }
        if (footstepsAudio.Stream is not AudioStreamPolyphonic)
        {
            footstepsAudio.VolumeLinear = Mathf.Lerp(footstepsAudio.VolumeLinear, 0f, (float)delta * 5f);
        }

        if (!controller.IsControllerActive)
            return;

        UpdateInputs();
        if (!Player.HasAuthority)
            return;

        if (Noclip && NoClipEnabled)
            NoclipMovement(delta);
        else
            Movement(delta);
        ApplySprint(delta);
        FootStep(delta);
    }

    public virtual void UpdateInputs()
    {
        if (!Player.IsLocalPlayer)
        {
            return;
        }
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerUse, ref ButtonUse);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerPrimary, ref ButtonPrimary);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSecondary, ref ButtonSecondary);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerJump, ref ButtonJump);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSprint, ref ButtonSprint);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSneak, ref ButtonCrouch);
        InputManager.UpdateInput(Player, LocalPlayerInput.AdminNoclip, ref ButtonNoclip);
        UpdateToggles();
    }

    public virtual void UpdateToggles()
    {
        IsSprinting = ButtonSprint.HasFlag(ButtonInputFlags.Pressed);
        IsCrouching = ButtonCrouch.HasFlag(ButtonInputFlags.Pressed);
        if (ButtonNoclip.HasFlag(ButtonInputFlags.JustPressed))
        {
            NoClipEnabled = !NoClipEnabled;
        }
    }

    public virtual void ApplyRotation(double delta)
    {
        var mouseMotion = MouseMotion;
        // TODO: janky workaround
        if (Player.TryGetAbility(out GrabbingAbility grabbing) && IsInstanceValid(grabbing.grabbed) && ButtonUse.HasFlag(ButtonInputFlags.Pressed))
        {
            return;
        }
        foreach (var ability in Player.Abilities)
        {
            ability.ModifyMouseMotion(this, ref mouseMotion);
        }
        ApplyMouseMotion(delta, mouseMotion);
    }

    public virtual void ApplyMouseMotion(double delta, Vector2 mouseMotion)
    {
        Rotation += new Vector3(0, mouseMotion.X, 0);
        if (!InputManager.Instance.IsVR)
        {
            head.Rotation += new Vector3(mouseMotion.Y, 0, 0);
            head.Rotation = new Vector3(Mathf.Clamp(head.Rotation.X, -1.55f, 1.55f), head.Rotation.Y, head.Rotation.Z);
        }
    }

    public virtual void ResetViewCamera()
    {
        Rotation = Rotation with { X = 0f, Z = 0f };
        head.Rotation = Vector3.Zero;
        camera.Rotation = Vector3.Zero;
    }

    public virtual void ViewBob(double delta)
    {
        head.Position = new Vector3(0f, Player.Role.CameraHeight, 0f);
        if (IsInstanceValid(speakingIndicator))
            speakingIndicator.Position = new Vector3(0f, Player.Role.CollisionHeight + 0.55f, 0f);
    }

    public static StringName FootstepMetaName = "footsteps";

    public virtual void FootStep(double delta)
    {
        if (IsOnFloor() && (!IsCrouching || Player.Role.team == TeamID.SCP))
        {
            footstepTimer += delta * Velocity.Length() / EffectiveSpeed;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                if (!forceDefaultFootsteps && !string.IsNullOrEmpty(lastFootsteps))
                {
                    Rpc(MethodName.RpcFootStep, lastFootsteps);
                }
                else
                {
                    Rpc(MethodName.RpcFootStep, defaultFootsteps);
                }
            }
        }
        lastFootstepTimer = footstepTimer;
    }

    public void UpdateLastFootsteps()
    {
        bool found = false;
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (IsInstanceValid(collision))
            {
                if (collision.GetCollider() is PhysicsBody3D body && body.HasMeta(FootstepMetaName))
                {
                    Variant meta = body.GetMeta(FootstepMetaName);
                    lastFootsteps = meta.AsString();
                    found = true;
                }
                else if (collision.GetColliderShape() is CollisionShape3D collider && collider.HasMeta(FootstepMetaName))
                {
                    Variant meta = collider.GetMeta(FootstepMetaName);
                    lastFootsteps = meta.AsString();
                    found = true;
                }
                else
                {
                    // lastFootsteps = defaultFootsteps;
                }
            }
        }
        if (!found)
        {
            lastFootsteps = defaultFootsteps;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcFootStep(string footstepsSoundName)
    {
        if (IsInstanceValid(footstepsAudio) && Player.Data.TryGetSound(footstepsSoundName, out GameSound footsteps))
        {
            if (IsInstanceValid(footsteps))
            {
                if (footsteps.forceLoop)
                {
                    if (footstepsAudio.Stream != footsteps.Stream)
                    {
                        footstepsAudio.Stream = footsteps.Stream;
                    }
                    footstepsAudio.VolumeDb = footsteps.volumeDb;
                    footstepsAudio.UnitSize = footsteps.unitSize;
                    footstepsAudio.MaxDistance = footsteps.maxDistance;
                    footstepsAudio.PitchScale = footsteps.pitch;
                    if (!footstepsAudio.Playing)
                    {
                        footstepsAudio.Play();
                    }
                }
                else if (footstepsAudio.Stream is not AudioStreamPolyphonic)
                {
                    footstepsAudio.Stream = new AudioStreamPolyphonic();
                    footstepsAudio.Play();
                    footstepsAudio.VolumeDb = 0f;
                    footstepsAudio.PitchScale = 1f;
                    footstepsAudio.UnitSize = footsteps.unitSize;
                    footstepsAudio.MaxDistance = footsteps.maxDistance;
                }
                if (footstepsAudio.GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
                {
                    playback.PlayStream(footsteps.Stream, volumeDb: footsteps.volumeDb, pitchScale: footsteps.pitch);
                }
            }
        }
    }

    public virtual float GetSpeedMultiplier()
    {
        bool isSneaking = IsCrouching;
        bool isSprinting = CanSprint();
        float multiplier = isSneaking ? EffectiveSneakMultiplier : isSprinting ? EffectiveSprintMultiplier : 1.0f;
        float final = multiplier * ExtraSpeed;
        foreach (var ability in Player.Abilities)
        {
            ability.ModifySpeedMultiplier(this, ref final);
        }
        return final;
    }

    public virtual bool CanSprint()
    {
        bool can = Player.TryGetAbility(out StaminaAbility ability) && ability.sprintStamina > 0 && IsSprinting;
        foreach (var ab in Player.Abilities)
        {
            ab.ModifyCanSprint(this, ref can);
        }
        return can;
    }

    public virtual void ApplySprint(double delta)
    {
        if (Player.TryGetAbility(out StaminaAbility ability))
        {
            ability.ApplySprint(this, delta, false);
        }
    }

    public virtual Vector3 GetMovementDirection()
    {
        Vector2 inputDir = MovementDirection;
        Vector3 direction = (GlobalBasis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        foreach (var ability in Player.Abilities)
        {
            ability.ModifyMovementDirection(this, ref direction);
        }
        return direction;
    }

    public virtual Vector3 GetMovementVelocity(Vector3 vel, double delta)
    {
        Vector3 velocity = vel;

        Vector3 mask = GetGravity().Normalized().Abs();
        Vector3 invMask = Vector3.One - mask;

        float multiplier = GetSpeedMultiplier();

        Vector3 direction = GetMovementDirection();
        Vector3 targetVel = direction * EffectiveSpeed * multiplier;

        targetVel = targetVel * invMask + velocity * mask;
        targetVel = velocity.MoveToward(targetVel, EffectiveAccel * (float)delta);
        targetVel += GetGravity() * (float)delta * EffectiveGravityMultiplier;

        // Handle Jump.
        if (ButtonJump.HasFlag(ButtonInputFlags.JustPressed) && IsOnFloor())
        {
            targetVel += GetGravity().Normalized().Abs() * EffectiveJumpVelocity;
            footstepTimer = footstepInterval;
            if (IsInstanceValid(Player.model) && IsInstanceValid(Player.model.anims))
            {
                Player.model.anims.Rpc(PlayerAnims.MethodName.RpcJump, false);
            }
        }

        return targetVel;
    }

    public virtual void NoclipMovement(double delta)
    {
        float multiplier = GetSpeedMultiplier();
        Vector2 inputDir = MovementDirection;
        Vector3 direction = (camera.GlobalBasis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * EffectiveSpeed * 2f * multiplier;

        Velocity = Vector3.Zero;
        GlobalPosition += direction * (float)delta;
    }

    public virtual void Movement(double delta)
    {
        bool wasOnFloor = IsOnFloor();
        Vector3 velocity = Velocity;

        // UpDirection = -GetGravity().Normalized();

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

        Vector3 val = this.MoveAndStairs(delta, GetMovementDirection() * EffectiveSpeed * GetSpeedMultiplier(), UpDirection, FloorSnapLength);
        GlobalPosition += val;

        MoveAndSlide();

        Vector3 angVel = GetPlatformAngularVelocity();
        Rotation += angVel * Vector3.Up * (float)delta;

        if (!wasOnFloor && IsOnFloor())
        {
            if (IsInstanceValid(Player.model) && IsInstanceValid(Player.model.anims))
            {
                Player.model.anims.Rpc(PlayerAnims.MethodName.RpcJump, true);
            }
        }

        UpdateLastFootsteps();
    }

    public virtual void ElevatorTeleport(Vector3 position, Vector3 rotation)
    {
        Player.ForceTeleport(position, rotation);
    }

    public virtual void OnKilled(DamageInfo info)
    {
        int idx = Array.IndexOf(Player.Data.Effects, ragdollEffect);
        if (idx != -1)
        {
            var ragdollParent = ragdollEffect.scene.Instantiate<Node3D>();
            ragdollParent.Position = Player.PlayerPosition;
            ragdollParent.Rotation = new Vector3(0f, Player.PlayerRotation.Y, 0f);
            var ragdoll = ragdollParent.FindChildren("*").FirstOrDefault(x => x is Ragdoll) as Ragdoll;
            if (!IsInstanceValid(ragdoll))
            {
                Log.PrintWarn("Ragdoll class not found: " + ragdollParent.Name);
                ragdollParent.QueueFree();
                return;
            }
            ragdoll.Info = info;
            ragdoll.playerName = Player.AttackerDisplayName;
            ragdoll.roleId = Player.RoleIndex;
            ragdoll.TypeId = info?.TypeId ?? (int)DamageType.Unknown;
            foreach (string bone in RagdollMaker.HumanBones)
            {
                int boneId = Player.model.skeleton.FindBone(bone);
                if (boneId == -1)
                {
                    Log.PrintWarn("Can't find bone by name: " + bone);
                    continue;
                }
                Transform3D xform = Player.model.skeleton.GetBonePose(boneId);
                ragdoll.boneLocations.Add(bone, xform);
            }
            ItemManager.Instance.SpawnNode.AddChild(ragdollParent, true);
        }
    }

    public virtual void OnDamaged(DamageInfo info)
    {
        if (info.Source == NetworkPlayer.LocalInstance)
        {
            //prevents self damage from firing hitmarkers.
            if (Player == NetworkPlayer.LocalInstance)
            {
                return;
            }
            NetworkPlayer.LocalInstance.Hud.OnHit(info, Player);
        }
    }

    public virtual void Spawn(HealInfo info) => throw new NotImplementedException();
    public virtual void Damage(DamageInfo info) => Player.Damage(info);
    public virtual void Heal(HealInfo info) => Player.Heal(info);
    public virtual void Kill(DamageInfo info) => Player.Kill(info);
}

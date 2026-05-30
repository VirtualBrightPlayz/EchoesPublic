using Godot;
using System;
using System.Linq;

public partial class PlayerController : FPController
{
    [Export]
    [Obsolete]
    public float standingCameraHeight = 2.2f;
    [Export]
    public float viewBobOffsetScale = 0.1f;
    [Export]
    public float viewBobRotationScale = 1.2f;
    [Export]
    public float viewBobSpeed = 0.7f;
    [Export]
    public float viewBobFrequencyMultiplier = 2.0f;

    [Export]
    public float StandardFov = 70f;
    [Export]
    public float SprintFov = 76f;
    [Export]
    public float sprintFovLerpSpeed = 8f;
    [Export]
    public float aimFovLerpSpeed = 10f;
    [Export]
    public float aimMulti = 2f;

    [ExportGroup("Recoil")]
    [Export]
    public Vector2 timedRecoil = Vector2.Zero;
    [Export]
    public float recoilSpeed = 1f;
    [Export]
    public float aimMultiRecoil = 10f;
    [Export]
    public Timer recoilTimer;

    public float CalcRecoilSpeed => recoilTimer.IsStopped() ? (IsAimingWithGun ? recoilSpeed * aimMultiRecoil : recoilSpeed) : 0f;
    public bool IsAimingWithGun => Player.IsAimingDown && Player.TryGetAbility(out InventoryAbility inventory) && inventory.InventoryEquipped.Any(x => x.model is GunBase);
    
    [ExportGroup("Fall Damage")]
    [Export]
    public float minimumFallTime = 1f;
    [Export]
    public float minimumFallDistance = 1.5f;
    [Export]
    public bool doFallDamge = true;
    [Export]
    public float fallDamgeMultiplier = 3.5f;
    [Export]
    public GameSound fallDamageSound;
    
    protected double viewBobTimer;
    protected Vector3 lastPosition;
    protected Vector3 lastFloorPosition;
    protected ulong lastTimeOnFloor;
    
    protected float landingImpact;
    protected bool wasOnFloor;
    protected float verticalVelocity;
    protected float landingImpactTimer;
    protected float currentLandingImpact;
    protected float jumpTilt;
    
    protected float strafeTilt;

    protected ButtonInputFlags buttonInventory = ButtonInputFlags.None;

    public override void _Ready()
    {
        base._Ready();
        lastTimeOnFloor = Time.GetTicksMsec();
        lastFloorPosition = Position;
        wasOnFloor = true;
        camera.Fov = StandardFov;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Player.HasAuthority)
            return;
            
        HandleFov(delta);
        
        //bool spacePressed = Input.IsKeyPressed(Key.Space);

        if(!IsOnFloor())
        {
            inAir = true;
        }
        
        if (IsOnFloor() && inAir)
        {
            inAir = false;
        }
        // if (spacePressed && !spaceWasPressed) 
        // {
        //  	jumpTilt = jumpTiltAmount;
        // }
        //spaceWasPressed = spacePressed;
    }

    private bool inAir = false;

    public virtual void HandleFov(double delta)
    {
        float targetFov;
        float lerpSpeed;

        if (Player.TryGetAbility(out InventoryAbility inventory) && inventory.InventoryEquipped.FirstOrDefault(x => x.model is GunBase)?.model is GunBase gun && IsInstanceValid(camera))
        {
            if (Player.IsAimingDown)
            {
                targetFov = gun.aimFov;
                lerpSpeed = aimFovLerpSpeed;
            }
            else
            {
                targetFov = IsSprinting2() ? SprintFov : StandardFov;
                lerpSpeed = sprintFovLerpSpeed;
            }
        }
        else
        {
            targetFov = IsSprinting2() ? SprintFov : StandardFov;
            lerpSpeed = sprintFovLerpSpeed;
        }

        targetFov *= EffectiveFOVMultiplier;
        
        camera.Fov = Mathf.Lerp(camera.Fov, targetFov, (float)delta * lerpSpeed);
    }

    private bool IsSprinting2()
    {
        return CanSprint() && 
               !Player.IsAimingDown && 
               Velocity.Length() > Speed * 0.9f && 
               IsOnFloor();
    }

    public override bool CanSprint()
    {
        if (IsAimingWithGun)
            return false;
        return base.CanSprint();
    }

    public override void _PhysicsProcess(double delta)
    {
        IPlayerController controller = this;

        verticalVelocity = Velocity.Y;

        base._PhysicsProcess(delta);

        if (IsOnFloor())
        {
            wasOnFloor = true;
        }
        else
        {
            wasOnFloor = false;
        }

        if (Multiplayer.IsServer())
        {
            FallDamage(delta);
        }
        if (Player.model != null && Player.model.anims != null && Player.HasAuthority)
        {
            float targetWalk;
            if (MovementDirection.IsZeroApprox() || !controller.IsControllerActive)
            {
                targetWalk = 0f;
            }
            else
            {
                targetWalk = Velocity.Length() / EffectiveSpeed;
            }
            Player.model.anims.walkingDirection = MovementDirection * Mathf.Clamp(targetWalk, 0f, 1f);
            Player.model.anims.walking = targetWalk;
            lastPosition = Position;
        }
    }

    public virtual void FallDamage(double delta)
    {
        KinematicCollision3D collision = new KinematicCollision3D();
        bool isOnFloor = false;
        if (!Noclip)
        {
            if (TestMove(GlobalTransform, Vector3.Down * FloorSnapLength, collision))
            {
                if (collision.GetNormal().AngleTo(UpDirection) <= FloorMaxAngle)
                {
                    isOnFloor = true;
                }
            }
        }
        else
            isOnFloor = true;
        ulong ticksSinceEngineStarted = Time.GetTicksMsec();
        if (isOnFloor)
        {
            if (!Noclip && doFallDamge && lastTimeOnFloor + minimumFallTime * 1000 < ticksSinceEngineStarted && lastFloorPosition.Y - minimumFallDistance > Position.Y)
            {
                DamageInfo damageInfo = new DamageInfo((lastFloorPosition.Y - Position.Y) * fallDamgeMultiplier, Player, DamageType.Fall);
                Damage(damageInfo);
                int indexOfFallDamage = Player.Data.Sounds.IndexOf(fallDamageSound);
                if (indexOfFallDamage != -1)
                    Player.Rpc(nameof(BasePlayer.CL_PlaySound3D), indexOfFallDamage);
            }
            lastTimeOnFloor = ticksSinceEngineStarted;
            lastFloorPosition = Position;
        }
    }

    public override void ApplyRotation(double delta)
    {
        var start = new Vector2(Rotation.Y, head.Rotation.X);
        base.ApplyRotation(delta);
        if (InputManager.Instance.IsVR)
            return;
        timedRecoil = timedRecoil.Clamp(-45f, 45f).Lerp(Vector2.Zero, Mathf.Min((float)delta * CalcRecoilSpeed, 1f));
        
        float verticalRotation = Mathf.DegToRad(timedRecoil.Y);
        verticalRotation += Mathf.DegToRad(currentLandingImpact);
        verticalRotation += Mathf.DegToRad(jumpTilt);
        
        camera.Rotation = new Vector3(
            verticalRotation,
            Mathf.DegToRad(timedRecoil.X),
            Mathf.DegToRad(strafeTilt)
        );

        var end = new Vector2(Rotation.Y, head.Rotation.X);
    }

    public override void ViewBob(double delta)
    {
        if (IsOnFloor())
        {
            viewBobTimer += delta * Velocity.Length() * viewBobSpeed;
        }
        
        float frequency = viewBobFrequencyMultiplier;
        float horizontalBob = Mathf.Sin((float)viewBobTimer * frequency) * viewBobOffsetScale;
        float verticalBob = (Mathf.Sin((float)viewBobTimer * frequency * 2) * 0.5f + 0.5f) * viewBobOffsetScale;
        float rollBob = Mathf.Sin((float)viewBobTimer * frequency * 0.5f) * viewBobRotationScale * 0.5f;
        
        head.Position = new Vector3(
            horizontalBob,
            Player.Role.CameraHeight + verticalBob,
            0f
        );
        
        camera.RotateZ(Mathf.DegToRad(rollBob) * (float)delta * 5f);

        if (IsInstanceValid(speakingIndicator))
            speakingIndicator.Position = new Vector3(0f, Player.Role.CollisionHeight + 0.55f, 0f);
    }
}

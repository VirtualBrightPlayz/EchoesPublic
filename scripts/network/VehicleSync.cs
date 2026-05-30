using System.Linq;
using Godot;

[GlobalClass]
public partial class VehicleSync : VehicleBody3D, IDamageSource
{
    [Export]
    public TransformSync xformSync;
    [Export]
    public PlayerSeat driverSeat;
    [Export]
    public float EngineTarget = 1f;
    [Export]
    public float EngineAccel = 1f;
    [Export]
    public float SteeringTarget = 45f;
    [Export]
    public float SteeringAccel = 45f;
    [Export]
    public Curve SteeringSpeedCurve;
    [Export]
    public float BrakeTarget = 1f;
    [Export]
    public float BrakeAccel = 1f;

    public override void _Ready()
    {
        base._Ready();
        UpdateFreezeState();
        xformSync.OnSync += _OnSync;
        xformSync.OnClaimantChanged += UpdateFreezeState;
        xformSync.OnLastPositionSet += UpdateFreezeState;
        if (IsMultiplayerAuthority())
        {
            xformSync.SetClaimant((int)MultiplayerPeer.TargetPeerBroadcast);
        }
    }

    public BasePlayer Player => driverSeat.Player;
    private Vector2 MovementDirection => Player.GetStickPadActionData(LocalPlayerInput.PlayerMove);
    private ButtonInputFlags ButtonJump;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        IPlayerController controller = driverSeat;

        if (!xformSync.IsClaimant() && !IsMultiplayerAuthority())
        {
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            Brake = 0f;
            EngineForce = 0f;
        }

        if (!controller.IsControllerActive && IsMultiplayerAuthority())
        {
            Brake = Mathf.MoveToward(Brake, BrakeTarget, (float)delta * BrakeAccel);
            // Brake = BrakeTarget;
            // EngineForce = Mathf.MoveToward(EngineForce, 0f, (float)delta * EngineAccel);
            EngineForce = 0f;
            // Steering = Mathf.MoveToward(Steering, 0f, Mathf.DegToRad((float)delta * SteeringAccel));
        }
        else if (controller.IsControllerActive && Player.IsLocalPlayer)
        {
            InputManager.UpdateInput(Player, LocalPlayerInput.PlayerJump, ref ButtonJump);
            Vector2 movement = (MovementDirection * 2f).LimitLength();

            if (ButtonJump.HasFlag(ButtonInputFlags.Pressed))
            {
                Brake = Mathf.MoveToward(Brake, BrakeTarget, (float)delta * BrakeAccel);
            }
            else
            {
                Brake = Mathf.MoveToward(Brake, 0f, (float)delta * BrakeAccel);
            }
            EngineForce = Mathf.MoveToward(EngineForce, -movement.Y * EngineTarget, (float)delta * EngineAccel);
            float targetSteering = SteeringTarget;
            if (IsInstanceValid(SteeringSpeedCurve))
            {
                targetSteering *= SteeringSpeedCurve.Sample(LinearVelocity.Length() / EngineTarget);
            }
            Steering = Mathf.MoveToward(Steering, Mathf.DegToRad(-movement.X * targetSteering), Mathf.DegToRad((float)delta * SteeringAccel));
        }
    }

    private void _OnSync()
    {
        if (xformSync.IsClaimant())
        {
            Rpc(MethodName.Rpc_SetSteering, Steering, EngineForce);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void Rpc_SetSteering(float amount, float engine)
    {
        if (IsInstanceValid(Player) && Player.CallerHasAuthority)
        {
            Steering = amount;
            EngineForce = engine;
        }
    }

    public virtual void UpdateFreezeState()
    {
        // Freeze = !xformSync.IsClaimant() && !IsMultiplayerAuthority();
    }

    public virtual void DamageOther(IHealth health, DamageInfo info, Vector3 vel)
    {
        info.AddModifier(new PropDamageModifier());
        info.AddModifier(new ExplosiveDamageModifier(vel));
        health.Damage(info);
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        UpdateState(state);
    }

    public virtual void UpdateState(PhysicsDirectBodyState3D state)
    {
        if (IsMultiplayerAuthority())
        {
            for (int i = 0; i < state.GetContactCount(); i++)
            {
                UpdateCollisionWith(state, i);
            }
        }
    }

    public virtual void UpdateCollisionWith(PhysicsDirectBodyState3D state, int i)
    {
        GodotObject collider = state.GetContactColliderObject(i);
        IHealth hp = IHealth.GetHealth(collider);
        Vector3 vel = state.GetContactLocalVelocityAtPosition(i);
        Vector3 norm = state.GetContactLocalNormal(i);
        if (hp != null && !xformSync.IsClaimantServer())
        {
            float len = vel.Length();
            if (len >= 5f)
            {
                DamageOther(hp, new DamageInfo(len, this, DamageType.Crushed, Player?.Role?.team ?? TeamID.Dead), vel);
            }
        }
    }

    public virtual string AttackerDisplayName => "Physics body";

    public NodePath AbsolutePath => this.GetMultiplayerRoot().GetPathTo(this);
}
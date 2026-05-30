using Godot;

[GlobalClass]
public partial class BackDraftAbility : BaseAbility
{
    public enum State : int
    {
        Idle = 0,
        InUse = 1,
    }

    [Export] public ShapeCast3D cast;
    [Export] public float staminaRequired = 1f;
    [Export] public float dashSpeed = 1f;
    [Export] public double dashTime = 1d;
    [Export] public float damageAmountPerSecond = 100f;
    [Export] public DamageType damageType = DamageType.Generic;

    public Timer timer;
    public ButtonInputFlags ButtonSprint = ButtonInputFlags.None;
    public State state = State.Idle;

    public override void SetupFromDefinition()
    {
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "stamina_required", ref staminaRequired);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "dash_speed", ref dashSpeed);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "dash_time", ref dashTime);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "damage_per_second", ref damageAmountPerSecond);
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "damage_type", ref damageType);
    }

    public override void _Ready()
    {
        base._Ready();
        timer = new Timer()
        {
            Autostart = false,
            OneShot = true,
            ProcessCallback = Timer.TimerProcessCallback.Physics,
        };
        AddChild(timer);
        timer.Timeout += _Timeout;
        cast.Enabled = false;
        cast.Reparent(Player.RoleController.Root, false);
        cast.AddException(Player.MainCollider);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Player.IsLocalPlayer && Player.TryGetAbility(out StaminaAbility stamina))
        {
            InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSprint, ref ButtonSprint);
            if (ButtonSprint.HasFlag(ButtonInputFlags.JustPressed) && stamina.sprintStamina >= staminaRequired && state == State.Idle)
            {
                RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcTryAttack);
                stamina.sprintStamina = Mathf.Max(stamina.sprintStamina - staminaRequired, 0f);
                state = State.InUse;
                timer.Start(dashTime);
                stamina.ShowBar();
            }
        }
        if (Multiplayer.IsServer() && state == State.InUse)
        {
            cast.ForceShapecastUpdate();
            for (int i = 0; i < cast.GetCollisionCount(); i++)
            {
                var colliderNode = cast.GetCollider(i) as Node;
                if (colliderNode == Player.ActiveController.Root || Player.ActiveController.Root.IsAncestorOf(colliderNode))
                {
                    continue;
                }
                if (Player.model.IsAncestorOf(colliderNode))
                {
                    continue;
                }
                IHealth health = IHealth.GetHealth(colliderNode);
                if (health != null)
                {
                    health.Damage(new DamageInfo(damageAmountPerSecond * (float)delta, Player, damageType));
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcTryAttack()
    {
        if (!Multiplayer.IsServer())
            return;
        if (state == State.Idle)
        {
            state = State.InUse;
            timer.Start(dashTime);
        }
    }

    public void _Timeout()
    {
        state = State.Idle;
        // if (Player is NetworkPlayer plr2)
        //     plr2.Hud.SprintBarSpawnOut();
    }

    public override void ModifyCanSprint(FPController controller, ref bool value)
    {
        value = false;
    }

    public override void ModifyMovementDirection(FPController controller, ref Vector3 value)
    {
        if (state == State.InUse)
        {
            value = -controller.GlobalBasis.Z.Normalized();
        }
    }

    public override void ModifySpeedMultiplier(FPController controller, ref float value)
    {
        if (state == State.InUse)
        {
            value = dashSpeed;
        }
    }

    public override void ModifyMouseMotion(FPController controller, ref Vector2 value)
    {
        if (state == State.InUse)
        {
            value *= 0.5f;
        }
    }
}

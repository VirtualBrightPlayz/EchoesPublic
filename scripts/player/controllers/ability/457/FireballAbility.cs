using Godot;

[GlobalClass]
public partial class FireballAbility : BaseAbility
{
    public enum State : int
    {
        Idle = 0,
        WindUp = 1,
        Released = 2,
    }

    [Export] public float staminaRequired = 1f;
    // [Export] public Node3D fireballVisuals;
    [Export] public PackedScene fireballPrefab;

    public ButtonInputFlags ButtonRMB = ButtonInputFlags.None;
    public State state = State.Idle;

    public override void SetupFromDefinition()
    {
        TomlExtensions.GetValueOptional(AbilityDef.dataCfg, "stamina_required", ref staminaRequired);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Player.IsLocalPlayer && Player.TryGetAbility(out StaminaAbility stamina))
        {
            InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSecondary, ref ButtonRMB);
            if (ButtonRMB.HasFlag(ButtonInputFlags.JustReleased) && stamina.sprintStamina >= staminaRequired && state == State.Idle)
            {
                RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcSpawnBall);
                // state = State.WindUp;
                stamina.sprintStamina = Mathf.Max(stamina.sprintStamina - staminaRequired, 0f);
                stamina.ShowBar();
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcSpawnBall()
    {
        if (!Multiplayer.IsServer())
            return;
        if (state == State.Idle)
        {
            // state = State.WindUp;
            var node = fireballPrefab.Instantiate<ThrownGrenade>();
			node.throwerTeam = Player.Role.team;
            ItemManager.Instance.SpawnNode.AddChild(node, true);
            node.throwerPath = node.AbsolutePath;
            node.GlobalPosition = Player.AimTransform.Origin;
			node.SetDefaultVelocity(-Player.AimTransform.Basis.Z);
        }
    }
}

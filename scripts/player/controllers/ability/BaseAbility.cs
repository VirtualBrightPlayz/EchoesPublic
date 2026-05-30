using Godot;

[GlobalClass]
public abstract partial class BaseAbility : Node, IAbility
{
    public BasePlayer Player;
    public PlayerAbility AbilityDef;

    public override void _Ready()
    {
        base._Ready();
        if (IsInstanceValid(AbilityDef))
            SetupFromDefinition();
        if (Player.IsServer)
            OnSpawn(Player.Role);
    }

    public abstract void SetupFromDefinition();

    public virtual void OnSpawn(PlayerRole role)
    {
    }

    public virtual void OnKilled()
    {
    }

    public virtual void ModifyCanSprint(FPController controller, ref bool value)
    {
    }

    public virtual void ModifySpeedMultiplier(FPController controller, ref float value)
    {
    }

    public virtual void ModifyMovementDirection(FPController controller, ref Vector3 value)
    {
    }

    public virtual void ModifyMouseMotion(FPController controller, ref Vector2 value)
    {
    }
}

public interface IAbility
{
}

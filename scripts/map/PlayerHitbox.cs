using Godot;

[GlobalClass]
public partial class PlayerHitbox : Node, IHealth
{
    [Export]
    public NodePath HealthNode;
    [Export]
    public float DamageMultiplier = 1f;

    public IHealth HP => GetNodeOrNull<IHealth>(HealthNode);

    float IHealth.Health { get => HP.Health; set => HP.Health = value; }
    float IHealth.MaxHealth { get => HP.MaxHealth; set => HP.MaxHealth = value; }

    public override void _EnterTree()
    {
        base._EnterTree();
        GetParent().SetMeta(IHealth.MetaName, this);
        GetParent().SetMeta(ProjectileSimulationManager.PROJECTILE_RESISTANE_MEDIUM_KEY, ItemManager.Instance.fleshMedium);
    }

    public override void _Ready()
    {
        base._Ready();
        if (GetNode(HealthNode) is PlayerModel mdl)
        {
            HealthNode = GetPathTo(mdl.Player.ActiveControllerNode);
        }
    }

    public void Damage(DamageInfo info)
    {
        info.Hitbox = this;
        info.Amount *= DamageMultiplier;
        HP.Damage(info);
    }

    public void Heal(HealInfo info)
    {
        HP.Heal(info);
    }

    public void Kill(DamageInfo info)
    {
        info.Hitbox = this;
        info.Amount *= DamageMultiplier;
        HP.Kill(info);
    }

    public void Spawn(HealInfo info)
    {
        HP.Spawn(info);
    }
}
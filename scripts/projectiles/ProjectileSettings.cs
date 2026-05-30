using Godot;

[GlobalClass]
public partial class ProjectileSettings : Node
{
    [Export]
    public ProjectileResistanceMedium Medium;

    public override void _EnterTree()
    {
        base._EnterTree();
        GetParent().SetMeta(ProjectileSimulationManager.PROJECTILE_RESISTANE_MEDIUM_KEY, Medium);
    }
}

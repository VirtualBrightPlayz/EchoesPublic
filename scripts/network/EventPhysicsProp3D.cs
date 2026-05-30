using Godot;

[GlobalClass]
public partial class EventPhysicsProp3D : PhysicsProp3D
{
    [Signal]
    public delegate void OnSpawnEventHandler();
    [Signal]
    public delegate void OnDeathEventHandler();

    public override void Spawn(HealInfo info)
    {
        base.Spawn(info);
        EmitSignalOnSpawn();
    }

    public override void OnCustomDeath(DamageInfo info)
    {
        base.OnCustomDeath(info);
        EmitSignalOnDeath();
    }
}
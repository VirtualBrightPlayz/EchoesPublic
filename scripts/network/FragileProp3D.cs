using Godot;
using Godot.Collections;

[GlobalClass]
public partial class FragileProp3D : PhysicsProp3D
{
    [Export]
    public float MaximumVelocity { get; set; } = 1f;

    public override void UpdateCollisionWith(PhysicsDirectBodyState3D state, int i)
    {
        base.UpdateCollisionWith(state, i);
        // GodotObject collider = state.GetContactColliderObject(i);
        Vector3 vel = state.GetContactLocalVelocityAtPosition(i);
        float len = vel.Length();
        if (len >= MaximumVelocity && xformSync.IsClaimantServer() && !IsDead)
        {
            Kill(new DamageInfo(0f, DamageType.Crushed));
        }
    }
}
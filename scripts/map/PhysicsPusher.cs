using Godot;

[GlobalClass]
public partial class PhysicsPusher : Marker3D
{
    [Export]
    public RigidBody3D rb;
    [Export]
    public float Force = 1f;
    [Export]
    public bool Central = false;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (IsInstanceValid(rb) && IsVisibleInTree())
        {
            if (Central)
            {
                rb.ApplyCentralForce(-GlobalBasis.Z * Force);
            }
            else
            {
                rb.ApplyForce(-GlobalBasis.Z * Force, GlobalPosition - rb.GlobalPosition);
            }
        }
    }
}
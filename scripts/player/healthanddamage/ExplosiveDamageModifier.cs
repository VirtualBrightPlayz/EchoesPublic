using Godot;

public class ExplosiveDamageModifier : DamageInfoModifier
{
    public ExplosiveDamageModifier(Vector3 pushForce, bool applyToLiving = false)
    {
        PushForce = pushForce;
        ApplyToLiving = applyToLiving;
    }

    public ExplosiveDamageModifier(bool push, Node3D pushFrom, Node3D target, float force, bool applyToLiving = false)
    {
        Vector3 result;
        if (push)
        {
            result = pushFrom.GlobalPosition.DirectionTo(pushFrom.GlobalPosition) * force;
        }
        else
        {
            result = target.GlobalPosition.DirectionTo(pushFrom.GlobalPosition) * force;
        }
        PushForce = result;
        ApplyToLiving = applyToLiving;
    }
    
    public bool ApplyToLiving { get; set; }
    
    public Vector3 PushForce { get; set; }

    public override bool Active { get; set; } = true;
    
    public override void Apply(Node node)
    {
        if (!ApplyToLiving)
        {
            return;
        }
        ApplyForce(node);
    }

    public override void ApplyPostMortem(Node node)
    {
        ApplyForce(node);
    }

    public void ApplyForce(Node node)
    {
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }
        if (node is RigidbodySync sync)
        {
            sync.OnImpulse(PushForce, sync.GlobalPosition);
        }
        else if (node is RigidBody3D body)
        {
            body.ApplyImpulse(PushForce, body.GlobalPosition);
        }
        else if (node is PhysicalBone3D bone)
        {
            bone.ApplyCentralImpulse(PushForce);
        }
        else if (node is IPhysicsProp prop)
        {
            if (prop is Node3D propNode)
            {
                prop.OnImpulse(PushForce, propNode.GlobalPosition);
            }
        }
    }
}
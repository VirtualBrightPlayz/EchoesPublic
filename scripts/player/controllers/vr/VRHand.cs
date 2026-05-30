using Godot;

public partial class VRHand : Node3D
{
    [Export]
    public XRController3D hand;
    [Export]
    public AnimationTree tree;

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(tree))
        {
            tree.Set("parameters/Grip/blend_amount", hand.GetFloat("grip"));
            tree.Set("parameters/Trigger/blend_amount", hand.GetFloat("trigger"));
        }
    }
}
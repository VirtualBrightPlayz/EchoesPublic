using Godot;

[GlobalClass]
public partial class SCP173SpeedMapEffect : MapEffectBase
{
    [Export]
    public float Speed = 2f;
    [Export]
    public bool include106 = false;

    public override void Enter(Node3D body)
    {
        // TODO: use status effects
        // TODO: use new Role system
        /*
        if (body is FPController ctrl && (ctrl.Player.RoleIndex == (int)RoleID.SCP173 || (include106 && ctrl.Player.RoleIndex == (int)RoleID.SCP106)))
        {
            ctrl.ExtraSpeed = Speed;
        }
        */
    }

    public override void Exit(Node3D body)
    {
        /*
        if (body is FPController ctrl)
        {
            ctrl.ExtraSpeed = 1f;
        }
        */
    }
}

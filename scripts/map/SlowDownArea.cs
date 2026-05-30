using Godot;

[GlobalClass]
public partial class SlowDownArea : Area3D
{
    [Export]
    public float Intensity = 1f;

    public override void _EnterTree()
    {
        base._EnterTree();
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;
    }
    
    private void OnBodyEntered(Node3D body)
    {
        if (body is FPController controller)
        {
            controller.Player.statusEffectManager.EnableEffect(EffectType.SlimeEffect, float.PositiveInfinity, Intensity);
        }
    }
    
    private void OnBodyExited(Node3D body)
    {
        if (body is FPController controller)
        {
            controller.Player.statusEffectManager.DisableEffect(EffectType.SlimeEffect);
        }
    }
}
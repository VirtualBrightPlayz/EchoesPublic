using System;
using Godot;

public partial class MapEffectBase : Node
{
    public Area3D Area => GetParent<Area3D>();

    public override void _EnterTree()
    {
        Area.BodyEntered += Enter;
        Area.BodyExited += Exit;
    }

    public override void _ExitTree()
    {
        Area.BodyEntered -= Enter;
        Area.BodyExited -= Exit;
    }

    public virtual void Enter(Node3D body)
    {
    }

    public virtual void Exit(Node3D body)
    {
    }
}

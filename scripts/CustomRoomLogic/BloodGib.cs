using Godot;
using System;

[GlobalClass]
public partial class BloodGib : Node3D
{
    [Export]
    public AnimationPlayer anim;

    public override void _Ready()
    {
        anim.Active = IsMultiplayerAuthority();
    }
}

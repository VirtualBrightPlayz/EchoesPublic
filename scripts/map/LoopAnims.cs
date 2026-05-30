using Godot;
using System;

public partial class LoopAnims : AnimationPlayer
{
	[Export]
	public string anim;

	public override void _Process(double delta)
	{
		if (!IsPlaying())
			Play(anim);
	}
}

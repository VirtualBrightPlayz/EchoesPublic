using Godot;
using System;

[GlobalClass]
public partial class LoopAudio3D : AudioStreamPlayer3D
{
	public override void _Process(double delta)
	{
		if (!Playing && IsVisibleInTree() && !StreamPaused)
		{
			Play();
		}
		else if (!IsVisibleInTree())
		{
			Stop();
		}
	}
}

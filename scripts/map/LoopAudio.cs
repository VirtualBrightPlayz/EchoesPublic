using Godot;
using System;

[GlobalClass]
public partial class LoopAudio : AudioStreamPlayer
{
	public override void _Process(double delta)
	{
		if (!Playing)
			Play();
	}
}

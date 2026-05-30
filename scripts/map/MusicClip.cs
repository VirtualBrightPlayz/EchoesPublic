using System;
using Godot;

[GlobalClass]
public partial class MusicClip : Resource
{
    [Export]
    public AudioStream audio;
    [Export]
    public Curve volumeOverTime;
    [Export]
    public float volumeDb = 0f;
}

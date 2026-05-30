using System;
using Godot;

[GlobalClass]
public partial class RandAudio3D : AudioStreamPlayer3D
{
    [Export]
    public AudioStream[] streams = Array.Empty<AudioStream>();

    public void PlayRandom()
    {
        if (!HasStreamPlayback())
            Stream = new AudioStreamPolyphonic();
        if (!Playing)
            Play();
        if (HasStreamPlayback() && GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
        {
            playback.PlayStream(streams[GD.Randi() % streams.Length]);
        }
    }
}
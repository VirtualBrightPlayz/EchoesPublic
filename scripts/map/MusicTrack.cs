using System;
using Godot;

[GlobalClass]
public partial class MusicTrack : Resource
{
    [Export] public MusicTrackContext context = MusicTrackContext.None;
    [Export] public ZoneArea.Zone zone = ZoneArea.Zone.Unknown;
    [Export] public PlayerRole chaseRole;
    [Export] public MusicClip[] clips = Array.Empty<MusicClip>();
}

public enum MusicTrackContext
{
    None = 0,
    Zone,
    Chase,
    Alone,
}

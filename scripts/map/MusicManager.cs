using System;
using System.Linq;
using Godot;

public partial class MusicManager : Node
{
    public enum MusicState
    {
        Zone = 0,
        Chase,
        Alone,
    }

    public static MusicManager Instance;

    [Export]
    public AudioStreamPlayer[] audioSources = Array.Empty<AudioStreamPlayer>();
    [Export]
    public AudioStream[] soundTracks = Array.Empty<AudioStream>();
    [Export]
    public float[] volumeDb = Array.Empty<float>();
    [Export]
    public MusicTrack[] musicTracks = Array.Empty<MusicTrack>();
    [Export]
    public float speed = 1f;
    [Export]
    public Label debugLabel;

    private int audioIndex = 0;

    private int trackIndex = 0;
    private int subTrackIndex = 0;
    public MusicState state = MusicState.Zone;

    public static float MoveTowardDb(float fromDb, float toDb, float delta)
    {
        return Mathf.LinearToDb(Mathf.MoveToward(Mathf.DbToLinear(fromDb), Mathf.DbToLinear(toDb), delta));
    }

    public static float LerpDb(float fromDb, float toDb, float delta)
    {
        return Mathf.LinearToDb(Mathf.Lerp(Mathf.DbToLinear(fromDb), Mathf.DbToLinear(toDb), delta));
    }

    public override void _Ready()
    {
        Instance = this;
        for (int i = 0; i < audioSources.Length; i++)
        {
            audioSources[i].VolumeDb = -80f;
        }
        SwitchTrackTo(0, 0, false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(NetworkPlayer.LocalInstance) || !IsInstanceValid(NetworkPlayer.LocalInstance.ActiveControllerNode))
            return;
        if (NetworkPlayer.LocalInstance.Hud.chaseMusicScript.IsChased)
        {
            audioIndex = -1;
            state = MusicState.Chase;
        }
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (i == audioIndex)
            {
                if (!audioSources[i].Playing)
                {
                    switch (state)
                    {
                        default:
                            SwitchTrackToZone(false);
                            break;
                        case MusicState.Zone:
                        case MusicState.Chase:
                            SwitchTrackToZone(true);
                            break;
                    }
                }
                else if (IsInstanceValid(musicTracks[trackIndex].clips[subTrackIndex].volumeOverTime))
                {
                    float offset = audioSources[i].GetPlaybackPosition() + (float)AudioServer.GetTimeSinceLastMix();
                    audioSources[i].VolumeLinear = musicTracks[trackIndex].clips[subTrackIndex].volumeOverTime.Sample(offset) * Mathf.DbToLinear(musicTracks[trackIndex].clips[subTrackIndex].volumeDb);
                }
                else
                    audioSources[i].VolumeDb = LerpDb(audioSources[i].VolumeDb, musicTracks[trackIndex].clips[subTrackIndex].volumeDb, speed * (float)delta);
            }
            else
                audioSources[i].VolumeDb = LerpDb(audioSources[i].VolumeDb, i == audioIndex ? musicTracks[trackIndex].clips[subTrackIndex].volumeDb : -80f, speed * (float)delta);
        }
        if (IsInstanceValid(debugLabel))
        {
            if (audioIndex == -1)
            {
                debugLabel.Text = $"Track: {trackIndex}, SubTrack: {subTrackIndex}, VolumeDb: N/A, State: {state}";
            }
            else
            {
                debugLabel.Text = $"Track: {trackIndex}, SubTrack: {subTrackIndex}, VolumeDb: {audioSources[audioIndex].VolumeDb:0.0}, State: {state}";
            }
        }
        if (state == MusicState.Alone && !NetworkPlayer.LocalInstance.TryGetAbility(out SanityAbility _))
        {
            SwitchTrackToZone(true);
        }
        if (state == MusicState.Zone)
        {
            SwitchTrackToZone(false);
        }
    }

    public void SwitchTrackToAlone(bool force)
    {
        MusicTrack track = musicTracks.FirstOrDefault(x => x.context == MusicTrackContext.Alone);
        if (IsInstanceValid(track))
        {
            int i = Array.IndexOf(musicTracks, track);
            if (i != -1 && (trackIndex != i || force))
            {
                int j = (int)(GD.Randi() % track.clips.Length);
                SwitchTrackTo(i, j, false);
                state = MusicState.Alone;
            }
        }
    }

    public void SwitchTrackToChase(PlayerRole role)
    {
        MusicTrack track = musicTracks.FirstOrDefault(x => x.context == MusicTrackContext.Chase && x.chaseRole == role);
        if (IsInstanceValid(track))
        {
            int i = Array.IndexOf(musicTracks, track);
            if (i != -1 && trackIndex != i)
            {
                int j = (int)(GD.Randi() % track.clips.Length);
                SwitchTrackTo(i, j, true);
                state = MusicState.Chase;
            }
        }
    }

    public void SwitchTrackToZone(bool force)
    {
        ZoneArea.Zone zone = ZoneArea.Zone.Unknown;
        ZoneArea zoneArea = ZoneArea.GetZoneArea(NetworkPlayer.LocalInstance.ActiveController.Camera.GlobalPosition);
        if (IsInstanceValid(zoneArea))
        {
            zone = zoneArea.zone;
        }
        MusicTrack track = musicTracks.FirstOrDefault(x => x.context == MusicTrackContext.Zone && x.zone == zone);
        if (IsInstanceValid(track))
        {
            int i = Array.IndexOf(musicTracks, track);
            if (i != -1 && (trackIndex != i || force))
            {
                int j = (int)(GD.Randi() % track.clips.Length);
                SwitchTrackTo(i, j, false);
                state = MusicState.Zone;
            }
        }
    }

    private void SwitchTrackTo(int track, int subTrack, bool instant)
    {
        if (IsInstanceValid(musicTracks[track]) && IsInstanceValid(musicTracks[track].clips[subTrack]))
        {
            audioIndex = (audioIndex + 1) % audioSources.Length;
            audioSources[audioIndex].VolumeDb = instant ? 0f : -80f;
            audioSources[audioIndex].Stream = musicTracks[track].clips[subTrack].audio;
            audioSources[audioIndex].Play();
        }
        trackIndex = track;
        subTrackIndex = subTrack;
    }
}
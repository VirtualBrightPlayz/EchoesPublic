using Godot;

public partial class LczHallFanLogic : Node3D
{
    public enum FanSate
    {
        Off,
        TOn,
        On,
        TOff,
    }

    [Export]
    private Node3D fan;
    [Export]
    private Vector3 axis = Vector3.Right;
    [Export]
    private float maxFanSpeed = 10f;
    [Export]
    private float fanAccel = 2f;
    [Export]
    private AudioStreamPlayer3D fanAudioPlayer;
    [Export]
    private AudioStreamOggVorbis fanRunningAudio;
    [Export]
    private AudioStreamOggVorbis fanTurningOnAudio;
    [Export]
    private AudioStreamOggVorbis fanTurningOffAudio;

    public FanSate CurrentFanState { get; set; }
    public float FanSpeed { get; set; }

    public override void _Ready()
    {
        fanAudioPlayer.Finished += AudioFinished;

        CurrentFanState = FanSate.Off;
    }

    public override void _ExitTree()
    {
        fanAudioPlayer.Finished -= AudioFinished;
    }

    public override void _Process(double delta)
    {
        switch (CurrentFanState)
        {
            default:
                FanSpeed = Mathf.Lerp(FanSpeed, maxFanSpeed, (float)delta * fanAccel);
                break;
            case FanSate.TOff:
            case FanSate.Off:
                FanSpeed = Mathf.Lerp(FanSpeed, 0f, (float)delta * fanAccel);
                break;
        }
        fan.Rotate(axis, Mathf.DegToRad((float)delta * FanSpeed));
    }

    public void NextState()
    {
        switch (CurrentFanState) 
        {
            case FanSate.Off:
                fanAudioPlayer.Stream = fanTurningOnAudio;
                fanAudioPlayer.Play();
                CurrentFanState = FanSate.TOn;
                break;
            case FanSate.On:
                fanAudioPlayer.Stream = fanTurningOffAudio;
                fanAudioPlayer.Play();
                CurrentFanState = FanSate.TOff;
                break;
        }
    }

    private void AudioFinished()
    {
        switch (CurrentFanState)
        {
            case FanSate.TOn:
                fanAudioPlayer.Stream = fanRunningAudio;
                fanAudioPlayer.Play();
                CurrentFanState = FanSate.On;
                break;
            case FanSate.TOff:
                fanAudioPlayer.Stream = null;
                CurrentFanState = FanSate.Off;
                break;

        }
    }
}

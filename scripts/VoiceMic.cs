using Godot;

public partial class VoiceMic : AudioStreamPlayer
{
    public override void _Ready()
    {
        int currentNumber = AudioServer.GetBusIndex("VoiceMicRecord");
        if (currentNumber == -1)
        {
            Log.PrintErr("VoiceMicRecord not found");
            return;
        }

        Bus = "VoiceMicRecord";
        Stream = new AudioStreamMicrophone();
        VolumeDb = Settings.User.MicVolumeDb;
        Play();
    }

    public override void _Process(double delta)
    {
        VolumeDb = Settings.User.MicVolumeDb;
        if (!Playing)
            Play();
    }
}

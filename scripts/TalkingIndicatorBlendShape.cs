using Godot;

public partial class TalkingIndicatorBlendShape : Node
{
    public IPlayerController player => GetParent() as IPlayerController;
    [Export]
    public LipSync lipSync;

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(lipSync))
            return;
        if (player == null || player.Player.IsLocalPlayer)
        {
            lipSync.busIndex = AudioServer.GetBusIndex("VoiceMicRecord");
            lipSync.effectIndex = 2;
            lipSync.speaking = false;
            return;
        }
        if (player.Player is NetworkPlayer plr && IsInstanceValid(plr.voiceChat) && plr.voiceChat.Bus != null)
        {
            lipSync.busIndex = plr.voiceChat.Bus.Index;
            lipSync.effectIndex = -1;
            int count = AudioServer.GetBusEffectCount(lipSync.busIndex);
            for (int i = 0; i < count; i++)
            {
                if (AudioServer.GetBusEffect(lipSync.busIndex, i) is AudioEffectCapture)
                {
                    lipSync.effectIndex = i;
                    break;
                }
            }
            if (lipSync.effectIndex == -1)
            {
                plr.voiceChat.Bus.AddEffect(new AudioEffectCapture()
                {
                    BufferLength = 0.1f,
                });
                lipSync.effectIndex = count;
            }
            if (IsInstanceValid(NetworkPlayer.LocalInstance))
            {
                lipSync.speaking = plr.voiceChat.IsSpeaking && plr.PlayerPosition.DistanceSquaredTo(NetworkPlayer.LocalInstance.PlayerPosition) <= plr.voicePlayback.MaxDistance * plr.voicePlayback.MaxDistance;
            }
            else
                lipSync.speaking = false;
        }
    }
}
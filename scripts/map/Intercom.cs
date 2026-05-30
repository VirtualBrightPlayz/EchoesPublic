using Godot;
using System;
using System.Linq;

public partial class Intercom : Area3D
{
    public static StringName IntercomName = "Intercom";
    public static StringName MuteName = "Mute";

    [Export]
    public AudioStreamPlayer audio;
    [Export]
    public AudioStreamPlayer3D audioFx;
    [Export]
    public AudioStreamPlayer3D[] audioFxArray = new AudioStreamPlayer3D[0];
    [Export]
    public AudioStream onSound;
    [Export]
    public AudioStream offSound;
    [Export]
    public int PlayerAuthorityId = 0;
    [Export]
    public int state;
    [Export]
    public int onTime;
    [Export]
    public int offTime;
    [Export]
    public Timer onDelay;
    private IPlayerList players;
    private NetworkPlayer lastSpeaking;
    private float timer;

    public override void _Ready()
    {
        players = IPlayerList.List(this);
        onDelay.Timeout += _Timeout;
    }

    private void _Timeout()
    {
        foreach (var fx in audioFxArray)
        {
            fx.Play();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer() || !IsInstanceValid((GodotObject)players))
            return;
        {
            NetworkPlayer speaking = players.PlayerList.FirstOrDefault(x => x.GetMultiplayerAuthority() == PlayerAuthorityId);
            if (lastSpeaking != speaking && lastSpeaking != null)
            {
                lastSpeaking.voiceChat.SetVoice(lastSpeaking.voicePlayback);
            }
            if (state == 2 && speaking != null && speaking.voiceChat.CustomVoiceAudioPlayer != audio.GetPath())
            {
                speaking.voiceChat.SetVoice(audio);
            }
            lastSpeaking = speaking;
        }
        if (!Multiplayer.IsServer())
        {
            return;
        }

        bool found = false;
        NetworkPlayer player = null;
        foreach (var body in GetOverlappingBodies())
        {
            if (body is IPlayerController ctrl && ctrl.Player is NetworkPlayer plr && plr.voiceChat.IsSpeaking && ctrl.Player.Role.canUseIntercom)
            {
                found = true;
                player = plr;
                break;
            }
        }

        switch (state)
        {
            case 1:
                timer -= (float)delta;
                if (timer <= 0f)
                {
                    state = 2;
                }
                break;
            case 3:
                timer -= (float)delta;
                if (timer <= 0f)
                {
                    state = 0;
                }
                break;
        }

        if (found && state == 0)
        {
            state = 1;
            Rpc(nameof(RpcStateChanged), state);
            timer = onTime;
            PlayerAuthorityId = player.GetMultiplayerAuthority();
        }
        else if (!found && state == 2)
        {
            state = 3;
            Rpc(nameof(RpcStateChanged), state);
            timer = offTime;
            PlayerAuthorityId = 0;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcStateChanged(int newState)
    {
        switch (newState)
        {
            case 1:
                audioFx.Stream = onSound;
                audioFx.Play();
                onDelay.Start();
                break;
            case 3:
                audioFx.Stream = offSound;
                audioFx.Play();
                onDelay.Stop();
                foreach (var fx in audioFxArray)
                {
                    fx.Stop();
                }
                break;
        }
    }
}

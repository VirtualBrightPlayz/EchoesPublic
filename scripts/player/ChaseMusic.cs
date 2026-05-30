using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ChaseMusic : Node
{
    public enum SCP106ChaseState : int
    {
        Silent = -1,
        PreChase = 0,
        Chase = 1,
        PostChase = 2,
    }

    [Export]
    public LocalPlayerHUD hud;
    public NetworkPlayer Player => hud.Player;

    private IPlayerList _playerList;
    public bool IsChased => scp106State != SCP106ChaseState.Silent;
    public double chaseTimer = 0d;
    public double chaseStartTimer = 0d;

    [ExportGroup("Effects")]
    [Export]
    public ColorRect blinkOverlay;

    [ExportGroup("Chase Music")]
    [Export]
    public AudioStreamPlayer chaseMusic;
    [Export]
    public AudioStreamPlayer chaseMusic106;
    [Export]
    public AudioStreamPlayer tickingSound;
    [Export]
    public AudioStreamPlayer bellSound;
    [Export]
    public AudioStreamPlayer horrorSound;
    [Export]
    public AudioStream[] horrorSoundQueues;
    [Export]
    public float chaseVolumeDb = 0f;
    [Export]
    public double chaseMusicTime = 60d;
    [Export]
    public AudioStream ticking;
    [Export]
    public AudioStream bell;
    [Export]
    public float nearbyRange = 50f;

    public double lastBlinkTimer = 0d;
    public double blinkTimer = 0d;
    public PlayerRole chasedBy;
    public NetworkPlayer scp106;
    public SCP106ChaseState scp106State = SCP106ChaseState.Silent;

    [ExportSubgroup("SCP-106")]
    [Export]
    public float preChase106Range = 40f;
    [Export]
    public float chase106Range = 15f;
    [Export]
    public Curve scp106DrumsCurve;
    [Export]
    public double preChase106Time = 30d;
    [Export]
    public double postChase106Time = 30d;
    [Export]
    public AudioStream stinger106;

    public override void _Ready()
    {
        base._Ready();

        _playerList = IPlayerList.List(this);
        chaseMusic.VolumeDb = Mathf.LinearToDb(0f);
    }

    public void PlayTickingSound()
    {
        tickingSound.Stream = ticking;
        tickingSound.Play();
    }

    public void PlayBellSound()
    {
        bellSound.Stream = bell;
        bellSound.Play();
    }

    public bool Process106(double delta, IEnumerable<NetworkPlayer> seenScps, IEnumerable<NetworkPlayer> nearbyScps)
    {
        if (!Player.TryGetAbility(out SanityAbility _))
        {
            scp106State = SCP106ChaseState.Silent;
            chaseMusic106.Stop();
            scp106 = null;
            return false;
        }
        chaseTimer -= delta;
        var near106 = nearbyScps.FirstOrDefault(x => x.Role.team == TeamID.SCP && x.PlayerPosition.DistanceSquaredTo(Player.PlayerPosition) < preChase106Range * preChase106Range);
        if (IsInstanceValid(near106) && !IsInstanceValid(scp106))
        {
            scp106 = near106;
            Start106PreChase();
        }
        if (IsInstanceValid(scp106))
        {
            Process106Chase(delta);
        }
        if (!IsInstanceValid(scp106) || chaseTimer <= 0d)
        {
            Start106PostChase();
        }
        // play clip according to state
        if (!chaseMusic106.Playing && scp106State != SCP106ChaseState.Silent)
            chaseMusic106.Play();
        if (chaseMusic106.Playing && chaseMusic106.Stream is AudioStreamInteractive stream && chaseMusic106.GetStreamPlayback() is AudioStreamPlaybackInteractive playback)
        {
            if (scp106State == SCP106ChaseState.Silent)
            {
                chaseMusic106.Stop();
            }
            else if (playback.GetCurrentClipIndex() != (int)scp106State)
            {
                playback.SwitchToClip((int)scp106State);
            }
        }
        return IsInstanceValid(scp106);
    }

    public void Start106PreChase()
    {
        // Log.Print("PreChase");
        if (IsInstanceValid(scp106.Role.PreChaseAudio))
        {
            scp106State = SCP106ChaseState.PreChase;
            chaseTimer = scp106.Role.PreChaseAudio?.Stream?.GetLength() ?? 0d;
        }
        chaseMusic106.Stream = scp106.Role.ChaseMusicStream;
    }

    public void Start106Chase()
    {
        // Log.Print("Chase");
        if (!chaseMusic106.Playing)
        {
            chaseMusic106.Stream = scp106.Role.ChaseMusicStream;
            chaseMusic106.Play();
        }
        if (scp106State != SCP106ChaseState.Chase)
        {
            scp106State = SCP106ChaseState.Chase;
            horrorSound.Stream = scp106.Role.SpottedAudio?.Stream;
            horrorSound.Play();
            chaseStartTimer = 0d;
        }
        chaseTimer = scp106.Role.ChaseMusicTime;
    }

    public void Start106PostChase()
    {
        // Log.Print("PostChase");
        if (scp106State == SCP106ChaseState.Chase)
        {
            if (IsInstanceValid(scp106) && IsInstanceValid(scp106.Role.PostChaseAudio))
            {
                scp106State = SCP106ChaseState.PostChase;
                chaseTimer = scp106.Role.PostChaseAudio?.Stream?.GetLength() ?? 0d;
            }
            else
            {
                scp106State = SCP106ChaseState.Silent;
                MusicManager.Instance?.SwitchTrackToZone(true);
            }
            StatusRpcManager.Instance.EndChase((float)chaseStartTimer);
            scp106 = null;
        }
        else if (scp106State == SCP106ChaseState.PreChase)
        {
            scp106State = SCP106ChaseState.Silent;
            MusicManager.Instance?.SwitchTrackToZone(true);
            scp106 = null;
        }
    }

    public void Process106Chase(double delta)
    {
        if (scp106State == SCP106ChaseState.Chase)
        {
            chaseStartTimer += delta;
        }
        if (chaseMusic106.Playing && chaseMusic106.Stream is AudioStreamInteractive stream && chaseMusic106.GetStreamPlayback() is AudioStreamPlaybackInteractive playback)
        {
            if (stream.GetClipStream(1) is AudioStreamSynchronized sync)
            {
                float dist = scp106.PlayerPosition.DistanceTo(Player.PlayerPosition);
                sync.SetSyncStreamVolume(1, Mathf.LinearToDb(scp106DrumsCurve.Sample(dist)));
            }
        }
        if (scp106.PlayerPosition.DistanceTo(Player.PlayerPosition) < chase106Range && (/*IPlayerController.IsSeenBy(Player.Controller, scp106.controller) ||*/ IPlayerController.IsSeenBy(scp106.ActiveController, Player.ActiveController.Root)))
        {
            Start106Chase();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
            return;

        var seenScps = _playerList.PlayerList.Where(x => x.Role.team == TeamID.SCP && IPlayerController.IsSeenBy(x.ActiveController, Player.ActiveController.Root)).ToArray();
        var nearbyScps = _playerList.PlayerList.Where(x => x.Role.team == TeamID.SCP && x.PlayerPosition.DistanceSquaredTo(Player.PlayerPosition) < nearbyRange * nearbyRange).ToArray();

        bool skip = Process106(delta, seenScps, nearbyScps);

        if (IsInstanceValid(blinkOverlay) && IsInstanceValid(RoundManager.Instance))
        {
            bool blinking = Player.TryGetAbility(out SanityAbility _) && RoundManager.Instance.IsBlinking && _playerList.PlayerList.Any(x => x.TryGetAbility(out StatueAbility ability) && ability.IsSeenBy(Player.ActiveController.Root));
            if (blinkOverlay.Visible != blinking)
            {
                blinkOverlay.Visible = blinking;
                if (blinking)
                {
                    // PlayBellSound();
                }
            }
            blinkTimer += delta;
            if (!blinking && seenScps.Any(x => x.TryGetAbility(out StatueAbility _)) && Player.TryGetAbility(out SanityAbility _))
            {
                int cur = (int)blinkTimer;
                int last = (int)lastBlinkTimer;
                if (cur != last && cur >= 0)
                {
                    // PlayTickingSound();
                }
            }
            lastBlinkTimer = blinkTimer;
        }

        /*
        if (isBeingChased)
        {
            chaseTimer -= delta;
            chaseStartTimer += delta;
            if (Player.Role.team == TeamID.Dead || Player.Role.team == TeamID.SCP)
            {
                chaseTimer = 0d;
            }
        }
        else
        {
            float volLin = Mathf.DbToLinear(chaseMusic.VolumeDb);
            volLin = Mathf.MoveToward(volLin, 0f, (float)delta * 0.2f);
            chaseMusic.VolumeDb = Mathf.LinearToDb(volLin);
        }
        */
        /*
        if (chaseTimer <= 0d && isBeingChased && !skip)
        {
            isBeingChased = false;
            isAudible = false;
            if (IsInstanceValid(MusicManager.Instance))
            {
                MusicManager.Instance.SwitchTrackToZone(true);
            }
            StatusRpcManager.Instance.EndChase((float)chaseStartTimer);
        }
        */

        // if (!chaseMusic.Playing)
        {
            // chaseMusic.Play();
        }
    }
}
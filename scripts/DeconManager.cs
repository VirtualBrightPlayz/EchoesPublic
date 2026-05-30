using System;
using System.Linq;
using Godot;

public partial class DeconManager : Node
{
    public RoundManager Round => GetParent<RoundManager>();

    [Export]
    public Area3D area;
    [Export]
    public AudioStreamPlayer intercom;
    [Export]
    public AudioStreamPlayer intercomGlobal;
    [Export]
    public float volumeDb;
    [Export]
    public Timer timer;
    [Export]
    public int stageOpenCheckpoints = 4;
    [Export]
    public double[] intervals = new double[6];
    [ExportGroup("Audio Clips")]
    [Export]
    public AudioStream decon15; // 0
    [Export]
    public AudioStream decon10; // 1
    [Export]
    public AudioStream decon5; // 2
    [Export]
    public AudioStream decon1; // 3
    [Export]
    public AudioStream decon030; // 4
    [Export]
    public AudioStream deconStart; // 5

    public int stage = 0;
    public bool running = false;

    private Node3D found = null;

    public override void _EnterTree()
    {
        // Round.EventOnRoundStart += StartDeconSequence;
        area.BodyEntered += OnEnter;
        area.BodyExited += OnExit;
        area.AreaEntered += OnEnter;
        area.AreaExited += OnExit;
    }

    public override void _ExitTree()
    {
        // Round.EventOnRoundStart -= StartDeconSequence;
        area.BodyEntered -= OnEnter;
        area.BodyExited -= OnExit;
        area.AreaEntered -= OnEnter;
        area.AreaExited -= OnExit;
    }

    private void OnEnter(Node3D body)
    {
        if (IsInstanceValid(found))
            return;
        if (body is StaticBody3D)
            return;
        if (body is IPlayerController player && player.Player.IsLocalPlayer)
        {
            found = body;
            // Log.Print("Entered LCZ");
        }
        else
        {
            var par = body.GetParent();
            if (IsInstanceValid(par) && par is Camera3D cam && cam.Current)
            {
                found = body;
                // Log.Print("Entered LCZ");
            }
        }
    }

    private void OnExit(Node3D body)
    {
        if (found == body)
        {
            found = null;
            // Log.Print("Exited LCZ");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(NetworkPlayer.LocalInstance))
        {
            if (IsInstanceValid(found))
                intercom.VolumeDb = volumeDb;
            else
                intercom.VolumeDb = -80f;
        }
        if (Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer())
            return;
        if (timer.TimeLeft <= 0d && running)
        {
            NextStage(false);
        }
        if (stage >= intervals.Length)
        {
            foreach (var body in area.GetOverlappingBodies())
            {
                if (body is IPlayerController ctrl)
                {
                    ctrl.Player.Damage(new DamageInfo(15f * (float)delta));
                }
            }
        }
    }

    public void PlayIntercom(int id)
    {
        switch (id)
        {
            default:
                return;
            case 0:
                intercom.Stream = decon15;
                break;
            case 1:
                intercom.Stream = decon10;
                break;
            case 2:
                intercom.Stream = decon5;
                break;
            case 3:
                intercom.Stream = decon1;
                break;
            case 4:
                intercom.Stream = decon030;
                break;
            case 5:
                intercomGlobal.Stream = deconStart;
                intercomGlobal.Play();
                return;
        }
        intercom.Play();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcIntercom(int id)
    {
        if (Multiplayer.GetRemoteSenderId() == 1)
            PlayIntercom(id);
    }

    public void NextStage(bool skip)
    {
        if (stage >= intervals.Length)
        {
            return;
        }
        if (!skip)
        {
            timer.Start(intervals[stage]);
        }
        if (stage == stageOpenCheckpoints)
        {
            var nodes = GetTree().GetNodesInGroup("lcz_checkpoint");
            foreach (TimedDoor door in nodes.Where(x => x is TimedDoor))
            {
                door.isLocked = true;
                if (door.timerState != TimedDoor.TimerStateEnum.Idle)
                    continue;
                door.SV_Toggle();
            }
        }
        else if (stage == stageOpenCheckpoints + 1)
        {
            var nodes = GetTree().GetNodesInGroup("lcz_checkpoint");
            foreach (TimedDoor door in nodes.Where(x => x is TimedDoor))
            {
                door.isLocked = false;
            }
            var elevs = GetTree().GetNodesInGroup("lcz_elevator");
            foreach (Elevator door in elevs.Where(x => x is Elevator))
            {
                door.isLocked = true;
            }
        }
        if (!skip)
        {
            Rpc(nameof(RpcIntercom), stage);
        }
        stage++;
    }

    public void StartDeconSequence(double time)
    {
        intercom.Stop();
        intercomGlobal.Stop();
        running = false;
        if (Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer())
            return;
        var nodes = GetTree().GetNodesInGroup("lcz_checkpoint");
        foreach (TimedDoor door in nodes.Where(x => x is TimedDoor))
        {
            door.isLocked = false;
        }
        var elevs = GetTree().GetNodesInGroup("lcz_elevator");
        foreach (Elevator door in elevs.Where(x => x is Elevator))
        {
            door.isLocked = false;
        }
        stage = intervals.Length;
        double timeLeft = time;
        // for (int i = 0; i < intervals.Length; i++)
        for (int i = intervals.Length - 1; i >= 0; i--)
        {
            if (timeLeft - intervals[i] >= 0d)
            {
                timeLeft -= intervals[i];
                stage--;
                // NextStage(true);
            }
            else
            {
                break;
            }
        }
        timer.Start(timeLeft);
        running = true;
        // timer.Stop();
    }

    public void StopDeconSequence()
    {
        running = false;
        timer.Stop();
    }
}
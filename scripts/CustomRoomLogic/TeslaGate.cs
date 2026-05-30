using Godot;

[GlobalClass]
public partial class TeslaGate : Area3D, IDamageSource
{
    public enum State
    {
        Idle,
        ChargeUp,
        Shock,
        Cooldown,
    }

    [Signal]
    public delegate void ShockEventHandler();

    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public AudioStreamPlayer3D audioShock;
    [Export]
    public AudioStream idleSound;
    [Export]
    public AudioStream chargeUpSound;
    [Export]
    public AudioStream shockSound;

    [Export]
    public GpuParticles3D[] idleParticles;

    [Export]
    public Node3D teslaShock;
    [Export]
    public Area3D killArea;

    [Export]
    public double Damage { get; set; } = 300f;
    
    [Export]
    public double chargeUpTime;
    [Export]
    public double shockTime;
    [Export]
    public double postShockTime;

    private AudioStreamPlaybackPolyphonic playback;

    [ExportGroup("Sync")]
    [Export]
    public State state;

    public override void _Process(double delta)
    {
        if (IsInstanceValid(teslaShock))
            teslaShock.Visible = state == State.Shock;
        if (state == State.Idle && !audio.Playing)
        {
            audio.Stream = idleSound;
            audio.Play();
        }
        foreach (var item in idleParticles)
        {
            item.Emitting = state == State.ChargeUp || state == State.Shock;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(teslaShock))
            teslaShock.Visible = state == State.Shock;
        if (state == State.Idle && !audio.Playing)
        {
            audio.Stream = idleSound;
            audio.Play();
        }
        if (IsMultiplayerAuthority())
        {
            if (HasOverlappingBodies())
            {
                foreach (var body in GetOverlappingBodies())
                {
                    if (body is IPlayerController ctrl && ctrl.Player.Role.team != TeamID.Dead)
                    {
                        if (state == State.Idle)
                        {
                            Rpc(MethodName.RpcShock);
                            break;
                        }
                    }
                }
            }
            if (killArea.HasOverlappingBodies())
            {
                foreach (var body in killArea.GetOverlappingBodies())
                {
                    if (body is IPlayerController ctrl && ctrl.Player.Role.team != TeamID.Dead)
                    {
                        if (state == State.Shock)
                        {
                            ctrl.Player.Kill(new DamageInfo((float)(Damage * delta), this, DamageType.ElectricShock));
                        }
                    }
                    else if (IHealth.GetHealth(body) != null)
                    {
                        if (state == State.Shock)
                        {
                            IHealth health = IHealth.GetHealth(body);
                            health.Damage(new DamageInfo((float)(Damage * delta), this, DamageType.ElectricShock));
                        }
                    }
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcShock()
    {
        ShockAsync();
    }

    public async void ShockAsync()
    {
        audio.Stop();
        audioShock.Stream = chargeUpSound;
        audioShock.Play();
        state = State.ChargeUp;
        await ToSignal(GetTree().CreateTimer(chargeUpTime), SceneTreeTimer.SignalName.Timeout);
        audioShock.Stream = shockSound;
        audioShock.Play();
        EmitSignalShock();
        state = State.Shock;
        await ToSignal(GetTree().CreateTimer(shockTime), SceneTreeTimer.SignalName.Timeout);
        state = State.Cooldown;
        await ToSignal(GetTree().CreateTimer(postShockTime), SceneTreeTimer.SignalName.Timeout);
        state = State.Idle;
    }

    public string AttackerDisplayName => "Tesla Gate";
    public NodePath AbsolutePath => GetPath();
}
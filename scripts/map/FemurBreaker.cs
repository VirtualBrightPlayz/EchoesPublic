using System;
using System.Linq;
using Godot;

public partial class FemurBreaker : Node
{
    [Signal]
    public delegate void OnSacrificedEventHandler();
    [Signal]
    public delegate void OnSacrificeResetEventHandler();

    [Export]
    public Area3D trigger;
    [Export]
    public bool hasSacrifice = false;
    [Export]
    public bool hasRun = false;
    [Export]
    public AudioStreamPlayer3D intercom;
    [Export]
    public AudioStream scream;
    [Export]
    public float timeToContain = 5f;

    private bool hadSacrifice = false;

    public override void _EnterTree()
    {
        trigger.BodyEntered += _BodyEnter;
        hadSacrifice = !hasSacrifice;
    }

    public override void _ExitTree()
    {
        trigger.BodyEntered -= _BodyEnter;
    }

    public override void _Process(double delta)
    {
        if (hadSacrifice != hasSacrifice)
        {
            hadSacrifice = hasSacrifice;
            if (hasSacrifice)
                EmitSignalOnSacrificed();
            else
                EmitSignalOnSacrificeReset();
        }
    }

    private void _BodyEnter(Node3D body)
    {
        if (!IsMultiplayerAuthority())
            return;
        if (hasSacrifice)
            return;
        if (body is IPlayerController ctrl && ctrl.Player.Role.team != TeamID.SCP && body is IHealth hp)
        {
            hasSacrifice = true;
            hp.Kill(new DamageInfo());
        }
    }

    public async void Run()
    {
        if (hasRun)
            return;
        if (!hasSacrifice)
            return;
        hasRun = true;
        Rpc(nameof(RpcScream));
        await ToSignal(GetTree().CreateTimer(timeToContain), SceneTreeTimer.SignalName.Timeout);
        if (!IsInstanceValid(this))
            return;
        foreach (var plr in IPlayerList.List(this).PlayerList.Where(x => x.RoleIndex == (int)RoleID.SCP106).ToArray())
        {
            plr.Kill(new DamageInfo());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcRun()
    {
        // TODO: anti-cheat checks
        if (IsMultiplayerAuthority())
            Run();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcScream()
    {
        intercom.Stream = scream;
        intercom.Play();
    }
}

using System;
using Godot;

[GlobalClass]
public partial class NukeEvents : Node
{
    [Signal]
    public delegate void OnNukeStartedEventHandler();
    [Signal]
    public delegate void OnNukeStoppedEventHandler();
    [Signal]
    public delegate void OnNukeDetonatedEventHandler();

    public override void _Ready()
    {
        RoundManager.Instance.OnNukeDetonated += OnDetonate;
        RoundManager.Instance.OnNukeStarted += OnStarted;
        RoundManager.Instance.OnNukeStopped += OnStopped;
    }

    public override void _ExitTree()
    {
        RoundManager.Instance.OnNukeDetonated -= OnDetonate;
        RoundManager.Instance.OnNukeStarted -= OnStarted;
        RoundManager.Instance.OnNukeStopped -= OnStopped;
    }

    private void OnDetonate()
    {
        EmitSignal(SignalName.OnNukeDetonated);
    }

    private void OnStarted()
    {
        EmitSignal(SignalName.OnNukeStarted);
    }

    private void OnStopped()
    {
        EmitSignal(SignalName.OnNukeStopped);
    }
}

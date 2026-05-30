using System;
using Godot;

public partial class ViewmodelGrenade : Node3D
{
    public ItemObject Item => GetParent<ItemObject>();

    [Export]
    public AnimationTree tree;
    [Export]
    public string gunWalkPath = "walk";
    [Export]
    public string gunFirePath = "fire";
    [Export]
    public StringName statePath = "parameters/playback";
    [Export]
    public string[] stateSwitchPaths = new string[0];

    public override void _Ready()
    {
        var playback = (AnimationNodeStateMachinePlayback)tree.Get(statePath);
        if (IsInstanceValid(playback))
        {
            playback.Start("Start");
        }
    }

    public void ViewmodelEventThrowStart()
    {
        if (!IsVisibleInTree())
            return;
        tree.Set(gunFirePath, true);
    }

    public bool IsSwitchingState()
    {
        if (!IsVisibleInTree())
            return false;
        var playback = (AnimationNodeStateMachinePlayback)tree.Get(statePath);
        if (IsInstanceValid(playback))
        {
            string current = playback.GetCurrentNode();
            return Array.IndexOf(stateSwitchPaths, current) != -1 && playback.GetCurrentPlayPosition() < playback.GetCurrentLength();
        }
        return false;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;
        if (Item.model is GrenadeBase gren)
        {
            gren.CanFire = !IsSwitchingState();
        }
        if (!IsInstanceValid(NetworkPlayer.LocalInstance))
            return;
        if (!string.IsNullOrEmpty(gunWalkPath) && NetworkPlayer.LocalInstance.ActiveController is FPController fp)
        {
            tree.Set(gunWalkPath, !fp.GetRealVelocity().IsZeroApprox());
        }
        else if (!string.IsNullOrEmpty(gunWalkPath))
        {
            tree.Set(gunWalkPath, false);
        }
    }
}

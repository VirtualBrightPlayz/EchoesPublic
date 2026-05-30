using System;
using Godot;

public partial class ViewmodelFlashlight : Node3D
{
    public ItemObject Item => GetParent<ItemObject>();

    [Export]
    public Node3D root;
    [Export]
    public Camera3D viewCam;
    [Export]
    public BoneAttachment3D viewCamBone;

    [Export]
    public SpotLight3D light;

    [Export]
    public AnimationTree tree;
    [Export]
    public string statePath = "playback";
    [Export]
    public string[] stateSwitchPaths = new string[0];

    public void Click()
    {
        if (!IsVisibleInTree())
            return;
        var playback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{statePath}");
        if (IsInstanceValid(playback))
        {
            playback.Travel("Click");
        }
    }

    public void Inspect()
    {
        if (!IsVisibleInTree())
            return;
        var playback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{statePath}");
        if (IsInstanceValid(playback))
        {
            playback.Travel("Inspect");
        }
    }

    public bool IsSwitchingState()
    {
        if (!IsVisibleInTree())
            return false;
        var playback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{statePath}");
        if (IsInstanceValid(playback))
        {
            string current = playback.GetCurrentNode();
            return Array.IndexOf(stateSwitchPaths, current) != -1 && playback.GetCurrentPlayPosition() < playback.GetCurrentLength();
        }
        return false;
    }

    public override void _Ready()
    {
        var playback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{statePath}");
        if (IsInstanceValid(playback))
        {
            playback.Start("Start");
        }
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;
        Camera3D cam = GetViewport().GetCamera3D();
        if (IsInstanceValid(viewCam) && IsInstanceValid(viewCamBone) && IsInstanceValid(root) && IsInstanceValid(Item.Player))
        {
            var cam2 = Item.Player.ActiveController.Camera;
            var skel = viewCamBone.GetSkeleton();
            var pose = skel.GetBonePose(viewCamBone.BoneIdx);
            cam2.Transform = skel.Transform * pose * viewCam.Transform;
            root.GlobalTransform = (cam2.GlobalTransform * viewCam.GlobalTransform.AffineInverse() * root.GlobalTransform);
        }
        if (Item.model is Flashlight fl && IsInstanceValid(light))
        {
            light.Visible = fl.isOn;
            fl.CanFire = !IsSwitchingState();
        }
    }
}

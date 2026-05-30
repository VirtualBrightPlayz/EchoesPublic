using Godot;

public partial class ViewmodelSCP1930 : Node3D
{
    public ItemObject Item => GetParent<ItemObject>();

    [Export]
    public Node3D root;
    [Export]
    public Camera3D viewCam;
    [Export]
    public BoneAttachment3D viewCamBone;

    [Export]
    public AnimationTree tree;
    [Export]
    public string statePath = "playback";

    public void Inspect()
    {
        if (!IsVisibleInTree())
            return;
        var playback = (AnimationNodeStateMachinePlayback)tree.Get(statePath);
        if (IsInstanceValid(playback))
        {
            playback.Travel("Inspect");
        }
    }

    public void Use()
    {
        var playback = (AnimationNodeStateMachinePlayback)tree.Get(statePath);
        if (IsInstanceValid(playback))
        {
            playback.Travel("Use");
        }
    }

    public void HealCallback()
    {
        if (IsInstanceValid(Item.model) && Item.model is Scp1930 mdl)
        {
            mdl.SendHeal();
        }
    }

    public override void _Ready()
    {
        var playback = (AnimationNodeStateMachinePlayback)tree.Get(statePath);
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

        if (IsInstanceValid(Item.Player))
        {
            if (Item.Player.InputPrimary.HasFlag(ButtonInputFlags.JustPressed))
            {
                Use();
            }
        }
    }
}

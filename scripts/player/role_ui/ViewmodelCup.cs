using System.Threading.Tasks;
using Godot;

public partial class ViewmodelCup : Node3D
{
    public ItemObject Item => GetParent<ItemObject>();

    [Export]
    public AnimationTree tree;
    [Export]
    public MeshInstance3D liquid;
    [Export]
    public MeshInstance3D cap;
    [Export]
    public string gunWalkPath = "walk";
    [Export]
    public string gunFirePath = "fire";
    [Export]
    public string drinkPath;
    [Export]
    public BoneAttachment3D viewCamBone;
    [Export]
    public Node3D viewCam;
    [Export]
    public Node3D root;

    private bool drinking = false;

    public async void ViewmodelEvent(int id)
    {
        if (!IsVisibleInTree())
            return;
        switch (id)
        {
            case Cup.ViewmodelEventDrink:
            {
                tree.Set(gunFirePath, true);
                var playback = (AnimationNodeStateMachinePlayback)tree.Get("parameters/playback");
                if (IsInstanceValid(playback) && !string.IsNullOrEmpty(drinkPath))
                {
                    playback.Travel(drinkPath);
                    drinking = true;
                    if (Item.model is Scp207 scp207)
                    {
                        await ToSignal(GetTree().CreateTimer(scp207.drinkDelay), SceneTreeTimer.SignalName.Timeout);
                        if (IsInstanceValid(cap))
                        {
                            cap.Visible = !scp207.empty;
                        }
                    }
                    drinking = false;
                }
                break;
            }
        }
    }

    public override void _Ready()
    {
        if (Item.model is Cup cup)
        {
            liquid.Visible = !cup.empty;
        }
        if (Item.model is Scp207 scp207)
        {
            if (IsInstanceValid(cap))
            {
                cap.Visible = !scp207.empty;
            }
            if (IsInstanceValid(liquid))
            {
                liquid.Visible = !scp207.empty;
            }
        }
        var playback = (AnimationNodeStateMachinePlayback)tree.Get("parameters/playback");
        if (IsInstanceValid(playback))
        {
            playback.Travel("Start");
        }
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;
        if (Item.model is Cup cup)
        {
            // liquid.Visible = !cup.empty;
            if (liquid.GetActiveMaterial(0) is ShaderMaterial shmat)
            {
                shmat.SetShaderParameter("liquid_color", cup.color);
                shmat.SetShaderParameter("foam_color", cup.color);
            }
        }
        if (Item.model is Scp207 scp207 && !drinking)
        {
            var playback = (AnimationNodeStateMachinePlayback)tree.Get("parameters/playback");
            if (IsInstanceValid(cap))
            {
                cap.Visible = !scp207.empty;
            }
            if (IsInstanceValid(liquid))
            {
                liquid.Visible = !scp207.empty;
                /*
                if (liquid.GetActiveMaterial(0) is ShaderMaterial shmat && scp207.empty)
                {
                    shmat.SetShaderParameter("fill_amount", 0f);
                }
                */
            }
        }
        if (!string.IsNullOrEmpty(gunWalkPath) && IsInstanceValid(Item.Player) && Item.Player.ActiveController is FPController fp)
        {
            tree.Set(gunWalkPath, !fp.GetRealVelocity().IsZeroApprox());
        }
        else if (!string.IsNullOrEmpty(gunWalkPath))
        {
            tree.Set(gunWalkPath, false);
        }
        if (IsInstanceValid(viewCam) && IsInstanceValid(viewCamBone) && IsInstanceValid(root) && IsInstanceValid(Item.Player))
        {
            var cam = Item.Player.ActiveController.Camera;
            var skel = viewCamBone.GetSkeleton();
            var rest = skel.GetBoneRest(viewCamBone.BoneIdx);
            var pose = skel.GetBonePose(viewCamBone.BoneIdx);
            var gPose = skel.GlobalTransform * skel.GetBoneGlobalPose(viewCamBone.BoneIdx);
            cam.Transform = skel.Transform * pose * viewCam.Transform;
            root.GlobalTransform = (cam.GlobalTransform * viewCam.GlobalTransform.AffineInverse() * root.GlobalTransform);
            // root.GlobalTransform = (cam.GlobalTransform * (gPose * viewCam.Transform).AffineInverse() * root.GlobalTransform);
            // Item.Player.Controller.Camera.Transform = viewCam.Transform;
        }
    }
}

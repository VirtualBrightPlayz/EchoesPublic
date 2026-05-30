using System;
using Godot;

// [Tool]
public partial class Viewmodel : Node3D
{
    public ItemObject Item => GetParent<ItemObject>();

    [Export]
    public AnimationTree tree;
    [Export]
    public Node3D root;
    [Export]
    public BoneAttachment3D viewCamBone;
    [Export]
    public Camera3D viewCam;
	[Export]
	public Node3D attachmentRoot;
	[Export]
    public float aimLerpSpeed = 2f;
    [Export]
    public Node3D aimMarker;
    [Export]
    public Node3D aimHandMarker;
    [Export]
    public string gunStatePath = "playback";
    [Export]
    public string gunReloadPath = "reload";
    [Export]
    public string gunAimPath = "aim";
    [Export]
    public string gunIdlePath = "idle";
    [Export]
    public string gunWalkPath = "walk";
    [Export]
    public string[] gunFirePath = new string[0];
    [Export]
    public string gunAimFirePath = string.Empty;
    [Export]
    public string gunInspectPath = "inspect";
    [Export]
    public string gunAimCondPath = "aim";
    [Export]
    public string[] gunStateSwitchPaths = new string[0];
    [Export]
    public bool reload = false;
    [Export]
    public bool aim = false;
    [Export]
    public bool inspect = false;
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public GameSound fireAudio;

    public bool canFire = true;

    private Vector3 lastPosition;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;
        lastPosition = GetViewport().GetCamera3D().GlobalPosition;
        var gunPlayback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{gunStatePath}");
        if (IsInstanceValid(gunPlayback))
        {
            gunPlayback.Travel("Start");
            canFire = false;
        }
        else
        {
            canFire = true;
        }
    }

    public void ViewmodelInspect()
    {
        if (!IsVisibleInTree())
            return;
        var gunPlayback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{gunStatePath}");
        if (IsInstanceValid(gunPlayback))
        {
            gunPlayback.Travel(gunInspectPath);
        }
    }

    public void ViewmodelShoot()
    {
        if (!IsVisibleInTree())
            return;
        var gunPlayback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{gunStatePath}");
        if (IsInstanceValid(gunPlayback))
        {
            if (aim && !string.IsNullOrEmpty(gunAimFirePath))
            {
                gunPlayback.Start(gunAimFirePath);
            }
            else
            {
                gunPlayback.Start(gunFirePath[GD.Randi() % gunFirePath.Length]);
            }
            if (IsInstanceValid(audio) && IsInstanceValid(fireAudio) && audio.GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
            {
                playback.PlayStream(fireAudio.streams[GD.Randi() % fireAudio.streams.Length], 0, fireAudio.volumeDb, fireAudio.pitch);
            }
            canFire = true;
            // gunPlayback.Travel(gunFirePath);
        }
    }

    public void ViewmodelAim(bool isAiming)
    {
        aim = isAiming;
        if (!IsVisibleInTree())
            return;
        var gunPlayback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{gunStatePath}");
        if (IsInstanceValid(gunPlayback))
        {
            if (aim && !string.IsNullOrEmpty(gunAimPath))
            {
                gunPlayback.Travel(gunAimPath);
            }
            else
            {
                gunPlayback.Travel(gunIdlePath);
            }
            canFire = false;
        }
    }

    public void ViewmodelReload()
    {
        if (!IsVisibleInTree())
            return;
        var gunPlayback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{gunStatePath}");
        if (IsInstanceValid(gunPlayback))
        {
            gunPlayback.Start(gunReloadPath);
        }
    }

    public bool IsSwitchingState()
    {
        if (!IsVisibleInTree())
            return false;
        var gunPlayback = (AnimationNodeStateMachinePlayback)tree.Get($"parameters/{gunStatePath}");
        if (IsInstanceValid(gunPlayback))
        {
            if (canFire)
                return false;
            string current = gunPlayback.GetCurrentNode();
            return Array.IndexOf(gunStateSwitchPaths, current) != -1 && gunPlayback.GetCurrentPlayPosition() < gunPlayback.GetCurrentLength();
        }
        return false;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;
        Camera3D cam = GetViewport().GetCamera3D();
        if (IsInstanceValid(aimMarker) && IsInstanceValid(aimHandMarker) && IsInstanceValid(cam))
        {
            if (aim && !reload)
            {
                var target = cam.GlobalPosition + (aimHandMarker.GlobalPosition - aimMarker.GlobalPosition);
                aimHandMarker.GlobalPosition = aimHandMarker.GlobalPosition.Lerp(target, (float)delta * aimLerpSpeed);
            }
            else
            {
                aimHandMarker.Position = aimHandMarker.Position.Lerp(Vector3.Zero, (float)delta * aimLerpSpeed);
            }
        }
        if (Engine.IsEditorHint())
            return;
        if (aim != (Item.PrimaryHolder != null && Item.PrimaryHolder.IsAimingDown))
        {
            ViewmodelAim(Item.PrimaryHolder != null && Item.PrimaryHolder.IsAimingDown);
        }
        tree.Set(gunAimCondPath, aim);
        if (Item.Player.ActiveController is FPController fp)
        {
            tree.Set(gunWalkPath, !fp.GetRealVelocity().IsZeroApprox() && !aim);
        }
        else
        {
            tree.Set(gunWalkPath, false);
        }
        if (IsInstanceValid(viewCam) && IsInstanceValid(viewCamBone) && IsInstanceValid(root) && IsInstanceValid(Item.Player))
        {
            var cam2 = Item.Player.ActiveController.Camera;
            var skel = viewCamBone.GetSkeleton();
            var pose = skel.GetBonePose(viewCamBone.BoneIdx);
            // cam2.Transform = skel.Transform * pose * viewCam.Transform;
            // root.GlobalTransform = (cam2.GlobalTransform * viewCam.GlobalTransform.AffineInverse() * root.GlobalTransform);
        }
        lastPosition = cam.GlobalPosition;
        if (Item.model is GunBase gun)
        {
            gun.CanFire = !IsSwitchingState();
            reload = gun.IsReloading;
        }
        else
            reload = false;
        aim = Item.PrimaryHolder.IsAimingDown;
    }
}

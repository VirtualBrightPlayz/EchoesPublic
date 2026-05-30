using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class FacilityCameraStation : StaticBody3D, IInteractable
{
    [Export]
    int curCamId = -1;

    int lastCamId = -1;

    FacilityCamera targetCamera;

    [Export]
    public FacilityCamera.SecurityCameraState CameraStationState { get; set; } =
        FacilityCamera.SecurityCameraState.None | FacilityCamera.SecurityCameraState.NoAudio;
    
    [Export]
    public string targetCameraTag = "";

    [Export]
    public bool onlyTargetCurrentZone = true;

    [Export]
    public bool doCameraTag = false;

    [Export]
    private Label camNameLabel;

    [Export]
    private Label camStatusLabel;

    [Export]
    private SubViewport viewport;

    [Export]
    private Camera3D childCam;

    [Export]
    private AudioStreamPlayer3D audioPlayer;

    [Export]
    float MaxViewDistance = 8f;

    //private AudioEffectCapture capture;
    //private AudioStreamGeneratorPlayback playback;

    private ZoneArea.Zone currentZone = ZoneArea.Zone.Unknown;

    public Vector3 WorldInteractPosition => GlobalPosition;

    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    public bool isSeen = false;

    private bool firstTimeSeen = false;
    
    //Yes, harded coded array. No im not stupid
    Color[] modulation = new Color[2];

    uint cullMask = 0;

    public override void _EnterTree()
    {
        base._EnterTree();
    }

    public override async void _Ready()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        currentZone = global::ZoneArea.GetZone(GlobalPosition);
        if (currentZone == ZoneArea.Zone.Unknown)
        {
            Log.PrintWarn("Can't find Zone for security camera station.");
        }
        modulation[0] = camStatusLabel.SelfModulate;
        modulation[1] = camNameLabel.SelfModulate;
        cullMask = childCam.CullMask;
        if (!IsInstanceValid(FacilityCameraManager.Instance))
        {
            Log.PrintWarn("Can't find Camera Manager.");
        }
    }

    public void RenderNextFrame()
    {
        if (isSeen)
        {
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // var cam = GetViewport().GetCamera3D();
        var cam = NetworkPlayer.LocalInstance?.ActiveController?.Camera;
        isSeen = /*!cam.IsPositionBehind(GlobalPosition) &&*/ IsInstanceValid(cam) && GlobalPosition.DistanceSquaredTo(cam.GlobalPosition) <= MaxViewDistance * MaxViewDistance;
        // if (IsInstanceValid(viewport))
        // {
        //     viewport.PositionalShadowAtlasSize = GetTree().Root.PositionalShadowAtlasSize;
        // }
        
        if (curCamId != -1 && isSeen && !firstTimeSeen)
        {
            firstTimeSeen = true;
            RenderNextFrame();
        }
        
        if(CameraStationState.HasFlag(FacilityCamera.SecurityCameraState.Offline))
        {
            Color newColor = new Color(0, 0, 0, 0);
            //camNameLabel.SelfModulate = newColor;
            camNameLabel.Text = "CAMERA_OFFLINE";
            Color camStatusColor = modulation[0];
            // camStatusColor.A = 1f;
            camStatusLabel.Text = "CAMERA_OFFLINE";
            // camStatusLabel.SelfModulate = camStatusColor;
            // childCam.CullMask = 2;
            //if offline, dont even bother figuring out what camera to look at.
            return;
        }
        else
        {
            camNameLabel.Text = IsInstanceValid(targetCamera) ? targetCamera.cameraName : "Unknown";
            // camStatusLabel.SelfModulate = modulation[0];
            // camNameLabel.SelfModulate = modulation[1];
            // childCam.CullMask = cullMask;
        }
        

        // RenderNextFrame();

        // if (!CameraStationState.HasFlag(FacilityCamera.SecurityCameraState.NoAudio))
        // {
        //     foreach (var area in childAreas)
        //     {
        //         area.ReverbBusEnabled = isSeen;
        //     }
        //     //Prevent audio from playing on cameras with no audio (default)
        //     if (IsInstanceValid(targetCamera) && targetCamera.HasAudio)
        //     {
        //         if (IsInstanceValid(viewport))
        //         {
        //             viewport.PositionalShadowAtlasSize = GetTree().Root.PositionalShadowAtlasSize;
        //         }
        //         viewport.AudioListenerEnable3D = isSeen;
        //         if (IsInstanceValid(audioPlayer))
        //         {
        //             if (!audioPlayer.Playing && isSeen)
        //             {
        //                 audioPlayer.Stream = new AudioStreamGenerator()
        //                 {
        //                     MixRate = AudioServer.GetMixRate(),
        //                 };
        //                 audioPlayer.Play();
        //                 playback = audioPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        //                 // for (int i = 0; i < playback.GetFramesAvailable(); i++)
        //                 //     playback.PushFrame(Vector2.Zero);
        //             }
        //             if (!isSeen)
        //                 audioPlayer.Stop();
        //             if (IsInstanceValid(playback) && IsInstanceValid(capture) && isSeen)
        //             {
        //                 int count = Mathf.Min(playback.GetFramesAvailable(), capture.GetFramesAvailable());
        //                 playback.PushBuffer(capture.GetBuffer(count));
        //             }
        //         }
        //     }
        // }


        if (IsMultiplayerAuthority())
        {
            if(curCamId == -1 && IsInstanceValid(FacilityCameraManager.Instance))
            {
                List<FacilityCamera> cameras = FacilityCameraManager.Instance.GetCameras().Where(x => x.cameraTag == targetCameraTag || !doCameraTag).Where(x => !onlyTargetCurrentZone || x.ZoneArea == currentZone).ToList();
                if (cameras.Count == 0)
                {
                    curCamId = -1;
                }
                else
                {
                    curCamId = cameras[GD.RandRange(0, cameras.Count - 1)].id;
                }
            }
            else if(!IsInstanceValid(FacilityCameraManager.Instance))
            {
                // Log.PrintWarn("Invalid CameraManager.");
            }
            if(lastCamId != curCamId)
            {
                //Avoid stackoverflow
                int oldId = curCamId;
                ValidateAndSetCamera(oldId);
                lastCamId = curCamId;
            }
        }
        else
        {
            if (lastCamId != curCamId)
            {
                int oldId = curCamId;
                ValidateAndSetCamera(oldId);
                lastCamId = curCamId;
            }
        }
        if(curCamId != -1 && IsInstanceValid(targetCamera))
        {
            childCam.GlobalPosition = targetCamera.CurrentPhysicalCameraPosition;
            childCam.GlobalRotation = targetCamera.CurrentPhysicalCameraRotation;
            camNameLabel.Text = targetCamera.cameraName;
        }
    }

    public void ServerForceRender()
    {
        Rpc(MethodName.RpcForceRender);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcForceRender()
    {
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
    }
    
    public void ServerQueueRender()
    {
        Rpc(FacilityCameraStation.MethodName.RpcServerQueueRender);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcServerQueueRender()
    {
        RenderNextFrame();
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        Rpc(nameof(RpcUse));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    void RpcUse()
    {
        if (!IsMultiplayerAuthority()) return;
        List<FacilityCamera> cameras = FacilityCameraManager.Instance.GetCameras().Where(x => x.cameraTag == targetCameraTag || !doCameraTag).Where(x => !onlyTargetCurrentZone || x.ZoneArea == currentZone).ToList();
        if (cameras.Count == 0)
            return;
        int camCanidate = cameras[GD.RandRange(0, cameras.Count - 1)].id;
        FacilityCamera camera = FacilityCameraManager.Instance.GetCamera(camCanidate);
        if (!IsInstanceValid(camera))
        {
            return;
        }
        //Log.Print("Change Camera. ID: " + camCanidate);
        curCamId = camCanidate;
        ServerQueueRender();
    }

    void ValidateAndSetCamera(int id)
    {
        if (FacilityCameraManager.Instance == null) return;
        FacilityCamera camera = FacilityCameraManager.Instance.GetCamera(id);
        if (!IsInstanceValid(camera))
        {
            return;
        }
        if (IsInstanceValid(targetCamera) && IsMultiplayerAuthority())
        {
            targetCamera.IsActivelyViewed = false;
        }
        if (IsInstanceValid(camera) && IsMultiplayerAuthority())
        {
            camera.IsActivelyViewed = true;
        }
        targetCamera = camera;
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        return;
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return true;
    }
}

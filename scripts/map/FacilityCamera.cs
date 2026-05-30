using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//I made this I could publish a branch and work on this away from home... I swear ;-;
public partial class FacilityCamera : Node3D
{
    [Export]
    public Marker3D Marker;
    
    [Export]
    public Node3D mesh;

    [Export]
    public Node3D pole;

    public Vector3 CurrentPhysicalCameraPosition => Marker.GlobalPosition;

    public Vector3 CurrentPhysicalCameraRotation => Marker.GlobalRotation;

    //The current Camera ID
    [Export]
    public int id;

    /// <summary>
    /// The "tag" or group of cameras this camera belongs to.
    /// </summary>
    [Export]
    public string cameraTag = "";

    /// <summary>
    /// The name of the camera to display on monitors and such
    /// </summary>
    [Export]
    public string cameraName = "";

    ZoneArea.Zone _zoneArea = global::ZoneArea.Zone.Unknown;

    [Export]
    public int ownerId = 0;

    [Export]
    bool _serverClaimed = false;

    [Export]
    public SecurityCameraState cameraState = SecurityCameraState.None | SecurityCameraState.NoAudio;

    [Export]
    public bool doCameraPan = true;

    [Export]
    public float rotSpeedMultiplier = 3f;

    [Export]
    public float maxAngleDeg = 20f;

    public override void _EnterTree()
    {
        //FacilityCameraManager.Singleton.Register(this);
    }

    public override async void _Ready()
    {
        FacilityCameraManager.Instance.Register(this);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        if (IsInstanceValid(this))
        {
            _zoneArea = global::ZoneArea.GetZone(GlobalPosition);
            if (_zoneArea == global::ZoneArea.Zone.Unknown)
            {
                Log.PrintWarn("Unknown Zone Area for FacilityCamera.");
            }
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        FacilityCameraManager.Instance.Unregister(this);
    }

    private float _time;

    public override void _Process(double delta)
    {
        base._Process(delta);
        pole.GlobalRotation = Vector3.Zero;
        if (IsActivelyViewed && doCameraPan)
        {
            _time += rotSpeedMultiplier * (float)delta;
            var targetRotation = Mathf.PingPong(_time, maxAngleDeg * 2) - maxAngleDeg;
            //ensure mesh inehrits the rotation of the whole object
            mesh.GlobalRotation = new Vector3(GlobalRotation.X, GlobalRotation.Y + Mathf.DegToRad(targetRotation), GlobalRotation.Z);
        }
        else
        {
            //Commented out, just stop panning now instead of resetting the rotation.
            //mesh.GlobalRotation = initialRotation;
        }
    }

    public bool IsOffline
    {
        get => cameraState.HasFlag(SecurityCameraState.Offline);
        set
        {
            if (value)
                cameraState |= SecurityCameraState.Offline;
            else
                cameraState &= ~SecurityCameraState.Offline;
        }
    }    
    
    public bool IsActivelyViewed
    {
        get => cameraState.HasFlag(SecurityCameraState.ActivelyViewed);
        set
        {
            if (value)
                cameraState |= SecurityCameraState.ActivelyViewed;
            else
                cameraState &= ~SecurityCameraState.ActivelyViewed;
        }
    }

    public bool HasAudio
    {
        get => false;
    }
    
    public bool HasVideo
    {
        get => !cameraState.HasFlag(SecurityCameraState.NoVideo);
        set
        {
            if (!value)
                cameraState |= SecurityCameraState.NoVideo;
            else
                cameraState &= ~SecurityCameraState.NoVideo;
        }
    }
    
    public bool CanControl
    {
        get => !cameraState.HasFlag(SecurityCameraState.NoControl);
        set
        {
            if (!value)
                cameraState |= SecurityCameraState.NoControl;
            else
                cameraState &= ~SecurityCameraState.NoControl;
        }
    }

    public void ClientClaim()
    {
        Rpc(nameof(RpcClaim));
    }

    public void ClientRelease()
    {
        Rpc(nameof(RpcRelease));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    void RpcClaim()
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }
        var sender = Multiplayer.GetRemoteSenderId();
        if(ownerId != 0)
        {
            return;
        }
        ownerId = sender;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    void RpcRelease()
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }
        var sender = Multiplayer.GetRemoteSenderId();
        if (ownerId == sender)
        {
            ownerId = 0;
        }
    }

    /// <summary>
    /// Releases the camera from being managed by any client.
    /// </summary>
    public void ServerRelease()
    {
        if(!Multiplayer.IsServer())
        {
            return;
        }
        ownerId = 0;
    }

    /// <summary>
    /// Effectively blocks clients from claiming a camera
    /// </summary>
    public void ServerClaim()
    {
        if(!Multiplayer.IsServer())
        {
            return;
        }
        _serverClaimed = true;
        ServerRelease();
    }

    public ZoneArea.Zone ZoneArea
    {
        get
        {
            return _zoneArea;
        }
    }

    [Flags]
    public enum SecurityCameraState : byte
    {
        None = 0,
        ActivelyViewed = 1,
        /// <summary>
        /// Setting this value will override anything else set in this property.
        /// </summary>
        Offline = 2,
        NoVideo = 4,
        /// <summary>
        /// Does nothing, unused as all code regarding audio has been removed.
        /// </summary>
        NoAudio = 8,
        NoControl = 16,
    }
}

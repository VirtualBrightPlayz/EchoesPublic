using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class DebugPlayerModeManager : SingletonNode3D<DebugPlayerModeManager>, IInitScript
{
	public string Username => "LocalPlayer";

    public XRInterface xrInterface;
    public bool IsXR => xrInterface != null && xrInterface.IsInitialized();

	[Export]
	public GameData Data { get; set; }

	[Export]
	public RoleID SpawnRole { get; set; }

	public override void _EnterTree()
	{
		base._EnterTree();
		IInitScript.Instance = this;
        Settings.SettingsModified = UpdateSettings;
        Settings.ReadSettings();
        UpdateSettings();
        xrInterface = XRServer.FindInterface("OpenXR");
        if (xrInterface != null && xrInterface.IsInitialized())
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            GetViewport().UseXR = true;
            GetViewport().UseHdr2D = false;
            // GetTree().PhysicsInterpolation = false;
            XRServer.WorldScale = 1.5f;
        }
        Data.Init();
	}

	public override void _Ready()
	{
		base._Ready();
        Data.Reload();
        if (IInitScript.IsTestClient)
        {
            NetworkManager.Instance.ClientEnet();
        }
        else
        {
            NetworkManager.Instance.HostEnet();
            NetworkPlayer.LocalInstance.SV_Spawn(SpawnRole, GlobalPosition);
        }
	}

    public void UpdateSettings()
    {
        float menuVolumeDb = Mathf.Clamp(Mathf.LinearToDb(Settings.User.Volume), -80f, 0f);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Menu"), menuVolumeDb);
        AudioServer.InputDevice = Settings.User.MicInputName;
        var root = GetTree().Root;
        root.Msaa3D = (Viewport.Msaa)Settings.User.MSAA;
        root.Msaa2D = (Viewport.Msaa)Settings.User.MSAA;

        if (IsXR)
        {
            GetTree().Root.Msaa2D = Viewport.Msaa.Disabled;
            GetTree().Root.Msaa3D = Viewport.Msaa.Disabled;
        }
        RenderingServer.DirectionalShadowAtlasSetSize(MenuManager.ShadowQualityToSize(Settings.User.ShadowQuality), true);
        GetTree().Root.PositionalShadowAtlasSize = Settings.User.ShadowQuality == Settings.GenericQuality.VeryLow ? 0 : MenuManager.ShadowQualityToSize(Settings.User.ShadowQuality);
    }
}


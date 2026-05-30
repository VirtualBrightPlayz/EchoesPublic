using Godot;
using System;

[GlobalClass]
[Tool]
public partial class PresetRoom : Resource
{
	public enum RoomClass : byte
	{
		Endoff = 0,
		Corner = 1,
		Hall = 2,
		TRoom = 3,
		XRoom = 4,
		Other = 5,
	}

	public enum RoomSize : byte
	{
		Small = 0,
		Normal = 1,
		Large = 2,
	}

	[Export]
	public RoomClass type;
	[Export]
	public RoomSize size;
	[Export]
	public int stage = -1;
	[Export]
	[Obsolete]
	private PackedScene[] scenes;
	public PackedScene Scene => ResourceLoader.Load<PackedScene>(sceneFile);
	public PackedScene SceneCached => IInitScript.Instance.Data.GetRoomScene(sceneFile);
	[Export(PropertyHint.File)]
	public string sceneFile = string.Empty;
	[Export]
	public bool isGeneric = false;
	[Export]
	public bool isInterest = false;
	[Export]
	public Rect2I gridRect = new Rect2I(Vector2I.Zero, Vector2I.One);
	[Export]
	public Texture2D layoutIcon;
	[Export]
	public Texture2D layoutIconOverride;
	[ExportGroup("Metadata")]
	[Export]
	public ZoneArea.Zone zone = ZoneArea.Zone.Unknown;
	[Export]
	public string category = string.Empty;
	[Export]
	public Godot.Collections.Array<RoomPropertyData> templateData = new Godot.Collections.Array<RoomPropertyData>();
	[Export]
	public Godot.Collections.Dictionary<string, string> templateDefaultValues = new Godot.Collections.Dictionary<string, string>();
}

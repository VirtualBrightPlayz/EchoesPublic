using Godot;

[GlobalClass]
public partial class RoomMapLink : Node3D
{
    public const string MapLinkName = "map_link";

    [Export]
    public RoomInfoNode infoNode;

    public override void _Ready()
    {
        base._Ready();
        if (IsInstanceValid(infoNode) && IsMultiplayerAuthority())
        {
            if (infoNode.keyvalues.TryGetValue(MapLinkName, out string value))
            {
            }
        }
    }
}
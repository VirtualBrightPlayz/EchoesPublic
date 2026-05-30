using Godot;

[GlobalClass]
public partial class ItemSpawnpoint : Marker3D
{
    public const string GroupName = "item_spawnpoint";

    [Export]
    public ItemSpawnGroup SpawnGroup;

    public override void _EnterTree()
    {
        AddToGroup(GroupName);
    }

    public override void _ExitTree()
    {
        RemoveFromGroup(GroupName);
    }

    public void Spawn(ItemPreset item)
    {
        if(!IsInstanceValid(item))
        {
            return;
        }
        ItemObject wItem = item.SpawnNew();
        wItem.ModelEnabled = true;
        wItem.SV_Teleport(GlobalPosition, GlobalRotation);
    }
}
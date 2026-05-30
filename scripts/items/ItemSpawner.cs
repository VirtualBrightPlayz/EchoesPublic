using Godot;

[GlobalClass]
public partial class ItemSpawner : Marker3D
{
    [Export]
    public ItemPreset Item;
    [Export]
    public bool SpawnOnReady = true;

    public override void _EnterTree()
    {
        AddToGroup(ItemSpawnpoint.GroupName);
    }

    public override void _ExitTree()
    {
        RemoveFromGroup(ItemSpawnpoint.GroupName);
    }

    public void Spawn()
    {
        if (!IsInstanceValid(Item))
        {
            Log.PrintWarn($"Item is null: {GetPath()}");
            return;
        }
        if (!Multiplayer.HasMultiplayerPeer() || IsMultiplayerAuthority())
        {
            ItemObject wItem = Item.SpawnNew();
            wItem.SV_Teleport(GlobalPosition, GlobalRotation);
        }
        else
            throw new NotMultiplayerAuthorityException();
    }

    public void SpawnRoundStart()
    {
        if (!SpawnOnReady)
            return;
        Spawn();
    }
}
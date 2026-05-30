using Godot;

[GlobalClass]
public partial class ItemSpawnGroup : Resource
{
    [Export]
    public ItemPreset[] Items = System.Array.Empty<ItemPreset>();
    [Export]
    public bool RemoveUsedItem = false;
    [Export]
    public bool RemoveUsedSpawn = true;
    [Export]
    public int MaxSpawned = -1;
}
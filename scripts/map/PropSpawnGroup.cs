using Godot;

[GlobalClass]
public partial class PropSpawnGroup : Resource
{
    [Export]
    public PackedScene[] Props = System.Array.Empty<PackedScene>();
    [Export]
    public bool RemoveUsedProp = false;
    [Export]
    public bool RemoveUsedSpawn = true;
    [Export]
    public int MaxSpawned = -1;
}
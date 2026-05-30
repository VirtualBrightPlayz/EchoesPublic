using System;
using Godot;

[GlobalClass]
public partial class PropSpawnpoint : Marker3D
{
    public const string GroupName = "prop_spawnpoint";

    [Signal]
    public delegate void OnSpawnedEventHandler();
    [Signal]
    public delegate void OnNotSpawnedEventHandler();

    [Export]
    public PropSpawnGroup SpawnGroup;

    public override void _EnterTree()
    {
        AddToGroup(GroupName);
    }

    public override void _ExitTree()
    {
        RemoveFromGroup(GroupName);
    }

    public void Spawn(PackedScene item)
    {
        if (!IsInstanceValid(item))
        {
            // EmitSignal(SignalName.OnNotSpawned);
            return;
        }
        if (Array.IndexOf(ItemManager.Instance.Data.Props, item) == -1)
        {
            Log.PrintWarn($"Prop {item.ResourcePath} isn't spawnable.");
            return;
        }
        Node3D prop = item.Instantiate<Node3D>();
        prop.Position = GlobalPosition;
        prop.Rotation = GlobalRotation;
        ItemManager.Instance.PropSpawnNode.AddChild(prop, true);
        // EmitSignal(SignalName.OnSpawned);
    }
}
using System.Collections.Generic;
using Godot;

public partial class NpcSpawnerTest : Marker3D
{
    [Export]
    public int maxSpawned = 5;
    [Export]
    public PackedScene scene;

    public Vector3 pos;
    public Vector3 rot;

    public List<Node3D> spawned = new List<Node3D>();

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(1);
    }

    public override void _Ready()
    {
        pos = GlobalPosition;
        rot = GlobalRotation;
        GlobalPosition = Vector3.Zero;
        GlobalRotation = Vector3.Zero;
    }

    public void Spawn()
    {
        if (!IsMultiplayerAuthority())
            return;
        spawned.RemoveAll(x => !IsInstanceValid(x));
        if (spawned.Count >= maxSpawned)
            return;
        var node = scene.Instantiate<Node3D>();
        node.Position = pos;
        node.Rotation = rot;
        // node.Position = Vector3.Zero;
        // node.Rotation = Vector3.Zero;
        AddChild(node, true);
        spawned.Add(node);
    }
}
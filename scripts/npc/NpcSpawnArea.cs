using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class NpcSpawnArea : Marker3D
{
    [Export] public int maxSpawned = 50;
    [Export] public int maxRange = 40;
    [Export] public PackedScene scene;
    [Export] public NavigationRegion3D region;

    public List<Node3D> spawned = new List<Node3D>();

    public void SpawnArea()
    {
        if (!IsMultiplayerAuthority())
            return;
        for (int i = 0; i < maxSpawned; i++)
        {
            float x = GD.RandRange(-maxRange, maxRange);
            float z = GD.RandRange(-maxRange, maxRange);
            Vector3 dir = new Vector3(x, 0f, z);
            Vector3 pt = NavigationServer3D.MapGetClosestPoint(region.GetNavigationMap(), GlobalPosition + dir);
            // DebugDrawManager.Instance.DrawDebugPoint(pt, Colors.Red, 10d);
            SpawnAt(pt);
        }
    }

    public void SpawnAt(Vector3 pos)
    {
        if (!IsMultiplayerAuthority())
            return;
        spawned.RemoveAll(x => !IsInstanceValid(x));
        if (spawned.Count >= maxSpawned)
            return;
        var node = scene.Instantiate<Node3D>();
        node.Position = (pos);
        // DebugDrawManager.Instance.DrawDebugString(pos, "thingy", Colors.White, 20d);
        Vector3 rot = new Vector3(0f, (float)GD.RandRange(-Mathf.Tau, Mathf.Tau), 0f);
        node.Rotation = rot;
        ItemManager.Instance.SpawnNode.AddChild(node, true);
        spawned.Add(node);
    }
}
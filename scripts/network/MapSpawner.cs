using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class MapSpawner : Node3D
{
    public List<string> Scenes = [];
    public List<(int, ulong, Transform3D)> SpawnedNodes = [];
    public Dictionary<ulong, Node3D> SpawnedLookup = [];
    public ulong IdCounter = 0;

    public override void _Ready()
    {
        base._Ready();
        Multiplayer.PeerConnected += _Joined;
        ChildExitingTree += _Despawn;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Multiplayer.PeerConnected -= _Joined;
        ChildExitingTree -= _Despawn;
    }

    private void _Despawn(Node node)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }
        Rpc(MethodName.RpcDespawn, SpawnedLookup.First(x => x.Value == node).Key);
    }

    private void _Joined(long id)
    {
        if (IsMultiplayerAuthority())
        {
            return;
        }
        foreach (var spawned in SpawnedNodes)
        {
            RpcId(id, MethodName.RpcSpawn, spawned.Item1, spawned.Item2, spawned.Item3);
        }
    }

    private async Task<Node3D> SpawnInternal(int id, ulong name, Transform3D xform)
    {
        if (id < 0 || id >= Scenes.Count)
        {
            Log.PrintErr($"Scene ID {id} is invalid.");
            return null;
        }
        string path = Scenes[id];
        Error err = ResourceLoader.LoadThreadedRequest(path);
        if (err != Error.Ok)
        {
            Log.PrintErr($"Unable to load scene {path}: {err}");
            return null;
        }
        var status = ResourceLoader.LoadThreadedGetStatus(path);
        while (status == ResourceLoader.ThreadLoadStatus.InProgress)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            status = ResourceLoader.LoadThreadedGetStatus(path);
        }
        if (status == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            var scnRes = ResourceLoader.LoadThreadedGet(path);
            if (scnRes is PackedScene scn)
            {
                var node = scn.Instantiate<Node3D>();
                node.Name = name.ToString();
                node.Transform = xform;
                return node;
            }
        }
        return null;
    }

    public async Task<Node3D> Spawn(int id, Transform3D xform)
    {
        if (!IsMultiplayerAuthority())
        {
            return null;
        }
        IdCounter++;
        var node = await SpawnInternal(id, IdCounter, xform);
        AddChild(node, true);
        SpawnedNodes.Add((id, IdCounter, xform));
        SpawnedLookup.Add(IdCounter, node);
        Rpc(MethodName.RpcSpawn, id, IdCounter, xform);
        return node;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private async void RpcSpawn(int id, ulong name, Transform3D xform)
    {
        var node = await SpawnInternal(id, name, xform);
        SpawnedLookup.Add(name, node);
        AddChild(node, true);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcDespawn(ulong name)
    {
        if (SpawnedLookup.Remove(name, out var node))
        {
            node.QueueFree();
        }
    }
}

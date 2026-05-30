using System.Linq;
using Godot;

[GlobalClass]
public partial class RoomProxyNode : Node3D
{
    public PackedScene scene;
    // public MultiplayerSynchronizer sync;
    // public MultiplayerSpawner spawner;
    private IPlayerList List;
    public Node node;

    public RoomProxyNode()
    {
        // sync = new MultiplayerSynchronizer();
        // sync.ReplicationConfig = new SceneReplicationConfig();
        // sync.AddVisibilityFilter(Callable.From<int, bool>(Filter));
        // AddChild(sync, true);
        // spawner = new MultiplayerSpawner();
        // AddChild(spawner, true);
        // spawner.SpawnPath = "..";
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        // spawner.AddSpawnableScene(scene.ResourcePath);
        Multiplayer.PeerConnected += _Joined;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Multiplayer.PeerConnected -= _Joined;
    }

    public override void _Ready()
    {
        base._Ready();
        List = IPlayerList.List(this);
        // if (IsMultiplayerAuthority())
        {
            // AddChild(scene.Instantiate(), true);
        }
    }

    private void _Joined(long id)
    {
        if (IsMultiplayerAuthority())
        {
            RpcId(id, MethodName.RpcSpawn, Filter((int)id));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RpcSpawn(bool visible)
    {
        if (!IsInstanceValid(node) && visible)
        {
            node = scene.Instantiate();
            AddChild(node, true);
        }
        else if (IsInstanceValid(node) && !visible)
        {
            node.QueueFree();
            node = null;
        }
    }

    public bool Filter(int peerId)
    {
        if (!IsInstanceValid((Node)List) || !IsInstanceValid(ItemManager.Instance))
        {
            return true;
        }
        NetworkPlayer player = List.PlayerList.FirstOrDefault(x => x.AuthorityId == peerId);
        if (!IsInstanceValid(player) || player.ActiveController == null)
        {
            return false;
        }
        return player.PlayerPosition.DistanceSquaredTo(GlobalPosition) <= ItemManager.Instance.propRenderDistance * ItemManager.Instance.propRenderDistance;
    }
}

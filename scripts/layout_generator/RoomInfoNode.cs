using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class RoomInfoNode : Node3D
{
    [Export]
    public MultiplayerSynchronizer sync;

    [Export]
    public Godot.Collections.Dictionary<string, Node3D> features = new Godot.Collections.Dictionary<string, Node3D>();

    [Export]
    public Godot.Collections.Dictionary<string, string> keyvalues = new Godot.Collections.Dictionary<string, string>();

    private IPlayerList List;

    public override void _EnterTree()
    {
        base._EnterTree();
        /*
        if (IsInstanceValid(sync))
            return;
        sync = new MultiplayerSynchronizer();
        sync.Name = "RoomInfoNodeSync";
        sync.ReplicationConfig = new SceneReplicationConfig();
        sync.AddVisibilityFilter(Callable.From<int, bool>(Filter));
        AddChild(sync, true);
        */
    }

    public override void _Ready()
    {
        base._Ready();
        List = IPlayerList.List(this);
        foreach (var kvp in keyvalues)
        {
            if (features.TryGetValue(kvp.Key, out Node3D node))
            {
                node.Visible = bool.Parse(kvp.Value);
            }
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
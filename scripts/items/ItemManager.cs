using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ItemManager : SingletonNode3D<ItemManager>
{
    public Node SpawnNode => spawner.GetNode(spawner.SpawnPath);
    public Node PropSpawnNode => propSpawner.GetNode(propSpawner.SpawnPath);

    [Export]
    public GameData Data;
    [Export]
    public MultiplayerSpawner spawner;
    
    [Export]
    public MultiplayerSpawner propSpawner;
    
    [Export(PropertyHint.Layers3DPhysics)]
    public uint attackLayer;
    [Export(PropertyHint.Layers3DPhysics)]
    public uint playerLayer;
    [Export]
    public ProjectileResistanceMedium fleshMedium;

    [Export]
    public float propRenderDistance = 10f;
    [Export]
    public float itemRenderDistance = 8f;
    [Export]
    public ulong propMinSpawnTimeMsec = 10000;
    [Export]
    public int propCullTickrate = 30;
    private ulong lastPropCullTickTime;

    public int nextSerial = 0;
    public Dictionary<int, ItemObject> Items = new();

    public PropDatabase savedProps = new();

    public class PropInfo
    {
        public string scenePath;
        public Transform3D transform;
        public ulong destroyTimestamp;
        public Dictionary<string, byte[]> data = new Dictionary<string, byte[]>();

        public PropInfo()
        {
        }

        public PropInfo(RigidbodySync prop)
        {
            scenePath = prop.SceneFilePath;
            if (string.IsNullOrEmpty(prop.SceneFilePath))
            {
                scenePath = prop.Owner.SceneFilePath;
            }
            transform = prop.GlobalTransform;
            destroyTimestamp = Time.GetTicksMsec();
            foreach (var propInfo in prop.GetPropertyList())
            {
                var name = propInfo["name"].AsString();
                var flags = propInfo["usage"].As<PropertyUsageFlags>();
                var type = propInfo["type"].As<Variant.Type>();
                if (flags.HasFlag(PropertyUsageFlags.ScriptVariable))
                {
                    switch (type)
                    {
                        case Variant.Type.Bool:
                        case Variant.Type.Int:
                        case Variant.Type.Float:
                        case Variant.Type.String:
                        case Variant.Type.StringName:
                            data.Add(name, GD.VarToBytes(prop.Get(name)));
                            break;
                    }
                }
            }
        }
    }

    public class PropDatabase
    {
        public List<PropInfo> props = new List<PropInfo>();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        _Ready(); // ?? why
    }

    public override void _Ready()
    {
        base._Ready();
        foreach (var item in Data.ItemPresets)
        {
            if (item.ItemScene == null)
                continue;
            spawner.AddSpawnableScene(item.ItemScene.ResourcePath);
        }
        foreach (var prop in Data.Props)
        {
            propSpawner.AddSpawnableScene(prop.ResourcePath);
        }
    }

    public void Reset()
    {
        savedProps.props.Clear();
    }

    public void RespawnPropIfNeeded(RigidbodySync prop)
    {
        if (!IsInstanceValid(RoundManager.Instance))
            return;
        // if (IsInstanceValid(MenuManager.Instance) && MenuManager.Instance.state != MenuManager.GameState.Game)
            // return;
        if (!PropSpawnNode.IsAncestorOf(prop))
        {
            string path = prop.SceneFilePath;
            if (string.IsNullOrEmpty(path))
            {
                path = prop.Owner.SceneFilePath;
            }
            if (string.IsNullOrEmpty(path))
                return;
            bool found = false;
            for (int i = 0; i < propSpawner.GetSpawnableSceneCount(); i++)
            {
                if (propSpawner.GetSpawnableScene(i) == path)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Log.PrintS("Prop not spawnable:", path);
                return;
            }
            if (IsMultiplayerAuthority())
            {
                savedProps.props.Add(new PropInfo(prop));
            }
            prop.QueueFree();
            prop.ProcessMode = ProcessModeEnum.Disabled;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        {
            ulong time = Time.GetTicksMsec();
            if (time - lastPropCullTickTime >= 1000d / propCullTickrate)
            {
                Tick();
                lastPropCullTickTime = time;
            }
        }
    }

    public void Tick()
    {
        if (IsMultiplayerAuthority())
        {
            IPlayerList playerList = IPlayerList.List(this);
            var list = playerList.PlayerList.Where(x => x.Role != null && x.Role.team != TeamID.Dead).ToList();
            ulong timestamp = Time.GetTicksMsec();
            for (int j = 0; j < list.Count; j++)
            {
                for (int i = 0; i < savedProps.props.Count; i++)
                {
                    float distSqr = savedProps.props[i].transform.Origin.DistanceSquaredTo(list[j].PlayerPosition);
                    if (distSqr <= propRenderDistance * propRenderDistance)
                    {
                        LoadProp(savedProps.props[i]);
                        savedProps.props.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
            var children = PropSpawnNode.GetChildren();
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is RigidbodySync prop)
                {
                    bool found = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        float distSqr = prop.GlobalPosition.DistanceSquaredTo(list[j].PlayerPosition);
                        if (distSqr <= propRenderDistance * propRenderDistance)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        SaveProp(prop);
                        continue;
                    }
                }
            }
            foreach (var kvp in Items)
            {
                if (kvp.Value.model is WorldItem item)
                {
                    bool found = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        float distSqr = item.GlobalPosition.DistanceSquaredTo(list[j].PlayerPosition);
                        if (distSqr <= itemRenderDistance * itemRenderDistance)
                        {
                            found = true;
                            break;
                        }
                    }
                    item.ShouldFreeze = !found;
                }
            }
        }
    }

    public void SaveProp(RigidbodySync prop)
    {
        if (string.IsNullOrEmpty(prop.SceneFilePath))
        {
            return;
        }
        //Log.Print($"Saved prop {prop.SceneFilePath}");
        savedProps.props.Add(new PropInfo(prop));
        prop.QueueFree();
        prop.ProcessMode = ProcessModeEnum.Disabled;
    }

    public Node3D LoadProp(PropInfo info)
    {
        var prop = GD.Load<PackedScene>(info.scenePath).Instantiate<Node3D>();
        prop.Transform = info.transform;
        foreach (var kvp in info.data)
        {
            prop.Set(kvp.Key, GD.BytesToVar(kvp.Value));
        }
        PropSpawnNode.AddChild(prop, true);
        //Log.Print($"Loaded prop {prop.SceneFilePath}");
        return prop;
    }

    public ItemObject UpgradeItem(WorldItem item, SCP914Knob.KnobSetting setting, out bool destroy)
    {
        destroy = false;
        if (!IsMultiplayerAuthority())
            return null;
        List<ItemPreset> outputs = new List<ItemPreset>();
        foreach (var upgrade in Data.ItemUpgrades)
        {
            if (upgrade.setting == setting && upgrade.IsValidFor(item.Item.Preset, out ItemPreset output))
            {
                outputs.Add(output);
            }
        }
        if (outputs.Count == 0)
        {
            return null;
        }
        var selected = outputs[(int)(GD.Randi() % outputs.Count)];
        if (IsInstanceValid(selected))
            return selected.SpawnNew();
        destroy = true;
        return null;
    }
}

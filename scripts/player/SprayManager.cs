using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class SprayManager : Node3D
{
    public static SprayManager Instance;

    public Dictionary<string, ImageTexture> SprayTextureCache = new Dictionary<string, ImageTexture>();
    public Dictionary<string, byte[]> SprayWebpCache = new Dictionary<string, byte[]>();

    [Export]
    public PlayerSpawner players;
    // [Export]
    public string[] imagePaths = Array.Empty<string>();
    [Export]
    public PackedScene sprayScene;
    [Export(PropertyHint.Layers3DPhysics)]
    public uint attackLayer;

    public Dictionary<int, double> peerSprayCooldowns = new Dictionary<int, double>();
    public Dictionary<int, PlayerSpray> spawnedSprays = new Dictionary<int, PlayerSpray>();

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        players.PlayerJoined += OnPlayerJoined;
    }

    public void LoadSprays()
    {
        imagePaths = Settings.Server.Sprays ?? Array.Empty<string>();
        if (!Multiplayer.IsServer())
            return;
        foreach (var path in imagePaths)
        {
            Log.Print($"Loading spray image: {path}");
            Image img = Image.LoadFromFile(path);
            img.Resize(256, 256);
            byte[] data = img.SaveWebpToBuffer(true);
            Log.Print($"Loaded spray image: {path}, size={data.Length}");
            ImageTexture tex = ImageTexture.CreateFromImage(img);
            SprayWebpCache.TryAdd(path, data);
            SprayTextureCache.TryAdd(path, tex);
            // byte[] data = FileAccess.GetFileAsBytes(path);
            // Rpc(MethodName.RpcSendSpray, path, data);
        }
    }

    public override void _Process(double delta)
    {
        foreach (var key in peerSprayCooldowns.Keys)
        {
            if (peerSprayCooldowns[key] > 0)
                peerSprayCooldowns[key] -= delta;
        }
    }

    private async void OnPlayerJoined(NetworkPlayer player)
    {
        if (!Multiplayer.IsServer())
            return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        foreach (var kvp in SprayWebpCache)
        {
            // GD.PrintS("Rpc", player.GetMultiplayerAuthority(), kvp.Key, kvp.Value.Length);
            RpcId(player.GetMultiplayerAuthority(), MethodName.RpcSendSpray, kvp.Key, kvp.Value);
            // GD.PrintErr(err);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferChannel = 1)]
    public void RpcSendSpray(string key, byte[] data)
    {
        // GD.PrintS("RpcSendSpray", key, data.Length);
        Image img = new Image();
        img.LoadWebpFromBuffer(data);
        ImageTexture tex = ImageTexture.CreateFromImage(img);
        SprayWebpCache.TryAdd(key, data);
        SprayTextureCache.TryAdd(key, tex);
    }

    public string GetRandomSpray()
    {
        var arr = SprayTextureCache.Keys.ToArray();
        if (arr.Length == 0)
            return string.Empty;
        return arr[GD.Randi() % arr.Length];
    }

    public void Spray(Vector3 pos, Vector3 rot, string key)
    {
        RpcId(GetMultiplayerAuthority(), MethodName.RpcRequestAddSpray, pos, rot, key);
    }

    public void SprayOn(Vector3 from, Vector3 dir, string key)
    {
        PhysicsRayQueryParameters3D args = PhysicsRayQueryParameters3D.Create(from, from + dir, attackLayer);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(args);
        if (result.Count != 0)
        {
            Vector3 position = result["position"].AsVector3();
            Vector3 normal = result["normal"].AsVector3();
            Vector3 rot = Basis.LookingAt(normal, normal.IsEqualApprox(Vector3.Up) ? normal.Cross(dir.Normalized()) : Vector3.Up).GetEuler();
            RpcId(GetMultiplayerAuthority(), MethodName.RpcRequestAddSpray, position, rot, key);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcRequestAddSpray(Vector3 position, Vector3 rot, string key)
    {
        if (!IsMultiplayerAuthority() || string.IsNullOrEmpty(key))
            return;
        int sender = Multiplayer.GetRemoteSenderId();
        if (!peerSprayCooldowns.ContainsKey(sender))
        {
            peerSprayCooldowns.Add(sender, 0);
        }
        if (peerSprayCooldowns[sender] <= 0)
        {
            peerSprayCooldowns[sender] = 10d;
            Rpc(MethodName.RpcAddSpray, position, rot, key, sender);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcAddSpray(Vector3 position, Vector3 rot, string key, int sender)
    {
        if (string.IsNullOrEmpty(key))
            return;
        if (spawnedSprays.TryGetValue(sender, out PlayerSpray lastSpray) && IsInstanceValid(lastSpray))
        {
            lastSpray.QueueFree();
        }
        PhysicsRayQueryParameters3D args = new PhysicsRayQueryParameters3D()
        {
            CollideWithBodies = true,
            CollideWithAreas = false,
            From = position,
            To = position + (Quaternion.FromEuler(rot) * Vector3.Back * 0.1f),
            CollisionMask = ItemManager.Instance.attackLayer,
            HitFromInside = true,
        };
        var arr = GetWorld3D().DirectSpaceState.IntersectRay(args);
        if (arr.Count != 0)
        {
            var obj = arr["collider"].As<Node3D>();
            PlayerSpray spray = sprayScene.Instantiate<PlayerSpray>();
            spray.url = key;
            obj.AddChild(spray, true);
            spray.GlobalPosition = position;
            spray.GlobalRotation = rot;
            spray.GlobalScale(Vector3.One / spray.GlobalBasis.Scale);
            spawnedSprays[sender] = spray;
        }
        else
        {
            PlayerSpray spray = sprayScene.Instantiate<PlayerSpray>();
            spray.Position = position;
            spray.Rotation = rot;
            spray.url = key;
            AddChild(spray, true);
            spawnedSprays[sender] = spray;
        }
    }
}
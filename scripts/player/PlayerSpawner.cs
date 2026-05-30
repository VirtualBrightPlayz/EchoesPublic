using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class PlayerSpawner : MultiplayerSpawner, IPlayerList
{
    public List<NetworkPlayer> Players { get; private set; } = new List<NetworkPlayer>();
    public IReadOnlyList<NetworkPlayer> PlayerList => Players;
    public Action<NetworkPlayer> PlayerJoined { get; set; } = (_) => { };
    public Action<NetworkPlayer> PlayerLeft { get; set; } = (_) => { };
    [Export]
    public PackedScene scene;

    private int _nextPlayerId = 1;

    public override void _EnterTree()
    {
        base._EnterTree();
        SpawnFunction = Callable.From<Variant, Node>(SpawnPlayer);
        Spawned += OnSpawned;
        NetworkManager.Instance.SV_Connected += Connected;
        NetworkManager.Instance.SV_Disconnected += Disconnected;
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        foreach (var plr in Players)
        {
            plr.QueueFree();
            PlayerLeft?.Invoke(plr);
        }
        Players.Clear();
        Spawned -= OnSpawned;
        NetworkManager.Instance.SV_Connected -= Connected;
        NetworkManager.Instance.SV_Disconnected -= Disconnected;
    }

    public override void _Process(double delta)
    {
        Players.RemoveAll(x => !IsInstanceValid(x));
    }

    public override void _PhysicsProcess(double delta)
    {
        Players.RemoveAll(x => !IsInstanceValid(x));
    }

    private void OnSpawned(Node node)
    {
        if (node is NetworkPlayer plr)
            PlayerJoined?.Invoke(plr);
    }

    private Node SpawnPlayer(Variant variant)
    {
        var scn = scene;
        NetworkPlayer plr = scn.Instantiate<NetworkPlayer>();
        plr.AuthorityId = variant.AsInt32();
        plr.PlayerId = _nextPlayerId++;
        plr.Name = plr.AuthorityId.ToString();
        Players.Add(plr);
        return plr;
    }

    private async void JoinedLate(NetworkPlayer plr)
    {
        await Task.Yield();
        await Task.Delay(100);
        PlayerJoined?.Invoke(plr);
    }

    private void Connected(int id)
    {
        if (!IsMultiplayerAuthority())
            return;
        Node n = Spawn(id);
        if (n is NetworkPlayer plr)
        {
            PlayerJoined?.Invoke(plr);
        }
    }

    private void Disconnected(int id)
    {
        foreach (var plr in Players)
        {
            if (plr.GetMultiplayerAuthority() == id)
            {
                // This MUST run before the player is removed from the list.
                PlayerLeft?.Invoke(plr);
                plr.QueueFree();
                plr.OnDisconnected();
            }
        }
        Players.RemoveAll(x => !IsInstanceValid(x) || x.IsQueuedForDeletion());
    }
}

public interface IPlayerList
{
    IReadOnlyList<NetworkPlayer> PlayerList { get; }
    Action<NetworkPlayer> PlayerJoined { get; set; }
    /// <summary>
    /// Invoked when the player leaves the game.
    /// This will always run before the player list is updated, so this player
    /// will still be in the player list.
    /// </summary>
    Action<NetworkPlayer> PlayerLeft { get; set; }
    public static StringName GroupName = "players";
    public static IPlayerList List(Node node) => node.GetTree().GetFirstNodeInGroup(GroupName) as IPlayerList;
}
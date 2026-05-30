using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class HazardMapLogic : Node
{
    public RoundManager Round => RoundManager.Instance;

    public enum LogicState : int
    {
        PreRound = 0,
        InCombat = 1,
        PostRound = 2,
    }

    [Signal]
    public delegate void GateOpenedEventHandler();

    [Export] public LogicState state = LogicState.PreRound;
    [Export] public double RespawnTime;
    [Export] public int RespawnWaveCount = 25;
    [Export] public float RespawnDistance = 40f;
    [Export] public Godot.Collections.Array<TeamID> SpawnQueue = new Godot.Collections.Array<TeamID>();
    [Export] public PackedScene npcScene;
    [Export] public StringName npcSpawnGroup;
    [Export(PropertyHint.Layers3DPhysics)] public uint visLayers;

    public double respawnTimerSCP;

    public Dictionary<NetworkPlayer, TeamID> playerToTeam = new Dictionary<NetworkPlayer, TeamID>();
    public List<NetworkPlayer> reachedEndPlayers = new List<NetworkPlayer>();

    public override void _EnterTree()
    {
        Round.EventOnRoundStart += StartRound;
        Round.EventOnPlayerJoined += PlayerJoined;
    }

    public override void _ExitTree()
    {
        Round.EventOnRoundStart -= StartRound;
        Round.EventOnPlayerJoined -= PlayerJoined;
    }

    public override void _Process(double delta)
    {
        if (!Round.IsMultiplayerAuthority())
            return;

        if (Round.state == RoundManager.RoundState.InGame)
        {
            switch (state)
            {
                case LogicState.PreRound:
                {
                    if (!HasAlivePlayers(TeamID.NTF))
                    {
                        Round.EndRound("SCPs win");
                    }
                    if (HasDeadPlayers(TeamID.SCP))
                    {
                        RespawnWave(TeamID.SCP);
                    }
                    break;
                }
                case LogicState.InCombat:
                {
                    if (HasDeadPlayers(TeamID.SCP))
                    {
                        respawnTimerSCP -= delta;
                        if (respawnTimerSCP <= 0d)
                        {
                            respawnTimerSCP = RespawnTime;
                            RespawnWaveSCPs();
                        }
                    }

                    if (!Round.Paused)
                    {
                        if (!HasPlayersMissingFromList(reachedEndPlayers, TeamID.NTF))
                        {
                            Round.EndRound("Humans win");
                        }
                        else if (!HasAlivePlayers(TeamID.NTF))
                        {
                            Round.EndRound("SCPs win");
                        }
                    }
                    break;
                }
                case LogicState.PostRound:
                    break;
            }
        }
    }

    private void PlayerJoined(NetworkPlayer player)
    {
        if (!Round.IsMultiplayerAuthority())
            return;
        // TODO: maybe not
        playerToTeam[player] = TeamID.SCP;
    }

    private void StartRound()
    {
        if (!Round.IsMultiplayerAuthority())
            return;

        playerToTeam.Clear();
        reachedEndPlayers.Clear();
        state = LogicState.PreRound;

        var plrs = Round.players.PlayerList.OrderBy(x => GD.Randi());
        int i = 0;
        List<PlayerRole> usedSCPs = new List<PlayerRole>();
        foreach (var plr in plrs)
        {
            if (SpawnQueue.Count == 0)
            {
                Log.PrintWarn("SpawnQueue is empty, spawning fallback role");
                plr.SV_Spawn(RoleID.Spectator);
                continue;
            }

            var team = SpawnQueue[i % SpawnQueue.Count];
            var roles = Round.Data.Roles.Where(x => x.team == team && x.spawnStage == 0 && !usedSCPs.Contains(x)).ToArray();
            if (roles.Length == 0)
            {
                plr.SV_Spawn(RoleID.Spectator);
                continue;
            }
            var role = roles[GD.Randi() % roles.Length];
            if (team == TeamID.SCP)
            {
                role = Round.Data.Roles[(int)RoleID.Spectator];
                usedSCPs.Add(role);
            }
            playerToTeam[plr] = team;
            plr.SV_Spawn(role.ResourceName);
            i++;
        }

        respawnTimerSCP = RespawnTime;
        RespawnWaveSCPs();
    }

    public PlayerSpawnpoint GetSpawn(PlayerRole role)
    {
        List<PlayerSpawnpoint> spawns = new List<PlayerSpawnpoint>();
        foreach (var sp in GetTree().GetNodesInGroup("spawn"))
        {
            if (sp is PlayerSpawnpoint spawn && spawn.role == role)
            {
                spawns.Add(spawn);
            }
        }
        if (spawns.Count > 0)
        {
            long idx = GD.Randi() % spawns.Count;
            return spawns[(int)idx];
        }
        return null;
    }

    public RoleID GetRandRoleFromTeam(TeamID team)
    {
        if (team == TeamID.SCP)
        {
            return RoleID.SCP049_2;
        }
        var arr = Round.Data.Roles.Where(x => x.team == team && x.spawnStage == 0).ToArray();
        if (arr.Length == 0)
        {
            return RoleID.Spectator;
        }
        return (RoleID)Array.IndexOf(Round.Data.Roles, arr[(int)(GD.Randi() % arr.Length)]);
    }

    public bool HasDeadPlayers(TeamID team)
    {
        for (int i = 0; i < Round.players.Players.Count; i++)
        {
            NetworkPlayer player = Round.players.Players[i];
            if (player.Role.team == TeamID.Dead && playerToTeam.TryGetValue(player, out TeamID id) && id == team)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasAlivePlayers(TeamID team)
    {
        for (int i = 0; i < Round.players.Players.Count; i++)
        {
            NetworkPlayer player = Round.players.Players[i];
            if (player.Role.team == team && playerToTeam.TryGetValue(player, out TeamID id) && id == team)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasPlayersMissingFromList(List<NetworkPlayer> players, TeamID team)
    {
        for (int i = 0; i < Round.players.Players.Count; i++)
        {
            NetworkPlayer player = Round.players.Players[i];
            if (playerToTeam.TryGetValue(player, out TeamID id) && id == team && !players.Contains(player))
            {
                return true;
            }
        }
        return false;
    }

    public bool IsVisibleFor(NetworkPlayer player, Vector3 pt)
    {
        if (player.ActiveController.Camera.IsPositionInFrustum(pt))
        {
            var rayQuery = PhysicsRayQueryParameters3D.Create(player.ActiveController.Camera.GlobalPosition, pt, visLayers);
            var result = GetViewport().FindWorld3D().DirectSpaceState.IntersectRay(rayQuery);
            if (result.Count > 0)
            {
                return result["position"].AsVector3().DistanceSquaredTo(pt) <= 0.01f * 0.01f;
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    public List<NetworkPlayer> GetPlayers(TeamID team)
    {
        List<NetworkPlayer> players = new List<NetworkPlayer>();
        for (int i = 0; i < Round.players.Players.Count; i++)
        {
            NetworkPlayer player = Round.players.Players[i];
            if (player.Role.team == team && playerToTeam.TryGetValue(player, out TeamID id) && id == team)
            {
                players.Add(player);
            }
        }
        return players;
    }

    public void RespawnWave(TeamID team)
    {
        if (!Round.IsMultiplayerAuthority())
            return;
        for (int i = 0; i < Round.players.Players.Count; i++)
        {
            NetworkPlayer player = Round.players.Players[i];
            if (player.Role.team == TeamID.Dead && playerToTeam.TryGetValue(player, out TeamID id) && id == team)
            {
                player.SV_Spawn(GetRandRoleFromTeam(team), GetSpawnNpc());
            }
        }
    }

    public Vector3 GetSpawnNpc()
    {
        if (!Round.IsMultiplayerAuthority())
            return Vector3.Zero;
        List<Node3D> spawns = new List<Node3D>();
        foreach (var sp in GetTree().GetNodesInGroup(npcSpawnGroup))
        {
            if (sp is Node3D spawn)
            {
                spawns.Add(spawn);
            }
        }
        Node3D node = spawns[(int)(GD.Randi() % spawns.Count)];
        return node.GlobalPosition;
    }
    
    public void SpawnNpcsAround(Vector3 position, int count)
    {
        if (!Round.IsMultiplayerAuthority())
            return;
        for (int i = 0; i < count; i++)
        {
            float x = (float)GD.RandRange(-1f, 1f);
            float z = (float)GD.RandRange(-1f, 1f);
            Vector3 dir = new Vector3(x, 0f, z).Normalized() * RespawnDistance;
            Vector3 pt = NavigationServer3D.MapGetClosestPoint(GetViewport().FindWorld3D().NavigationMap, position + dir);
            var node = npcScene.Instantiate<Node3D>();
            node.Position = pt;
            ItemManager.Instance.SpawnNode.AddChild(node, true);
        }
    }

    public void TrySpawnNpc(Vector3 point)
    {
        for (int i = 0; i < Round.players.Players.Count; i++)
        {
            NetworkPlayer player = Round.players.Players[i];
            if (player.Role.team == TeamID.NTF && IsVisibleFor(player, point))
            {
                // point is visible to a NTF
                return;
            }
        }
        var node = npcScene.Instantiate<Node3D>();
        node.Position = point;
        ItemManager.Instance.SpawnNode.AddChild(node, true);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcGateOpened()
    {
        EmitSignalGateOpened();
    }

    #region APIs

    public void RespawnWaveSCPs()
    {
        RespawnWave(TeamID.SCP);
        foreach (var sp in GetTree().GetNodesInGroup(npcSpawnGroup))
        {
            if (sp is Node3D spawn)
            {
                for (int i = 0; i < RespawnWaveCount; i++)
                {
                    /*
                    float t = (float)i / 2f * Mathf.Tau;
                    float x = Mathf.Cos(t);
                    float z = Mathf.Sin(t);
                    Vector3 v = new Vector3(x, 0f, z) * 2f;
                    */
                    float x = (float)GD.RandRange(-1f, 1f);
                    float z = (float)GD.RandRange(-1f, 1f);
                    Vector3 v = new Vector3(x, 0f, z).Normalized() * RespawnDistance;
                    CallDeferred(MethodName.TrySpawnNpc, spawn.GlobalPosition + v);
                }
            }
        }
    }

    public void ReachedEndPath(NetworkPlayer player)
    {
        reachedEndPlayers.Add(player);
        player.SV_Spawn(RoleID.Spectator, player.PlayerPosition);
    }

    public void NTFOpenGate(NetworkPlayer player)
    {
        if (Round.IsMultiplayerAuthority() && player.Role.team == TeamID.NTF)
        {
            state = LogicState.InCombat;
            Rpc(MethodName.RpcGateOpened);
            AlertHorde(player);
        }
    }

    public void AlertHorde(NetworkPlayer player)
    {
        if (Round.IsMultiplayerAuthority() && player.Role.team == TeamID.NTF)
        {
            foreach (var item in ItemManager.Instance.SpawnNode.GetChildren())
            {
                if (item is NpcZombie zombie)
                {
                    // zombie.ForceSetTarget(player.controller);
                }
            }
        }
    }

    #endregion
}

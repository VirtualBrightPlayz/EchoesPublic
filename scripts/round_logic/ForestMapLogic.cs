using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ForestMapLogic : Node
{
    public RoundManager Round => RoundManager.Instance;

    [Export] public double RespawnTime;
    [Export] public float MinRespawnDistance = 40f;
    [Export] public float MaxRespawnDistance = 40f;
    [Export] public Godot.Collections.Array<TeamID> SpawnQueue = new Godot.Collections.Array<TeamID>();
    [Export] public PlayerRole[] Roles = Array.Empty<PlayerRole>();
    [Export] public PackedScene npcScene;
    [Export] public Path3D[] mainPaths = Array.Empty<Path3D>();
    [Export] public Curve pathFogCurve;
    [Export] public AmbientProfile ambient;
    [ExportGroup("NPC Settings")]
    [Export] public int MaxSpawnedNpcs = 30;
    [Export] public int MinSpawnedNpcs = 30;
    [Export] public int MaxAttackingNpcs = 15;
    [Export] public float NpcSpawnPointDistance = 3f;
    [Export] public int RespawnWaveNpcCount = 25;
    [Export] public double HordeTimeInterval = 10d;

    public double respawnTimerSCP;
    public double hordeTimer;

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
            var npcs = GetForestMonsterNpcs();

            int foundMax = 0;
            for (int i = 0; i < npcs.Count; i++)
            {
                if (foundMax >= MaxAttackingNpcs)
                {
                    npcs[i].canAttack = false;
                }
                if (npcs[i].canAttack)
                {
                    foundMax++;
                }
                else if (foundMax < MaxAttackingNpcs)
                {
                    npcs[i].canAttack = true;
                    foundMax++;
                }
            }

            if (npcs.Count <= MinSpawnedNpcs)
            {
                hordeTimer += delta;
                if (hordeTimer >= HordeTimeInterval)
                {
                    hordeTimer = 0d;
                    CullFarNpcs(npcs);
                    RespawnWaveSCPs();
                }
            }

            if (HasDeadPlayers(TeamID.SCP))
            {
                respawnTimerSCP -= delta;
                if (respawnTimerSCP <= 0d)
                {
                    respawnTimerSCP = RespawnTime;
                    RespawnWave(TeamID.SCP);
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
        }
    }

    public void ComputeAmbientProfile()
    {
        if (IsInstanceValid(NetworkPlayer.LocalInstance) && IsInstanceValid(AmbientCalculator.Instance) && NetworkPlayer.LocalInstance.Role.team == TeamID.NTF)
        {
            // if (!AmbientCalculator.Instance.Profiles.Contains(ambient))
            //     AmbientCalculator.Instance.Profiles.Add(ambient);
            Vector3 pos = NetworkPlayer.LocalInstance.PlayerPosition;
            for (int i = 0; i < mainPaths.Length; i++)
            {
                Vector3 localPos = mainPaths[i].ToLocal(pos);
                Vector3 localPt = mainPaths[i].Curve.GetClosestPoint(localPos);
                Vector3 pt = mainPaths[i].ToGlobal(localPt);
                float dist = pos.DistanceTo(pt);
                AmbientCalculator.Instance.ComputedProfiles.Add((ambient, Mathf.Clamp(pathFogCurve.Sample(dist), 0f, 1f)));
                /*
                if (dist >= pathFogCurve.MaxDomain)
                {
                    Vector3 eulerRot = NetworkPlayer.LocalInstance.PlayerRotation;
                    eulerRot.Y = Basis.LookingAt(NetworkPlayer.LocalInstance.PlayerPosition.DirectionTo(pt)).GetEuler().Y;
                    NetworkPlayer.LocalInstance.ApplyRotation(eulerRot);
                }
                */
                // DebugDrawManager.Instance.DrawDebugString(pt, $"{dist:0.0}", Colors.OrangeRed, 0.5d);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // if (Round.state == RoundManager.RoundState.InGame)
        {
        }
    }

    private void PlayerJoined(NetworkPlayer player)
    {
        if (!Round.IsMultiplayerAuthority())
            return;
    }

    private void StartRound()
    {
        if (!Round.IsMultiplayerAuthority())
            return;

        playerToTeam.Clear();
        reachedEndPlayers.Clear();
        hordeTimer = 0d;

        var plrs = Round.players.PlayerList.OrderBy(x => GD.Randi());
        int i = 0;
        foreach (var plr in plrs)
        {
            if (SpawnQueue.Count == 0)
            {
                Log.PrintWarn("SpawnQueue is empty, spawning fallback role");
                plr.SV_Spawn(RoleID.Spectator);
                continue;
            }

            var team = SpawnQueue[i % SpawnQueue.Count];
            var roles = Roles.Where(x => x.team == team && x.spawnStage == 1).ToArray();
            if (roles.Length == 0)
            {
                plr.SV_Spawn(RoleID.Spectator);
                continue;
            }
            var role = roles[GD.Randi() % roles.Length];
            if (team == TeamID.SCP)
            {
                role = Round.Data.Roles[(int)RoleID.Spectator];
            }
            playerToTeam[plr] = team;
            plr.SV_Spawn(role.ResourceName);
            i++;
        }

        respawnTimerSCP = RespawnTime;
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
        /*
        if (team == TeamID.SCP)
        {
            if (GD.Randi() % 2 == 0)
            {
                return RoleID.ForestMonster;
            }
            else
            {
                return RoleID.BushMonster;
            }
        }
        */
        var arr = Roles.Where(x => x.team == team && x.spawnStage == 1).ToArray();
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

    public void RespawnWaveSCPs()
    {
        RespawnWave(TeamID.SCP);
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
        if (team == TeamID.SCP)
        {
            // var alive = Round.players.Players.Where(x => x.Role.team == TeamID.NTF).ToArray();
            // var position = alive[GD.Randi() % alive.Length].PlayerPosition;
            var npcs = GetForestMonsterNpcs();
            CullFarNpcs(npcs);
            SpawnNpcsAround(GetSpawnNpc(), RespawnWaveNpcCount, true);
        }
    }

    public Vector3 GetSpawnNpc()
    {
        if (!Round.IsMultiplayerAuthority())
            return Vector3.Zero;
        List<Node3D> spawns = new List<Node3D>();
        foreach (var sp in GetTree().GetNodesInGroup("npc_spawn"))
        {
            if (sp is Node3D spawn)
            {
                bool found = false;
                for (int i = 0; i < Round.players.Players.Count; i++)
                {
                    NetworkPlayer player = Round.players.Players[i];
                    if (player.PlayerPosition.DistanceSquaredTo(spawn.GlobalPosition) < MaxRespawnDistance * MaxRespawnDistance)
                    {
                        found = true;
                        break;
                    }
                    else if (player.PlayerPosition.DistanceSquaredTo(spawn.GlobalPosition) > MinRespawnDistance * MinRespawnDistance)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    spawns.Add(spawn);
                }
            }
        }
        if (spawns.Count > 0)
        {
            long idx = GD.Randi() % spawns.Count;
            return spawns[(int)idx].GlobalPosition;
        }
        Log.PrintWarn("GetSpawnNpc failed to find a spawnpoint.");
        // return Vector3.Zero;
        float x = (float)GD.RandRange(-1f, 1f);
        float z = (float)GD.RandRange(-1f, 1f);
        Vector3 dir = new Vector3(x, 0f, z).Normalized() * MinRespawnDistance;
        var alive = Round.players.Players.Where(x => x.Role.team == TeamID.NTF).ToArray();
        var position = alive[GD.Randi() % alive.Length].PlayerPosition;
        Vector3 pt = NavigationServer3D.MapGetClosestPoint(GetViewport().FindWorld3D().NavigationMap, position + dir);
        return pt;
    }

    public List<NpcForestMonster> GetForestMonsterNpcs()
    {
        List<NpcForestMonster> npcs = new List<NpcForestMonster>();
        foreach (var item in ItemManager.Instance.SpawnNode.GetChildren())
        {
            if (item is NpcForestMonster npc)
                npcs.Add(npc);
        }
        return npcs;
    }

    public void CullFarNpcs(List<NpcForestMonster> npcs)
    {
        foreach (var item in npcs)
        {
            if (item.state == NpcForestMonster.NpcState.Idle)
            {
                item.QueueFree();
            }
        }
    }

    public void SpawnNpcsAround(Vector3 position, int count, bool horde)
    {
        if (!Round.IsMultiplayerAuthority())
            return;
        var npcs = GetForestMonsterNpcs();
        for (int i = 0; i < count; i++)
        {
            if (npcs.Count + i >= MaxSpawnedNpcs)
                break;
            float x = (float)GD.RandRange(-1f, 1f);
            float z = (float)GD.RandRange(-1f, 1f);
            Vector3 dir = new Vector3(x, 0f, z).Normalized() * NpcSpawnPointDistance;
            Vector3 pt = NavigationServer3D.MapGetClosestPoint(GetViewport().FindWorld3D().NavigationMap, position + dir);
            var node = npcScene.Instantiate<NpcForestMonster>();
            node.isHorde = horde;
            node.Position = pt;
            node.Rotation = Vector3.Up * (float)GD.RandRange(-Mathf.Tau, Mathf.Tau);
            ItemManager.Instance.SpawnNode.AddChild(node, true);
        }
    }

    public void ReachedEndPath(NetworkPlayer player)
    {
        reachedEndPlayers.Add(player);
        player.SV_Spawn(RoleID.Spectator, player.PlayerPosition);
    }
}

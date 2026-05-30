using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class RoundLogic : Node
{
    public RoundManager Round => GetParent<RoundManager>();

    public RoundConfig Config = new RoundConfig();

    public List<TeamID> SpawnQueue = new();

    public double respawnTimer;
    public double forceNukeTimer;
    public int respawnsRemaining;

    [Signal]
    public delegate void TryRespawnNtfEventHandler();

    [Export]
    public GameSound respawnWaveSoundRRT;
    [Export]
    public GameSound respawnWaveSoundNTF;
    [Export]
    public GameSound respawnWaveSoundCI;
    [Export]
    public ObjectiveAsset[] globalStartObjectives = Array.Empty<ObjectiveAsset>();
    [Export]
    public ObjectiveAsset radioForBackupObjective;

    public override void _EnterTree()
    {
        Round.EventOnRoundStart += StartRound;
        // Round.EventOnPlayerJoined += PlayerJoined;
        if (IsInstanceValid(ObjectivePointManager.Instance))
        {
            ObjectivePointManager.Instance.OnObjectiveDone += _OnObjectiveDone;
        }
    }

    public override void _ExitTree()
    {
        Round.EventOnRoundStart -= StartRound;
        // Round.EventOnPlayerJoined -= PlayerJoined;
        if (IsInstanceValid(ObjectivePointManager.Instance))
        {
            ObjectivePointManager.Instance.OnObjectiveDone -= _OnObjectiveDone;
        }
    }

    public override void _Process(double delta)
    {
        if (!Round.IsMultiplayerAuthority())
            return;

        if (Round.state == RoundManager.RoundState.InGame)
        {
            if (respawnsRemaining != 0)
            {
                respawnTimer -= delta;
                if (respawnTimer <= 0d)
                {
                    respawnTimer = Config.RespawnTime;
                    TryRespawnWave(false);
                }
            }

            if (forceNukeTimer > 0d)
            {
                forceNukeTimer -= delta;
                if (forceNukeTimer <= 0d)
                {
                    Round.canStopNuke = false;
                    Round.StartNuke();
                }
            }

            if (!Round.Paused)
            {
                var list = Round.players.Players;
                int classd = list.Count(x => x.Role.team == TeamID.ClassD);
                int ntf = list.Count(x => x.Role.team == TeamID.NTF);
                int scp = list.Count(x => x.Role.team == TeamID.SCP);
                if (list.Count > 1)
                {
                    if (classd == 0 && ntf == 0)
                    {
                        Round.EndRound("SCPs win");
                    }
                    else if (ntf == 0 && scp == 0 && !Config.NoCi)
                    {
                        Round.EndRound("Insurgents win");
                    }
                    else if (scp == 0 && classd == 0)
                    {
                        Round.EndRound("Foundation win");
                    }
                }
            }
        }
    }

    private void _OnObjectiveDone(string key)
    {
        if (IsInstanceValid(radioForBackupObjective) && key == radioForBackupObjective.key)
        {
            respawnTimer += Config.BackupCalledTimeAdded;
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

        if (IsInstanceValid(ObjectivePointManager.Instance))
        {
            ObjectivePointManager.Instance.completeObjectives.Clear();
            ObjectivePointManager.Instance.activeObjectives.Clear();
            foreach (var obj in globalStartObjectives)
            {
                ObjectivePointManager.Instance.activeObjectives.Add(obj.key);
            }
        }

        Round.canStopNuke = true;

        SpawnQueue = Config.SpawnQueue.ToList();
        bool repeating = Config.SpawnQueueMode == RoundConfig.QueueMode.RoundRobinRepeating || Config.SpawnQueueMode == RoundConfig.QueueMode.RandomRepeating;
        var plrs = Round.players.PlayerList.OrderBy(x => GD.Randi());
        int i = 0;
        List<PlayerRole> usedSCPs = new List<PlayerRole>();
        foreach (var plr in plrs)
        {
            if (SpawnQueue.Count == 0)
            {
                Log.PrintWarn("SpawnQueue is empty, spawning fallback role");
                plr.SV_Spawn(Config.FallbackRole);
                continue;
            }
            if (i >= SpawnQueue.Count && !repeating)
            {
                plr.SV_Spawn(Config.FallbackRole);
                continue;
            }

            var team = SpawnQueue[i % SpawnQueue.Count];
            var roles = Round.Data.RoleLookup.Select(x => x.Value).Where(x => x.team == team && x.spawnStage == 0 && !usedSCPs.Contains(x)).ToArray();
            if (roles.Length == 0)
            {
                plr.SV_Spawn(Config.FallbackRole);
                continue;
            }
            var role = roles[GD.Randi() % roles.Length];
            if (team == TeamID.SCP)
            {
                usedSCPs.Add(role);
            }
            plr.SV_Spawn(role.ResourceName);
            i++;
        }

        if (IsInstanceValid(Round.decon))
        {
            Round.decon.StartDeconSequence(Config.LczDeconTime);
        }
        respawnTimer = Config.RespawnTime;
        respawnsRemaining = Config.RespawnWaves;
        forceNukeTimer = Config.ForceNukeTime;
    }

    public void TryRespawnWave(bool force)
    {
        if (!Round.IsMultiplayerAuthority())
            return;
        bool canSpawn = false;
        foreach (var plr in Round.players.Players)
        {
            if (plr.Role.team == TeamID.Dead)
            {
                canSpawn = true;
                break;
            }
        }
        if (!canSpawn && !force)
            return;
        bool isCi = false;
        if (!Config.NoCi)
            isCi = GD.Randf() <= Config.CiRespawnChance / 100f;
            // isCi = GD.Randi() % Mathf.Max(1, Config.CiRespawnChance) == 0;
        if (isCi)
        {
            RespawnWave(TeamID.ClassD);
        }
        else
        {
            EmitSignalTryRespawnNtf();
        }
    }

    public PlayerSpawnpoint GetSpawn(PlayerRole role)
    {
        List<PlayerSpawnpoint> spawns = new List<PlayerSpawnpoint>();
        foreach (var sp in GetTree().GetNodesInGroup("spawn"))
        {
            if (sp is PlayerSpawnpoint spawn && spawn.CanSpawn(role))
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

    public PlayerSpawnpoint GetSpawn(TeamID team)
    {
        List<PlayerSpawnpoint> spawns = new List<PlayerSpawnpoint>();
        foreach (var sp in GetTree().GetNodesInGroup("spawn"))
        {
            if (sp is PlayerSpawnpoint spawn && spawn.CanSpawn(team))
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

    public void RespawnWave(TeamID team, int stage = 1)
    {
        if (!Round.IsMultiplayerAuthority())
            return;
        List<long> netIds = new List<long>();
        int c = 0;
        var roles = Round.Data.RoleLookup.Select(x => x.Value).Where(x => x.team == team && x.spawnStage == stage).ToArray();
        foreach (var plr in Round.players.Players)
        {
            if (plr.Role.team == TeamID.Dead)
            {
                c++;
                var role = roles[GD.Randi() % roles.Length];
                PlayerSpawnpoint spawn = GetSpawn(role);
                plr.SV_Spawn(role.ResourceName, spawn.GlobalPosition);
            }
            else
            {
                netIds.Add(plr.GetMultiplayerAuthority());
            }
        }
        if (c > 0)
        {
            respawnsRemaining--;
            if (team == TeamID.NTF)
            {
                SendSpawnWaveTo(1, netIds);
            }
            else if (team == TeamID.ClassD)
            {
                SendSpawnWaveTo(2, netIds);
            }
        }
    }

    public void SendSpawnWaveTo(int team, IEnumerable<long> ids)
    {
        foreach (var id in ids)
        {
            RpcId(id, MethodName.RpcRespawnWave, team);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcRespawnWave(int id)
    {
        switch (id)
        {
            case 0:
                respawnWaveSoundRRT.PlayOneShotNoPosition(Round);
                break;
            case 1:
                respawnWaveSoundNTF.PlayOneShotNoPosition(Round);
                break;
            case 2:
                respawnWaveSoundCI.PlayOneShotNoPosition(Round);
                break;
        }
    }
}
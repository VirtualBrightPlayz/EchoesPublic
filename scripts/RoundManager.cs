using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

[GlobalClass]
public partial class RoundManager : SingletonNode3D<RoundManager>
{
    public enum RoundState : byte
    {
        WaitingForPlayers,
        Loading,
        InGame,
        End,
    }

    public enum RoundSize : byte
    {
        Tiny = 0, // <= 4
        Small = 1, // <= 6
        Medium = 2, // <= 10
        Large = 3, // > 10
    }

    public class ServerSettingsClass
    {
        public string Name { get; set; }
        public string InfoUrl { get; set; }
        public string IconUrl { get; set; }
        public int MinPlayers { get; set; } = 1;
        public int MaxPlayers { get; set; } = 20;
    }

    [Signal]
    public delegate void OnSeedEventHandler(ulong seed);
    [Signal]
    public delegate void OnNukeStartedEventHandler();
    [Signal]
    public delegate void OnNukeStoppedEventHandler();
    [Signal]
    public delegate void OnNukeDetonatedEventHandler();
    [Signal]
    public delegate void EventOnRoundStartEventHandler();
    [Signal]
    public delegate void EventOnRoundEndEventHandler(string winner);
    [Signal]
    public delegate void EventOnPlayerJoinedEventHandler(NetworkPlayer player);
    [Signal]
    public delegate void OnRoundStateEventHandler(RoundState state);

    public PlayerSpawner players => IPlayerList.List(this) as PlayerSpawner;
    [Export]
    public double TimeToStart = 10d;
    [Export]
    public Godot.Environment environment;
    // [Export]
    public GameData Data => IInitScript.Instance.Data;
    [Export]
    public NavigationRegion3D navmesh;

    [Export]
    public bool IsBlinking = false;
    [Export]
    public double blinkTime = 0.5d;
    [Export]
    public double blinkInterval = 3.0d;
    public double blinkTimer;

    [Export]
    public string ServerSettingsJson = string.Empty;
    public ServerSettingsClass ServerSettings
    {
        get
        {
            try
            {
                return JsonSerializer.Deserialize<ServerSettingsClass>(ServerSettingsJson);
            }
            catch (JsonException)
            {
                return new ServerSettingsClass();
            }
        }
        set
        {
            try
            {
                ServerSettingsJson = JsonSerializer.Serialize(value);
            }
            catch (JsonException)
            {
                ServerSettingsJson = "{}";
            }
        }
    }

    public int minPlayers => ServerSettings.MinPlayers;
    public double respawnTimer;
    public double waitingForPlayersTimer;
    private RoundState _state;
    [Export]
    public RoundState state
    {
        get => _state;
        set
        {
            MenuManager.Instance?.LocalRoundState(value);
            if (_state != value)
            {
                EmitSignal(SignalName.OnRoundState, (int)value);
            }
            _state = value;
        }
    }
    [Export]
    public RoundSize size;
    [Export]
    public RoundLogic Logic;
    [Export]
    public DeconManager decon;
    [Export]
    public string winningTeam = string.Empty;
    [Export]
    public ulong mapSeed;
    [Export]
    public PackedScene surfacePrefab;
    [Export]
    public Node3D targetSurfaceSpawn;

    [ExportGroup("UI")]
    [Export]
    public Label stateLabel;
    [Export]
    public UIBroadcast broadcast;
    [ExportSubgroup("Sync")]
    [Export]
    public int syncRoundTimer;
    [Export]
    public int syncTimeToStart;

    [ExportGroup("Nuke")]
    [Export]
    public AudioStreamPlayer nukeIntercomSounds;
    [Export]
    public AudioStreamPlayer nukeSounds;
    [Export]
    public AudioStream nukeTMinus;
    [Export]
    public AudioStream nukeCancel;
    [Export]
    public AudioStream nukeBlast;
    [Export]
    public AudioStream nukeSiren;
    [Export]
    public Timer nukeCooldown;
    [Export]
    public bool canStopNuke = true;
    [ExportSubgroup("Consts")]
    [Export]
    public double nukeDetonateTime = 90d;
    [Export]
    public float safeHeight = -25f;
    [ExportSubgroup("Sync")]
    [Export]
    public bool nukeActive;
    [Export]
    public double nukeTimer;
    [Export]
    public bool nukeDetonated = false;
    [Export]
    public bool nukeOnCooldown = false;

    public bool NukeActive
    {
        get => this.nukeActive;
        set
        {
            if (value)
                StartNuke();
            else
                StopNuke();
        }
    }

    [ExportGroup("Vent")]
    [ExportSubgroup("Consts")]
    [Export]
    public double ventCooldown = 90d;
    [Export]
    public float ventHealthPercentPerSecond = 0.03f;
    [ExportSubgroup("Sync")]
    [Export]
    public double ventTimer;

    public bool Paused = false;

    public bool UseCustomMap = false;

    public override void _EnterTree()
    {
        base._EnterTree();

        Multiplayer.PeerConnected += _PeerConnected;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        foreach (var prop in ItemManager.Instance.SpawnNode.GetChildren())
            prop.QueueFreeNow();
        foreach (var prop in ItemManager.Instance.PropSpawnNode.GetChildren())
            prop.QueueFreeNow();
        ItemManager.Instance.Reset();
        foreach (var plr in players.Players)
        {
            plr.QueueFree();
        }
        players.Players.Clear();
        // players.PlayerJoined -= OnPlayerJoined;
    }

    public override void _Ready()
    {
        base._Ready();
        if (Multiplayer.IsServer() || !Multiplayer.HasMultiplayerPeer())
        {
            LoadSettings();
            if (IsInstanceValid(SprayManager.Instance))
                SprayManager.Instance.LoadSprays();
            _ = LoadMap();
            PingServerList();
            MenuManager.Instance?.LocalRoundState(state);
        }
        MenuManager.Instance.SetEnv(environment);
    }

    public override void _Process(double delta)
    {
        if (stateLabel != null)
        {
            stateLabel.Visible = state != RoundState.InGame;
            stateLabel.Text = state == RoundState.Loading ? "Loading\nPlease wait" : state == RoundState.End ? $"Match complete!\n{winningTeam}" : $"Waiting for players\n{players.PlayerList.Count} connected";
        }
        if (nukeSounds != null)
        {
            if (nukeSounds.Stream != nukeSiren)
                nukeSounds.Stream = nukeSiren;
            if (nukeActive)
            {
                if (!nukeSounds.Playing)
                    nukeSounds.Play();
            }
            else
            {
                nukeSounds.Stop();
            }
        }

        if (Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
            return;

        // sync timers
        {
            syncTimeToStart = Mathf.RoundToInt(TimeToStart);
            syncRoundTimer = Mathf.RoundToInt(respawnTimer);
        }

        if (nukeActive)
        {
            nukeTimer -= delta;
            if (nukeTimer <= 0d)
            {
                EndNuke();
            }
        }
        if (IsInstanceValid(nukeCooldown))
            nukeOnCooldown = nukeCooldown.TimeLeft > 0d;

        if (state == RoundState.WaitingForPlayers)
        {
            if (players.PlayerList.Count >= minPlayers)
            {
                if (waitingForPlayersTimer > 0)
                    waitingForPlayersTimer -= delta;
                else
                    respawnTimer -= delta;
            }
            if (respawnTimer <= 0d)
            {
                StartRound();
            }
        }
        else if (state == RoundState.InGame)
        {
            blinkTimer -= delta;
            if (blinkTimer <= 0d)
            {
                blinkTimer = IsBlinking ? blinkInterval : blinkTime;
                IsBlinking = !IsBlinking;
            }

            ventTimer -= delta;
            if (ventTimer <= -ventCooldown)
            {
                ventTimer = ventCooldown;
            }
        }
    }

    public async void PingServerList()
    {
        if (string.IsNullOrEmpty(Settings.Server.ServerAddress) || string.IsNullOrEmpty(Settings.Server.ServerListKey))
            return;
        if (!IInitScript.IsServerOnly)
            return;
        using var client = new System.Net.Http.HttpClient();
        var data = new NetworkManager.ServerListPost()
        {
            key = Settings.Server.ServerListKey,
            address = Settings.Server.ServerAddress,
            name = ServerSettings.Name,
            infoUrl = ServerSettings.InfoUrl,
            iconUrl = ServerSettings.IconUrl,
            players = players.PlayerList.Count,
            maxPlayers = ServerSettings.MaxPlayers,
            tags = string.Join(',', Settings.Server.ServerTags) + $",{ProjectSettings.GetSetting("application/config/version")}",
        };
        var response = await client.PostAsJsonAsync($"{NetworkManager.Instance.httpHostAddress}/api/v2/serverlist", data);
        if (!response.IsSuccessStatusCode)
        {
            Log.PrintErr($"Failed to list server on list: {await response.Content.ReadAsStringAsync()}");
        }
    }

    public async void EndRound(string team = "Tie")
    {
        state = RoundState.End;
        winningTeam = team;
        Rpc(MethodName.RpcRoundEvent, 1); // 1 = roundend
        Rpc(MethodName.RpcRoundEnd, team);
        await ToSignal(GetTree().CreateTimer(10f), SceneTreeTimer.SignalName.Timeout);
        foreach (var item in ItemManager.Instance.SpawnNode.GetChildren())
        {
            item.QueueFreeNow();
        }
        foreach (var plr in IPlayerList.List(this).PlayerList)
        {
            plr.SV_Spawn(RoleID.Spectator);
        }
        await LoadMap();
    }

    public async Task LoadMap()
    {
        Stopwatch sw = new Stopwatch();
        state = RoundState.Loading;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        foreach (var prop in ItemManager.Instance.PropSpawnNode.GetChildren())
            prop.QueueFreeNow();
        ItemManager.Instance.Reset();
        foreach (var sz in targetSurfaceSpawn.GetChildren())
            sz.QueueFreeNow();

        UseCustomMap = false;
        if (Settings.Server.CustomMaps != null && Settings.Server.CustomMaps.Length != 0)
        {
            UseCustomMap = true;
        }

        ulong seed = GD.Randi();
        mapSeed = seed;
        Rpc(MethodName.RpcSetSeed, seed);
        Log.PrintInfo($"Map Seed set to {seed}");

        if (IsInstanceValid(surfacePrefab))
        {
            Node3D node = surfacePrefab.Instantiate<Node3D>();
            node.Position = Vector3.Up * 200;
            targetSurfaceSpawn.AddChild(node, true);
        }

        foreach (var ch in GetChildren())
        {
            if (ch is MapGenerator map && map.Visible)
            {
                ulong offset = 0;
                do
                {
                    Log.PrintInfo($"Spawning Map {map.Name} with seed {seed + offset}...");
                    // await AsyncTools.RunAsync(map, map.LoadMap(seed+offset));
                    try
                    {
                        sw.Restart();
                        await map.LoadMapAsync(seed+offset);
                        sw.Stop();
                        Log.Print($"LoadMapAsync took {sw.ElapsedMilliseconds} ms");
                    }
                    catch (Exception e)
                    {
                        Log.PrintErr(e);
                    }
                    await ToSignal(GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
                    offset++;
                    if (UseCustomMap)
                        break;
                }
                while (map.foundOverlap);
                if (UseCustomMap)
                    break;
            }
        }

        // just in case something spawns during map generation
        // foreach (var item in ItemManager.Instance.SpawnNode.GetChildren())
        {
            // GD.PrintErr(item.SceneFilePath, item.GetPath());
        }

        Log.PrintInfo("Spawning Props...");
        sw.Restart();
        SpawnProps(seed);
        sw.Stop();
        Log.Print($"Prop spawning took {sw.ElapsedMilliseconds} ms");

        Log.PrintInfo("Spawning Items...");
        sw.Restart();
        SpawnItems(seed);
        sw.Stop();
        Log.Print($"Item spawning took {sw.ElapsedMilliseconds} ms");

        if (IsInstanceValid(navmesh))
        {
            navmesh.BakeNavigationMesh();
        }

        Log.PrintInfo("Map Loaded, waiting for players");
        state = RoundState.WaitingForPlayers;
        TimeToStart = Settings.Server.TimeToRoundStart;
        respawnTimer = TimeToStart;
        waitingForPlayersTimer = 2;

        // nuke stuffs
        nukeActive = false;
        nukeDetonated = false;
        var nodes = GetTree().GetNodesInGroup("nuke_disable");
        foreach (var node in nodes)
        {
            if (node is Elevator elevator)
            {
                elevator.isLocked = false;
            }
            if (node is Door door)
            {
                door.isLocked = false;
                door.SV_SetState(false);
            }
        }
    }

    private void _PeerConnected(long id)
    {
        if (IsMultiplayerAuthority())
            RpcId(id, MethodName.RpcSetSeed, mapSeed);
    }

    public void OnPlayerJoined(NetworkPlayer player)
    {
        if (Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
            return;

        if (Settings.Server.ServerTags.Contains(NetworkManager.NoVrOnlyTag) && player.IsVR)
        {
            player.Kick("Playing in VR isn't allowed on this server.");
            return;
        }
        if (Settings.Server.ServerTags.Contains(NetworkManager.VrOnlyTag) && !player.IsVR)
        {
            player.Kick("Playing without VR isn't allowed on this server.");
            return;
        }

        if (state == RoundState.WaitingForPlayers)
        {
            respawnTimer = Mathf.Min(respawnTimer + 1, TimeToStart);
            waitingForPlayersTimer = 2;
        }
        else if (state == RoundState.InGame)
        {
            EmitSignalEventOnPlayerJoined(player);
        }
    }

    public void LoadSettings()
    {
        if (!string.IsNullOrEmpty(Settings.Server.AdminPassword))
            Log.PrintInfo("Password set.");
        if (!string.IsNullOrEmpty(Settings.Server.ServerAddress) && !string.IsNullOrEmpty(Settings.Server.ServerListKey))
            Log.PrintInfo("Server address and list key set, server will be publicly listed.");
        ServerSettings = new ServerSettingsClass()
        {
            Name = Settings.Server.ServerName,
            InfoUrl = Settings.Server.ServerInfoUrl,
            IconUrl = Settings.Server.ServerIconUrl,
            MinPlayers = Settings.Server.MinPlayers,
            MaxPlayers = Settings.Server.MaxPlayers,
        };
        Log.Print($"Name: {ServerSettings.Name} IconUrl: {ServerSettings.IconUrl} MinPlayers: {ServerSettings.MinPlayers}");
        if (!string.IsNullOrWhiteSpace(Settings.Server.OverrideRoundConfig) && IsInstanceValid(Logic))
        {
            string json = FileAccess.GetFileAsString(Settings.Server.OverrideRoundConfig);
            if (string.IsNullOrWhiteSpace(json))
            {
                using FileAccess fs = FileAccess.Open(Settings.Server.OverrideRoundConfig, FileAccess.ModeFlags.Write);
                fs.StoreString(JsonSerializer.Serialize(Logic.Config, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                }));
            }
            else
            {
                Logic.Config = JsonSerializer.Deserialize<RoundConfig>(json);
            }
        }
    }

    public void StartRound()
    {
        state = RoundState.InGame;
        ventTimer = ventCooldown;
        Rpc(MethodName.RpcRoundEvent, 0); // 0 = RoundStartEvent
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcSetSeed(ulong seed)
    {
        mapSeed = seed;
        EmitSignalOnSeed(mapSeed);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcPlayEffect(int id, Vector3 pos, Vector3 rot)
    {
        Data.Effects[id].PlayOneShotAt3D(this, pos, rot, true);
    }

    public void StartNuke()
    {
        if (nukeCooldown.TimeLeft > 0d || Logic.Config.DisableNuke || NukeActive)
            return;
        
        nukeActive = true;
        nukeDetonated = false;
        nukeTimer = nukeDetonateTime;
        Rpc(nameof(RpcNukeIntercom), false);
        EmitSignal(SignalName.OnNukeStarted);
        var nodes = GetTree().GetNodesInGroup("nuke_disable");
        foreach (var node in nodes)
        {
            if (node is Elevator elevator)
            {
                elevator.isLocked = false;
            }
            if (node is Door door)
            {
                door.isLocked = false;
            }
        }
    }

    public void StopNuke()
    {
        if (!canStopNuke || !NukeActive)
            return;
        
        nukeActive = false;
        nukeDetonated = false;
        Rpc(nameof(RpcNukeIntercom), true);
        nukeCooldown.Start();
        EmitSignal(SignalName.OnNukeStopped);
        var nodes = GetTree().GetNodesInGroup("nuke_disable");
        foreach (var node in nodes)
        {
            if (node is Elevator elevator)
            {
                elevator.isLocked = false;
            }
            if (node is Door door)
            {
                door.isLocked = false;
            }
        }
    }

    public void EndNuke()
    {
        nukeActive = false;
        nukeDetonated = true;
        Rpc(nameof(RpcNukeSound));
        var list = IPlayerList.List(this).PlayerList;
        foreach (var item in list)
        {
            if (item.PlayerPosition.Y > safeHeight)
            {
                continue;
            }
            item.Kill(new DamageInfo());
        }
        EmitSignal(SignalName.OnNukeDetonated);
        var nodes = GetTree().GetNodesInGroup("nuke_disable");
        foreach (var node in nodes)
        {
            if (node is Elevator elevator)
            {
                elevator.isLocked = true;
            }
            if (node is Door door)
            {
                door.SV_SetState(true);
                door.isLocked = true;
            }
        }
    }

    public void SpawnProps(ulong seed)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = seed;
        var spawns = GetTree().GetNodesInGroup(PropSpawnpoint.GroupName).Where(x => x is PropSpawnpoint).Select(x => x as PropSpawnpoint).ToArray();
        var groups = spawns.Select(x => x.SpawnGroup).Distinct().ToArray();
        Dictionary<ItemSpawnGroup, int> groupsSpawned = new Dictionary<ItemSpawnGroup, int>();
        foreach (var group in groups)
        {
            // var group = spawn.SpawnGroup;
            var validSpawns = spawns.Where(x => x.SpawnGroup == group).ToList();
            var lst = new List<PackedScene>(group.Props);
            int max = group.MaxSpawned < 0 ? lst.Count + spawns.Length : group.MaxSpawned;
            for (int i = 0; i < max; i++)
            {
                if (validSpawns.Count == 0)
                    break;
                if (lst.Count == 0)
                    break;
                int idx = (int)(rng.Randi() % validSpawns.Count);
                int idx2 = (int)(rng.Randi() % lst.Count);
                validSpawns[idx].Spawn(lst[idx2]);
                if (group.RemoveUsedSpawn)
                    validSpawns.RemoveAt(idx);
                if (group.RemoveUsedProp)
                    lst.RemoveAt(idx2);
            }
            for (int i = 0; i < validSpawns.Count; i++)
            {
                validSpawns[i].Spawn(null);
            }
        }
    }

    public void SpawnItems(ulong seed)
    {
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = seed;
        var spawners = GetTree().GetNodesInGroup(ItemSpawnpoint.GroupName).Where(x => x is ItemSpawner).Select(x => x as ItemSpawner).ToArray();
        foreach (var spawner in spawners)
        {
            spawner.SpawnRoundStart();
        }
        var spawns = GetTree().GetNodesInGroup(ItemSpawnpoint.GroupName).Where(x => x is ItemSpawnpoint).Select(x => x as ItemSpawnpoint).ToArray();
        foreach(var spawner in spawns)
        {
            if(!IsInstanceValid(spawner.SpawnGroup) || spawner.SpawnGroup == null)
            {
                Log.PrintWarn("Invalid or null spawn group for ItemSpawnpoint named: " + spawner.Name);
            }
        }
        var groups = spawns.Select(x => x.SpawnGroup).Distinct().ToArray();
        Dictionary<ItemSpawnGroup, int> groupsSpawned = new Dictionary<ItemSpawnGroup, int>();
        foreach (var group in groups)
        {
            // var group = spawn.SpawnGroup;
            if(!IsInstanceValid(group))
            {
                //Log.PrintWarn("SpawnGroup is null!");
                continue;
            }
            if(group.Items == null)
            {
                //Log.PrintWarn("SpawnGroup items is null!");
                continue;
            }
            var validSpawns = spawns.Where(x => x.SpawnGroup == group).ToList();
            var lst = new List<ItemPreset>(group.Items);
            int max = group.MaxSpawned < 0 ? lst.Count + spawns.Length : group.MaxSpawned;
            for (int i = 0; i < max; i++)
            {
                if (validSpawns.Count == 0)
                    break;
                if (lst.Count == 0)
                    break;
                int idx = (int)(rng.Randi() % validSpawns.Count);
                int idx2 = (int)(rng.Randi() % lst.Count);
                validSpawns[idx].Spawn(lst[idx2]);
                if (group.RemoveUsedSpawn)
                    validSpawns.RemoveAt(idx);
                if (group.RemoveUsedItem)
                    lst.RemoveAt(idx2);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcBroadcast(string msg, double t)
    {
        if (IsInstanceValid(broadcast))
        {
            broadcast.AddMessage(msg, t);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcRoundEvent(int ev)
    {
        switch (ev)
        {
            case 0:
                EmitSignalEventOnRoundStart();
                break;
            case 1:
                break;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcRoundEnd(string ev)
    {
        EmitSignalEventOnRoundEnd(ev);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcNukeSound()
    {
        nukeIntercomSounds.Stream = nukeBlast;
        nukeIntercomSounds.Play();
        if (IsInstanceValid(NetworkPlayer.LocalInstance))
        {
            var tween = NetworkPlayer.LocalInstance.Hud.CreateTween();
            NetworkPlayer.LocalInstance.Hud.GlitchEffectAmount = 1f;
            tween.TweenProperty(NetworkPlayer.LocalInstance.Hud, nameof(LocalPlayerHUD.GlitchEffectAmount), 0f, 2d);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcNukeIntercom(bool cancel)
    {
        if (cancel)
        {
            nukeIntercomSounds.Stream = nukeCancel;
            nukeIntercomSounds.Play();
        }
        else
        {
            nukeIntercomSounds.Stream = nukeTMinus;
            nukeIntercomSounds.Play();
        }
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;

[GlobalClass]
public partial class GameData : Resource
{
    [Export]
    [Obsolete]
    public PlayerRole[] Roles = Array.Empty<PlayerRole>();
    public Dictionary<string, PlayerRole> RoleLookup = new Dictionary<string, PlayerRole>();
    [Export]
    public Godot.Collections.Dictionary<string, PackedScene> PlayerControllers = new Godot.Collections.Dictionary<string, PackedScene>();
    [Export]
    public Godot.Collections.Dictionary<string, PackedScene> PlayerAbilities = new Godot.Collections.Dictionary<string, PackedScene>();
    public Dictionary<string, PlayerAbility> AbilityLookup = new Dictionary<string, PlayerAbility>();
    [Export]
    public Godot.Collections.Dictionary<string, PackedScene> PlayerModels = new Godot.Collections.Dictionary<string, PackedScene>();
    // public Dictionary<string, PackedScene> ModelLookup = new Dictionary<string, PackedScene>();
    public ItemPreset[] ItemPresets = Array.Empty<ItemPreset>();
    [Export]
    public Godot.Collections.Array<GameSound> Sounds = new Godot.Collections.Array<GameSound>();
    [Export]
    public GameEffect[] Effects = Array.Empty<GameEffect>();
    [Export]
    public StatusEffectBase[] StatusEffects = Array.Empty<StatusEffectBase>();
    public ItemUpgradeRecipe[] ItemUpgrades = Array.Empty<ItemUpgradeRecipe>();
    [Export]
    public PackedScene[] AmmoItems = Array.Empty<PackedScene>();
    [Export]
    public Godot.Collections.Dictionary<string, string[]> MapGraphs = new Godot.Collections.Dictionary<string, string[]>();
    [Export]
    public Godot.Collections.Dictionary<string, string[]> MapFiles = new Godot.Collections.Dictionary<string, string[]>();
    public PresetRoom[] Rooms = Array.Empty<PresetRoom>();
    public List<(string, PackedScene)> RoomScenes = new List<(string, PackedScene)>();
    [Export]
    public PackedScene[] Props = Array.Empty<PackedScene>();
    // [Export]
    // public Godot.Collections.Dictionary<string, AudioStream> FootstepSounds = new Godot.Collections.Dictionary<string, AudioStream>();

    public void Init()
    {
        FillRooms();
        FillItems();
        FillUpgrades();
    }

    public bool TryGetSound(string name, out GameSound sound)
    {
        if (string.IsNullOrEmpty(name))
        {
            sound = null;
            return false;
        }
        sound = Sounds.FirstOrDefault(x => x.ResourceName == name);
        return IsInstanceValid(sound);
    }

    public static async Task<PackedScene> LoadSceneAsync(string path, CancellationToken token = default)
    {
        Error err = ResourceLoader.LoadThreadedRequest(path);
        if (err != Error.Ok)
        {
            Log.PrintErr($"Unable to load scene {path}: {err}");
            return null;
        }
        var status = ResourceLoader.LoadThreadedGetStatus(path);
        while (status == ResourceLoader.ThreadLoadStatus.InProgress)
        {
            token.ThrowIfCancellationRequested();
            await IInitScript.SceneTree.ToSignal(IInitScript.SceneTree, SceneTree.SignalName.ProcessFrame);
            status = ResourceLoader.LoadThreadedGetStatus(path);
        }
        if (status != ResourceLoader.ThreadLoadStatus.Loaded)
        {
            return null;
        }
        var asset = ResourceLoader.LoadThreadedGet(path);
        if (asset is PackedScene scn)
        {
            return scn;
        }
        return null;
    }

    public void LoadRoomScenes()
    {
        RoomScenes = Rooms.Select(x => (x.sceneFile, ResourceLoader.Load<PackedScene>(x.sceneFile))).ToList();
    }

    public async Task LoadRoomScenesAsync(CancellationToken token)
    {
        RoomScenes.Clear();
        foreach (var room in Rooms)
        {
            token.ThrowIfCancellationRequested();
            RoomScenes.Add((room.sceneFile, await LoadSceneAsync(room.sceneFile, token)));
        }
    }

    public void UnloadRoomScenes()
    {
        RoomScenes.Clear();
    }

    public PackedScene GetRoomScene(string path)
    {
        if (!RoomScenes.Any(x => x.Item1 == path))
        {
            PackedScene scn = GD.Load<PackedScene>(path);
            RoomScenes.Add((path, scn));
            return scn;
        }
        return RoomScenes.FirstOrDefault(x => x.Item1 == path).Item2;// ?? GD.Load<PackedScene>(path);
    }

    public void Reload()
    {
        if (IsInstanceValid(ModLoader.Instance))
        {
            ModLoader.Instance.UnloadAllMods();
        }
        ReloadRoles();
        ReloadAbilities();
        if (IsInstanceValid(ModLoader.Instance))
        {
            ModLoader.Instance.InitModFolder();
            if (IsInstanceValid(SteamManager.Instance))
            {
                foreach (var kvp in SteamManager.Instance.SubscribedItems)
                {
                    GameMod mod = ModLoader.Instance.InitModAt(kvp.Value);
                    if (mod != null)
                    {
                        mod.WorkshopFileId = kvp.Key;
                    }
                }
            }
            ModLoader.Instance.LoadAllMods();
        }
    }

    #region Player Abilities

    public void ReloadAbilities()
    {
        List<PlayerAbility> abilities = new List<PlayerAbility>();
        foreach (var kvp in PlayerAbilities)
        {
            PlayerAbility ability = new PlayerAbility();
            ability.ResourceName = kvp.Key;
            ability.BaseAbility = kvp.Key;
            abilities.Add(ability);
        }
        LoadAbilities(abilities);
        foreach (var kvp in AbilityLookup)
        {
            kvp.Value.ReadFromFile();
            abilities.RemoveAll(x => x.ResourceName == kvp.Key);
        }
        foreach (var item in abilities)
        {
            if (AbilityLookup.TryAdd(item.ResourceName, item))
            {
                Log.Print($"Ability registered with name {item.ResourceName}");
            }
            else
            {
                Log.PrintWarn($"Ability already registered with name {item.ResourceName}");
            }
        }
    }

    private void LoadAbilities(List<PlayerAbility> roles, string path = "res://roles/abilities/")
    {
        if (path.EndsWith('/') || DirAccess.DirExistsAbsolute(path))
        {
            string[] list = DirAccess.GetDirectoriesAt(path);
            for (int i = 0; i < list.Length; i++)
            {
                LoadAbilities(roles, path.PathJoin(list[i]));
            }
            string[] list2 = DirAccess.GetFilesAt(path);
            for (int i = 0; i < list2.Length; i++)
            {
                LoadAbilities(roles, path.PathJoin(list2[i]));
            }
        }
        else if (path.GetExtension() == "toml")
        {
            try
            {
                PlayerAbility role = new PlayerAbility();
                role.file = path;
                if (role.ReadFromFile())
                {
                    roles.Add(role);
                }
            }
            catch (Exception e)
            {
                Log.PrintErr(e);
            }
        }
    }

    #endregion

    #region Roles

    public bool RegisterPlayerRole(PlayerRole role)
    {
        bool val = RoleLookup.TryAdd(role.ResourceName, role);
        if (val)
        {
            Log.Print($"Role registered with name {role.ResourceName}");
        }
        else
        {
            Log.PrintWarn($"Role already exists with name {role.ResourceName}");
        }
        return val;
    }

    public void UnregisterPlayerRole(string id)
    {
        RoleLookup.Remove(id);
    }

    private void LoadRoles(List<PlayerRole> roles, string path = "res://roles/")
    {
        if (path.EndsWith('/') || DirAccess.DirExistsAbsolute(path))
        {
            string[] list = DirAccess.GetDirectoriesAt(path);
            for (int i = 0; i < list.Length; i++)
            {
                LoadRoles(roles, path.PathJoin(list[i]));
            }
            string[] list2 = DirAccess.GetFilesAt(path);
            for (int i = 0; i < list2.Length; i++)
            {
                LoadRoles(roles, path.PathJoin(list2[i]));
            }
        }
        else if (path.GetExtension() == "toml")
        {
            try
            {
                PlayerRole role = new PlayerRole();
                role.file = path;
                if (role.ReadFromFile())
                {
                    roles.Add(role);
                }
            }
            catch (Exception e)
            {
                Log.PrintErr(e);
            }
        }
    }

    public void ReloadRoles()
    {
        List<PlayerRole> roles = new List<PlayerRole>();
        LoadRoles(roles);
        foreach (var kvp in RoleLookup)
        {
            kvp.Value.ReadFromFile();
            roles.RemoveAll(x => x.ResourceName == kvp.Key);
        }
        foreach (var item in roles)
        {
            RegisterPlayerRole(item);
        }
    }

    #endregion

    private void LoadItems(List<ItemPreset> items, string path = "res://items/")
    {
        if (path.EndsWith('/'))
        {
            string[] list = ResourceLoader.ListDirectory(path);
            for (int i = 0; i < list.Length; i++)
            {
                LoadItems(items, path.PathJoin(list[i]));
            }
        }
        else
        {
            Resource res = ResourceLoader.Load(path);
            if (res is ItemPreset room && !string.IsNullOrEmpty(room.ResourceName))
            {
                items.Add(room);
            }
        }
    }

    public void FillItems()
    {
        List<ItemPreset> items = new List<ItemPreset>();
        LoadItems(items);
        ItemPresets = items.ToArray();
    }

    private void LoadItemUpgrades(List<ItemUpgradeRecipe> upgrades, string path = "res://items/upgrades/")
    {
        if (path.EndsWith('/'))
        {
            string[] list = ResourceLoader.ListDirectory(path);
            for (int i = 0; i < list.Length; i++)
            {
                LoadItemUpgrades(upgrades, path.PathJoin(list[i]));
            }
        }
        if (DirAccess.DirExistsAbsolute(path))
        {
            string[] list = DirAccess.GetDirectoriesAt(path);
            for (int i = 0; i < list.Length; i++)
            {
                LoadItemUpgrades(upgrades, path.PathJoin(list[i]));
            }
            string[] list2 = DirAccess.GetFilesAt(path);
            for (int i = 0; i < list2.Length; i++)
            {
                LoadItemUpgrades(upgrades, path.PathJoin(list2[i]));
            }
        }
        else if (path.GetExtension() == "toml")
        {
            TomlTable cfg = TomlSerializer.Deserialize<TomlTable>(FileAccess.GetFileAsString(path));
            if (TomlExtensions.TryGetTableArray(cfg, "item_upgrades", out TomlTableArray array))
            {
                foreach (var aCfg in array)
                {
                    ItemUpgradeRecipe obj = new ItemUpgradeRecipe();
                    if (obj.ReadFromTomlTable(aCfg))
                    {
                        upgrades.Add(obj);
                        // GD.Print($"{obj.inputItemName} -> {obj.outputItemName}");
                    }
                }
            }
        }
        else
        {
            Resource res = ResourceLoader.Load(path);
            if (res is ItemUpgradeRecipe room)
            {
                upgrades.Add(room);
            }
        }
    }

    public void FillUpgrades()
    {
        List<ItemUpgradeRecipe> upgrades = new List<ItemUpgradeRecipe>();
        LoadItemUpgrades(upgrades);
        ItemUpgrades = upgrades.ToArray();
    }

    private void LoadRooms(List<PresetRoom> rooms, string path = "res://rooms/")
    {
        if (path.EndsWith('/'))
        {
            string[] list = ResourceLoader.ListDirectory(path);
            for (int i = 0; i < list.Length; i++)
            {
                LoadRooms(rooms, path.PathJoin(list[i]));
            }
        }
        else
        {
            Resource res = ResourceLoader.Load(path);
            if (res is PresetRoom room)
            {
                rooms.Add(room);
            }
        }
    }

    public void FillRooms()
    {
        List<PresetRoom> rooms = new List<PresetRoom>();
        LoadRooms(rooms, "res://rooms/preset_rooms/");
        Rooms = rooms.ToArray();
    }
}
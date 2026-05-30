using System;
using System.Collections.Generic;
using Godot;
using Tomlyn;
using Tomlyn.Model;

public partial class GameMod
{
    public string BasePath { get; set; }
    public ModInformation Info { get; private set; }

    public long WorkshopFileId { get; set; } = 0;

    public bool Loaded { get; private set; } = false;

    private List<PlayerRole> Roles = new List<PlayerRole>();
    private List<PlayerAbility> Abilities = new List<PlayerAbility>();
    private List<ModModelInfo> Models = new List<ModModelInfo>();
    private List<GameSound> Sounds = new List<GameSound>();

    private GameData GameData => IInitScript.Instance.Data;

    private string RolesPath => BasePath.PathJoin("roles");
    private string AbilitiesPath => BasePath.PathJoin("abilities");
    private string ModelsPath => BasePath.PathJoin("models");
    private string SoundsPath => BasePath.PathJoin("sounds");
    public string MapsPath => BasePath.PathJoin("maps");

    public GameMod(string path, ModInformation data)
    {
        BasePath = path;
        Info = data;
    }

    public void Load()
    {
        if (Loaded)
        {
            Log.PrintWarn($"Mod {BasePath} already loaded");
            return;
        }
        LoadRoles();
        LoadAbilities();
        LoadModels(ModelsPath);
        LoadSounds(SoundsPath);
        foreach (var role in Roles)
        {
            Log.Print($"Adding Role {role.ResourceName}");
            GameData.RegisterPlayerRole(role);
        }
        foreach (var ability in Abilities)
        {
            Log.Print($"Adding Ability {ability.ResourceName}");
            GameData.AbilityLookup.TryAdd(ability.ResourceName, ability);
        }
        foreach (var mdl in Models)
        {
            Log.Print($"Adding Model {mdl.ModelId}");
            GameData.PlayerModels.TryAdd(mdl.ModelId, mdl.Scene);
        }
        foreach (var snd in Sounds)
        {
            Log.Print($"Adding Sound {snd.ResourceName}");
            GameData.Sounds.Add(snd);
        }
        Log.PrintInfo($"Mod {Info.Name} loaded");
        Loaded = true;
    }

    public void Unload()
    {
        if (!Loaded)
        {
            Log.PrintWarn($"Mod {BasePath} already un-loaded");
            return;
        }
        foreach (var role in Roles)
        {
            Log.Print($"Removing Role {role.ResourceName}");
            GameData.UnregisterPlayerRole(role.ResourceName);
        }
        Roles.Clear();
        foreach (var ability in Abilities)
        {
            Log.Print($"Removing Ability {ability.ResourceName}");
            GameData.AbilityLookup.Remove(ability.ResourceName);
        }
        Abilities.Clear();
        foreach (var mdl in Models)
        {
            Log.Print($"Removing Model {mdl.ModelId}");
            GameData.PlayerModels.Remove(mdl.ModelId);
        }
        Models.Clear();
        foreach (var snd in Sounds)
        {
            Log.Print($"Removing Sound {snd.ResourceName}");
            GameData.Sounds.Remove(snd);
        }
        Sounds.Clear();
        Log.PrintInfo($"Mod {BasePath} un-loaded");
        Loaded = false;
    }

    public static bool LoadModInfo(string path, out ModInformation info)
    {
        info = new ModInformation();
        string tomlFilePath = path.PathJoin("info.toml");
        if (DirAccess.DirExistsAbsolute(path))
        {
            FileAccess fs = FileAccess.Open(tomlFilePath, FileAccess.ModeFlags.Read);
            if (!GodotObject.IsInstanceValid(fs))
            {
                Log.PrintErr($"{tomlFilePath} was not readable, error code: {FileAccess.GetOpenError()}");
                return false;
            }
            TomlTable cfg = TomlSerializer.Deserialize<TomlTable>(fs.GetAsText());
            fs.Close();
            if (!TomlExtensions.TryGetTable(cfg, "modinfo", out TomlTable infoCfg))
            {
                Log.PrintErr($"{tomlFilePath} was missing a '[modinfo]' section");
                return false;
            }
            if (!TomlExtensions.TryGetValue(infoCfg, "name", out info.Name))
            {
                Log.PrintErr($"{tomlFilePath} was missing a 'name' key");
                return false;
            }
            if (!TomlExtensions.TryGetValue(infoCfg, "id", out info.ModId))
            {
                Log.PrintErr($"{tomlFilePath} was missing a 'id' key");
                return false;
            }
            TomlExtensions.GetValueOptional(infoCfg, "description", ref info.Description);
            TomlExtensions.GetValueOptional(infoCfg, "version", ref info.Version);
            TomlExtensions.GetArrayOptional(infoCfg, "authors", ref info.Authors);
            TomlExtensions.GetArrayOptional(infoCfg, "required_game_versions", ref info.RequiredGameVersions);
            return true;
        }
        Log.PrintErr($"Folder {path} does not exist");
        return false;
    }

    private void LoadRoles()
    {
        // Roles.Clear(); // shouldn't be needed
        string[] files = DirAccess.GetFilesAt(RolesPath);
        foreach (var file in files)
        {
            // if (file.GetExtension().Equals("toml", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    PlayerRole role = new PlayerRole();
                    role.file = RolesPath.PathJoin(file);
                    if (role.ReadFromFile())
                    {
                        role.ResourceName = TryPrefixWithModId(role.ResourceName);
                        Roles.Add(role);
                    }
                }
                catch (Exception e)
                {
                    Log.PrintErr(e);
                }
            }
        }
    }

    private void LoadAbilities()
    {
        // Abilities.Clear(); // shouldn't be needed
        string[] files = DirAccess.GetFilesAt(AbilitiesPath);
        foreach (var file in files)
        {
            // if (file.GetExtension().Equals("toml", StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    PlayerAbility ability = new PlayerAbility();
                    ability.file = AbilitiesPath.PathJoin(file);
                    if (ability.ReadFromFile())
                    {
                        ability.ResourceName = TryPrefixWithModId(ability.ResourceName);
                        Abilities.Add(ability);
                    }
                }
                catch (Exception e)
                {
                    Log.PrintErr(e);
                }
            }
        }
    }

    private void LoadSounds(string dirPath)
    {
        string[] files = DirAccess.GetFilesAt(dirPath);
        foreach (var file in files)
        {
            GameSound info = LoadSound(dirPath.PathJoin(file));
            if (info != null)
            {
                Sounds.Add(info);
            }
        }
        string[] dirs = DirAccess.GetDirectoriesAt(dirPath);
        foreach (var dir in dirs)
        {
            LoadSounds(dirPath.PathJoin(dir));
        }
    }

    private GameSound LoadSound(string path)
    {
        if (path.GetExtension().Equals("toml", StringComparison.InvariantCultureIgnoreCase))
        {
            GameSound info = new GameSound();
            TomlTable cfg = TomlSerializer.Deserialize<TomlTable>(FileAccess.GetFileAsString(path));
            if (!TomlExtensions.TryGetTable(cfg, "sound", out TomlTable soundCfg))
            {
                Log.PrintErr($"{path} was missing a 'sound' section");
                return null;
            }
            if (!TomlExtensions.TryGetValue(soundCfg, "id", out string id))
            {
                Log.PrintErr($"{path} was missing a 'id' key");
                return null;
            }
            info.ResourceName = TryPrefixWithModId(id);
            TomlExtensions.GetValueOptional(soundCfg, "volume_db", ref info.volumeDb);
            TomlExtensions.GetValueOptional(soundCfg, "unit_size", ref info.unitSize);
            TomlExtensions.GetValueOptional(soundCfg, "max_distance", ref info.maxDistance);
            TomlExtensions.GetValueOptional(soundCfg, "pitch", ref info.pitch);
            TomlExtensions.GetValueOptional(soundCfg, "loop", ref info.forceLoop);
            info.streams = [LoadAudioStream(path.GetBaseName(), info.forceLoop)];
            return info;
        }
        return null;
    }

    private AudioStream LoadAudioStream(string path, bool loop)
    {
        if (path.GetExtension().Equals("wav", StringComparison.InvariantCultureIgnoreCase))
        {
            var wav = AudioStreamWav.LoadFromFile(path);
            wav.LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
            if (loop)
            {
                wav.LoopEnd = Mathf.FloorToInt(wav.MixRate * wav.GetLength());
                if (wav.Stereo)
                {
                    wav.LoopEnd *= 2;
                }
            }
            return wav;
        }
        else if (path.GetExtension().Equals("ogg", StringComparison.InvariantCultureIgnoreCase))
        {
            var ogg = AudioStreamOggVorbis.LoadFromFile(path);
            ogg.Loop = loop;
            return ogg;
        }
        else if (path.GetExtension().Equals("mp3", StringComparison.InvariantCultureIgnoreCase))
        {
            var mp3 = AudioStreamMP3.LoadFromFile(path);
            mp3.Loop = loop;
            return mp3;
        }
        Log.PrintErr($"Error loading {path} (Unsupported file format)");
        return null;
    }

    private void LoadModels(string dirPath)
    {
        string[] files = DirAccess.GetFilesAt(dirPath);
        foreach (var file in files)
        {
            ModModelInfo info = LoadModel(dirPath.PathJoin(file));
            if (info != null)
            {
                Models.Add(info);
            }
        }
        string[] dirs = DirAccess.GetDirectoriesAt(dirPath);
        foreach (var dir in dirs)
        {
            LoadModels(dirPath.PathJoin(dir));
        }
    }

    private ModModelInfo LoadModel(string path)
    {
        if (path.GetExtension().Equals("toml", StringComparison.InvariantCultureIgnoreCase))
        {
            ModModelInfo info = new ModModelInfo();
            TomlTable cfg = TomlSerializer.Deserialize<TomlTable>(FileAccess.GetFileAsString(path));
            if (!TomlExtensions.TryGetTable(cfg, "model", out TomlTable modelCfg))
            {
                Log.PrintErr($"{path} was missing a 'model' section");
                return null;
            }
            if (!TomlExtensions.TryGetValue(modelCfg, "id", out info.ModelId))
            {
                Log.PrintErr($"{path} was missing a 'id' key");
                return null;
            }
            if (!TomlExtensions.TryGetValue(modelCfg, "type", out info.Type))
            {
                Log.PrintErr($"{path} was missing a 'type' key");
                return null;
            }
            TomlExtensions.TryGetTable(modelCfg, "data", out info.dataCfg); // It's optional when loading the mod.
            info.ModelId = TryPrefixWithModId(info.ModelId);
            Node node = LoadModelAsNode(path.GetBaseName());
            if (GodotObject.IsInstanceValid(node))
            {
                node = PlayerModel.CreateFromToml(info, node) ?? node;
                foreach (var n in node.FindChildren("*", owned: false))
                {
                    n.Owner = node;
                }
                info.Scene = new PackedScene();
                Error err = info.Scene.Pack(node);
                node.QueueFree();
                if (err != Error.Ok)
                {
                    Log.PrintErr($"Error packing scene for {path} ({err})");
                    return null;
                }
                info.Scene.ResourceName = info.ModelId;
            }
            return info;
        }
        return null;
    }

    private Node LoadModelAsNode(string path)
    {
        if (path.GetExtension().Equals("glb", StringComparison.InvariantCultureIgnoreCase))
        {
            GltfDocument document = new GltfDocument();
            GltfState state = new GltfState();
            byte[] data = FileAccess.GetFileAsBytes(path);
            Error err = document.AppendFromBuffer(data, string.Empty, state);
            if (err != Error.Ok)
            {
                Log.PrintErr($"Error loading {path} ({err})");
                return null;
            }
            return document.GenerateScene(state);
        }
        else if (path.GetExtension().Equals("fbx", StringComparison.InvariantCultureIgnoreCase))
        {
            // TODO
        }
        Log.PrintErr($"Error loading {path} (Unsupported file format)");
        return null;
    }

    private string TryPrefixWithModId(string name)
    {
        if (name.StartsWith(Info.ModId + ':'))
        {
            return name;
        }
        return $"{Info.ModId}:{name}";
    }
}

public class ModModelInfo
{
    public string ModelId;
    public string Type;
    public PackedScene Scene;
    public TomlTable dataCfg;
}

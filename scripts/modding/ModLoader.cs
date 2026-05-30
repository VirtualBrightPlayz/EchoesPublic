using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class ModLoader : SingletonNode3D<ModLoader>
{
    public string[] Whitelist = [];
    [Export] public Godot.Collections.Array<Script> ValidScripts = [];
    public List<GameMod> Mods = [];

    public void ReloadWhitelist()
    {
        FileAccess fs = FileAccess.Open("res://addons/modding/whitelist.txt", FileAccess.ModeFlags.Read);
        Whitelist = fs.GetAsText().Replace("\r", string.Empty).Split("\n", false);
        fs.Close();
    }

    public PackedScene LoadModPack(string path)
    {
        using AssetSystem sys = AssetSystem.New();
        sys.Whitelist = Whitelist;
        sys.ClassLoadLookup.Add("GDScript", Callable.From<StreamPeer, string, Resource>(LoadGDScript));
        sys.OpenRead(path);
        return sys.ReadResource<PackedScene>("map.dat");
    }

    public async Task<Node> ReadNodeAsync(string path)
    {
        using AssetSystem sys = AssetSystem.New();
        sys.Whitelist = Whitelist;
        sys.ClassLoadLookup.Add("GDScript", Callable.From<StreamPeer, string, Resource>(LoadGDScript));
        sys.OpenRead(path);
        Node node = sys.sys.Call("read_scene", "map2.dat").As<Node>();
        Stopwatch sw = new Stopwatch();
        while (sys.GetDependencyCount() > 0)
        {
            sw.Restart();
            string depPath = sys.ReadNextDependency();
            sw.Stop();
            // Log.Print($"Loaded dependency {depPath}. {sw.ElapsedMilliseconds}ms");
        }
        return node;
    }

    public Resource LoadGDScript(StreamPeer buffer, string path)
    {
        string globalName = buffer.GetUtf8String();
        Log.Print($"Load GDScript: {globalName} {path}");
        return ValidScripts.FirstOrDefault(x => x.GetGlobalName() == globalName);
    }

    public void InitModFolder(string path = "user://mods/")
    {
        foreach (var folder in DirAccess.GetDirectoriesAt(path))
        {
            InitModAt(path.PathJoin(folder));
        }
    }

    public GameMod InitModAt(string path)
    {
        if (GameMod.LoadModInfo(path, out ModInformation info) && !Mods.Any(x => x.BasePath == path || x.Info.ModId == info.ModId))
        {
            GameMod mod = new GameMod(path, info);
            Mods.Add(mod);
            return mod;
        }
        return Mods.FirstOrDefault(x => x.BasePath == path || x.Info.ModId == info.ModId);
    }

    public void LoadAllMods()
    {
        foreach (var mod in Mods)
        {
            try
            {
                mod.Load();
            }
            catch (Exception e)
            {
                Log.PrintErr($"Error loading mod {mod.BasePath}:\n{e}");
            }
        }
    }

    public void UnloadAllMods()
    {
        foreach (var mod in Mods)
        {
            try
            {
                mod.Unload();
            }
            catch (Exception e)
            {
                Log.PrintErr($"Error un-loading mod {mod.BasePath}:\n{e}");
            }
        }
    }

    public List<string> LoadModList(string[] list)
    {
        List<string> missing = new List<string>(list);
        foreach (var mod in Mods)
        {
            if (Array.IndexOf(list, mod.Info.ModId) == -1)
            {
                continue;
            }
            try
            {
                mod.Load();
                missing.Remove(mod.Info.ModId);
            }
            catch (Exception e)
            {
                Log.PrintErr($"Error loading mod {mod.BasePath}:\n{e}");
            }
        }
        return missing;
    }
}


public struct ModInformation
{
    public string Name;

    public string Description;

    public string ModId;

    public string[] Authors;

    public string[] RequiredGameVersions;

    public string Version;
}
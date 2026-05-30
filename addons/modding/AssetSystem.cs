using System;
using Godot;
using Godot.Collections;

public class AssetSystem : IDisposable
{
    private static StringName OpenReadName = "open_read";
    private static StringName OpenWriteName = "open_read";
    private static StringName CloseName = "close";
    private static StringName WhitelistName = "whitelist";
    private static StringName ReadResourceName = "read_resource";
    private static StringName WriteResourceName = "write_resource";
    private static StringName ClassLoadLookupName = "class_load_lookup";
    private static StringName ClassSaveLookupName = "class_save_lookup";
    private static StringName GetDependencyCountName = "get_dependency_count";
    private static StringName ReadNextDependencyName = "read_next_dependency";

    public RefCounted sys;

    public string[] Whitelist
    {
        get => sys.Get(WhitelistName).AsStringArray();
        set => sys.Set(WhitelistName, value);
    }

    public Dictionary<string, Callable> ClassLoadLookup
    {
        get => sys.Get(ClassLoadLookupName).AsGodotDictionary<string, Callable>();
        set => sys.Set(ClassLoadLookupName, value);
    }

    public Dictionary<string, Callable> ClassSaveLookup
    {
        get => sys.Get(ClassSaveLookupName).AsGodotDictionary<string, Callable>();
        set => sys.Set(ClassSaveLookupName, value);
    }

    public static AssetSystem New()
    {
        return new AssetSystem() { sys = (RefCounted)GD.Load<GDScript>("res://addons/modding/asset_system.gd").New() };
    }

    public Error OpenRead(string path)
    {
        return sys.Call(OpenReadName, path).As<Error>();
    }

    public Error OpenWrite(string path)
    {
        return sys.Call(OpenWriteName, path).As<Error>();
    }

    public void Close()
    {
        sys.Call(CloseName);
    }

    public T ReadResource<[MustBeVariant] T>(string path) where T : Resource
    {
        return sys.Call(ReadResourceName, path).As<T>();
    }

    public void WriteResource(Resource res)
    {
        sys.Call(WriteResourceName, res);
    }

    public ulong GetDependencyCount()
    {
        return sys.Call(GetDependencyCountName).AsUInt64();
    }

    public string ReadNextDependency()
    {
        return sys.Call(ReadNextDependencyName).AsString();
    }

    public void Dispose()
    {
        Close();
    }
}
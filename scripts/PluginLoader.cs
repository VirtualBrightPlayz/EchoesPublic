using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

public static class PluginLoader
{
    public static void LoadPlugins()
    {
        try
        {
            AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            string[] paths = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Loaders"));
            foreach (var path in paths)
            {
                if (!path.ToLower().EndsWith(".dll"))
                {
                    continue;
                }
                try
                {
                    Assembly asm = alc.LoadFromAssemblyPath(path);
                    foreach (var type in asm.GetTypes())
                    {
                        MethodInfo main = type.GetMethod("PluginMain", BindingFlags.Static | BindingFlags.Public);
                        if (main == null)
                            continue;
                        main.Invoke(null, Array.Empty<object>());
                    }
                }
                catch (Exception e)
                {
                    Log.PrintErr(e);
                }
            }
        }
        catch
        {
        }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
        string name = args.Name.Split(',')[0];
        string path = Path.Combine(Environment.CurrentDirectory, "Loaders", name + ".dll");
        if (File.Exists(path))
            return alc.LoadFromAssemblyPath(path);
        return null;
    }
}
#if false
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Godot;
using HarmonyLib;

[Tool]
public partial class CSharpProfiler : EngineProfiler
{
    public const string ProfilerPrefix = "CSharp";
    public const string ProfilerName = "CSharp:Profiler";

    public struct FrameData
    {
        public string TypeName;
        public string MethodName;
        public ulong TimeUSec;
        public bool IsPostfix;

        public FrameData(Godot.Collections.Array data)
        {
            TypeName = default;
            MethodName = default;
            TimeUSec = default;
            IsPostfix = default;
            if (data.Count > 0)
                TypeName = data[0].AsString();
            if (data.Count > 1)
                MethodName = data[1].AsString();
            if (data.Count > 2)
                TimeUSec = data[2].AsUInt64();
            if (data.Count > 3)
                IsPostfix = data[3].AsBool();
        }

        public Godot.Collections.Array GetArray()
        {
            return new Godot.Collections.Array()
            {
                TypeName,
                MethodName,
                TimeUSec,
                IsPostfix,
            };
        }
    }

    public Harmony harmony = new Harmony("net.virtualwebsite.csharp_profiler");
    private static int mainThreadId;
    private static bool IsMainThread => mainThreadId == Thread.CurrentThread.ManagedThreadId;
    private static List<FrameData> frame = new List<FrameData>();
    private static Stopwatch stopwatch = new Stopwatch();

    public void PatchType(Type type)
    {
        GD.Print($"Patching type {type.FullName}");
        var prefix = GetType().GetMethod(nameof(_Prefix));
        var postfix = GetType().GetMethod(nameof(_Postfix));
        foreach (var method in type.GetMethods())
        {
            if (method.DeclaringType == type && !method.IsAbstract && !method.ContainsGenericParameters)
                harmony.Patch(method, prefix, postfix);
        }
    }

    public static void _Prefix(MethodBase __originalMethod/*, object __instance, object[] __args*/)
    {
        if (IsMainThread)
            frame.Add(new FrameData()
            {
                TypeName = __originalMethod.DeclaringType?.FullName,
                MethodName = __originalMethod.Name,
                TimeUSec = Time.GetTicksUsec(),
                IsPostfix = false,
            });
    }

    public static void _Postfix(MethodBase __originalMethod/*, object __instance, object[] __args*/)
    {
        if (IsMainThread)
            frame.Add(new FrameData()
            {
                TypeName = __originalMethod.DeclaringType?.FullName,
                MethodName = __originalMethod.Name,
                TimeUSec = Time.GetTicksUsec(),
                IsPostfix = true,
            });
    }

    public override void _Toggle(bool enable, Godot.Collections.Array options)
    {
        if (enable)
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            foreach (var type in GetType().Assembly.GetTypes())
            {
                if (type == typeof(CSharpProfiler) || type == typeof(CSharpProfilerNode))
                    continue;
                if (type.IsAssignableTo(typeof(GodotObject)))
                    PatchType(type);
            }
        }
        else
        {
            harmony.UnpatchAll();
            frame.Clear();
        }
    }

    public override void _Tick(double frameTime, double processTime, double physicsTime, double physicsFrameTime)
    {
        if (!stopwatch.IsRunning || stopwatch.ElapsedMilliseconds >= 750)
        {
            var arr = new Godot.Collections.Array();
            arr.AddRange(frame.Select(x => x.GetArray()));
            EngineDebugger.SendMessage(ProfilerName, arr);
            stopwatch.Restart();
        }
        frame.Clear();
    }

    public override void _AddFrame(Godot.Collections.Array data)
    {
        if (IsMainThread)
            frame.Add(new FrameData(data));
    }
}
#endif

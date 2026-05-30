using Godot;

public partial class CSharpProfilerNode : Godot.Node
{
// #if TOOLS || DEBUG
    // public CSharpProfiler debugger;

    public override void _EnterTree()
    {
        if (OS.HasFeature("profile"))
            OS.CreateProcess("dotnet-trace", ["collect", "-p", OS.GetProcessId().ToString(), "--format", "Chromium"], true);
            // Patcher.Patch();
        // debugger = new CSharpProfiler();
        // EngineDebugger.RegisterProfiler(CSharpProfiler.ProfilerName, debugger);
        // EngineDebugger.ProfilerEnable(CSharpProfiler.ProfilerName, true);
    }

    public override void _Process(double delta)
    {
        // if (OS.HasFeature("profile"))
        //     Profiler.EmitFrameMark();
    }

    public override void _ExitTree()
    {
        // if (OS.HasFeature("profile"))
            // Patcher.UnPatch();
        // EngineDebugger.ProfilerEnable(CSharpProfiler.ProfilerName, false);
        // EngineDebugger.UnregisterProfiler(CSharpProfiler.ProfilerName);
        // debugger = null;
    }
// #endif
}

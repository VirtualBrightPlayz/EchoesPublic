using System;
using System.Linq;
using Godot;

public class ProfileCommand : SimpleGameCommandBase
{
    public override string Command { get; } = "Profile";

    public override string[] Alias { get; } = ["perf"];

    public override string CommandDescription { get; } = "Runs \"dotnet-trace\" on the game, assuming it is installed.";

    public static GodotThread thread;
    public static FileAccess stdio;
    public static FileAccess stderr;
    public static int pid = -1;

    private static void _ThreadLoop()
    {
        OS.SetThreadName(nameof(_ThreadLoop));
        while (OS.IsProcessRunning(pid))
        {
            stdio.GetBuffer((long)stdio.GetLength());
            stderr.GetBuffer((long)stderr.GetLength());
        }
        Log.PrintInfo($"Profiler exited with code: {OS.GetProcessExitCode(pid)}");
        pid = -1;
    }

    public override bool Execute(string[] args, out string response)
    {
        if (pid != -1)
        {
            if (args.Length > 0 && args[0].Equals("kill", StringComparison.InvariantCultureIgnoreCase))
            {
                OS.Kill(pid);
                response = "Sending kill signal to profiler...";
                return true;
            }
            response = $"Already profiling PID: {pid}, use \"profile stop\" to end it early.";
            return false;
        }
        double seconds = 30d;
        if (args.Length > 0 && double.TryParse(args[0], out double result))
        {
            if (result > 0d)
            {
                seconds = result;
            }
            else
            {
                seconds = Engine.GetFramesPerSecond();
            }
        }
        string duration = TimeSpan.FromSeconds(seconds).ToString("hh\\:mm\\:ss");
        string file = OS.GetExecutablePath().GetBaseDir().PathJoin($"profile_perf_{OS.GetProcessId()}.nettrace");
        string exe = "dotnet-trace";
        string possibleExe = DirAccess.GetFilesAt(OS.GetExecutablePath().GetBaseDir()).FirstOrDefault(x => x.GetBaseName().Equals(exe, StringComparison.InvariantCultureIgnoreCase));
        if (!string.IsNullOrEmpty(possibleExe))
        {
            exe = OS.GetExecutablePath().GetBaseDir().PathJoin(exe);
        }
        var data = OS.ExecuteWithPipe(exe, ["collect", "-p", OS.GetProcessId().ToString(), "--duration", duration, "--format", "Chromium", "-o", file]);
        if (data.Count > 0)
        {
            pid = data["pid"].AsInt32();
            stdio = data["stdio"].As<FileAccess>();
            stderr = data["stderr"].As<FileAccess>();
            thread?.WaitToFinish();
            thread = new GodotThread();
            thread.Start(Callable.From(_ThreadLoop));
            response = $"Started profiling with PID: {pid}";
            return true;
        }
        response = "Failed to start \"dotnet-trace\" process.";
        return false;
    }
}

using System.Runtime.InteropServices;

public static class ExternalCrashHandler
{
    [DllImport("crash_handler")]
    public static extern void init_crash_handler([MarshalAs(UnmanagedType.LPWStr)] string path);
}
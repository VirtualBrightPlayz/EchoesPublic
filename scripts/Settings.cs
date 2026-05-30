using System;
using System.Text.Json;
using Godot;
using GodotSteam;

public static class Settings
{
    public const string UserSettingsFilePath = "user://user_settings.json";
    public const string ServerSettingsFilePath = "user://server_settings.json";

    [Flags]
    public enum VoiceDebugFlags : long
    {
        Listen = 1,
        RemoveZeros = 2,
    }

    public enum FSRScaling : long
    {
        UltraQuality,
        Quality,
        Balanced,
        Performance,
    }

    public enum GenericQuality : long
    {
        VeryLow = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        VeryHigh = 4,
    }

    public enum UpscalingMode : long
    {
        None = Viewport.Scaling3DModeEnum.Bilinear,
        Fsr = Viewport.Scaling3DModeEnum.Fsr,
        Fsr2 = Viewport.Scaling3DModeEnum.Fsr2,
    }

    public enum WindowMode : long
    {
        Windowed = 0,
        Borderless = 1,
        Fullscreen = 2,
    }
    
    public static float GetScale(this FSRScaling scaling)
    {
        return scaling switch
        {
            FSRScaling.UltraQuality => 0.77f,
            FSRScaling.Quality => 0.67f,
            FSRScaling.Balanced => 0.59f,
            FSRScaling.Performance => 0.5f,
            _ => 1.0f,
        };
    }

    public class UserSettings
    {
        [UseCustomScene("res://scenes/ui/settings/parts/username_edit.tscn")]
        [Limit(3, 64)]
        [NameOverride("Username")]
        [Tooltip("Your in game username. This cannot be changed if you use Steam or are currently in a game.")]
        public string UserName { get; set; }
        [Group("Audio")]
        [Limit(0.001d, 1d)]
        [Step(0.001d)]
        [NameOverride("Volume")]
        public float Volume { get; set; } = 1f;
        [NameOverride("Use Old Menu Music")]
        [Hidden]
        public bool OldMenuMusic { get; set; } = true;
        [UseHardwareDetector(HardwareDetectorType.AudioIn)]
        [NameOverride("Microphone Input")]
        [Tooltip("Determines what microphone to use")]
        public string MicInputName { get; set; } = string.Empty;
        [Limit(-20d, 20d)]
        [Step(1d)]
        [NameOverride("Microphone Volume (Db)")]
        public float MicVolumeDb { get; set; } = 0f;
        [Group("Video")]
        [NameOverride("V-Sync")]
        public DisplayServer.VSyncMode VSyncMode { get; set; } = DisplayServer.VSyncMode.Enabled;
        [NameOverride("Enable Shadows")]
        [Tooltip("Disables shadows. Greatly increases performance if switched off.")]
        [Hidden]
        public bool Shadows { get; set; } = true;
        [NameOverride("Shadow Quality")]
        public GenericQuality ShadowQuality { get; set; } = GenericQuality.Medium;
        [NameOverride("Volumetric Fog")]
        public bool VolumetricFog { get; set; } = true;
        [NameOverride("MSAA")]
        public Viewport.Msaa MSAA { get; set; } = 0;
        [Hidden]
        public int Width { get; set; } = 0;
        [Hidden]
        public int Height { get; set; } = 0;
        [NameOverride("Resolution Mode")]
        [Tooltip("What resolution mode to use.")]
        public WindowMode ScreenMode { get; set; } = WindowMode.Fullscreen; // 0 = windowed, 1 = borderless, 2 = exclusive
        [Hidden]
        public float ScreenScale { get; set; } = 1f;
        [UseHardwareDetector(HardwareDetectorType.Screen)]
        [NameOverride("Monitor")]
        public int ScreenId { get; set; } = -1;
        [NameOverride("Resolution Scaling")]
        [Tooltip("Helps on older GPUs or in systems with a better GPU than CPU.")]
        public UpscalingMode ScalingMode { get; set; } = UpscalingMode.None;
        [Group("FSR")]
        [NameOverride("FSR Quality")]
        public FSRScaling FsrScale { get; set; } = FSRScaling.UltraQuality;
        [Limit(1d, 1.5d)]
        [Step(0.01)]
        [NameOverride("FSR Sharpness")]
        public float FsrSharpness { get; set; } = 1.2f;
        [Hidden]
        public int TextureSizeLimit { get; set; } = 0;
        [Group("Preferences")]
        [Limit(0.01d, 4d)]
        [Step(0.01d)]
        [NameOverride("Mouse Sensitivity")]
        public float MouseSensitivity { get; set; } = 1f;
        [NameOverride("Skip Startup File Preloading")]
        public bool SkipPreloading { get; set; } = false;
        [NameOverride("VR Snap Turn")]
        public bool SnapTurn { get; set; } = false;
        [Limit(30, 60)]
        [Step(1)]
        [NameOverride("VR Turn Speed")]
        public int TurnSpeed { get; set; } = 45;
        [Group("Debug")]
        [NameOverride("Debug Mode")]
        public bool DebugMode { get; set; } = false;
        [NameOverride("Console Fullscreen")]
        public bool ConsoleFullscreen { get; set; } = false;
        [Hidden]
        public string LangFile { get; set; }
        [Hidden]
        public VoiceDebugFlags VoiceDebug { get; set; } = 0;

        public UserSettings()
        {
            UserName = System.Environment.MachineName;
            if (string.IsNullOrEmpty(UserName))
                UserName = "User";
            if (SteamManager.Supported)
                UserName = Steam.GetPersonaName();
        }
    }

    public class ServerSettings
    {
        public string ServerName { get; set; } = "My Server";
        public int ServerPort { get; set; } = 27015;
        public string ServerInfoUrl { get; set; }
        public string ServerIconUrl { get; set; }
        public int MinPlayers { get; set; } = 2;
        public int MaxPlayers { get; set; } = 20;
        public int TimeToRoundStart { get; set; } = 15;
        public string AdminPassword { get; set; }
        public string ServerListKey { get; set; }
        public string ServerAddress { get; set; }
        public bool ShowLogType { get; set; } = true;
        public bool RichTextLogs { get; set; } = true;
        public bool ShowTimestamps { get; set; } = true;
        public bool TimestampsInUTC { get; set; } = true;
        public int MaxDuplicateLogs { get; set; } = 10;
        public TimeSpan DuplicateLogsTimeWindow { get; set; } = new TimeSpan(0, 1, 0);
        public Log.LogLevel MinLogLevel { get; set; } = Log.LogLevel.Info;
        public string[] ServerTags { get; set; } = new string[0];
        public string[] CustomMaps { get; set; } = new string[0];
        public string[] Sprays { get; set; } = new string[0];
        public string OverrideRoundConfig { get; set; } = string.Empty;
        public int FPS { get; set; } = 0;
        public int TPS { get; set; } = 0;
        public bool FriendlyFire { get; set; } = false;
    }

    private static bool _modified = false;
    public static bool Modified
    {
        get => _modified;
        set
        {
            SettingsModified?.Invoke();
            _modified = value;
        }
    }
    public static UserSettings User { get; private set; } = new UserSettings();
    public static ServerSettings Server { get; private set; } = new ServerSettings();
    public static Action UserReadError { get; set; } = null;
    public static Action UserWriteError { get; set; } = null;
    public static Action ServerReadError { get; set; } = null;
    public static Action ServerWriteError { get; set; } = null;
    public static Action SettingsModified { get; set; } = null;

    public static string ParseArg(string key)
    {
        var args = OS.GetCmdlineUserArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(key, StringComparison.CurrentCultureIgnoreCase))
            {
                string val = args[i+1];
                if (args[i+1].StartsWith('"') && !args[i+1].EndsWith('"'))
                {
                    val = args[i+1].Substring(1);
                    i++;
                    while (i < args.Length)
                    {
                        if (args[i].EndsWith('"'))
                        {
                            val += args[i].Substring(0, args[i].Length - 2);
                            break;
                        }
                        val += args[i];
                        i++;
                    }
                }
                return val;
            }
        }
        return string.Empty;
    }

    public static void ReadSettings()
    {
        ReadUserSettings();
        ReadServerSettings();
    }

    public static void WriteSettings()
    {
        WriteUserSettings();
        WriteServerSettings();
    }

    public static void ReadUserSettings()
    {
        try
        {
            if (!FileAccess.FileExists(UserSettingsFilePath))
            {
                WriteUserSettings();
            }
            using var file = FileAccess.Open(UserSettingsFilePath, FileAccess.ModeFlags.Read);
            User = JsonSerializer.Deserialize<UserSettings>(file.GetAsText(true));
            foreach (var prop in User.GetType().GetProperties())
            {
                try
                {
                    string val = ParseArg($"--{prop.Name}");
                    if (string.IsNullOrEmpty(val))
                        continue;
                    switch (prop.PropertyType)
                    {
                        case Type when prop.PropertyType == typeof(string[]):
                            prop.SetValue(User, val.Split(','));
                            break;
                        case Type when prop.PropertyType == typeof(string):
                            prop.SetValue(User, val);
                            break;
                        case Type when prop.PropertyType == typeof(float):
                            prop.SetValue(User, float.Parse(val));
                            break;
                        case Type when prop.PropertyType == typeof(int):
                            prop.SetValue(User, int.Parse(val));
                            break;
                        case Type when prop.PropertyType == typeof(bool):
                            prop.SetValue(User, bool.Parse(val));
                            break;
                    }
                }
                catch
                {
                    Log.PrintErr($"Error overriding user setting \"{prop.Name}\"!");
                }
            }
        }
        catch (Exception e)
        {
            Log.PrintErr($"Error reading user settings, using defaults. {e}");
            User = new UserSettings();
            UserReadError?.Invoke();
        }
    }

    public static void WriteUserSettings()
    {
        try
        {
            using var file = FileAccess.Open(UserSettingsFilePath, FileAccess.ModeFlags.Write);
            file.StoreString(JsonSerializer.Serialize(User, new JsonSerializerOptions()
            {
                WriteIndented = true,
            }));
        }
        catch (Exception e)
        {
            Log.PrintErr($"Error writing user settings. {e}");
            UserWriteError?.Invoke();
        }
    }

    public static void ReadServerSettings()
    {
        try
        {
            if (!FileAccess.FileExists(ServerSettingsFilePath))
            {
                WriteServerSettings();
            }
            using var file = FileAccess.Open(ServerSettingsFilePath, FileAccess.ModeFlags.Read);
            Server = JsonSerializer.Deserialize<ServerSettings>(file.GetAsText(true));
            foreach (var prop in Server.GetType().GetProperties())
            {
                try
                {
                    string val = ParseArg($"--{prop.Name}");
                    if (string.IsNullOrEmpty(val))
                        continue;
                    switch (prop.PropertyType)
                    {
                        case Type when prop.PropertyType == typeof(string):
                            prop.SetValue(Server, val);
                            break;
                        case Type when prop.PropertyType == typeof(float):
                            prop.SetValue(Server, float.Parse(val));
                            break;
                        case Type when prop.PropertyType == typeof(int):
                            prop.SetValue(Server, int.Parse(val));
                            break;
                        case Type when prop.PropertyType == typeof(bool):
                            prop.SetValue(Server, bool.Parse(val));
                            break;
                    }
                }
                catch
                {
                    Log.PrintErr($"Error overriding server setting \"{prop.Name}\"!");
                }
            }
        }
        catch (Exception e)
        {
            Log.PrintErr($"Error reading server settings, using defaults. {e}");
            Server = new ServerSettings();
            ServerReadError?.Invoke();
        }
    }

    public static void WriteServerSettings()
    {
        try
        {
            using var file = FileAccess.Open(ServerSettingsFilePath, FileAccess.ModeFlags.Write);
            file.StoreString(JsonSerializer.Serialize(Server, new JsonSerializerOptions()
            {
                WriteIndented = true,
            }));
        }
        catch (Exception e)
        {
            Log.PrintErr($"Error writing server settings. {e}");
            ServerWriteError?.Invoke();
        }
    }
}

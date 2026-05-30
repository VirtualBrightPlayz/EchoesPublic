using Godot;
using GodotSteam;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

[GlobalClass]
public partial class MenuManager : SingletonNode3D<MenuManager>, IInitScript, ISettingsUpdater
{
    public enum GameState : byte
    {
        Menu,
        Loading,
        Game,
    }

    [Export]
    public string[] mapPaths = Array.Empty<string>();
    public List<PackedScene> maps = [];
    [Export]
    public MultiplayerSpawner mapSpawner;
    [Export]
    public Label errorTextLabel;
    [Export]
    public Control errorTextPanel;
    [Export]
    public Control textPanel;
    [Export]
    public AudioStreamPlayer menuMusicPlayer;
    [Export]
    public AudioStreamPlayer musicCuePlayer;
    [Export]
    public float menuMusicPlayerVolumeDb = 0f;

    [Export]
    public AudioStream musicNormal;
    [Export]
    public AudioStream musicIntense;
    [Export]
    public AudioStream musicIntenseStart;
    [Export]
    public AudioStream musicIntenseEnd;
    [Export]
    public AudioStream oldMenuMusic;

    public AudioStream newMenuMusic
    {
        get
        {
            switch (state)
            {
                default:
                case GameState.Menu:
                    return musicNormal;
                case GameState.Loading:
                case GameState.Game:
                    return musicIntense;
            }
        }
    }

    [Export]
    public PackedScene loadingScene;
    [Export]
    public PackedScene menuScene;
    [Export]
    public WorldEnvironment environment;
    public Godot.Environment environmentAsset;
    [Export]
    public StatusRpcManager statusManager;
    [Export]
    public SubViewport mainViewport;
    [Export]
    public Node mainWorld;
    [Export]
    public TextureRect mainViewTexture;
    [Export]
    public double viewTweenTime = 2.0d;
    [Export]
    public Tween.TransitionType viewTransition;
    [Export]
    public Tween.EaseType viewEase;
    [Export]
    public GameData Data { get; set; }
    [Export]
    public int eq6EffectEffect = 0;

    public Node menu;

    private float menuVolumeDb = 0f;
    private float worldVolumeDb = 0f;
    private float menuVolume => Mathf.DbToLinear(menuVolumeDb);
    public bool menuUseOldMusic = true;
    public string Username { get; set; }
    public Action OnSettingsChanged = () => { };
    public Action<string> OnMenuMessage = _ => { };

    public GameState state = GameState.Menu;
    public GameState State
    {
        get => state;
        set
        {
            /*
            if (state != value)
            {
                if (value == GameState.Menu)
                {
                    menuMusicPlayer.Stream = musicIntenseEnd;
                    menuMusicPlayer.Play();
                }
                else
                {
                    menuMusicPlayer.Stream = musicIntenseStart;
                    menuMusicPlayer.Play();
                }
            }
            */
            AudioStream before = newMenuMusic;
            state = value;
            AudioStream after = newMenuMusic;
            if (before != after && !menuUseOldMusic)
            {
                MusicStateChanged();
            }
        }
    }

    public Thread thread;
    public PlayerConsole serverConsole;

    public SceneTreeTimer menuTimer;
    public bool InMatrix = false;
    public Tween menuTween;
    public int menuTweenCount = 0;

    public Tween musicTween;
    public SceneTreeTimer musicTimer;

    public XRInterface xrInterface;
    public bool IsXR => xrInterface != null && xrInterface.IsInitialized();

    [Export]
    public PackedScene layoutEditor;
    [Export]
    public PackedScene workshopEditor;
    public Control layoutEditorInstance;
    public Control workshopEditorInstance;

    public bool steamPublicLobby = false;
    public int gameMap = 0;

    public static PlaceholderTexture2D PlaceholderTexture;

    public static bool CrashServer = false;
    public static System.Diagnostics.Process crashServerProcess;
    public AudioBus menuBus;

    public CancellationTokenSource loadingTokenSource;

    static MenuManager()
    {
        NativeLibrary.SetDllImportResolver(typeof(MenuManager).Assembly, Resolver);
    }

    private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.Contains("crash_handler"))
        {
            if (OS.HasFeature("editor"))
            {
                if (OS.GetName().Equals("Windows"))
                    return NativeLibrary.Load(ProjectSettings.GlobalizePath("res://export_data/windows/").PathJoin(libraryName));
            }
            if (OS.GetName().Equals("Linux"))
                    return NativeLibrary.Load(System.Environment.CurrentDirectory.PathJoin("libcrash_handler.so"));
        }

        return IntPtr.Zero;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        IInitScript.Instance = this;

        // Log.RichTextLogs = !OS.GetName().Equals("Android");
        Settings.UserReadError = UserReadSettingsError;
        Settings.UserWriteError = UserWriteSettingsError;
        GD.Print($"SCP: Echoes - v{ProjectSettings.GetSetting("application/config/version")}");
        Log.PrintInfo($"SCP: Echoes - v{ProjectSettings.GetSetting("application/config/version")}");
        if (OS.GetName().Equals("Android"))
        {
            GetTree().Root.PositionalShadowAtlasSize = 0;
            mainViewport.PositionalShadowAtlasSize = GetTree().Root.PositionalShadowAtlasSize;
            RenderingServer.DirectionalShadowAtlasSetSize(256, true);
        }
        Settings.SettingsModified = WriteSettings;
        Data.Init();
    }

    public override void _Ready()
    {
        base._Ready();
        ReadSettings();
        Data.Reload();

        NetworkManager.Instance.SV_Started += Server_Start;
        NetworkManager.Instance.SV_Stopped += Server_Stop;
        NetworkManager.Instance.CL_Connected += Client_Connect;
        NetworkManager.Instance.CL_Disconnected += Client_Disconnect;

        // UnloadCache();
        // UnloadCache("res://materials/");
        // UnloadCache("res://textures/");
        GetTree().Root.SizeChanged += ViewResize;

        maps = mapPaths.Select(x => GD.Load<PackedScene>(x)).ToList();
        mapSpawner.SpawnFunction = Callable.From<Variant, Node>(SpawnMap);

        // GetTree().SetMultiplayer(null);
        GetTree().SetMultiplayer(NetworkManager.Instance.mp, mainWorld.GetPath());

        menuBus = new AudioBus("Menu");

        if (IInitScript.IsHeadless || IInitScript.IsServerOnly)
        {
            Engine.MaxFps = 60;
        }
        if (IInitScript.IsServerOnly)
        {
            OS.LowProcessorUsageMode = true;
            Engine.PhysicsTicksPerSecond = 30;
            if (Settings.Server.FPS > 0)
                Engine.MaxFps = Settings.Server.FPS;
            if (Settings.Server.TPS > 0)
                Engine.PhysicsTicksPerSecond = Settings.Server.TPS;
        }
        if (IInitScript.IsTestClient)
        {
            Engine.MaxFps = 30;
        }

        if (IInitScript.IsServerOnly)
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            serverConsole = new PlayerConsole();
            serverConsole.IsAdmin = true;
            AddChild(serverConsole);
            thread = new Thread(ThreadServer);
            thread.Name = "Server Console";
            thread.Start();
            PluginLoader.LoadPlugins();
            LoadMenu();
            return;
        }
        else
        {
            xrInterface = XRServer.FindInterface("OpenXR");
            if (xrInterface != null && xrInterface.IsInitialized())
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                GetViewport().UseXR = true; // TODO: don't do this here
                GetViewport().UseHdr2D = false;
                // if (xrInterface is OpenXRInterface xr)
                //     Engine.PhysicsTicksPerSecond = (int)xr.DisplayRefreshRate;
                // GetTree().PhysicsInterpolation = false;
                XRServer.WorldScale = 1.5f;
                mainWorld.Reparent(this);
                GetTree().SetMultiplayer(NetworkManager.Instance.mp, mainWorld.GetPath());
            }
        }
        LoadTextures();
    }

    public override void _ExitTree()
    {
        NetworkManager.Instance.SV_Started -= Server_Start;
        NetworkManager.Instance.SV_Stopped -= Server_Stop;
        NetworkManager.Instance.CL_Connected -= Client_Connect;
        NetworkManager.Instance.CL_Disconnected -= Client_Disconnect;

        GetTree().Root.SizeChanged -= ViewResize;
        crashServerProcess?.Close();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("menu_pause") || State != GameState.Menu)
        {
            errorTextPanel.Hide();
            if (State == GameState.Menu)
                Close();
        }

        if (!menuMusicPlayer.Playing)
        {
            menuMusicPlayer.Stream = menuUseOldMusic ? oldMenuMusic : newMenuMusic;
            // menuMusicPlayer.Bus = menuUseOldMusic ? "MenuCompress" : "Menu";
            menuMusicPlayer.Play();
        }
        if ((IsInstanceValid(musicTimer) && musicTimer.TimeLeft > 0) || (musicTween != null && musicTween.IsValid() && musicTween.IsRunning()))
        {
            // menuMusicPlayer.VolumeDb = -80f;
        }
        else
        {
            menuMusicPlayer.VolumeDb = State == GameState.Game ? Mathf.MoveToward(menuMusicPlayer.VolumeDb, -80f, 15f * (float)delta) : menuMusicPlayerVolumeDb;
            // musicCuePlayer.VolumeDb = -80f;
        }
        worldVolumeDb = Mathf.MoveToward(worldVolumeDb, State == GameState.Game ? 0f : -24f, 25f * (float)delta);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("World"), worldVolumeDb);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("NonWorld"), menuVolumeDb);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Menu"), menuVolumeDb);
        // AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Voice"), menuVolumeDb);
    }

    public override void _Input(InputEvent ev)
    {
        if (IsInstanceValid(GameConsole.Instance) && IsInstanceValid(GameConsole.Instance.consoleRoot) && GameConsole.Instance.consoleRoot.Visible)
            return;
        if (!GetViewport().UseXR)
        {
            if (errorTextPanel.Visible)
            {
                GetViewport().SetInputAsHandled();
                return;
            }
            if (textPanel.Visible)
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        if (InMatrix || IsInstanceValid(RoundManager.Instance))
        {
            mainViewport.PushInput(ev);
            return;
        }
        if (IsInstanceValid(menu) && menu is MainMenu m)
        {
            if (m.HandleMouse(ev, m.viewport, m.meshInst))
                GetViewport().SetInputAsHandled();
            else
                m.viewport.PushInput(ev);
        }
        else
            mainViewport.PushInput(ev);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && !IsInstanceValid(layoutEditorInstance))
        {
            GetTree().Quit();
        }
    }

    public static int ShadowQualityToSize(Settings.GenericQuality quality)
    {
        switch (quality)
        {
            default:
            case Settings.GenericQuality.VeryLow:
                return 512;
            case Settings.GenericQuality.Low:
                return 512;
            case Settings.GenericQuality.Medium:
                return 1024;
            case Settings.GenericQuality.High:
                return 2048;
            case Settings.GenericQuality.VeryHigh:
                return 4096;
        }
    }

    public void UpdateSettings()
    {
        menuVolumeDb = Mathf.Clamp(Mathf.LinearToDb(Settings.User.Volume), -80f, 0f);
        Username = Settings.User.UserName;
        if (SteamManager.Supported)
        {
            Username = Steam.GetPersonaName();
        }
        AudioServer.InputDevice = Settings.User.MicInputName;
        Vector4I vec = ResolutionEdit.GetResolution();
        bool isWindowed = Settings.User.ScreenMode == Settings.WindowMode.Windowed;
        bool shouldBeWindowed = vec.W == 0;
        if (isWindowed != shouldBeWindowed || (!shouldBeWindowed && (vec.X != Settings.User.Width || vec.Y != Settings.User.Height)))
        {
            ResolutionEdit.SetResolution(Settings.User.Width, Settings.User.Height, (int)Settings.User.ScreenMode, Settings.User.ScreenId);
        }
        mainViewport.Scaling3DMode = (Viewport.Scaling3DModeEnum)Settings.User.ScalingMode;
        mainViewport.Scaling3DScale = Settings.User.ScalingMode == Settings.UpscalingMode.None ? Settings.User.ScreenScale : Settings.User.FsrScale.GetScale();
        var root = GetTree().Root;
        root.Msaa3D = (Viewport.Msaa)Settings.User.MSAA;
        root.Msaa2D = (Viewport.Msaa)Settings.User.MSAA;
        DisplayServer.WindowSetVsyncMode(Settings.User.VSyncMode, root.GetWindowId());

        if (IsXR)
        {
            GetTree().Root.Msaa2D = Viewport.Msaa.Disabled;
            GetTree().Root.Msaa3D = Viewport.Msaa.Disabled;
        }
        RenderingServer.DirectionalShadowAtlasSetSize(ShadowQualityToSize(Settings.User.ShadowQuality), true);
        GetTree().Root.PositionalShadowAtlasSize = Settings.User.ShadowQuality == Settings.GenericQuality.VeryLow && !RenderingServer.GetCurrentRenderingMethod().Equals("gl_compatibility") ? 0 : ShadowQualityToSize(Settings.User.ShadowQuality);
        mainViewport.PositionalShadowAtlasSize = GetTree().Root.PositionalShadowAtlasSize;
        mainViewport.Msaa2D = GetTree().Root.Msaa2D;
        mainViewport.Msaa3D = GetTree().Root.Msaa3D;
        mainViewport.ScreenSpaceAA = GetTree().Root.ScreenSpaceAA;
        if (IsInstanceValid(environment.Environment))
        {
            environment.Environment.VolumetricFogEnabled = Settings.User.VolumetricFog;
        }
    }

    private async void MusicStateChanged()
    {
        if (musicTween != null && musicTween.IsValid())
        {
            musicTween.Kill();
        }
        double time = 0.2d;
        float offDb = Mathf.LinearToDb(0.5f);
        musicCuePlayer.Stop();
        if (State == GameState.Menu)
        {
            musicCuePlayer.Stream = musicIntenseEnd;
        }
        else
        {
            musicCuePlayer.Stream = musicIntenseStart;
        }
        double delay = musicCuePlayer.Stream.GetLength() - time * 0d;
        SceneTreeTimer timer;
        if (IsInstanceValid(musicTimer) && musicTimer.TimeLeft > 0)
        {
            musicTimer.TimeLeft = delay;
            timer = musicTimer;
        }
        else
            timer = GetTree().CreateTimer(delay, ignoreTimeScale: true);
        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(menuMusicPlayer, new NodePath(AudioStreamPlayer.PropertyName.VolumeDb), -80f, time).FromCurrent();
        tween.TweenProperty(musicCuePlayer, new NodePath(AudioStreamPlayer.PropertyName.VolumeDb), menuMusicPlayerVolumeDb, time).From(offDb);
        musicTween = tween;
        musicTimer = timer;
        musicCuePlayer.Play();
        await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
        musicCuePlayer.Stop();
        menuMusicPlayer.VolumeDb = menuMusicPlayerVolumeDb;
        menuMusicPlayer.Stream = menuUseOldMusic ? oldMenuMusic : newMenuMusic;
        menuMusicPlayer.Play();
    }

    private void ThreadServer()
    {
        while (true)
        {
            string line = OS.ReadStringFromStdIn().Trim();
            if (!string.IsNullOrEmpty(line))
                GameConsole.Instance.CallDeferred(nameof(GameConsole.OnCommand), line);
            Thread.Sleep(100);
        }
    }

    public async void LoadTextures()
    {
        UnloadMenu();
        State = GameState.Game;
        menuMusicPlayer.VolumeDb = -80f;
        menuMusicPlayer.Stop();
        menu = loadingScene.Instantiate();
        AddChild(menu);
        // errorTextLabel.Text = "Please Wait...\nLoading Textures...";
        // errorTextPanel.Show();
        if (!IInitScript.IsHeadless && !Settings.User.SkipPreloading)
            await PreloadShaders();
        if (Settings.User.TextureSizeLimit != lastSize)
            await EnforceMaxTexSizes(false);
        UnloadMenu();
        State = GameState.Menu;
        LoadMenu();
        WipWarning();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ResolutionEdit.SetResolution(Settings.User.Width, Settings.User.Height, (int)Settings.User.ScreenMode, Settings.User.ScreenId);
        if (IInitScript.IsTestClient)
        {
            Join(Settings.Server.ServerAddress, string.Empty);
        }
    }

    private async void WipWarning()
    {
        if (errorTextPanel.Visible)
        {
            await ToSignal(GetTree().CreateTimer(5d), SceneTreeTimer.SignalName.Timeout);
        }
        errorTextLabel.Text = Tr("GAME_WIP_WARNING").Replace("\\n", "\n");
        errorTextPanel.Show();
        // await ToSignal(GetTree().CreateTimer(5d), SceneTreeTimer.SignalName.Timeout);
        errorTextLabel.Text = Tr("GAME_WIP_WARNING").Replace("\\n", "\n") + Tr("GAME_WIP_WARNING_CLOSE").Replace("\\n", "\n");
        // errorTextPanel.Hide();
    }

    private async void UserReadSettingsError()
    {
        errorTextLabel.Text = Tr("SETTINGS_USER_READ_ERROR").Replace("\\n", "\n");
        errorTextPanel.Show();
        await ToSignal(GetTree().CreateTimer(10d), SceneTreeTimer.SignalName.Timeout);
        errorTextPanel.Hide();
    }

    private async void UserWriteSettingsError()
    {
        errorTextLabel.Text = Tr("SETTINGS_USER_WRITE_ERROR").Replace("\\n", "\n");
        errorTextPanel.Show();
        await ToSignal(GetTree().CreateTimer(10d), SceneTreeTimer.SignalName.Timeout);
        errorTextPanel.Hide();
    }

    public void SetEnv(Godot.Environment env)
    {
        Log.Print("SetEnv");
        environmentAsset = env;
        environment.Environment = env.Duplicate() as Godot.Environment;
        environment.Environment.VolumetricFogEnabled = Settings.User.VolumetricFog;
        if (xrInterface != null && xrInterface.IsInitialized())
        {
            environment.Environment.GlowEnabled = false;
            // environment.Environment.VolumetricFogEnabled = false;
            environment.Environment.SsaoEnabled = false;
        }
        var vp = GetViewport();
        float scl = vp.Scaling3DScale;
        // vp.Scaling3DScale = 0.1f;
        // vp.Scaling3DScale = scl;
        CallDeferred(nameof(ViewResize));
        // GetViewport().Scaling3DScale = 0.1f;
        // GetViewport().Scaling3DScale = 1.0f;
    }

    public Godot.Environment GetEnv()
    {
        return environmentAsset;
    }

    public void ViewResize()
    {
        mainViewport.Size = (Vector2I)GetTree().Root.GetTexture().GetSize();
        // mainViewport.Scaling3DScale = 0.1f;
        // mainViewport.Scaling3DScale = Settings.User.ScreenScale;
    }

    private Node SpawnMap(Variant variant)
    {
        PackedScene scn = maps[variant.AsInt32() % maps.Count];
        return scn.Instantiate();
    }

    private void ExitGame()
    {
        Log.Print("Goodbye!");
        GetTree().Quit();
    }

    public void LoadMenu()
    {
        if (IInitScript.IsServerOnly)
        {
            if (!NetworkManager.Instance.IsServer)
                Server(Settings.Server.ServerPort);
            State = GameState.Game;
            return;
        }
        if (IsInstanceValid(layoutEditorInstance))
        {
            layoutEditorInstance.QueueFree();
            layoutEditorInstance = null;
            State = GameState.Menu;
        }
        if (IsInstanceValid(workshopEditorInstance))
        {
            workshopEditorInstance.QueueFree();
            workshopEditorInstance = null;
            State = GameState.Menu;
        }
        if (menu == null)
        {
            menuMusicPlayer.Stop();
            UnloadMenu();
            menu = menuScene.Instantiate();
            AddChild(menu);
        }
        if (xrInterface != null && xrInterface.IsInitialized())
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            GetViewport().UseXR = true;
            GetViewport().UseHdr2D = false;
            // if (xrInterface is OpenXRInterface xr)
            //     Engine.PhysicsTicksPerSecond = (int)xr.DisplayRefreshRate;
            GetTree().PhysicsInterpolation = false;
            XRServer.WorldScale = 1.5f;
        }
        statusManager?.SetMenuActivity();
        mainViewTexture.Modulate = Colors.Transparent;
        mainViewTexture.Visible = false;
        mainViewport.AudioListenerEnable3D = false;
        statusManager.steam.DestroyLobby();
        AudioServer.SetBusEffectEnabled(menuBus.Index, eq6EffectEffect, false);
        statusManager.steam.RefreshWorkshopContent();
        Data.UnloadRoomScenes();
    }

    public void LoadLayoutEditor()
    {
        if (IsXR)
            return;
        UnloadMenu();
        State = GameState.Game;
        if (IsInstanceValid(layoutEditorInstance))
        {
            return;
        }
        Data.LoadRoomScenes();
        statusManager?.SetLevelEditorActivity();
        layoutEditorInstance = layoutEditor.Instantiate<Control>();
        AddChild(layoutEditorInstance, true);
    }

    public void LoadWorkshopEditor()
    {
        if (IsXR || !SteamManager.Supported)
            return;
        UnloadMenu();
        State = GameState.Game;
        if (IsInstanceValid(workshopEditorInstance))
        {
            return;
        }
        statusManager?.SetLevelEditorActivity();
        workshopEditorInstance = workshopEditor.Instantiate<Control>();
        AddChild(workshopEditorInstance, true);
    }

    public void UnloadMenu()
    {
        ViewResize();
        mainViewTexture.Modulate = Colors.White;
        mainViewTexture.Visible = true;
        mainViewport.AudioListenerEnable3D = true;
        // GetViewport().UseXR = false;
        if (!IsInstanceValid(menu))
            return;
        menu.QueueFree();
        menu = null;
        // _ = EnforceMaxTexSizes();
    }

    public void ReadSettings()
    {
        Settings.ReadSettings();
        InputSettings.Read();
        LangSelect.ReadTranslations();
        // menuUseOldMusic = Settings.User.OldMenuMusic;
        menuUseOldMusic = true;
        OnSettingsChanged?.Invoke();
        menuMusicPlayer.Stop();

        Log.NeverDupelicatePrefixes.Add(nameof(Log));
        Log.NeverDupelicatePrefixes.Add(nameof(PlayerConsole));
        Log.NeverDupelicatePrefixes.Add(nameof(GameConsole));
        Log.NeverDupelicatePrefixes.Add(nameof(NetworkManager));

        if (IInitScript.IsServerOnly || Settings.User.DebugMode)
        {
            Log.ShowLogType = Settings.Server.ShowLogType;
            Log.RichTextLogs = Settings.Server.RichTextLogs;
            Log.ShowTimestamps = Settings.Server.ShowTimestamps;
            Log.TimestampsInUTC = Settings.Server.TimestampsInUTC;
            Log.MaxDuplicateLogs = Settings.Server.MaxDuplicateLogs;
            Log.DuplicateLogsTimeWindow = Settings.Server.DuplicateLogsTimeWindow;
            Log.MinLogLevel = Settings.Server.MinLogLevel;
        }
        else
        {
            if (EngineDebugger.IsActive())
                Log.MinLogLevel = Log.LogLevel.Debug;
        }
    }

    public void WriteSettings()
    {
        // Settings.User.UserName = Username;
        if (SteamManager.Supported)
        {
            Username = Steam.GetPersonaName();
        }
        // Settings.User.MicInputName = AudioServer.InputDevice;
        // Settings.User.VolumeDb = float.IsFinite(menuVolumeDb) ? menuVolumeDb : -80f;
        // Settings.User.OldMenuMusic = menuUseOldMusic;
        var root = GetTree().Root;
        // Settings.User.MSAA = (int)GetTree().Root.Msaa3D;
        // Settings.User.Width = root.ContentScaleSize.X;
        // Settings.User.Height = root.ContentScaleSize.Y;
        // Settings.User.ScreenMode = root.Mode == Window.ModeEnum.Windowed ? 0 : root.Mode == Window.ModeEnum.Fullscreen ? 1 : 2;
        Settings.WriteUserSettings();
        InputSettings.Write();
        UpdateSettings();
        OnSettingsChanged?.Invoke();
    }

    public void LocalRoundState(RoundManager.RoundState state) => LocalMatrixState(false /*state == RoundManager.RoundState.Loading*/);

    public async void LocalMatrixState(bool loading)
    {
        if (IInitScript.IsServerOnly)
            return;
        if (this.State == GameState.Menu)
            return;
        if (xrInterface != null && xrInterface.IsInitialized())
        {
            if (loading)
            {
                LoadMenu();
                bool wasGame = this.State == GameState.Game;
                this.State = GameState.Loading;
                if (IsInstanceValid(RoundManager.Instance) && RoundManager.Instance.state != RoundManager.RoundState.Loading)
                {
                    this.State = GameState.Game;
                }
                mainViewTexture.Modulate = Colors.Transparent;
                mainViewTexture.Visible = false;
            }
            else
            {
                this.State = GameState.Game;
                mainViewTexture.Modulate = Colors.White;
                mainViewTexture.Visible = true;
                SetMenuCameraCurrent(false);
                UnloadMenu();
            }

            OnMenuMessage?.Invoke(string.Empty);
            return;
        }
        InMatrix = loading;
        OnMenuMessage?.Invoke(string.Empty);
        if (menuTween != null && menuTween.IsValid())
        {
            menuTween.Kill();
            /*
            menuTweenCount++;
            int count = menuTweenCount;
            await ToSignal(menuTween, Tween.SignalName.Finished);
            await ToSignal(GetTree().CreateTimer(0.05d * count), SceneTreeTimer.SignalName.Timeout);
            menuTweenCount--;
            */
        }
        menuTween = null;
        if (loading)
        {
            LoadMenu();
            bool wasGame = this.State == GameState.Game;
            this.State = GameState.Loading;
            if (IsInstanceValid(RoundManager.Instance) && RoundManager.Instance.state != RoundManager.RoundState.Loading)
            {
                this.State = GameState.Game;
            }
            for (int i = 0; i < 5; i++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!IsInstanceValid(RoundManager.Instance))
                    return;
                SetMenuCameraCurrent(false);
            }
            mainViewport.AudioListenerEnable3D = true;

            // mainViewTexture.Modulate = Colors.White;
            mainViewTexture.Visible = true;

            var tween = menu.CreateTween();
            menuTween = tween;
            tween.SetParallel();
            tween.SetEase(viewEase);
            tween.SetTrans(viewTransition);
            double time = viewTweenTime;
            double time2 = 0.5d;
            double time3 = 1.0d;
            if (menu is MainMenu m)
            {
                // m.ScreenUVAmount = 1f;
                m.ScreenColor = Colors.DarkGray;
                var spos = m.camera.points[1];
                var pos = m.camera.points[2];
                // tween.TweenProperty(menu, nameof(MainMenu.ScreenUVAmount), 0f, time)/*.From(0.5f)*/.SetDelay(time2);
                tween.TweenProperty(m.camera, new NodePath(Node3D.PropertyName.GlobalPosition), spos.GlobalPosition, time)/*.From(pos.GlobalPosition)*/.SetDelay(time2);
                tween.TweenProperty(m.camera, new NodePath(Node3D.PropertyName.GlobalRotation), spos.GlobalRotation, time-time3)/*.From(pos.GlobalRotation)*/.SetDelay(time2);
            }
            tween.TweenProperty(menu, nameof(MainMenu.ScreenColor), new Color(1f, 1f, 1f, 0.5f), time-time3).From(Colors.DarkGray).SetDelay(time2);
            tween.TweenProperty(mainViewTexture, new NodePath(CanvasItem.PropertyName.Modulate), Colors.Transparent, time2);
            await ToSignal(tween, Tween.SignalName.Finished);
            if (menuTween != tween || this.State == GameState.Menu)
                return;
            mainViewTexture.Visible = false;
        }
        else
        {
            this.State = GameState.Game;
            if (menu == null)
                return;
            SetMenuCameraCurrent(false);

            mainViewTexture.Modulate = Colors.Transparent;
            mainViewTexture.Visible = true;
            var tween = menu.CreateTween();
            menuTween = tween;
            tween.SetParallel();
            tween.SetEase(viewEase);
            tween.SetTrans(viewTransition);
            double time = viewTweenTime;
            double time2 = 1.0d;
            double time3 = 1.0d;
            if (menu is MainMenu m)
            {
                var spos = m.camera.points[1];
                var pos = m.camera.points[2];
                // tween.TweenProperty(menu, nameof(MainMenu.ScreenUVAmount), 1f, time).From(-1f);
                tween.TweenProperty(m.camera, new NodePath(Node3D.PropertyName.GlobalPosition), pos.GlobalPosition, time);//.From(spos.GlobalPosition);
                tween.TweenProperty(m.camera, new NodePath(Node3D.PropertyName.GlobalRotation), pos.GlobalRotation, time-time3);//.From(spos.GlobalRotation);
            }
            tween.TweenProperty(menu, nameof(MainMenu.ScreenColor), Colors.DarkGray, time-time3);
            tween.TweenProperty(mainViewTexture, new NodePath(CanvasItem.PropertyName.Modulate), Colors.White, time2).SetDelay(time);
            await ToSignal(tween, Tween.SignalName.Finished);
            // await ToSignal(GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
            if (menuTween != tween || this.State == GameState.Menu)
                return;
            mainViewTexture.Modulate = Colors.White;

            UnloadMenu();
        }
    }

    private bool SetMenuCameraCurrent(bool main)
    {
        if (menu is MainMenu m)
        {
            if (main)
            {
                InMatrix = false;
                m._Ready();
            }
            else
            {
                mainViewport.AudioListenerEnable3D = true;
            }
            if (m.meshInst.MaterialOverride is StandardMaterial3D std)
            {
                if (main)
                {
                    std.AlbedoTexture = m.viewport.GetTexture();
                }
                else
                {
                    std.AlbedoTexture = mainViewport.GetTexture();
                    mainViewport.Size = (Vector2I)GetTree().Root.GetTexture().GetSize();
                }
            }
            else
            {
                if (main)
                {
                    m.ScreenTexture = m.viewport.GetTexture();
                    // m.ScreenUVAmount = 0f;
                    m.ScreenColor = new Color(1f, 1f, 1f, 0.5f);
                }
                else
                {
                    m.ScreenTexture = mainViewport.GetTexture();
                    // m.ScreenUVAmount = 0f;
                    // m.ScreenColor = new Color(1f, 1f, 1f, 0.5f);
                    mainViewport.Size = (Vector2I)GetTree().Root.GetTexture().GetSize();

                    float ratio = (float)mainViewport.Size.X / mainViewport.Size.Y;
                    if (ratio > 1f)
                        m.MeshSize = new Vector2(1f, 1f / ratio);
                    else
                        m.MeshSize = new Vector2(ratio, 1f);
                    // m.MeshSize = new Vector2(ratio, 1f);
                    // m.MeshSize = new Vector2(1f, 1f / ratio);
                }
            }
            return true;
        }
        return false;
    }

    public async Task PreloadShaders()
    {
        var preloader = new ShaderPreloader();
        menu.AddChild(preloader);
        while (IsInstanceValid(preloader))
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        await ToSignal(GetTree().CreateTimer(1d), SceneTreeTimer.SignalName.Timeout);
    }

    #region Texture Scaler
    public static CancellationTokenSource cts;
    public static int lastSize = 0;

    public static async Task EnforceMaxTexSizes(bool threaded)
    {
        return;
        threaded = false;
        convertedTextures.Clear();
        convertedImgTexs.Clear();
        convertedToOgTexs.Clear();
        cts?.Cancel();
        cts = new CancellationTokenSource();
        int size = Settings.User.TextureSizeLimit;
        await EnforceMaxTexSize(size, threaded, "res://textures/", cts.Token);
        await EnforceMaxTexSize(size, threaded, "res://models/", cts.Token);
        foreach (var kvp in convertedToOgTexs)
        {
            RenderingServer.TextureReplace(kvp.Value.GetRid(), kvp.Key.GetRid());
        }
        convertedToOgTexs.Clear();
        lastSize = size;
    }

    public static ConcurrentBag<string> convertedTextures = new ConcurrentBag<string>();
    public static ConcurrentDictionary<Texture2D, Texture2D> convertedToOgTexs = new ConcurrentDictionary<Texture2D, Texture2D>();
    public static ConcurrentDictionary<string, Texture2D> convertedImgTexs = new ConcurrentDictionary<string, Texture2D>();
    public static ConcurrentDictionary<string, Texture2D> ogImgTexs = new ConcurrentDictionary<string, Texture2D>();

    public static bool IsImageFile(string ext)
    {
        return ext.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            ext.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            ext.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            ext.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            ext.EndsWith(".exr", StringComparison.OrdinalIgnoreCase);
    }

    public static string StripImportFile(string ext)
    {
        if (ext.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
            return ext.Substring(0, ext.Length - ".import".Length);
        return ext;
    }

    public static bool IsCompressed(Image.Format fmt)
    {
        return fmt > Image.Format.Rgbe9995;
    }

    public static Image.CompressMode GetCompressMode(Image.Format fmt)
    {
        if ((fmt >= Image.Format.Dxt1 && fmt <= Image.Format.RgtcRg) || fmt == Image.Format.Dxt5RaAsRg)
        {
            return Image.CompressMode.S3Tc;
        }
        else if (fmt >= Image.Format.BptcRgba && fmt <= Image.Format.BptcRgbfu)
        {
            return Image.CompressMode.Bptc;
        }
        else if (fmt == Image.Format.Etc)
        {
            return Image.CompressMode.Etc;
        }
        else if (fmt >= Image.Format.Etc2R11 && fmt <= Image.Format.Etc2RaAsRg)
        {
            return Image.CompressMode.Etc2;
        }
        else if (fmt >= Image.Format.Astc4X4 && fmt <= Image.Format.Astc8X8Hdr)
        {
            return Image.CompressMode.Astc;
        }
        else
        {
            return Image.CompressMode.Max;
        }
    }

    public static void ResizeTextureFile((string fpath, int size) args)
    {
        if (convertedTextures.Contains(args.fpath))
            return;
        using var ogRes = ResourceLoader.Load(args.fpath, cacheMode: ResourceLoader.CacheMode.Ignore);
        var res = ResourceLoader.Load(args.fpath, cacheMode: ResourceLoader.CacheMode.Replace);
        if (IsInstanceValid(res) && IsInstanceValid(ogRes) && res is Texture2D tex && ogRes is CompressedTexture2D ogTex && (res is CompressedTexture2D || res is PortableCompressedTexture2D))
        {
            // GD.PrintS(tex.ResourcePath, tex.GetReferenceCount());
            if (args.size <= 0 || (ogTex.GetWidth() <= args.size && ogTex.GetHeight() <= args.size) || ogTex.GetWidth() != ogTex.GetHeight())
            {
                convertedTextures.Add(args.fpath);
            }
            else
            {
                Image img = ogTex.GetImage();
                bool hasMips = img.HasMipmaps();
                bool hasCompress = img.IsCompressed();
                Image.Format fmt = img.GetFormat();
                // img.ClearMipmaps();
                img.Decompress();
                img.Resize(args.size, args.size, Image.Interpolation.Nearest);
                // img.ResizeToPo2(true, Image.Interpolation.Bilinear);
                // if (hasMips)
                //     img.GenerateMipmaps();
                if (hasCompress)
                    img.Compress(GetCompressMode(fmt));
                else
                    img.Convert(fmt);
                ImageTexture ctex = ImageTexture.CreateFromImage(img);
                RenderingServer.TextureReplace(tex.GetRid(), ctex.GetRid());
                // convertedToOgTexs.TryAdd(ctex, tex);
                // ogImgTexs.TryAdd(args.fpath, tex);
                // convertedImgTexs.TryAdd(args.fpath, ctex);
                convertedTextures.Add(args.fpath);
            }
        }
        else
        {
            convertedTextures.Add(args.fpath);
        }
    }

    public static async Task EnforceMaxTexSize(int size, bool threaded, string path = "res://", CancellationToken token = default)
    {
        var da = DirAccess.Open(path);
        if (da == null)
        {
            GD.PrintErr(DirAccess.GetOpenError());
            return;
        }
        foreach (var dir in da.GetDirectories())
        {
            if (!dir.StartsWith('.'))
                await EnforceMaxTexSize(size, threaded, $"{path}{dir}/", token);
        }
        var files = da.GetFiles().Select(StripImportFile).Distinct().Where(IsImageFile).Select(x => (path + x, size)).ToArray();
        if (threaded)
        {
            await Task.Run(async () =>
            {
                var p = Parallel.ForEach(files, new ParallelOptions()
                {
                    CancellationToken = token,
                }, ResizeTextureFile);
                while (!p.IsCompleted)
                    await Task.Delay(100, token);
            }, token);
        }
        else
        {
            var tree = (SceneTree)Engine.GetMainLoop();
            foreach (var file in files)
            {
                ResizeTextureFile(file);
            }
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
    #endregion

    public async Task PreLoadAssetsAsync(CancellationToken token)
    {
        // maps.Clear();
        // foreach (var m in mapPaths)
        {
            // maps.Add(await GameData.LoadSceneAsync(m));
        }
        await Data.LoadRoomScenesAsync(token);
    }

    private void Server_Stop()
    {
        foreach (var node in mapSpawner.GetNode(mapSpawner.SpawnPath).GetChildren())
        {
            node.QueueFree();
        }
        State = GameState.Menu;
        LoadMenu();
        SetMenuCameraCurrent(true);
    }

    private void Server_Start()
    {
        State = GameState.Loading;
        mapSpawner.Spawn(gameMap);
    }

    private void Close()
    {
        if (NetworkManager.Instance.ConnectionState == NetworkManager.MultiplayerConnectionState.Connected)
            return;
        NetworkManager.Instance.Shutdown();
        InputManager.Instance.ChangeActionSet("Menu");
    }

    private void Client_Connect()
    {
        // OnMenuMessage?.Invoke(string.Empty);
        // errorTextPanel.Hide();
        State = GameState.Loading;
    }

    private void Client_Disconnect(NetworkManager.DisconnectReason reason)
    {
        foreach (var node in mapSpawner.GetNode(mapSpawner.SpawnPath).GetChildren())
        {
            node.QueueFree();
        }
        State = GameState.Menu;
        LoadMenu();
        SetMenuCameraCurrent(true);
        if (reason == NetworkManager.DisconnectReason.User)
        {
            OnMenuMessage?.Invoke(string.Empty);
            return;
        }
        // errorTextLabel.Text = $"{Tr("NETWORK_DISCONNECTED")}\n{reason}";
        OnMenuMessage?.Invoke($"{Tr("NETWORK_DISCONNECTED")}");
        // errorTextPanel.Show();
        // if (IsTestClient && reason == NetworkManager.DisconnectReason.Failed)
        if (IInitScript.IsTestClient)
        {
            Join(Settings.Server.ServerAddress, string.Empty);
        }
    }

    public async void Host(int port)
    {
        loadingTokenSource?.Cancel();
        loadingTokenSource = new CancellationTokenSource();
        State = GameState.Loading;
        // errorTextLabel.Text = $"{Tr("LOADING_TEXT")}\n{Tr("PLEASE_WAIT_TEXT")}";
        OnMenuMessage?.Invoke($"{Tr("LOADING_TEXT")}\n{Tr("PLEASE_WAIT_TEXT")}");
        // errorTextPanel.Show();
        await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        await PreLoadAssetsAsync(loadingTokenSource.Token);
        await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        if (SteamManager.Supported)
            NetworkManager.Instance.HostSteam(Settings.Server.ServerName, steamPublicLobby ? Steam.LobbyType.Public : Steam.LobbyType.FriendsOnly, Settings.Server.MaxPlayers);
        else
            NetworkManager.Instance.HostEnet(port, Settings.Server.MaxPlayers);
    }

    public async void Server(int port)
    {
        loadingTokenSource?.Cancel();
        loadingTokenSource = new CancellationTokenSource();
        State = GameState.Loading;
        await PreLoadAssetsAsync(loadingTokenSource.Token);
        NetworkManager.Instance.ServerEnet(port, Settings.Server.MaxPlayers);
    }

    public async void Join(string addr, string serverName)
    {
        loadingTokenSource?.Cancel();
        loadingTokenSource = new CancellationTokenSource();
        State = GameState.Loading;
        OnMenuMessage?.Invoke($"{Tr("LOADING_TEXT")}\n{Tr("PLEASE_WAIT_TEXT")}");
        await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        await PreLoadAssetsAsync(loadingTokenSource.Token);
        OnMenuMessage?.Invoke($"{Tr("NETWORK_CONNECTING")}\n{Tr("PLEASE_WAIT_TEXT")}");
        await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
        // LoadMap(scn =>
        // {
            // errorTextLabel.Text = $"{Tr("NETWORK_CONNECTING")}\n{Tr("PLEASE_WAIT_TEXT")}";
            // errorTextPanel.Show();
            if (string.IsNullOrWhiteSpace(addr))
                NetworkManager.Instance.ClientEnet(servername: serverName);
            else if (addr.Contains('.'))
                NetworkManager.Instance.ClientEnet(addr, serverName);
            else if (SteamManager.Supported && ulong.TryParse(addr, out ulong id))
                NetworkManager.Instance.ClientSteam(id);
        // }, Close);
    }

    public void JoinSteamId(ulong steamId)
    {
        if (SteamManager.Supported)
        {
            State = GameState.Loading;
            OnMenuMessage?.Invoke($"{Tr("NETWORK_CONNECTING")}\n{Tr("PLEASE_WAIT_TEXT")}");
            Data.LoadRoomScenes();
            NetworkManager.Instance.OnSteamLobbyJoined(steamId);
        }
    }

    public void Kicked(string message)
    {
        NetworkManager.Instance.Shutdown(NetworkManager.DisconnectReason.Kicked);
        // errorTextLabel.Text = $"{Tr("NETWORK_KICKED")}\n{message}";
        OnMenuMessage?.Invoke($"{Tr("NETWORK_KICKED")}\n{message}");
        // errorTextPanel.Show();
    }

    public void Banned(string message)
    {
        NetworkManager.Instance.Shutdown(NetworkManager.DisconnectReason.Kicked);
        // errorTextLabel.Text = $"{Tr("NETWORK_BANNED")}\n{message}";
        OnMenuMessage?.Invoke($"{Tr("NETWORK_BANNED")}\n{message}");
        // errorTextPanel.Show();
    }

    public void MissingMods(string message)
    {
        NetworkManager.Instance.Shutdown(NetworkManager.DisconnectReason.Kicked);
        // errorTextLabel.Text = $"{Tr("NETWORK_BANNED")}\n{message}";
        OnMenuMessage?.Invoke($"{Tr("NETWORK_MISSING_MODS")}\n{message}");
        // errorTextPanel.Show();
    }
}

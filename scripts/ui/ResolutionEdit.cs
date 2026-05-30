using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ResolutionEdit : OptionButton
{
    [Export]
    public bool isSubwindow = false;
    public List<Vector4I> resolutions = new List<Vector4I>();

    public override void _Ready()
    {
        GetPopup().MinSize = Vector2I.Zero;
        VisibilityChanged += OnChanged;
        Pressed += Press;
        ItemSelected += Change;
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= OnChanged;
        Pressed -= Press;
        ItemSelected -= Change;
    }

    public static Vector4I GetResolution()
    {
        var tree = IInitScript.SceneTree;
        var vec = new Vector4I();
        vec.X = tree.Root.Size.X;
        vec.Y = tree.Root.Size.Y;
        vec.Z = tree.Root.CurrentScreen;
        vec.W = tree.Root.Mode == Window.ModeEnum.ExclusiveFullscreen ? 1 : 0;
        return vec;
    }

    public static void SetResolution(int width, int height, int mode, int screen)
    {
        // Log.PrintInfo($"SetResolution({width}, {height}, {mode}, {screen})");

        var tree = IInitScript.SceneTree;

        tree.Root.Borderless = mode != 0;
        tree.Root.Unresizable = mode != 0;

        if (screen == -1)
            screen = DisplayServer.GetPrimaryScreen();
        tree.Root.CurrentScreen = screen;

        float fps = DisplayServer.ScreenGetRefreshRate(screen);

        // tree.PhysicsInterpolation = fps <= 0f || fps > Engine.PhysicsTicksPerSecond;

        if (mode != 0)
        {
            var size = DisplayServer.ScreenGetSize(screen);
            if ((width <= 0 || height <= 0) || (width > size.X || height > size.Y))
            {
                width = size.X;
                height = size.Y;
            }
            tree.Root.Mode = mode == 1 ? Window.ModeEnum.Fullscreen : Window.ModeEnum.ExclusiveFullscreen;
            // tree.Root.InitialPosition = Window.WindowInitialPosition.Absolute;
            // tree.Root.Position = DisplayServer.ScreenGetPosition(screen);
            tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
            tree.Root.Size = size;
            tree.Root.ContentScaleSize = new Vector2I(width, height);

            tree.Root.CurrentScreen = screen;
        }
        else
        {
            if (tree.Root.Mode != Window.ModeEnum.Windowed)
            {
                tree.Root.Mode = Window.ModeEnum.Windowed;
                tree.Root.Size = new Vector2I(1280, 720);
            }
            tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
            tree.Root.ContentScaleSize = Vector2I.Zero;
        }
    }

    private void Press()
    {
        if (isSubwindow && !GetViewport().GuiEmbedSubwindows)
            GetPopup().Position = DisplayServer.MouseGetPosition();
    }

    private void Change(long index)
    {
        if (!IsVisibleInTree())
            return;
        if (index != 0)
        {
            var vec = resolutions[(int)index-1];
            Settings.User.Width = vec.X;
            Settings.User.Height = vec.Y;
            Settings.User.ScreenMode = (Settings.WindowMode)(vec.W == 0 ? 1 : 2);
            Settings.User.ScreenId = vec.Z;
        }
        else
        {
            Settings.User.Width = 0;
            Settings.User.Height = 0;
            Settings.User.ScreenMode = 0;
            Settings.User.ScreenId = -1;
        }
        Settings.Modified = true;
        // SetResolution(Settings.User.Width, Settings.User.Height, (int)Settings.User.ScreenMode, Settings.User.ScreenId);
    }

    private void OnChanged()
    {
        if (!IsVisibleInTree())
            return;

        var max = DisplayServer.ScreenGetSize();
        var size = GetTree().Root.ContentScaleSize;

        resolutions.Clear();
        AddResPreset(640, 480);
        AddResPreset(800, 600);
        AddResPreset(960, 540);
        AddResPreset(540, 960);
        AddResPreset(1280, 720);
        AddResPreset(1280, 800);
        AddResPreset(1366, 768);
        AddResPreset(1600, 900);
        AddResPreset(1440, 1080);
        AddResPreset(1920, 1080);
        AddResPreset(2560, 1440);
        AddResPreset(max.X, max.Y);
        if (GetTree().Root.Borderless)
            AddRes(size, exclusive: GetTree().Root.Mode == Window.ModeEnum.ExclusiveFullscreen);

        Clear();
        AddItem("Windowed");
        foreach (var res in resolutions)
        {
            string type = res.W == 0 ? "Borderless" : "Exclusive";
            if (res.Z == -1)
                AddItem($"{res.X}x{res.Y} {type}");
            else
                AddItem($"{res.X}x{res.Y} {type} (Screen {res.Z})");
        }

        if (GetTree().Root.Borderless)
            Selected = resolutions.FindIndex(x => x.X == size.X && x.Y == size.Y && x.W == (GetTree().Root.Mode == Window.ModeEnum.ExclusiveFullscreen ? 1 : 0)) + 1;
        else
            Selected = 0;

        // GetPopup().MinSize = Vector2I.Zero;
        // GetPopup().MaxSize = new Vector2I((int)Size.X, (int)Size.Y * 5);
    }

    private void AddResPreset(int x, int y, int screen = -1)
    {
        AddRes(new Vector2I(x, y), screen, false);
        AddRes(new Vector2I(x, y), screen, true);
    }

    private void AddRes(int x, int y, int screen = -1, bool exclusive = false)
    {
        AddRes(new Vector2I(x, y), screen, exclusive);
    }

    private void AddRes(Vector2I res, int screen = -1, bool exclusive = false)
    {
        var max = DisplayServer.ScreenGetSize(screen);
        if (res <= max && !resolutions.Any(x => x.X == res.X && x.Y == res.Y && x.Z == screen && x.W == (exclusive ? 1 : 0)))
        {
            resolutions.Add(new Vector4I(res.X, res.Y, screen, exclusive ? 1 : 0));
        }
    }
}

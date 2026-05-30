using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TextureLimitEdit : OptionButton
{
    public bool isSubwindow = false;
    public List<int> resolutions = new List<int>();

    public override void _Ready()
    {
        GetPopup().MinSize = Vector2I.Zero;
        VisibilityChanged += OnChanged;
        Pressed += Press;
        ItemSelected += Change;
        OnChanged();
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= OnChanged;
        Pressed -= Press;
        ItemSelected -= Change;
    }

    public static void SetLimit(int limit)
    {
        if (Settings.User.TextureSizeLimit == limit)
            return;
        Settings.User.TextureSizeLimit = limit;
        MenuManager.Instance.LoadTextures();
    }

    private void Press()
    {
        if (isSubwindow && !GetViewport().GuiEmbedSubwindows)
            GetPopup().Position = DisplayServer.MouseGetPosition();
    }

    private void Change(long index)
    {
        // var vec = resolutions[(int)index];
        SetLimit(GetItemId((int)index));
        MenuManager.Instance.WriteSettings();
    }

    private void OnChanged()
    {
        var size = Settings.User.TextureSizeLimit;

        resolutions.Clear();
        resolutions.Add(0);
        resolutions.Add(256);
        resolutions.Add(512);
        resolutions.Add(1024);

        Clear();
        AddItem("Low", 256);
        AddItem("Medium", 512);
        AddItem("High", 1024);
        AddItem("Very High", 0);
        // foreach (var res in resolutions.Skip(1))
        // {
        //     AddItem($"{res}x{res}");
        // }

        Selected = GetItemIndex(size);
        // Selected = resolutions.FindIndex(x => x == size);
    }
}

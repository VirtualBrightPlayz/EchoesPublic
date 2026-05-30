using Godot;

public partial class VSyncSelect : OptionButton
{
    public override void _Ready()
    {
        Selected = (int)Settings.User.VSyncMode;
        ItemSelected += OnChanged;
        OnChanged(Selected);
    }

    public override void _ExitTree()
    {
        ItemSelected -= OnChanged;
    }

    private void OnChanged(long index)
    {
        if (!IsVisibleInTree())
            return;
        Settings.User.VSyncMode = (DisplayServer.VSyncMode)index;
        Settings.Modified = true;
        DisplayServer.WindowSetVsyncMode(Settings.User.VSyncMode);
    }
}
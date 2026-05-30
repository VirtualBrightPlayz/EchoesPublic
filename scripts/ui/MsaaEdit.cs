using Godot;
using System;

public partial class MsaaEdit : OptionButton
{
    public override void _Ready()
    {
        Selected = (int)Settings.User.MSAA;
        ItemSelected += OnChanged;
    }

    public override void _ExitTree()
    {
        ItemSelected -= OnChanged;
    }

    private void OnChanged(long index)
    {
        if (!IsVisibleInTree())
            return;
        Settings.User.MSAA = (Viewport.Msaa)index;
        Settings.Modified = true;
    }
}

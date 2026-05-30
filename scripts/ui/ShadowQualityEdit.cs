using Godot;
using System;

public partial class ShadowQualityEdit : OptionButton
{
    public override void _Ready()
    {
        ItemSelected += OnChanged;
        Selected = GetItemIndex((int)Settings.User.ShadowQuality);
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
        Settings.User.ShadowQuality = (Settings.GenericQuality)GetItemId((int)index);
        Settings.Modified = true;
    }
}

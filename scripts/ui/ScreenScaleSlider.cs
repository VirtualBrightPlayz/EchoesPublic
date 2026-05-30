using System;
using Godot;

public partial class ScreenScaleSlider : HSlider
{
    public override void _Ready()
    {
        Value = Settings.User.ScreenScale;
        ValueChanged += OnVal;
    }

    public override void _ExitTree()
    {
        ValueChanged -= OnVal;
    }

    private void OnVal(double value)
    {
        if (!IsVisibleInTree())
            return;
        Settings.User.ScreenScale = (float)value;
        Settings.Modified = true;
    }
}
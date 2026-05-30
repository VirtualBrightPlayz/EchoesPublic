using Godot;
using System;

public partial class ShadowsEdit : CheckBox
{
    public override void _Ready()
    {
        ButtonPressed = Settings.User.Shadows;
        Pressed += OnChanged;
        OnChanged();
    }

    public override void _ExitTree()
    {
        Pressed -= OnChanged;
    }

    private void OnChanged()
    {
        if (!IsVisibleInTree())
            return;
        Settings.User.Shadows = ButtonPressed;
        Settings.Modified = true;
    }
}

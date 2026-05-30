using Godot;
using System;

public partial class SnapTurnEdit : CheckBox
{
    public override void _Ready()
    {
        ButtonPressed = Settings.User.SnapTurn;
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
        Settings.User.SnapTurn = ButtonPressed;
        Settings.Modified = true;
    }
}

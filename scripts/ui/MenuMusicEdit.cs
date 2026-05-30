using Godot;
using System;

public partial class MenuMusicEdit : CheckBox
{
    public override void _Ready()
    {
        ButtonPressed = MenuManager.Instance.menuUseOldMusic;
        Pressed += OnChanged;
    }

    public override void _ExitTree()
    {
        Pressed -= OnChanged;
    }

    private void OnChanged()
    {
        // MenuManager.Instance.menuUseOldMusic = ButtonPressed;
        // MenuManager.Instance.StopMusic();
    }
}

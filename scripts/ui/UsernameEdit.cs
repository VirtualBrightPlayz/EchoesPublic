using Godot;
using System;

public partial class UsernameEdit : LineEdit
{
    [Export]
    public Label label;

    public override void _Ready()
    {
        Text = Settings.User.UserName;
        TextChanged += OnChanged;
        if (SteamManager.Supported)
        {
            Visible = false;
            if (IsInstanceValid(label))
            {
                label.Visible = false;
            }
        }
    }

    public override void _ExitTree()
    {
        TextChanged -= OnChanged;
    }

    private void OnChanged(string newText)
    {
        if (!IsVisibleInTree())
            return;
        Settings.User.UserName = newText;
        Settings.Modified = true;
    }
}

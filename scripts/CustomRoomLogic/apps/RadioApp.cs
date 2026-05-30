using Godot;

[GlobalClass]
public partial class RadioApp : Node
{
    [Export]
    public BaseButton appIcon;
    [Export]
    public Window appWindow;

    public override void _Ready()
    {
        base._Ready();
        appIcon.Pressed += appWindow.Show;
        appWindow.CloseRequested += appWindow.Hide;
    }
}
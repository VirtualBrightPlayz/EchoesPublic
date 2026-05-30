using System.Linq;
using Godot;

public partial class Launcher : Control
{
    [Export]
    public OptionButton gfxApiBtn;

    public override void _Ready()
    {
        if (OS.GetCmdlineArgs().Contains("--nolauncher"))
            GetTree().ChangeSceneToFile("res://scenes/menu_manager.tscn");
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventScreenDrag drag)
        {
            InputEventMouseMotion motion = new InputEventMouseMotion();
            motion.ButtonMask = 0;
            motion.Position = drag.Position;
            GetViewport().PushInput(motion);
            return;
        }

        if (ev is InputEventScreenTouch touch)
        {
            InputEventMouseButton motion = new InputEventMouseButton();
            motion.ButtonIndex = MouseButton.Left;
            motion.ButtonMask = touch.Pressed ? MouseButtonMask.Left : 0;
            motion.Pressed = touch.Pressed;
            motion.Position = touch.Position;
            GetViewport().PushInput(motion);
            return;
        }
    }

    public void RunNow()
    {
        switch (gfxApiBtn.Selected)
        {
            case 0:
                RunVulkan();
                break;
            case 1:
                RunD3D12();
                break;
            case 2:
                RunOpenGL();
                break;
        }
    }

    public void RunVulkan()
    {
        OS.SetRestartOnExit(true, new[] { "--nolauncher", "--rendering-driver", "vulkan" });
        GetTree().Quit();
    }

    public void RunD3D12()
    {
        OS.SetRestartOnExit(true, new[] { "--nolauncher", "--rendering-driver", "d3d12" });
        GetTree().Quit();
    }

    public void RunOpenGL()
    {
        OS.SetRestartOnExit(true, new[] { "--nolauncher", "--rendering-driver", "opengl3" });
        GetTree().Quit();
    }
}

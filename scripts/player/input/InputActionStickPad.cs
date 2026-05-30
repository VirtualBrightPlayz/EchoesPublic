using Godot;

[Tool]
[GlobalClass]
public partial class InputActionStickPad : Resource
{
    [Export] public StringName GodotAction;
    [Export] public StringName GodotActionPositiveX;
    [Export] public StringName GodotActionNegativeX;
    [Export] public StringName GodotActionPositiveY;
    [Export] public StringName GodotActionNegativeY;
    [Export] public string LocaleKey;
    [Export] public InputMode Mode;

    public enum InputMode
    {
        AbsoluteMouse,
        JoystickMove,
    }

    public static string GetVdfInputMode(InputMode mode)
    {
        switch (mode)
        {
            default:
                return string.Empty;
            case InputMode.AbsoluteMouse:
                return "absolute_mouse";
            case InputMode.JoystickMove:
                return "joystick_move";
        }
    }

    public string GetVdf()
    {
        string data = string.Empty;
        data += $"\t\t\t\t\"{ResourceName}\"\n";
        data += "\t\t\t\t{\n";
        data += $"\t\t\t\t\t\"title\"\t\"#{LocaleKey}\"\n";
        data += $"\t\t\t\t\t\"input_mode\"\t\"{GetVdfInputMode(Mode)}\"\n";
        data += "\t\t\t\t}\n";
        return data;
    }
}

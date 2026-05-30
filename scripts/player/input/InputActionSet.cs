using Godot;

[Tool]
[GlobalClass]
public partial class InputActionSet : Resource
{
    [Export] public string LocaleKey;
    [Export] public bool LockMouse = false;
    [Export] public Godot.Collections.Array<InputActionStickPad> StickPadActions = [];
    [Export] public Godot.Collections.Array<InputActionAnalog> AnalogActions = [];
    [Export] public Godot.Collections.Array<InputActionDigital> DigitalActions = [];

    public string GetVdf()
    {
        string data = string.Empty;
        data += $"\t\t\"{ResourceName}\"\n";
        data += "\t\t{\n";
        data += $"\t\t\t\"title\"\t\"#{LocaleKey}\"\n";

        data += "\t\t\t\"StickPadGyro\"\n";
        data += "\t\t\t{\n";
        for (int i = 0; i < StickPadActions.Count; i++)
        {
            data += StickPadActions[i].GetVdf();
        }
        data += "\t\t\t}\n";

        data += "\t\t\t\"AnalogTrigger\"\n";
        data += "\t\t\t{\n";
        for (int i = 0; i < AnalogActions.Count; i++)
        {
            data += AnalogActions[i].GetVdf();
        }
        data += "\t\t\t}\n";

        data += "\t\t\t\"Button\"\n";
        data += "\t\t\t{\n";
        for (int i = 0; i < DigitalActions.Count; i++)
        {
            data += DigitalActions[i].GetVdf();
        }
        data += "\t\t\t}\n";

        data += "\t\t}\n";
        return data;
    }
}

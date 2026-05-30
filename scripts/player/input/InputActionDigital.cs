using Godot;

[Tool]
[GlobalClass]
public partial class InputActionDigital : Resource
{
    [Export] public StringName GodotAction;
    [Export] public string LocaleKey;

    public string GetVdf()
    {
        return $"\t\t\t\t\"{ResourceName}\"\t\"#{LocaleKey}\"\n";
    }
}

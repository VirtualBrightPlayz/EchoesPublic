using System.Reflection;

public class Setting
{
    public Setting(string name, string group, string tooltip, MemberInfo member, ControlType controlType, object settingsObject)
    {
        Name = name;
        Group = group;
        Tooltip = tooltip;
        Member = member;
        ControlType = ControlType;
        SettingsObject = settingsObject;
    }

    public Setting()
    {

    }

    public MemberInfo Member { get; set; }

    public string Group { get; set; }

    public string Name { get; set; }

    public string Tooltip { get; set; }

    public ControlType ControlType { get; set; }

    public object SettingsObject { get; set; }
}

public enum ControlType
{
    Text,
    Slider,
    Dropdown,
    Checkbox,
    Button,
}
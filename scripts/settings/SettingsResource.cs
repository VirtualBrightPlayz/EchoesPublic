using Godot;
using System.Reflection;

[Tool]
public partial class SettingsResource : Resource
{
    [Export]
    public string Group { get; set; }

    [Export]
    public string Name { get; set; }

    [Export]
    public string MemberName { get; set; }

    [Export]
    public string Tooltip { get; set; }

    [Export]
    public ControlType ControlType { get; set; }

    [Export]
    public string ObjectFullClass { get; set; }

    [Export]
    public string ObjectPropertyName { get; set; }

    public object SettingsObject
    {
        get
        {
            return Assembly.GetExecutingAssembly().GetType(ObjectFullClass).GetProperty(ObjectPropertyName).GetValue(null);
        }
    }

    public MemberInfo Info
    {
        get
        {
            return SettingsObject.GetType().GetMember(MemberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)[0];
        }
    }
}

using Godot;

[GlobalClass]
public partial class LogicSwitch : Node
{
    [Signal]
    public delegate void ValueOnEventHandler();
    [Signal]
    public delegate void ValueOffEventHandler();

    public void TriggerValue(int value)
    {
        if (value == 0)
        {
            EmitSignalValueOff();
        }
        else
        {
            EmitSignalValueOn();
        }
    }
}
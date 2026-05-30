using Godot;
using System;

public partial class InputRemapDebug : Button
{
    [Export]
    public bool remapping = false;
    [Export]
    public string action;

    public override void _EnterTree()
    {
        InputSettings.Read();
    }

    public override void _ExitTree()
    {
        InputSettings.Write();
    }

    public override void _GuiInput(InputEvent ev)
    {
        if (InputMap.HasAction(action))
        {
            if (ev is InputEventMouseButton || ev is InputEventKey)
            {
                GD.Print(ev);
                foreach (var ev2 in InputMap.ActionGetEvents(action))
                {
                    if (ev2 is InputEventMouseButton || ev2 is InputEventKey)
                        InputMap.ActionEraseEvent(action, ev2);
                }
                InputMap.ActionAddEvent(action, ev);
                AcceptEvent();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed(action))
        {
            GD.PrintS(action, InputMap.ActionGetEvents(action));
            // GD.PrintS("Pressed", action);
        }
    }
}

using Godot;
using System;

[GlobalClass]
public partial class InputRemapButton : Button
{
    [Export]
    public string action;

    public bool IsReset => action.Equals("RESET");
    public bool Valid => InputMap.HasAction(action);

    public bool focus = false;

    public override void _GuiInput(InputEvent ev)
    {
        if (Valid && HasFocus() && new Rect2(Vector2.Zero, Size).HasPoint(GetLocalMousePosition()))
        {
            if ((ev is InputEventMouseButton mouseButton &&
                mouseButton.ButtonIndex != MouseButton.WheelUp &&
                mouseButton.ButtonIndex != MouseButton.WheelDown &&
                mouseButton.ButtonIndex != MouseButton.WheelLeft &&
                mouseButton.ButtonIndex != MouseButton.WheelRight) || ev is InputEventKey)
            {
                foreach (var ev2 in InputMap.ActionGetEvents(action))
                {
                    if (ev2 is InputEventMouseButton || ev2 is InputEventKey)
                        InputMap.ActionEraseEvent(action, ev2);
                }
                AcceptEvent();
                if (ev is InputEventKey key && key.PhysicalKeycode == Key.Escape)
                {
                    InputSettings.Write();
                    return;
                }
                InputMap.ActionAddEvent(action, ev);
                InputSettings.Write();
            }
        }
    }

    public override void _Ready()
    {
        MouseEntered += Enter;
        MouseExited += Exit;
        Pressed += Press;
        VisibilityChanged += Exit;
        if (string.IsNullOrWhiteSpace(action))
        {
            action = Name;
        }
    }

    private void Press()
    {
        if (IsReset)
        {
            InputSettings.Reset();
        }
    }

    private void Enter()
    {
        if (new Rect2(Vector2.Zero, Size).HasPoint(GetLocalMousePosition()))
            CallDeferred(MethodName.GrabFocus);
    }

    private void Exit()
    {
        if (!new Rect2(Vector2.Zero, Size).HasPoint(GetLocalMousePosition()))
            CallDeferred(MethodName.ReleaseFocus);
    }

    public override void _Process(double delta)
    {
        // if (InputMap.HasAction(action) && new Rect2(Vector2.Zero, Size).HasPoint(GetLocalMousePosition()))
        //     GrabFocus();
        // else
        //     ReleaseFocus();
        if (InputMap.HasAction(action))
        {
            Text = $"{action}: NONE";
            foreach (var ev in InputMap.ActionGetEvents(action))
            {
                switch (ev)
                {
                    case InputEventMouseButton mouseButton:
                        Text = new InputSettings.InputMapping(action, mouseButton).ToString();
                        break;
                    case InputEventKey key:
                        Text = new InputSettings.InputMapping(action, key).ToString();
                        break;
                }
            }
        }
    }
}

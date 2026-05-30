using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
[GlobalClass]
public partial class InputManager : SingletonNode3D<InputManager>, IInputSource
{
    [ExportToolButton("Generate VDF")] public Callable ToolGenerateVdf => Callable.From(GenerateVdf);
    [Export] public Godot.Collections.Array<InputActionSet> ActionSets = [];
    [Export] public int CurrentSetIndex = 0;
    public InputActionSet CurrentSet => ActionSets[CurrentSetIndex];
    public bool IsVR => IInitScript.Instance.IsXR;
    public Vector2 MouseMotion = Vector2.Zero;
    public bool HasMotion = false;
    public bool IsLocked = false;

    public override void _Input(InputEvent ev)
    {
        if (Engine.IsEditorHint())
            return;
        if (ev is InputEventMouseMotion motion)
        {
            MouseMotion = motion.ScreenRelative * -0.001f;
            HasMotion = true;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Engine.IsEditorHint())
            return;
        if (HasMotion)
        {
            HasMotion = false;
        }
        else
        {
            MouseMotion = Vector2.Zero;
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        switch ((long)what)
        {
            case NotificationWMWindowFocusIn:
                if (GetWindow().HasFocus())
                {
                    Input.MouseMode = IsLocked ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                }
                break;
            case NotificationWMWindowFocusOut:
                break;
        }
    }

    public void ChangeActionSet(string next)
    {
        var set = ActionSets.FirstOrDefault(x => x.ResourceName == next);
        if (IsInstanceValid(set))
        {
            CurrentSetIndex = ActionSets.IndexOf(set);
            IsLocked = set.LockMouse;
            if (GetWindow().HasFocus())
            {
                Input.MouseMode = set.LockMouse ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
            }
        }
    }

    public void PushDigitalInput(string actionName, bool state)
    {
        var action = CurrentSet.DigitalActions.FirstOrDefault(x => x.ResourceName == actionName);
        if (IsInstanceValid(action) && Input.IsActionPressed(action.GodotAction) != state)
        {
            if (state)
                Input.ActionPress(action.GodotAction);
            else
                Input.ActionRelease(action.GodotAction);
        }
    }

    public void PushStickPadInput(string actionName, Vector2 state)
    {
        var action = CurrentSet.StickPadActions.FirstOrDefault(x => x.ResourceName == actionName);

        if (IsInstanceValid(action))
        {
            Input.ActionPress(action.GodotActionNegativeX, Mathf.Clamp(-state.X, 0f, 1f));
            Input.ActionPress(action.GodotActionPositiveX, Mathf.Clamp(state.X, 0f, 1f));
            Input.ActionPress(action.GodotActionNegativeY, Mathf.Clamp(-state.Y, 0f, 1f));
            Input.ActionPress(action.GodotActionPositiveY, Mathf.Clamp(state.Y, 0f, 1f));
        }
    }

    public void GenerateVdf()
    {
        string data = string.Empty;
        data += "\"In Game Actions\"\n";
        data += "{\n";
        data += "\t\"actions\"\n";
        data += "\t{\n";

        for (int i = 0; i < ActionSets.Count; i++)
        {
            data += ActionSets[i].GetVdf();
        }

        data += "\t}\n";
        data += "}\n";

        var fs = FileAccess.Open("user://game_actions.vdf", FileAccess.ModeFlags.Write);
        fs.StoreString(data);
        fs.Close();
    }

    public bool GetDigitalActionData(string actionName)
    {
        if (!GetWindow().HasFocus())
        {
            // return false;
        }
        var action = CurrentSet.DigitalActions.FirstOrDefault(x => x.ResourceName == actionName);
        if (IsInstanceValid(action))
        {
            return Input.IsActionPressed(action.GodotAction);
        }
        return false;
    }

    public float GetAnalogActionData(string actionName)
    {
        return 0f;
    }

    public Vector2 GetStickPadActionData(string actionName)
    {
        if (!GetWindow().HasFocus())
        {
            // return Vector2.Zero;
        }
        var action = CurrentSet.StickPadActions.FirstOrDefault(x => x.ResourceName == actionName);
        if (IsInstanceValid(action))
        {
            Vector2 output = Input.GetVector(action.GodotActionNegativeX, action.GodotActionPositiveX, action.GodotActionNegativeY, action.GodotActionPositiveY);
            if (action.Mode == InputActionStickPad.InputMode.AbsoluteMouse)
            {
                output += MouseMotion;
            }
            return output;
        }
        return Vector2.Zero;
    }

    public static void UpdateInput(IInputSource source, string actionName, ref ButtonInputFlags flags)
    {
        UpdateInput(source.GetDigitalActionData(actionName), ref flags);
    }

    public static void UpdateInput(bool state, ref ButtonInputFlags flags)
    {
        flags = UpdateInput(state, flags);
    }

    public static ButtonInputFlags UpdateInput(bool state, ButtonInputFlags flags)
    {
        if (state != flags.HasFlag(ButtonInputFlags.Pressed))
        {
            if (state)
                flags = ButtonInputFlags.JustPressed | ButtonInputFlags.Pressed;
            else
                flags = ButtonInputFlags.JustReleased;
        }
        else if (state)
            flags = ButtonInputFlags.Pressed;
        else
            flags = ButtonInputFlags.None;
        return flags;
    }
}


public interface IInputSource
{
    void ChangeActionSet(string next);
    bool GetDigitalActionData(string action);
    float GetAnalogActionData(string action);
    Vector2 GetStickPadActionData(string action);
}
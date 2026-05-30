using Godot;
using System.Linq;

[Tool]
[GlobalClass]
public partial class KeyInput : BaseSettingsControl
{
    [Export]
    public LineEdit Edit { get; set; }

    [Export]
    public ControlsUIController Controller { get; set; }

    [Export]
    public string InputActionName { get; set; }

    [Export]
    public string InputActionType { get; set; }

    [Export]
    public string InputActionData { get; set; }

    public override object Value { get => InputActionData; set => InputActionData = (string)value; }

    public override bool Enabled
    {
        get
        {
            if (Engine.IsEditorHint())
            {
                return false;
            }
            if (!IsInstanceValid(Edit))
            {
                return false;
            }
            return Edit.Editable;
        }
        set
        {
            if (Engine.IsEditorHint())
            {
                return;
            }
            if (!IsInstanceValid(Edit))
            {
                return;
            }
            Edit.Editable = value;
        }
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint())
        {
            return;
        }
        Edit.EditingToggled += Edit_EditingToggled;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_listenNextFrame)
        {
            _listenNextFrame = false;
            _keyListen = true;
        }
    }

    private void Edit_EditingToggled(bool toggledOn)
    {
        _listenNextFrame = true;
        GetViewport().SetInputAsHandled();
    }

    private bool _keyListen = false;

    private bool _listenNextFrame = false;

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (!_keyListen || !Edit.HasFocus())
        {
            return;
        }
        if (@event is InputEventMouseButton mouse && !mouse.IsReleased())
        {
            Edit.Unedit();
            GetViewport().GuiReleaseFocus();
            Edit.Text = mouse.ButtonIndex.ToString();
            InputActionData = mouse.ButtonIndex.ToString();
            InputActionType = InputSettings.InputMapping.MOUSE_BUTTON;
            _keyListen = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (!_keyListen || !Edit.HasFocus())
        {
            return;
        }
        if (@event is InputEventKey key)
        {
            GetViewport().SetInputAsHandled();
            Edit.Unedit();
            GetViewport().GuiReleaseFocus();
            _keyListen = false;
            Edit.Text = key.PhysicalKeycode.ToString();
            InputActionData = key.PhysicalKeycode.ToString();
            InputActionType = InputSettings.InputMapping.KEY_BUTTON;
            _keyListen = false;
        }
    }

    public override bool Validate(out string reason)
    {
        if (Controller.Inputs.Where(x => x.InputActionData == InputActionData && x.InputActionName != InputActionName).Any())
        {
            reason = "This key is already used, try another one.";
            return false;
        }
        reason = "";
        return true;
    }

    public override void Reset()
    {
        //base.Reset();
    }

    public override void Save()
    {
        //base.Save();
        //int index = InputSettings.Mappings.ToList().FindIndex(x => x.Action == InputActionName);
        //if (index == -1)
        //{
        //    Log.PrintErr($"Unable to find any input mapping by name: {InputActionName}!");
        //    return;
        //}
        //InputSettings.Mappings[index].Data = InputActionData;
    }

    public override void Load()
    {
        //base.Load();
        InputSettings.InputMapping[] mapping = InputSettings.Mappings.Where(x => x.Action == InputActionName).ToArray();
        if (mapping.Length == 0)
        {
            Log.PrintErr("Unable to find a key mapping for action: " + InputActionName);
            return;
        }
        InputActionData = mapping[0].Data;
        InputActionName = mapping[0].Action;
        InputActionType = mapping[0].Type;
        Edit.Text = mapping[0].Data;
    }
}

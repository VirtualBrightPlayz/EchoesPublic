using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
[Tool]
public partial class ControlsUIController : Node
{
    [Export]
    public Array<KeyInput> Inputs { get; set; }

    [Export]
    public SettingsUIController SettingsUIController { get; set; }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (Engine.IsEditorHint())
        {
            return;
        }
        SettingsUIController.OnSaveCompleted += SettingsUIController_OnSaveCompleted;
    }

    private void SettingsUIController_OnSaveCompleted()
    {
        List<InputSettings.InputMapping> mappings = InputSettings.Mappings.ToList();
        foreach (KeyInput keyInput in Inputs)
        {
            int index = mappings.FindIndex(x => x.Action == keyInput.InputActionName);
            if (index != -1)
            {
                mappings[index] = new InputSettings.InputMapping()
                {
                    Action = keyInput.InputActionName,
                    Type = keyInput.InputActionType,
                    Data = keyInput.InputActionData,
                };
            }
            else
            {
                mappings.Add(new InputSettings.InputMapping()
                {
                    Action = keyInput.InputActionName,
                    Type = keyInput.InputActionType,
                    Data = keyInput.InputActionData,
                });
            }
        }
        InputSettings.Mappings = mappings.ToArray();
        InputSettings.Write();
    }
}

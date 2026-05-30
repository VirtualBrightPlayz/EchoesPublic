using Godot;
using System;

[GlobalClass]
[Tool]
public partial class Dropdown : BaseSettingsControl, ISettingsDropdown
{
    [Export]
    private OptionButton DropdownControl { get; set; }
    
    public OptionButton DropDown => DropdownControl;

    public override object Value { get => Convert.ToInt64(DropdownControl.Selected); set => DropdownControl.Select(Convert.ToInt32(value)); }

    public override bool Enabled
    {
        get
        {
            if (Engine.IsEditorHint())
            {
                return false;
            }
            if (!IsInstanceValid(DropdownControl))
            {
                return false;
            }
            return !DropdownControl.Disabled;
        }
        set
        {
            if (Engine.IsEditorHint())
            {
                return;
            }
            if (!IsInstanceValid(DropdownControl))
            {
                return;
            }
            DropdownControl.Disabled = !value;
        }
    }

    public override bool Validate(out string reason)
    {
        reason = "";
        return true;
    }
}

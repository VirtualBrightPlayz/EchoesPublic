using Godot;
using System;

[GlobalClass]
[Tool]
public partial class Checkbox : BaseSettingsControl, ISettingsCheckbox
{
	[Export]
	private CheckBox CheckboxControl { get; set; }

	public override object Value
	{
		get
		{
			return CheckboxControl.ButtonPressed;
		}
		set
		{
			if (value is not bool b)
			{
				throw new InvalidOperationException($"Can't cast {value.GetType().Name} to {nameof(Boolean)}!");
			}
			CheckboxControl.ButtonPressed = b;
		}
	}


	public override bool Enabled
	{
		get
		{
			if (Engine.IsEditorHint())
			{
				return false;
			}
			if (!IsInstanceValid(CheckboxControl))
			{
				return false;
			}
			return !CheckboxControl.Disabled;
		}
		set
		{
			if (Engine.IsEditorHint())
			{
				return;
			}
			if (!IsInstanceValid(CheckboxControl))
			{
				return;
			}
			CheckboxControl.Disabled = !value;
		}
	}

	public override bool Validate(out string value)
	{
		if (Value is not bool b)
		{
			value = "Value is not a boolean";
			return false;
		}
		value = "";
		return true;
	}
}

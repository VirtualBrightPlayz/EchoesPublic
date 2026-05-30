using Godot;
using System;

[GlobalClass]
[Tool]
public partial class Text : BaseSettingsControl, ISettingsText
{
	[Export]
	protected LineEdit TextEdit;

	/// <summary>
	/// Do not modify this property directly.
	/// </summary>
	public int MaxLength { get; set; }

	/// <summary>
	/// Do not modify this property directly.
	/// </summary>
	public int MinLength { get; set; }

	[Export]
	private int _minLengthPrivate { get; set; }

    public override void EditorOnElementCreated()
    {
        base.EditorOnElementCreated();
		if (!IsInstanceValid(TextEdit))
		{
			return;
		}
        TextEdit.Set(LineEdit.PropertyName.MaxLength, MaxLength);
		Set(Text.PropertyName._minLengthPrivate, MinLength);
    }

	public override void _Ready()
	{
		base._Ready();
	}

	public override object Value
	{
		get
		{
			return TextEdit.Text;
		}
		set
		{
			if (value is not string s)
			{
				throw new InvalidOperationException("This setting only accepts strings.");
			}
			TextEdit.Text = s;
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
			if (!IsInstanceValid(TextEdit))
			{
				return false;
			}
			return TextEdit.Editable;
		}
		set
		{
			if (Engine.IsEditorHint())
			{
				return;
			}
			if (!IsInstanceValid(TextEdit))
			{
				return;
			}
			TextEdit.Editable = value;
		}
	}

	public override bool Validate(out string reason)
	{
		string str = (string)Convert.ChangeType(Value, typeof(string));
		if (str.Length > TextEdit.MaxLength || str.Length < _minLengthPrivate)
		{
			Log.PrintWarn($"Length: {str.Length}, Min: {MaxLength}, Max: {MinLength}");
			reason = "Too long or too short!";
			return false;
		}
		reason = "";
		return true;
	}
}

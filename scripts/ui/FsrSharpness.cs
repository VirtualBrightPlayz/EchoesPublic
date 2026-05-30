using Godot;
using System;

public partial class FsrSharpness : HSlider
{
	[Export]
	public Label label;

	[Export]
	public bool rawLabel = true;
	
	public override void _Ready()
	{
		Value = Settings.User.FsrSharpness;
		ValueChanged += OnChanged;
		OnChanged(Value);
	}

	public override void _ExitTree()
	{
		ValueChanged -= OnChanged;
	}

	public void UpdateLabel()
	{
		if (!IsInstanceValid(label))
			return;
		if (rawLabel)
			label.Text = Value.ToString("0.00");
		else
			label.Text = (Value * 100).ToString("0") + "%";
	}

	private void OnChanged(double value)
	{
		float val = (float)Mathf.Clamp(value, 0, 2);
		Settings.User.FsrSharpness = val;
		Settings.WriteSettings();
		// if (MenuManager.Instance.xrInterface == null || !MenuManager.Instance.xrInterface.IsInitialized())
		if (!IInitScript.Instance.IsXR)
		{
			GetViewport().SetFsrSharpness(2.0f - val); // FSR Scale is 0.0 as most sharp
			MenuManager.Instance.mainViewport.SetFsrSharpness(2.0f - val);
		}

		UpdateLabel();
	}
}

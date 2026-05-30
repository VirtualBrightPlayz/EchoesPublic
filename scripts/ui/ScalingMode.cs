using Godot;
using System;

public partial class ScalingMode : OptionButton
{
	public override void _Ready()
	{
		Selected = (int)Settings.User.ScalingMode;
		ItemSelected += OnChanged;
		OnChanged(Selected);
	}

	public override void _ExitTree()
	{
		ItemSelected -= OnChanged;
	}

	private void OnChanged(long index)
	{
        if (!IsVisibleInTree())
            return;
		Settings.User.ScalingMode = (Settings.UpscalingMode)index;
        Settings.Modified = true;
		/*
		Settings.WriteUserSettings();
		if (!IInitScript.Instance.IsXR)
		{
			float scale = Settings.User.ScalingMode != Viewport.Scaling3DModeEnum.Bilinear
				? Settings.User.FsrScale.GetScale()
				: 1.0f;
			GetViewport().SetScaling3DMode(Settings.User.ScalingMode);
			GetViewport().SetScaling3DScale(scale);
			MenuManager.Instance.mainViewport.SetScaling3DMode(Settings.User.ScalingMode);
			MenuManager.Instance.mainViewport.SetScaling3DScale(scale);
		}
		*/
	}
}

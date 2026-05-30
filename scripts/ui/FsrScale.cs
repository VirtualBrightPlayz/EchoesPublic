using Godot;
using System;

public partial class FsrScale : OptionButton
{
	public override void _Ready()
	{
		Selected = (int)Settings.User.FsrScale;
		ItemSelected += OnChanged;
		OnChanged(Selected);
	}

	public override void _ExitTree()
	{
		ItemSelected -= OnChanged;
	}

	private void OnChanged(long index)
	{
		Settings.User.FsrScale = (Settings.FSRScaling)index;
		/*
		Settings.WriteUserSettings();
		if (!IInitScript.Instance.IsXR)
		{
			switch (Settings.User.ScalingMode)
			{
				case Viewport.Scaling3DModeEnum.Fsr:
				case Viewport.Scaling3DModeEnum.Fsr2:
					GetViewport().SetScaling3DScale(Settings.User.FsrScale.GetScale());
					MenuManager.Instance.mainViewport.SetScaling3DScale(Settings.User.FsrScale.GetScale());
				break;
				default:
					// todo: use scaling mode when added
					GetViewport().SetScaling3DScale(1.0f);
					MenuManager.Instance.mainViewport.SetScaling3DScale(1.0f);
					break;
			}
		}
		*/
	}
	
}

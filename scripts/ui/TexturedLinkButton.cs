using Godot;

public partial class TexturedLinkButton : TextureButton
{
	[Export]
	public string uri = "";
	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPressed;
	}
	
	private void OnPressed()
	{
		OS.ShellOpen(uri);
	}
}

using Godot;

[GlobalClass]
public partial class MouseFollow : TextureRect
{
	public override void _Process(double delta)
	{
		base._Process(delta);
		GlobalPosition = GetGlobalMousePosition() - PivotOffset;
	}
}

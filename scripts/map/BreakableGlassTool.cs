using Godot;

[Tool]
public partial class BreakableGlassTool : Node3D
{
    [Export] 
    private StaticBody3D breakableGlass;
    
    [Export] 
    public float MaxHealth { get; set; } = 50;

    [Export]
    public float Health { get; set; } = 50;

    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            UpdateGlass();
        }
    }
    
    private void UpdateGlass()
    {
        if (!Engine.IsEditorHint())
            return;
        if (!IsInstanceValid(breakableGlass) && breakableGlass is BreakableGlass glass)
        {
            BoxShape3D boxShape = glass.Shape.Shape as BoxShape3D;
            boxShape.Size = Scale;
            glass.MaxHealth = MaxHealth;
            glass.Health = Health;
        }
    }
}
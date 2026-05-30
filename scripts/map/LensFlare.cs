using Godot;

[GlobalClass]
[Tool]
public partial class LensFlare : Sprite3D
{
    [Export]
    public float FlareScale = 1f;
    [Export]
    public float MinEnergy = 0.9f;
    [Export]
    public float MaxEnergy = 1.1f;
    [Export]
    public float Frequency = 0f;

    public Light3D parent;

    private float timer;
    public float energy;

    public override void _EnterTree()
    {
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        DoubleSided = false;
        energy = 1f;
        parent = GetParentOrNull<Light3D>();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            DoubleSided = false;
            Modulate = Colors.White;
            Scale = Vector3.One;
            parent = GetParentOrNull<Light3D>();
        }
    }

    public override void _Process(double delta)
    {
        // var parent = GetParentOrNull<Node3D>();
        // var camera = GetViewport().GetCamera3D();
        if (!IsInstanceValid(parent))
            return;
        // var pos = camera.UnprojectPosition(parent.GlobalPosition);
        // var dir = camera.GlobalPosition - parent.GlobalPosition;
        // var pos = parent.GlobalPosition + dir.Normalized() * 1f;
        // GlobalPosition = pos;
        timer -= (float)delta;
        if (timer <= 0f || timer > Frequency)
        {
            timer = Frequency;
            energy = (float)GD.RandRange(MinEnergy, MaxEnergy);
        }
        // if (parent is Light3D li)
        Light3D li = parent;
        {
            Modulate = new Color(li.LightColor.R, li.LightColor.G, li.LightColor.B, energy);
            Scale = Vector3.One * li.LightEnergy * FlareScale;
        }
    }
}

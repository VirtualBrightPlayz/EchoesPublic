using Godot;

public partial class BuilderController : Node
{
    [Export]
    public Node3D rig;
    [Export]
    public Camera3D cam;

    private Vector2 mouseMotion;
    private CursorManager cursor;

    public override void _Ready()
    {
        cursor.IsActive = true;
    }

    public override void _EnterTree()
    {
        cursor = new CursorManager(this);
    }

    public override void _Input(InputEvent ev)
    {
        if (!cursor.IsActive)
            return;
        if (ev is InputEventMouseMotion mev)
        {
            if (cursor.CursorLocked)
            {
                mouseMotion += mev.ScreenRelative * -0.001f;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!cursor.IsActive)
            return;

        rig.Rotation += new Vector3(0, mouseMotion.X, 0);
        cam.Rotation += new Vector3(mouseMotion.Y, 0, 0);

        cam.Rotation = new Vector3(Mathf.Clamp(cam.Rotation.X, -1.55f, 1.55f), cam.Rotation.Y, cam.Rotation.Z);

        mouseMotion = Vector2.Zero;

        if (Input.IsActionJustPressed("menu_pause"))
        {
            cursor.Toggle(CursorManager.CursorType.Pause);
        }
    }
}
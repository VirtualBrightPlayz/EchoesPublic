using Godot;
using System;
using System.Linq;

public partial class MenuCamera2 : Camera3D
{
    [Export]
    public Marker3D[] points = Array.Empty<Marker3D>();
    [Export]
    public WorldCanvas[] worldCanvas = Array.Empty<WorldCanvas>();
    public int index = 0;
    [Export]
    public double Speed;
    [Export]
    public double MoveTime = 1f;
    [Export]
    public double timeToMain = 3d;
    [Export]
    public Rect2 rect = new Rect2(-Vector2.One, Vector2.One * 2f);
    public double timer;
    public double menuTimer;
    public Vector3 offset = Vector3.Zero;
    [Export]
    public MainMenuUI menu;

    public override void _Ready()
    {
        menuTimer = timeToMain;
    }

    /*
    public override void _PhysicsProcess(double delta)
    {
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 origin = ProjectRayOrigin(mouse);
        Vector3 ray = ProjectRayNormal(mouse);
        var args = PhysicsRayQueryParameters3D.Create(origin, origin + ray * Far);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(args);
        if (result.Count > 0)
        {
            Node node = result["collider"].As<Node>();
            if (node is RigidBody3D rb)
            {
                // rb.LinearVelocity =  result["position"].AsVector3() - rb.CenterOfMass;
            }
        }
    }
    */

    public override void _Process(double delta)
    {
        // index = 0;
        if (IsInstanceValid(RoundManager.Instance))
        {
            // index = MenuManager.Instance.InMatrix ? 1 : 2;
        }
        else
        {
            menuTimer += delta;
            var active = menu.GetActiveMenu();
            // var active = worldCanvas.FirstOrDefault(x => x.wasInputValid);
            // if (IsInstanceValid(active) && (active.Name.Equals("ServerList") || active.Name.Equals("InputSettings") || active.Name.Equals("Credits")))
            if (IsInstanceValid(active) && menu.menus[0] != active)
            {
                menuTimer = 0;
                index = 1;
                // index = Array.IndexOf(worldCanvas, active) + 1;
                // GD.Print(index);
            }
            if (menuTimer > timeToMain)
            {
                index = 0;
            }
        }

        float x = (float)Mathf.Sin(Mathf.DegToRad(timer * MoveTime)) * (5f/180f);
        float y = (float)Mathf.Cos(Mathf.DegToRad(timer * MoveTime)) * (5f/180f);
        offset.X = x;
        offset.Z = y;
        if (IsInstanceValid(RoundManager.Instance))
        {
            // return;
        }
        timer += delta;
        float t = (float)(delta * Speed);
        GlobalPosition = GlobalPosition.Lerp(points[index].GlobalPosition, t);
        // GlobalRotation = GlobalRotation.Slerp(points[index].GlobalRotation + offset, t);
        Vector2 mouse = GetWindow().GetMousePosition() / GetWindow().Size;
        // GD.Print(mouse.Y);
        mouse = mouse.Clamp(0f, 1f);
        Vector2 mouseOffset = new Vector2(Mathf.Lerp(rect.Position.X, rect.End.X, 1f-mouse.X), Mathf.Lerp(rect.Position.Y, rect.End.Y, 1f-mouse.Y));
        Vector3 rot = new Vector3(Mathf.DegToRad(mouseOffset.Y), Mathf.DegToRad(mouseOffset.X), 0f);
        GlobalRotation = GlobalRotation.Slerp(points[index].GlobalRotation + rot + offset, t);
        // float targetFov = points[index].HasMeta("fov") ? points[index].GetMeta("fov").AsSingle() : 70f;
        // Fov = Mathf.Lerp(Fov, targetFov, t);
    }
}

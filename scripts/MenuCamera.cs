using Godot;
using System;

public partial class MenuCamera : Camera3D
{
    [Export]
    public Marker3D[] points = Array.Empty<Marker3D>();
    public int index = 0;
    [Export]
    public double Speed;
    [Export]
    public double UnitSpeed;
    [Export]
    public double MoveTime = 1f;
    [Export]
    public double timeToMain = 3d;
    public double timer;
    public double menuTimer;
    public Vector3 offset = Vector3.Zero;
    [Export]
    public MainMenuUI menu;
    [Export]
    public MeshInstance3D disableAndroid;

    public override void _Ready()
    {
        index = OS.GetName().Equals("Android") ? 1 : 0;
        // disableAndroid.Visible = !OS.GetName().Equals("Android");
        // offset = points[index].GlobalRotation;
        if (IsInstanceValid(RoundManager.Instance))
        {
            // index = MenuManager.Instance.InMatrix ? 1 : 2;
            index = 2;
            SetMenuPos();
            GlobalPosition = points[index].GlobalPosition;
            GlobalRotation = points[index].GlobalRotation;
        }
        menuTimer = timeToMain;
    }

    public void SetMenuPos()
    {
        if (MenuManager.Instance.menu is MainMenu m)
        {
            var size = GetTree().Root.GetTexture().GetSize();
            var mSize = m.MeshSize;
            var mScale = m.meshInst.GlobalTransform.Basis.Scale.X;

            // https://forum.unity.com/threads/dynamic-loaded-object-fit-to-screen-size.349794/
            var up = new Quaternion(m.meshInst.GlobalBasis.X.Normalized(), Mathf.DegToRad(90f)) * (-GlobalBasis.Z.Normalized());
            var hFov = GetCameraProjection().GetFov();
            var margin = 1f;
            var tri = m.meshInst.GlobalPosition + up * (mScale * 0.5f * mSize.X) * margin;
            var dist = tri.DistanceTo(m.meshInst.GlobalPosition) / Mathf.Tan(Mathf.DegToRad(hFov) * 0.5f);
            points[2].Position = -Vector3.Forward * dist;
        }
    }

    public override void _Process(double delta)
    {
        index = OS.GetName().Equals("Android") ? 1 : 0;
        SetMenuPos();
        if (IsInstanceValid(RoundManager.Instance))
        {
            index = MenuManager.Instance.InMatrix ? 1 : 2;
        }
        else if (IsInstanceValid(menu))
        {
            menuTimer += delta;
            var active = menu.GetActiveMenu();
            if (IsInstanceValid(active) && (active.Name.Equals("ServerList") || active.Name.Equals("InputSettings") || active.Name.Equals("Credits")))
            {
                menuTimer = 0;
                index = 1;
            }
            if (menuTimer < timeToMain)
            {
                index = 1;
            }
        }

        float x = (float)Mathf.Sin(Mathf.DegToRad(timer * MoveTime)) * (5f/180f);
        float y = (float)Mathf.Cos(Mathf.DegToRad(timer * MoveTime)) * (5f/180f);
        offset.X = x;
        offset.Z = y;
        if (IsInstanceValid(RoundManager.Instance))
        {
            return;
        }
        timer += delta;
        float t = (float)(delta * Speed);
        // t = (float)(timer / MoveTime);
        GlobalPosition = GlobalPosition.Lerp(points[index].GlobalPosition, t);
        GlobalRotation = GlobalRotation.Slerp(points[index].GlobalRotation + offset, t);
        float targetFov = points[index].HasMeta("fov") ? points[index].GetMeta("fov").AsSingle() : 70f;
        Fov = Mathf.Lerp(Fov, targetFov, t);
        /*
        if (timer >= MoveTime)
        {
            index++;
            index %= points.Length;
            timer = MoveTime;
        }
        */
    }
}

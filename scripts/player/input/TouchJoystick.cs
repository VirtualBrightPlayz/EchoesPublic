using Godot;

// [GlobalClass]
public partial class TouchJoystick : Control
{
    [Export]
    public Control middle;
    [Export]
    public float joysitck_size = 25f;

    [Export]
    public string action_xp;
    [Export]
    public string action_xn;
    [Export]
    public string action_yp;
    [Export]
    public string action_yn;

    public int index = -1;

    public override void _Ready()
    {
        if (!IsVisibleInTree())
            return;
        var origin = Vector2.Zero;
        middle.Position = origin + Size / 2f - middle.PivotOffset;
        ProcessInput(origin / joysitck_size);
    }

    public override void _Input(InputEvent ev)
    {
        if (!IsVisibleInTree())
            return;
        if (ev is InputEventScreenTouch touch)
        {
            var pos = touch.Position - (GlobalPosition + Size / 2f);
            if (touch.Pressed)
            {
                var origin = pos.LimitLength(joysitck_size);
                if (!GetRect().HasPoint(touch.Position))
                    return;
                index = touch.Index;
                middle.Position = origin + Size / 2f - middle.PivotOffset;
                ProcessInput(origin / joysitck_size);
                GetViewport().SetInputAsHandled();
            }
            else
            {
                if (index != touch.Index && !GetRect().HasPoint(touch.Position))
                    return;
                index = -1;
                var origin = Vector2.Zero;
                middle.Position = origin + Size / 2f - middle.PivotOffset;
                ProcessInput(origin / joysitck_size);
                GetViewport().SetInputAsHandled();
            }
        }
        if (ev is InputEventScreenDrag drag)
        {
            var pos = drag.Position - (GlobalPosition + Size / 2f);
            var origin = pos.LimitLength(joysitck_size);
            if (!GetRect().HasPoint(drag.Position) && index != drag.Index)
                return;
            index = drag.Index;
            middle.Position = origin + Size / 2f - middle.PivotOffset;
            ProcessInput(origin / joysitck_size);
                GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        // index = -1;
    }

    public void ProcessInput(Vector2 origin)
    {
        if (origin.Y > 0)
            Input.ActionPress(action_yp, origin.Y);
        else
            Input.ActionRelease(action_yp);
        if (origin.Y < 0)
            Input.ActionPress(action_yn, -origin.Y);
        else
            Input.ActionRelease(action_yn);
        if (origin.X > 0)
            Input.ActionPress(action_xp, origin.X);
        else
            Input.ActionRelease(action_xp);
        if (origin.X < 0)
            Input.ActionPress(action_xn, -origin.X);
        else
            Input.ActionRelease(action_xn);
    }
}

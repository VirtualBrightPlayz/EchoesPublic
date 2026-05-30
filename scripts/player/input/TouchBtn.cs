using Godot;

public partial class TouchBtn : Control
{
    [Export]
    public string action;
    [Export]
    public bool delay = false;
    [Export]
    public bool hold = true;
    [Export]
    public bool toggle = false;
    [Export]
    public Color toggleColor = Colors.DarkBlue;

    public int index = -1;

    public override void _ExitTree()
    {
        Input.ActionRelease(action);
    }

    public override void _Input(InputEvent ev)
    {
        if (!IsVisibleInTree())
            return;
        if (ev is InputEventScreenTouch touch)
        {
            if (touch.Pressed)
            {
                if (GetRect().HasPoint(touch.Position))
                {
                    index = touch.Index;
                    if (hold)
                    {
                        if (delay)
                        {
                            CallDeferred(nameof(InputDelayed));
                        }
                        else
                        {
                            InputDelayed();
                        }
                    }
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (touch.Index == index)
            {
                index = -1;
                if (!toggle)
                {
                    if (!hold)
                    {
                        InputDelayed();
                    }
                    Input.ActionRelease(action);
                }
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void InputDelayed()
    {
        if (toggle)
        {
            if (Input.IsActionPressed(action))
            {
                Input.ActionRelease(action);
                Modulate = Colors.White;
            }
            else
            {
                Input.ActionPress(action);
                Modulate = toggleColor;
            }
        }
        else
        {
            Input.ActionPress(action);
        }
    }
}

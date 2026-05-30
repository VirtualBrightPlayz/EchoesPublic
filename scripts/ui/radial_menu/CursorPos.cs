using Godot;
using System;

public partial class CursorPos : Control
{
    [Signal]
    public delegate void HoveredEventHandler(int index);

    [Signal]
    public delegate void SelectedEventHandler(int index);

    public int Count = 0;
    public float MinDistancePercent = 0;
    int Touches = 0;
    int LastIndex = 0;
    public Vector2 Cursor = Vector2.Zero;

    public float IndexOffset => (2 * (float)Math.PI) / Count;

    int GetCursorIndex()
    {
        if (Count == 0)
            return int.MaxValue;
        if (MinDistancePercent > 0)
        {
            var viewportRect = GetViewportRect();
            var middle = viewportRect.Size / 2;
            var percentFactor = (MinDistancePercent / 100);
            var distance = middle.DistanceTo(GetGlobalMousePosition());
            if (viewportRect.Size.X * percentFactor > distance || viewportRect.Size.Y * percentFactor > distance)
                return int.MaxValue;
        }

        var cursorNormalized = Cursor.Normalized();
        var angle = Vector2.Zero.AngleToPoint(cursorNormalized);

        // Calculate the angle as a percentage of the entire circle
        // divided up into equal sized arcs based checked the number of items (count)
        var toReturn = angle / IndexOffset;

        // Clip to the min-max of our array of buttons
        toReturn = Math.Min(toReturn, Count - 1);

        return (int)Math.Round(toReturn, MidpointRounding.AwayFromZero);
    }

    void ComputeIndex()
    {
        var index = GetCursorIndex();
        if (index == LastIndex)
            return;

        LastIndex = index;
        EmitSignal(SignalName.Hovered, index);
    }

    void TouchStart(InputEventScreenTouch ev)
    {
        if (Touches < 1)
            Cursor = Vector2.Zero;
        Touches++;
    }

    void TouchEnd(InputEventScreenTouch ev)
    {
        Touches--;
        if (Touches <= 0)
        {
            Touches = 0;
            EmitSignal(SignalName.Selected, GetCursorIndex());
        }
    }

    void TouchDrag(InputEventScreenDrag ev)
    {
        Cursor += ev.Relative;
        ComputeIndex();
    }

    void MouseStart(InputEventMouseButton ev)
    {
        if (Touches < 1)
            MouseDrag();
        Touches++;
    }

    void MouseEnd(InputEventMouseButton ev)
    {
        Touches = 0;
        if (Touches <= 0)
        {
            Touches = 0;
            EmitSignal(SignalName.Selected, GetCursorIndex());
        }
    }

    void MouseDrag()
    {
        var center = GlobalPosition;

        Cursor = (center - GetGlobalMousePosition()) * -1;

        ComputeIndex();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized || what == NotificationApplicationFocusIn)
        {
            Touches = 0;
        }
    }

    public override void _Input(InputEvent ev)
    {
        switch (ev)
        {
            case InputEventScreenTouch touchEv when touchEv.Pressed:
                TouchStart(touchEv);
                break;
            case InputEventScreenTouch touchEv when !touchEv.Pressed:
                TouchEnd(touchEv);
                break;
            case InputEventScreenDrag dragEv:
                TouchDrag(dragEv);
                break;
            case InputEventMouseButton mouseEv when mouseEv.Pressed && (mouseEv.ButtonIndex == MouseButton.Left || mouseEv.ButtonIndex == MouseButton.Right):
                MouseStart(mouseEv);
                break;
            case InputEventMouseButton mouseEv when !mouseEv.Pressed && (mouseEv.ButtonIndex == MouseButton.Left || mouseEv.ButtonIndex == MouseButton.Right):
                MouseEnd(mouseEv);
                break;
            case InputEventMouseMotion:
                MouseDrag();
                break;
        }
    }
}
using Godot;

public partial class TouchLayer : Control
{
    [Export]
    public LocalPlayerInput localInput;
    [Export]
    public InputManagerOld manager;
    [Export]
    public bool root = true;

    public override void _EnterTree()
    {
        if (root)
            Visible = OS.GetName().Equals("Android") && IsMultiplayerAuthority();
    }

    public override void _Input(InputEvent ev)
    {
        if (!IsMultiplayerAuthority() || !root)
            return;
        if (manager.Cursor.CursorLocked || manager.InventoryOpen)
            return;
        if (ev is InputEventScreenDrag drag)
        {
            using InputEventMouseMotion motion = new InputEventMouseMotion();
            motion.ButtonMask = 0;//MouseButtonMask.Left;
            motion.Position = drag.Position;
            GetViewport().PushInput(motion);
            // Input.ParseInputEvent(motion);
            GetViewport().SetInputAsHandled();
        }

        if (ev is InputEventScreenTouch touch)
        {
            using InputEventMouseButton motion = new InputEventMouseButton();
            // motion.ButtonIndex = touch.Pressed ? MouseButton.Left : MouseButton.None;
            motion.ButtonIndex = MouseButton.Left;
            motion.ButtonMask = touch.Pressed ? MouseButtonMask.Left : 0;
            motion.Pressed = touch.Pressed;
            motion.Position = touch.Position;
            GetViewport().PushInput(motion);
            // Input.ParseInputEvent(motion);
            GetViewport().SetInputAsHandled();
        }
    }
}

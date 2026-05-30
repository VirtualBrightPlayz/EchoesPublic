using Godot;
using System;

public partial class FreeCam : Node3D
{
    public static FreeCam Instance { get; private set; }

    [Export]
    public float Speed = 5.0f;
    [Export]
    public float SprintMultiplier = 2.0f;
    [Export]
    public float JumpVelocity = 5.0f;
    [Export]
    public float FarClip = 100f;
    [Export]
    public Godot.Environment env;
    [Export]
    public Window window;
    [Export]
    public Window controlsWindow;
    [Export(PropertyHint.Layers3DRender)]
    public uint cullFlags = uint.MaxValue;

    private Vector2 mouseMotion;
    private CursorManager cursor;
    private Camera3D head;
    private Node3D target;
    private Vector3 lastTargetPos;

    public override void _Ready()
    {
        Instance = this;
        cursor.IsActive = false;
        head = new Camera3D();
        head.Environment = env;
        head.CullMask = cullFlags;
        if (window != null)
        {
            window.CloseRequested += CloseWindow;
            window.PositionalShadowAtlasSize = 0;
            cursor.IsActive = true;
        }
        AddChild(head);
        head.GlobalRotation = GlobalRotation;
        head.MakeCurrent();
        head.Far = FarClip;
    }

    public void CloseWindow()
    {
        cursor.IsActive = false;
        window?.Hide();
        controlsWindow?.Hide();
    }

    public void OpenWindow()
    {
        window?.Show();
        controlsWindow?.Show();
        var t = NetworkPlayer.LocalInstance?.ActiveController?.Root;
        if (IsInstanceValid(t))
        {
            GlobalPosition = t.GlobalPosition;
        }
    }

    public void SetPlayerLock(bool value)
    {
        if (value)
        {
            target = NetworkPlayer.LocalInstance?.ActiveController?.Root;
            lastTargetPos = target?.GlobalPosition ?? Vector3.Zero;
        }
        else
            target = null;
    }

    public void SetUseWorldEnv(bool value)
    {
        if (value)
        {
            head.Environment = null;
            window.PositionalShadowAtlasSize = GetTree().Root.PositionalShadowAtlasSize;
            window.UseOcclusionCulling = true;
        }
        else
        {
            head.Environment = env;
            window.PositionalShadowAtlasSize = 0;
            window.UseOcclusionCulling = false;
        }
    }

    public async void TakeScreenshot()
    {
        var size = window.Size;
        window.Size = new Vector2I(3840, 2160);
        await ToSignal(GetTree().CreateTimer(1d), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Image img = window.GetTexture().GetImage();
        img.Convert(Image.Format.Rgb8);
        byte[] png = img.GetData();
        window.Size = size;
        // DisplayServer.ClipboardSet();
        if (SteamManager.Supported)
            GodotSteam.Steam.WriteScreenshot(png, img.GetWidth(), img.GetHeight());
    }

    public void FocusPlayer()
    {
        var t = NetworkPlayer.LocalInstance?.ActiveController?.Root;
        if (IsInstanceValid(t))
        {
            GlobalPosition = t.GlobalPosition;
        }
    }

    public override void _EnterTree()
    {
        cursor = new CursorManager(window);
    }

    public override void _ExitTree()
    {
        head.QueueFree();
    }

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationWMWindowFocusIn:
                if (cursor != null)
                    cursor.Update();
                break;
            case NotificationWMWindowFocusOut:
                if (cursor != null)
                    cursor.Update();
                break;
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (!Visible || !head.Current || !window.HasFocus())
            return;

        if (ev is InputEventMouseMotion mev)
        {
            if (mev.ButtonMask.HasFlag(MouseButtonMask.Right))
            {
                mouseMotion += mev.ScreenRelative * -0.001f;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible || !head.Current || !window.HasFocus())
            return;

        Rotation += new Vector3(0, mouseMotion.X, 0);
        head.Rotation += new Vector3(mouseMotion.Y, 0, 0);

        head.Rotation = new Vector3(Mathf.Clamp(head.Rotation.X, -1.55f, 1.55f), head.Rotation.Y, head.Rotation.Z);

        mouseMotion = Vector2.Zero;

        // if (Input.IsActionJustPressed("menu_pause"))
        {
            // cursor.Toggle(CursorManager.CursorType.Pause);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (target != null && IsInstanceValid(target))
        {
            GlobalPosition -= (lastTargetPos - target.GlobalPosition);
            lastTargetPos = target.GlobalPosition;
        }

        if (!Visible || !head.Current || !cursor.IsActive)
            return;

        Vector3 velocity = Vector3.Zero;

        bool isSprinting = Input.IsActionPressed("player_sprint");
        float multiplier = isSprinting ? SprintMultiplier : 1.0f;

        if (Input.IsActionPressed("player_jump"))
            velocity.Y = JumpVelocity * multiplier;
        if (Input.IsActionPressed("player_sneak"))
            velocity.Y = -JumpVelocity * multiplier;
        
        Vector2 inputDir = Input.GetVector("player_left", "player_right", "player_forward", "player_backward");
        Vector3 direction = (Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed * multiplier;
            velocity.Z = direction.Z * Speed * multiplier;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed * multiplier);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed * multiplier);
        }

        GlobalPosition += velocity * (float)delta;
    }
}

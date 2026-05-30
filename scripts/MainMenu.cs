using Godot;
using System;

public partial class MainMenu : Node3D
{
    [Export]
    public SubViewport viewport;
    [Export]
    public MeshInstance3D meshInst;
    [Export]
    public MeshInstance3D monitorMeshInst;
    [Export]
    public Godot.Environment environment;
    [Export]
    public SpotLight3D laser;
    [Export]
    public float laserSize = 1f;
    [Export]
    public TextureRect testScreen;
    [Export]
    public ColorRect glitchEffect;
    [Export]
    public Label okLabel;
    [Export]
    public MainMenuUI ui;
    [Export]
    public MenuCamera camera;
    [Export]
    public XRCamera3D xrCamera;

    public Vector2 mousePos;
    public Vector3 worldPos;

    public bool SettingsMenuOpen = false;

    public float ScreenUVAmount
    {
        get => ((ShaderMaterial)meshInst.MaterialOverride).GetShaderParameter("screen_uv_amount").AsSingle();
        set => ((ShaderMaterial)meshInst.MaterialOverride).SetShaderParameter("screen_uv_amount", Mathf.Clamp(value, 0f, 1f));
    }

    public Color ScreenColor
    {
        get => ((ShaderMaterial)meshInst.MaterialOverride).GetShaderParameter("albedo_color").AsColor();
        set => ((ShaderMaterial)meshInst.MaterialOverride).SetShaderParameter("albedo_color", value);
    }

    public Texture2D ScreenTexture
    {
        get => ((ShaderMaterial)meshInst.MaterialOverride).GetShaderParameter("albedo_map").As<Texture2D>();
        set => ((ShaderMaterial)meshInst.MaterialOverride).SetShaderParameter("albedo_map", value);
    }

    public Texture2D MonitorScreenTexture
    {
        get => ((ShaderMaterial)monitorMeshInst.MaterialOverride).GetShaderParameter("albedo_map").As<Texture2D>();
        set => ((ShaderMaterial)monitorMeshInst.MaterialOverride).SetShaderParameter("albedo_map", value);
    }

    public Vector2 ScreenUVOffset
    {
        get => ((ShaderMaterial)monitorMeshInst.MaterialOverride).GetShaderParameter("albedo_uv_offset").As<Vector2>();
        set => ((ShaderMaterial)monitorMeshInst.MaterialOverride).SetShaderParameter("albedo_uv_offset", value);
    }

    public Vector2 MeshSize
    {
        get => ((QuadMesh)meshInst.Mesh).Size;
        set => ((QuadMesh)meshInst.Mesh).Size = value;
    }

    public Vector2 CursorPosition { get; private set; } = Vector2.Zero;
    [Export]
    public NodePath inputPath;
    public IPlayerInputSource InputSource => GetNode<IPlayerInputSource>(inputPath);
    public IExtendedPlayerInputSource ExtendedInputSource => GetNode(inputPath) is IExtendedPlayerInputSource src ? src : null;

    public override void _Input(InputEvent ev)
    {
        return;
        if (MenuManager.Instance.InMatrix || IsInstanceValid(RoundManager.Instance))
            return;
        if (MenuManager.Instance.errorTextPanel.Visible)
            return;
        if (MenuManager.Instance.textPanel.Visible)
            return;
        if (GetViewport().UseXR)
            return;
        if (ev is InputEventMouse)
            GetViewport().SetInputAsHandled();
        if (HandleMouse(ev, viewport, SettingsMenuOpen ? monitorMeshInst : meshInst))
        {
            // GetViewport().SetInputAsHandled();
            return;
        }
        // viewport.PushInput(ev);
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        return;
        if (MenuManager.Instance.InMatrix || IsInstanceValid(RoundManager.Instance))
            return;
        if (MenuManager.Instance.errorTextPanel.Visible)
            return;
        if (MenuManager.Instance.textPanel.Visible)
            return;
        if (GetViewport().UseXR)
            return;
        if (ev is InputEventMouse)
            return;
        viewport.PushInput(ev);
    }

    InputEventMouseMotion Amotion = new InputEventMouseMotion();
    InputEventMouseButton Bmotion = new InputEventMouseButton();
    InputEventMouseMotion Cmotion = new InputEventMouseMotion();
    InputEventMouseButton Dmotion = new InputEventMouseButton();

    public bool HandleMouse(InputEvent ev, SubViewport view, Node3D mesh)
    {
        if (GetViewport().UseXR)
            return false;
        var invSize = MeshSize;
        invSize.Y = -1f / invSize.Y;
        var halfInvSize = MeshSize * 0.5f;
        halfInvSize.Y *= -1f;
        bool local = false;

        switch (ev)
        {
            //Mouse Move
            case InputEventMouseMotion mev:
                Vector3 Aorigin = GetViewport().GetCamera3D().ProjectRayOrigin(mev.Position);
                Vector3 Anormal = GetViewport().GetCamera3D().ProjectRayNormal(mev.Position);
                Plane Apl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
                Vector3 Av3 = Apl.IntersectsRay(Aorigin, Anormal) ?? Vector3.Zero;
                worldPos = Av3;
                Av3 = mesh.GlobalTransform.AffineInverse() * Av3;
                Vector2 v2 = new Vector2(Av3.X, Av3.Y);
                Amotion.ButtonMask = mev.ButtonMask;
                Amotion.Position = (v2 + halfInvSize) * invSize * view.Size;
                Amotion.GlobalPosition = Amotion.Position;
                Amotion.Relative = mev.Relative;
                view.PushInput(Amotion, local);
                var c = view.GuiGetHoveredControl();
                if (IsInstanceValid(c))
                    DisplayServer.CursorSetShape((DisplayServer.CursorShape)c.GetCursorShape(Amotion.Position));
                return true;

            //Mouse Click
            case InputEventMouseButton meb:
                Vector3 Borigin = GetViewport().GetCamera3D().ProjectRayOrigin(meb.Position);
                Vector3 Bnormal = GetViewport().GetCamera3D().ProjectRayNormal(meb.Position);
                Plane Bpl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
                Vector3 Bv3 = Bpl.IntersectsRay(Borigin, Bnormal) ?? Vector3.Zero;
                // worldPos = origin + normal * 10f;
                worldPos = Bv3;
                Bv3 = mesh.GlobalTransform.AffineInverse() * Bv3;
                Vector2 Bv2 = new Vector2(Bv3.X, Bv3.Y);
                Bmotion.ButtonIndex = meb.ButtonIndex;
                Bmotion.ButtonMask = meb.ButtonMask;
                Bmotion.Pressed = meb.Pressed;
                Bmotion.Position = (Bv2 + halfInvSize) * invSize * view.Size;
                Bmotion.GlobalPosition = Bmotion.Position;
                view.PushInput(Bmotion, local);
                var c2 = view.GuiGetHoveredControl();
                if (IsInstanceValid(c2))
                    DisplayServer.CursorSetShape((DisplayServer.CursorShape)c2.GetCursorShape(Bmotion.Position));
                return true;

            //Touch Screen Drag
            case InputEventScreenDrag drag:
                Vector3 Corigin = GetViewport().GetCamera3D().ProjectRayOrigin(drag.Position);
                Vector3 Cnormal = GetViewport().GetCamera3D().ProjectRayNormal(drag.Position);
                Plane Cpl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
                Vector3 Cv3 = Cpl.IntersectsRay(Corigin, Cnormal) ?? Vector3.Zero;
                worldPos = Cv3;
                Cv3 = mesh.GlobalTransform.AffineInverse() * Cv3;
                Vector2 Cv2 = new Vector2(Cv3.X, Cv3.Y);
                Cmotion.ButtonMask = 0;//MouseButtonMask.Left;
                Cmotion.Position = (Cv2 + halfInvSize) * invSize * view.Size;
                Cmotion.GlobalPosition = Cmotion.Position;
                view.PushInput(Cmotion, local);
                return true;

            //Touch Screen Touch
            case InputEventScreenTouch touch:
                Vector3 Dorigin = GetViewport().GetCamera3D().ProjectRayOrigin(touch.Position);
                Vector3 Dnormal = GetViewport().GetCamera3D().ProjectRayNormal(touch.Position);
                Plane Dpl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
                Vector3 Dv3 = Dpl.IntersectsRay(Dorigin, Dnormal) ?? Vector3.Zero;
                // worldPos = origin + normal * 10f;
                worldPos = Dv3;
                Dv3 = mesh.GlobalTransform.AffineInverse() * Dv3;
                Vector2 Dv2 = new Vector2(Dv3.X, Dv3.Y);
                // motion.ButtonIndex = touch.Pressed ? MouseButton.Left : MouseButton.None;
                Dmotion.ButtonIndex = MouseButton.Left;
                Dmotion.ButtonMask = touch.Pressed ? MouseButtonMask.Left : 0;
                Dmotion.Pressed = touch.Pressed;
                Dmotion.Position = (Dv2 + halfInvSize) * invSize * view.Size;
                Dmotion.GlobalPosition = Dmotion.Position;
                view.PushInput(Dmotion, local);
                return true;

            default: return false;
        }
    }

    public void HandleRaycastPress(bool leftPressed, Vector3 pos, Vector3 dir)
    {
        HandleRaycastPress(leftPressed, viewport, SettingsMenuOpen ? monitorMeshInst : meshInst, pos, dir);
    }

    public void HandleRaycastMotion(bool leftPressed, Vector3 pos, Vector3 dir, Vector3 lastPos, Vector3 lastDir)
    {
        HandleRaycastMotion(leftPressed, viewport, SettingsMenuOpen ? monitorMeshInst : meshInst, pos, dir, lastPos, lastDir);
    }

    public Vector2 CalcPosition(SubViewport view, Node3D mesh, Vector3 pos, Vector3 dir)
    {
        var invSize = MeshSize;
        invSize.Y = -1f / invSize.Y;
        var halfInvSize = MeshSize * 0.5f;
        halfInvSize.Y *= -1f;

        Plane pl = new Plane(mesh.GlobalBasis.Z, mesh.GlobalPosition);
        Vector3? v3 = pl.IntersectsRay(pos, dir);
        if (!v3.HasValue)
            return Vector2.Zero;
        v3 = mesh.GlobalTransform.AffineInverse() * v3;
        Vector2 v2 = new Vector2(v3.Value.X, v3.Value.Y);

        return new Vector2((v2.X / MeshSize.X) + 0.5f, 0.5f - (v2.Y / MeshSize.Y)) * view.Size;
        // return (v2 / invSize) * view.Size;
    }

    public void HandleRaycastPress(bool leftPressed, SubViewport view, Node3D mesh, Vector3 pos, Vector3 dir)
    {
        bool local = false;

        // InputEventMouseButton input = new InputEventMouseButton();
        var input = Bmotion;
        input.ButtonIndex = MouseButton.Left;
        input.ButtonMask = leftPressed ? MouseButtonMask.Left : 0;
        input.Pressed = leftPressed;
        input.Position = CalcPosition(view, mesh, pos, dir);
        input.GlobalPosition = input.Position;
        view.PushInput(input, local);
    }

    public void HandleRaycastMotion(bool leftPressed, SubViewport view, Node3D mesh, Vector3 pos, Vector3 dir, Vector3 lastPos, Vector3 lastDir)
    {
        bool local = false;

        var input = Amotion;
        // InputEventMouseMotion input = new InputEventMouseMotion();
        input.ButtonMask = leftPressed ? MouseButtonMask.Left : 0;
        input.Pressure = leftPressed ? 1f : 0f;
        input.Position = CalcPosition(view, mesh, pos, dir);
        input.Relative = input.Position - CalcPosition(view, mesh, lastPos, lastDir);
        input.GlobalPosition = input.Position;
        view.PushInput(input, local);
    }

    public override async void _Ready()
    {
        // viewport.GuiEmbedSubwindows = GetViewport().UseXR;
        // MenuManager.Instance.SetEnv(environment);
        /*
        SettingsChanged();
        MenuManager.Instance.OnSettingsChanged += SettingsChanged;
        usernameEdit.TextSubmitted += UsernameChanged;
        hostBtn.Pressed += Host;
        joinBtn.Pressed += Join;
        oldMenuCheck.Pressed += StopMusic;
        volumeSlider.ValueChanged += VolumeChanged;
        */
        // RenderingServer.FramePreDraw += Draw;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Draw();
        // RenderingServer.RequestFrameDrawnCallback(Callable.From(Draw));
        // Input.MouseMode = Input.MouseModeEnum.Hidden;
        CursorPosition = Vector2.One * GetTree().Root.Size / 2f;
        // ProjectSettings.SetSetting("input_devices/pointing/emulate_mouse_from_touch", true);
        MeshSize = Vector2.One;
        ui.SwitchMenu(string.Empty);
        GlitchAsync();
    }

    public async void GlitchAsync()
    {
        return;
        glitchEffect.Visible = true;
        testScreen.Visible = true;
        okLabel.Visible = false;
        glitchEffect.Material.Set("shader_parameter/amount", 1f);
        var tween = CreateTween();
        tween.TweenProperty(glitchEffect.Material, "shader_parameter/amount", 0f, 0.5d).SetEase(Tween.EaseType.OutIn).SetTrans(Tween.TransitionType.Expo);
        // tween.TweenCallback();
        await ToSignal(GetTree().CreateTimer(1d), SceneTreeTimer.SignalName.Timeout);
        glitchEffect.Visible = false;
        testScreen.Visible = false;
        okLabel.Visible = true;
        await ToSignal(GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
        okLabel.Visible = false;
    }

    public override void _EnterTree()
    {
        InputManager.Instance.ChangeActionSet("Menu");
        viewport.GuiFocusChanged += FocusChange;
        if (SteamManager.Supported)
            GodotSteam.Steam.GamepadTextInputDismissed += TextInputted;
    }

    public override void _ExitTree()
    {
        if (SteamManager.Supported)
            GodotSteam.Steam.GamepadTextInputDismissed -= TextInputted;
    }

    private void FocusChange(Control c)
    {
        if (IsInstanceValid(c) && c is LineEdit e && SteamManager.Supported)
        {
            GodotSteam.Steam.ShowGamepadTextInput(GodotSteam.GamepadTextInputMode.Normal, GodotSteam.GamepadTextInputLineMode.SingleLine, Tr(e.PlaceholderText), (uint)64 + 1, e.Text);
        }
    }

    private void TextInputted(bool submitted, string enteredText, uint appId)
    {
        if (submitted)
        {
            viewport.PushTextInput(enteredText);
        }
    }

    public override void _Process(double delta)
    {
        if (GetViewport().UseXR)
        {
            xrCamera.MakeCurrent();
        }
        laser.LookAt(worldPos);
        laser.Visible = (InputSource.IsJoy || InputSource.IsTouch) && (!IsInstanceValid(RoundManager.Instance) || MenuManager.Instance.InMatrix) && (Input.MouseMode == Input.MouseModeEnum.Visible || Input.MouseMode == Input.MouseModeEnum.Hidden);
        InputSource.Tick(delta);
        if (InputSource.IsJoy)
        {
            CursorPosition -= InputSource.MouseMotion * 256f;
            var motion = new InputEventMouseMotion()
            {
                Relative = -InputSource.MouseMotion,
                Position = CursorPosition,
                GlobalPosition = CursorPosition,
                Device = -1,
            };
            GetViewport().PushInput(motion);
            if (InputSource.PrimaryFire.HasFlag(ButtonInputFlags.JustPressed))
            {
                var input = new InputEventMouseButton()
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = MouseButtonMask.Left,
                    Pressed = true,
                    Position = CursorPosition,
                    GlobalPosition = CursorPosition,
                    Device = -1,
                };
                GetViewport().PushInput(input);
            }
            if (InputSource.PrimaryFire.HasFlag(ButtonInputFlags.JustReleased))
            {
                var input = new InputEventMouseButton()
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = 0,
                    Pressed = false,
                    Position = CursorPosition,
                    GlobalPosition = CursorPosition,
                    Device = -1,
                };
                GetViewport().PushInput(input);
            }
        }
        else if (InputSource.IsMouse && !IsInstanceValid(RoundManager.Instance))
        {
            InputEventMouseMotion motion = new InputEventMouseMotion();
            motion.Position = GetViewport().GetMousePosition();
            motion.GlobalPosition = GetTree().Root.GetMousePosition();
            // HandleMouse(motion, viewport, SettingsMenuOpen ? monitorMeshInst : meshInst);
        }
        InputSource.Flush();
    }

    private void Draw()
    {
        ScreenTexture = viewport.GetTexture();
        // MonitorScreenTexture = viewport.GetTexture();
        ScreenUVAmount = 0f;
        // RenderingServer.FramePreDraw -= Draw;
    }
}

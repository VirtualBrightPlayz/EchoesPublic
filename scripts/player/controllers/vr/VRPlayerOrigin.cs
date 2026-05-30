using System;
using Godot;

public partial class VRPlayerOrigin : XROrigin3D
{
    public static VRPlayerOrigin Instance;

    public bool CanInput => Multiplayer.HasMultiplayerPeer() && Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected ? IsMultiplayerAuthority() : true;

    [Export]
    public XRCamera3D head;
    [Export]
    public XRController3D left;
    [Export]
    public XRController3D right;

    [Export]
    public VRPhysicsHand physLeft;
    [Export]
    public VRPhysicsHand physRight;

    [Export]
    public Shape3D headShape;

    [ExportGroup("GUI")]
    [Export]
    public MeshInstance3D handUIMesh;
    [Export]
    public MeshInstance3D viewUIMesh;

    public Vector2 MouseMotion { get; set; } = Vector2.Zero;
    public Vector2 MovementDirection => left.GetVector2("primary") * new Vector2(1f, -1f);
    public ButtonInputFlags Sprint { get; set; } = ButtonInputFlags.None;
    public bool ShouldSprint = false;
    public ButtonInputFlags Jump { get; set; } = ButtonInputFlags.None;
    public ButtonInputFlags VoiceChat => right.GetInput("by_button").AsBool() ? ButtonInputFlags.Pressed : ButtonInputFlags.None;
    public ButtonInputFlags VoiceChatAlt => right.GetInput("ax_button").AsBool() ? ButtonInputFlags.Pressed : ButtonInputFlags.None;
    public ButtonInputFlags Reload => right.GetInput("primary_click").AsBool() ? ButtonInputFlags.Pressed : ButtonInputFlags.None;
    public ButtonInputFlags Inventory { get; set; } = ButtonInputFlags.None;
    public ButtonInputFlags PauseMenu { get; set; } = ButtonInputFlags.None;
    public ButtonInputFlags DebugHideUi { get; set; } = ButtonInputFlags.None;
    public ButtonInputFlags Crouch { get; set; } = ButtonInputFlags.None;

    public ButtonInputFlags LeftTrigger = ButtonInputFlags.None;
    public ButtonInputFlags RightTrigger = ButtonInputFlags.None;

    private Vector3 lastFloorPos;
    private Vector3 lastFloorPosMult;
    private float lastYRotation;
    private float lastTurnInput;

    public override void _EnterTree()
    {
        if (CanInput)
        {
            if (IInitScript.Instance.IsXR)
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                // IInitScript.SceneTree.Root.UseXR = false;
                // IInitScript.SceneTree.Root.OwnWorld3D = true;
                // IInitScript.SceneTree.Root.World3D = GetViewport().World3D;
                // GetViewport().UseXR = true;
                // Engine.PhysicsTicksPerSecond = (int)xr.DisplayRefreshRate;
                head.MakeCurrent();
            }
            else
            {
                if (GetViewport() is SubViewport sub)
                {
                    // sub.RenderTargetUpdateMode = SubViewport.UpdateMode.ServerDisabled;
                }
            }
        }
        OnSettings();
        if (IsInstanceValid(MenuManager.Instance))
            MenuManager.Instance.OnSettingsChanged += OnSettings;
    }

    public override void _ExitTree()
    {
        if (CanInput)
        {
            // GetViewport().UseXR = false;
            if (GetViewport() is SubViewport sub)
            {
                sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            }
        }
        if (IsInstanceValid(MenuManager.Instance))
            MenuManager.Instance.OnSettingsChanged -= OnSettings;
    }

    private void OnSettings()
    {
        GetViewport().PositionalShadowAtlasSize = GetTree().Root.PositionalShadowAtlasSize;
    }

    public override void _Ready()
    {
        if (!CanInput)
        {
            Visible = false;
            left.QueueFree();
            right.QueueFree();
            // QueueFree();
        }
        else
        {
            physLeft.AddCollisionExceptionWith(physRight);
            physRight.AddCollisionExceptionWith(physLeft);
            Instance = this;
        }
    }

    public void ProcessSpectator(double delta)
    {
        if (!CanInput)
            return;
        if (IsInstanceValid(NetworkPlayer.LocalInstance) && NetworkPlayer.LocalInstance.ActiveController != null && InputManager.Instance.IsVR)
        {
            if (NetworkPlayer.LocalInstance.ActiveController is SpectatorController spec)
            {
                RightTrigger = IPlayerInputSource.UpdateInput(right.GetFloat("trigger") > 0.5f, RightTrigger);
                LeftTrigger = IPlayerInputSource.UpdateInput(left.GetFloat("trigger") > 0.5f, LeftTrigger);
                if (RightTrigger.HasFlag(ButtonInputFlags.JustPressed))
                {
                    spec.Next();
                    // if (IsInstanceValid(spec.OtherPlayer))
                    //     spec.GlobalPosition =  spec.OtherPlayer.PlayerPosition;
                }
                if (LeftTrigger.HasFlag(ButtonInputFlags.JustPressed))
                {
                    spec.Prev();
                    // if (IsInstanceValid(spec.OtherPlayer))
                    //     spec.GlobalPosition =  spec.OtherPlayer.PlayerPosition;
                }
            }
        }
    }

    public void ProcessMovement(double delta)
    {
        if (!CanInput)
            return;
        if (IsInstanceValid(NetworkPlayer.LocalInstance) && NetworkPlayer.LocalInstance.ActiveController != null && InputManager.Instance.IsVR)
        {
            head.MakeCurrent();
            Current = true;
            var ctrl = NetworkPlayer.LocalInstance.ActiveController.Root;
            if (ctrl is CharacterBody3D body)
            {
                var floorPosition = head.Position;
                floorPosition.Y = 0f;
                float rotationY = head.Rotation.Y;
                ctrl.GlobalRotation -= new Vector3(0f, lastYRotation, 0f);
                var vel = body.Velocity;
                {
                    var targetVel = GlobalBasis * (floorPosition - lastFloorPos);
                    targetVel.Y = 0f;
                    var result = body.MoveAndCollide(targetVel, false);
                    if (IsInstanceValid(result))
                    {
                        // prevent head collisions
                        var args = new PhysicsShapeQueryParameters3D()
                        {
                            CollisionMask = body.CollisionMask,
                            Exclude = [body.GetRid()],
                            Shape = headShape,
                            Transform = head.GlobalTransform,
                        };
                        var overlaps = GetWorld3D().DirectSpaceState.IntersectShape(args);
                        if (overlaps.Count == 0)
                        {
                            // move body into wall, but not physically
                            floorPosition -= GlobalBasis.Inverse() * result.GetRemainder();
                        }
                    }
                    else
                    {
                    }
                    lastFloorPos = floorPosition;
                }
                body.Velocity = vel;
                GlobalRotation = ctrl.GlobalRotation;
                GlobalPosition = ctrl.GlobalPosition - GlobalBasis * floorPosition;
                ctrl.GlobalRotation += new Vector3(0f, rotationY, 0f);
                lastYRotation = rotationY;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        ProcessMovement(delta);
    }

    public override void _Process(double delta)
    {
        if (!CanInput)
            return;
        Tick(delta);
        // if (!IInitScript.SceneTree.Root.UseXR)
        {
            // IInitScript.SceneTree.Root.World3D = GetViewport().World3D;
        }
        /*
        if (!IInitScript.SceneTree.Root.UseXR && !GetViewport().UseXR)
        {
            if (GetViewport() is SubViewport sub)
            {
                sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
                // sub.VrsMode = Viewport.VrsModeEnum.XR;
                // sub.VrsUpdateMode = Viewport.VrsUpdateModeEnum.Once;
            }
            MenuManager.Instance.mainViewTexture.Visible = false;
            GetViewport().UseXR = true;
        }
        */
        Visible = IsInstanceValid(NetworkPlayer.LocalInstance) && InputManager.Instance.IsVR;
        left.ProcessMode = IsInstanceValid(NetworkPlayer.LocalInstance) && InputManager.Instance.IsVR ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        right.ProcessMode = IsInstanceValid(NetworkPlayer.LocalInstance) && InputManager.Instance.IsVR ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        // ProcessMovement(delta);
        ProcessSpectator(delta);
        if (IsInstanceValid(NetworkPlayer.LocalInstance) && NetworkPlayer.LocalInstance.ActiveController != null && InputManager.Instance.IsVR)
        {
            if (!head.Current)
                head.MakeCurrent();
            if (!Current)
                Current = true;
            
            NetworkPlayer.LocalInstance.HandSyncTargets[(int)PlayerModel.BoneName.Head].GlobalTransform = head.GlobalTransform;
            var ctrl = NetworkPlayer.LocalInstance.ActiveController.Root;
            if (ctrl is not CharacterBody3D)
            {
                var floorPosition = head.Position;
                floorPosition.Y = 0f;
                float rotationY = head.Rotation.Y;
                ctrl.GlobalRotation -= new Vector3(0f, lastYRotation, 0f);
                // ctrl.GlobalRotation = new Vector3(0f, head.GlobalRotation.Y, 0f);
                ctrl.GlobalPosition -= ctrl.GlobalBasis * lastFloorPos;
                lastFloorPos = floorPosition;
                ctrl.GlobalPosition += ctrl.GlobalBasis * floorPosition;
                // ctrl.ForceUpdateTransform();
                GlobalPosition = ctrl.GlobalPosition - ctrl.GlobalBasis * floorPosition;
                GlobalRotation = ctrl.GlobalRotation;
                ctrl.GlobalRotation += new Vector3(0f, rotationY, 0f);
                lastYRotation = rotationY;
            }

            NetworkPlayer.LocalInstance.ActiveController.Camera.GlobalTransform = head.GlobalTransform;
            head.Far = NetworkPlayer.LocalInstance.ActiveController.Camera.Far;

            NetworkPlayer.LocalInstance.HudVR.menuHandLeft.hand = left;
            NetworkPlayer.LocalInstance.HudVR.menuHandRight.hand = right;
            NetworkPlayer.LocalInstance.HudVR.menuRoot.GlobalPosition = head.GlobalPosition;

            if (handUIMesh != null && handUIMesh.MaterialOverride is BaseMaterial3D mat)
                mat.AlbedoTexture = NetworkPlayer.LocalInstance.HudVR.handUI.GetTexture();
            if (viewUIMesh != null && viewUIMesh.MaterialOverride is ShaderMaterial mat2)
                mat2.SetShaderParameter("main_texture", NetworkPlayer.LocalInstance.HudVR.viewUI.GetTexture());
        }
    }

    public void Tick(double delta)
    {
        if (!IInitScript.Instance.IsXR)
            return;
        MouseMotion = Vector2.Zero;
        InputManager.Instance.PushStickPadInput(LocalPlayerInput.PlayerMove, MovementDirection);
        Vector2 rightPrimary = right.GetVector2("primary");
        if (Settings.User.SnapTurn)
        {
            float turn = rightPrimary.X;
            if (turn > 0.5f && lastTurnInput <= 0.5f)
            {
                MouseMotion = new Vector2(Mathf.DegToRad(-Settings.User.TurnSpeed), 0f);
            }
            if (turn < -0.5f && lastTurnInput >= -0.5f)
            {
                MouseMotion = new Vector2(Mathf.DegToRad(Settings.User.TurnSpeed), 0f);
            }
        }
        else if (Mathf.Abs(rightPrimary.X) > 0.5f)
            MouseMotion = new Vector2(rightPrimary.X * (float)delta, 0f) * Mathf.DegToRad(-Settings.User.TurnSpeed);
        lastTurnInput = rightPrimary.X;
        if (!MouseMotion.IsZeroApprox())
        {
            lastYRotation -= MouseMotion.X;
            // InputManager.Instance.MouseMotion = MouseMotion;
            // InputManager.Instance.HasMotion = true;
        }

        Sprint = InputManager.UpdateInput(left.GetInput("primary_click").AsBool(), Sprint);
        Jump = InputManager.UpdateInput(rightPrimary.Y > 0.5f, Jump);
        Crouch = InputManager.UpdateInput(rightPrimary.Y < -0.5f, Crouch);
        Inventory = InputManager.UpdateInput(left.GetInput("ax_button").AsBool(), Inventory);
        PauseMenu = InputManager.UpdateInput(left.GetInput("by_button").AsBool(), PauseMenu);

        if (Sprint.HasFlag(ButtonInputFlags.JustPressed))
        {
            ShouldSprint = !ShouldSprint;
        }

        InputManager.Instance.PushDigitalInput(LocalPlayerInput.PlayerSprint, ShouldSprint);
        InputManager.Instance.PushDigitalInput(LocalPlayerInput.PlayerJump, Jump.HasFlag(ButtonInputFlags.Pressed));
        InputManager.Instance.PushDigitalInput(LocalPlayerInput.PlayerSneak, Crouch.HasFlag(ButtonInputFlags.Pressed));
        InputManager.Instance.PushDigitalInput(LocalPlayerInput.PlayerInventory, Inventory.HasFlag(ButtonInputFlags.Pressed));
        InputManager.Instance.PushDigitalInput(LocalPlayerInput.MenuPause, PauseMenu.HasFlag(ButtonInputFlags.Pressed));

        InputManager.Instance.PushDigitalInput(LocalPlayerInput.PlayerVoiceChat, VoiceChat.HasFlag(ButtonInputFlags.Pressed));
        InputManager.Instance.PushDigitalInput(LocalPlayerInput.PlayerVoiceChatAlt, VoiceChatAlt.HasFlag(ButtonInputFlags.Pressed));
        InputManager.Instance.PushDigitalInput(LocalPlayerInput.PlayerReload, Reload.HasFlag(ButtonInputFlags.Pressed));
    }
}